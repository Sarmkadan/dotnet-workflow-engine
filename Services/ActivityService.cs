// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Exceptions;
using DotNetWorkflowEngine.Models;
using System.Threading;
using ExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

namespace DotNetWorkflowEngine.Services;

/// <summary>
/// Service for executing activities and managing activity handlers.
/// </summary>
public class ActivityService
{
    private readonly Dictionary<string, IActivityHandler> _handlers = new();
    private readonly RetryPolicyService _retryPolicyService;

    /// <summary>
    /// Interface for activity handlers.
    /// </summary>
    public interface IActivityHandler
    {
        Task<Dictionary<string, object?>> ExecuteAsync(Activity activity, ExecutionContext context);
    }

    /// <summary>
    /// Initializes the activity service.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when retry policy service is null.</exception>
    public ActivityService(RetryPolicyService retryPolicyService)
    {
        _retryPolicyService = retryPolicyService ?? throw new ArgumentNullException(nameof(retryPolicyService));
    }

    /// <summary>
    /// Registers a handler for a specific activity type.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when handler is null.</exception>
    public void RegisterHandler(string handlerType, IActivityHandler handler)
    {
        if (string.IsNullOrWhiteSpace(handlerType))
            throw new ArgumentException("Handler type cannot be null or empty", nameof(handlerType));

        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        _handlers[handlerType] = handler;
    }

    /// <summary>
    /// Executes an activity with its configured retry policy.
    /// </summary>
    /// <param name="activity">The activity to execute.</param>
    /// <param name="context">The execution context.</param>
    /// <returns>A task representing the asynchronous operation, returning the <see cref="ActivityResult"/>.</returns>
    /// <remarks>
    /// <see cref="Activity.TimeoutSeconds"/> is enforced per attempt: each retry iteration gets
    /// its own <see cref="CancellationTokenSource"/> window rather than sharing a budget across
    /// the whole call. The worst-case total run time is therefore up to
    /// <c>MaxAttempts * TimeoutSeconds</c> plus retry delays, not a single
    /// <c>TimeoutSeconds</c> window. Callers who need to bound the total execution time must
    /// impose their own outer cancellation/timeout around this call.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="activity"/> or <paramref name="context"/> is null.</exception>
    /// <exception cref="ValidationException">Thrown if activity validation fails.</exception>
    /// <exception cref="ActivityException">Thrown if execution fails after all retries.</exception>
    public virtual async Task<ActivityResult> ExecuteAsync(Activity activity, ExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(activity.Id))
            throw new ValidationException("Activity ID cannot be empty", "INVALID_ACTIVITY_ID", "Activity");

        if (!activity.Validate(out var errors))
        {
            throw new ValidationException("Invalid activity configuration", errors, activity.Name);
        }

        var result = new ActivityResult(activity.Id);
        var retryConfig = BuildRetryConfig(activity);

