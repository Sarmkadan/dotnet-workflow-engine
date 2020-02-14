// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Exceptions;
using DotNetWorkflowEngine.Models;
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
    public ActivityService(RetryPolicyService retryPolicyService)
    {
        _retryPolicyService = retryPolicyService;
    }

    /// <summary>
    /// Registers an activity handler.
    /// </summary>
    public void RegisterHandler(string handlerType, IActivityHandler handler)
    {
        _handlers[handlerType] = handler;
    }

    /// <summary>
    /// Executes an activity with retry logic.
    /// </summary>
    public async Task<ActivityResult> ExecuteAsync(Activity activity, ExecutionContext context)
    {
        if (activity.Validate(out var errors))
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
                var output = await handler.ExecuteAsync(activity, context);
                result.SetSuccess(output);
                return result;
            }
            catch (Exception ex)
            {
                result.SetFailure(ex.Message, ex.StackTrace);

                // Check if we should retry
                if (!retryConfig.ShouldRetry(attemptNumber, ex.GetType().Name))
                {
                    throw new ActivityException(
                        $"Activity '{activity.Id}' failed after {attemptNumber} attempt(s): {ex.Message}",
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
        // Simple condition evaluation - can be extended with expression evaluator
        if (expression == "true")
            return true;
        if (expression == "false")
            return false;

        // Check if it's a variable reference
        if (expression.StartsWith("${") && expression.EndsWith("}"))
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
        return activity.Validate(out errors);
    }
}