        int attemptNumber = 0;
        while (attemptNumber < retryConfig.MaxAttempts)
        {
            attemptNumber++;
            result.AttemptNumber = attemptNumber;
            result.TotalAttempts = retryConfig.MaxAttempts;

            try
            {
                // Handle gateways without handlers
                if (activity.IsGateway())
                {
                    HandleGateway(activity, context, result);
                    return result;
                }

                // Check for conditional skip
                if (activity.ConditionExpression != null && !EvaluateCondition(activity.ConditionExpression, context))
                {
                    result.SetSkipped("Conditional expression evaluated to false");
                    return result;
                }

                // Execute activity handler
                if (!activity.RequiresHandler())
                {
                    result.SetSuccess(new Dictionary<string, object?>());
                    return result;
                }

                if (string.IsNullOrEmpty(activity.HandlerType) || !_handlers.TryGetValue(activity.HandlerType, out var handler))
                {
                    throw new ActivityException($"No handler registered for type '{activity.HandlerType}'", activity.Id);
                }

                context.ActivityId = activity.Id;

                // Enforce timeout if configured
                if (activity.TimeoutSeconds > 0)
                {
                    using var cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromSeconds(activity.TimeoutSeconds));

                    try
                    {
                        var output = await handler.ExecuteAsync(activity, context).WaitAsync(cts.Token);
                        result.SetSuccess(output);
                        return result;
                    }
                    catch (OperationCanceledException)
                    {
                        result.SetTimeout();
                        return result;
                    }
                }
                else
                {
                    var output = await handler.ExecuteAsync(activity, context);
                    result.SetSuccess(output);
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.SetFailure(ex.Message, ex.StackTrace);

                // Check if we should retry
                if (!retryConfig.ShouldRetry(attemptNumber, ex.GetType().Name))
                {
                    var message = attemptNumber >= retryConfig.MaxAttempts
                        ? $"Activity '{activity.Id}' failed after {attemptNumber} attempt(s), retry attempts exhausted: {ex.Message}"
                        : $"Activity '{activity.Id}' failed after {attemptNumber} attempt(s): {ex.Message}";

                    throw new ActivityException(
                        message,
                        activity.Id,
                        attemptNumber,
                        context.CorrelationId,
                        ex
                    );
                }

                // Wait before retry
                var delayMs = retryConfig.CalculateDelayMs(attemptNumber);
                await Task.Delay(delayMs);
            }
        }

        throw new ActivityException(
            $"Activity '{activity.Id}' exhausted all retry attempts",
            activity.Id,
            attemptNumber,
            context.CorrelationId
        );
    }

    /// <summary>
    /// Handles gateway activities (fork/join).
    /// </summary>
    private void HandleGateway(Activity activity, ExecutionContext context, ActivityResult result)
    {
        result.SetSuccess(new Dictionary<string, object?>());
    }

    /// <summary>
    /// Evaluates a condition expression.
    /// </summary>
    private bool EvaluateCondition(string expression, ExecutionContext context)
    {
        if (string.IsNullOrEmpty(expression))
            return true;

        if (expression == "true")
            return true;
        if (expression == "false")
            return false;

        // Check if it's a variable reference
        if (expression.StartsWith("${" ) && expression.EndsWith("}"))
        {
            var varName = expression.Substring(2, expression.Length - 3);
            var value = context.GetVariable(varName);
            return value is true or 1 or "true";
        }

        return true;
    }

    /// <summary>
    /// Builds retry configuration from activity settings.
    /// </summary>
    private Models.RetryPolicyConfig BuildRetryConfig(Activity activity)
    {
        if (activity == null)
            throw new ArgumentNullException(nameof(activity));

        var config = activity.RetryPolicy switch
        {
            RetryPolicy.FixedDelay => Models.RetryPolicyConfig.CreateFixedDelay(
                activity.MaxRetries,
                Constants.WorkflowConstants.DefaultRetryDelayMs
            ),
            RetryPolicy.ExponentialBackoff => Models.RetryPolicyConfig.CreateExponentialBackoff(
                activity.MaxRetries,
                Constants.WorkflowConstants.DefaultRetryDelayMs,
                Constants.WorkflowConstants.MaxBackoffDelayMs
            ),
            RetryPolicy.LinearBackoff => Models.RetryPolicyConfig.CreateFixedDelay(
                activity.MaxRetries,
                Constants.WorkflowConstants.DefaultRetryDelayMs
            ),
            _ => Models.RetryPolicyConfig.CreateNoRetry()
        };

        return config;
    }

    /// <summary>
    /// Gets all registered handlers.
    /// </summary>
    public List<string> GetRegisteredHandlerTypes()
    {
        return _handlers.Keys.ToList();
    }

    /// <summary>
    /// Validates activity before execution.
    /// </summary>
    public bool ValidateActivity(Activity activity, out List<string> errors)
    {
        if (activity == null)
        {
            errors = new List<string> { "Activity cannot be null" };
            return false;
        }

        return activity.Validate(out errors);
    }
}