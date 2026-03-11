// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Utilities;
using ExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

namespace DotNetWorkflowEngine.Services;

/// <summary>
/// Evaluates conditional expressions on workflow transitions to determine which
/// branches to activate after an activity completes.
/// <para>
/// Resolution order per activity:
/// <list type="number">
///   <item>Conditional transitions (those with a <c>ConditionExpression</c>), ordered by
///   <c>Priority</c> descending — all matching ones are selected.</item>
///   <item>Unconditional transitions (no expression, not default) — always selected.</item>
///   <item>Default transition(s) — selected only when no conditional branch matched and
///   no unconditional transition exists.</item>
/// </list>
/// </para>
/// </summary>
public class ConditionalBranchingService
{
    private readonly ILogger<ConditionalBranchingService> _logger;

    /// <summary>
    /// Initializes the service with the required logger.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public ConditionalBranchingService(ILogger<ConditionalBranchingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Resolves the set of transitions to follow from a completed activity, based on
    /// the current execution context and each transition's condition expression.
    /// </summary>
    /// <param name="workflow">The workflow definition that owns the transitions.</param>
    /// <param name="activityId">The ID of the activity that just completed.</param>
    /// <param name="context">Execution context whose variables are used when evaluating expressions.</param>
    /// <param name="cancellationToken">Token used to cancel in-progress evaluation.</param>
    /// <returns>
    /// A <see cref="BranchingResult"/> that lists selected and skipped transitions,
    /// along with any expression evaluation errors.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="workflow"/> or <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="activityId"/> is null or whitespace.
    /// </exception>
    public async Task<BranchingResult> ResolveBranchesAsync(
        Workflow workflow,
        string activityId,
        ExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(activityId))
            throw new ArgumentException("Activity ID is required.", nameof(activityId));

        cancellationToken.ThrowIfCancellationRequested();

        var outgoing = workflow.Transitions
            .Where(t => t.FromActivityId == activityId)
            .OrderByDescending(t => t.Priority)
            .ToList();

        if (outgoing.Count == 0)
        {
            _logger.LogDebug(
                "Activity '{ActivityId}' has no outgoing transitions in workflow '{WorkflowId}'.",
                activityId, workflow.Id);
            return BranchingResult.Empty(activityId);
        }

        var result = new BranchingResult { ActivityId = activityId };

        var conditionals   = outgoing.Where(t => !t.IsDefault && t.ConditionExpression != null).ToList();
        var unconditionals = outgoing.Where(t => !t.IsDefault && t.ConditionExpression == null).ToList();
        var defaults       = outgoing.Where(t => t.IsDefault).ToList();

        // Evaluate each conditional transition independently
        foreach (var transition in conditionals)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool matched;
            try
            {
                matched = EvaluateExpression(transition.ConditionExpression!, context);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Expression evaluation failed for transition '{TransitionId}' " +
                    "(expression: '{Expression}'). Transition treated as non-matching.",
                    transition.Id, transition.ConditionExpression);

                result.EvaluationErrors.Add(new TransitionEvaluationError
                {
                    TransitionId = transition.Id,
                    Expression   = transition.ConditionExpression!,
                    ErrorMessage = ex.Message
                });
                matched = false;
            }

            if (matched)
            {
                result.SelectedTransitions.Add(transition);
                _logger.LogDebug(
                    "Transition '{TransitionId}' selected (expression '{Expression}' = true).",
                    transition.Id, transition.ConditionExpression);
            }
            else
            {
                result.SkippedTransitions.Add(transition);
                _logger.LogDebug(
                    "Transition '{TransitionId}' skipped (expression '{Expression}' = false).",
                    transition.Id, transition.ConditionExpression);
            }
        }

        // Unconditional transitions are always followed
        result.SelectedTransitions.AddRange(unconditionals);

        // Use a default branch only when nothing else was selected
        if (result.SelectedTransitions.Count == 0 && defaults.Count > 0)
        {
            if (defaults.Count > 1)
            {
                _logger.LogWarning(
                    "Activity '{ActivityId}' has {Count} default transitions; " +
                    "selecting the one with the highest Priority.",
                    activityId, defaults.Count);
            }

            var chosen = defaults.OrderByDescending(t => t.Priority).First();
            result.SelectedTransitions.Add(chosen);
            result.UsedDefaultTransition = true;

            _logger.LogDebug(
                "No conditional branch matched for activity '{ActivityId}'; " +
                "falling back to default transition '{TransitionId}'.",
                activityId, chosen.Id);
        }

        result.AnyConditionMatched = conditionals.Any(t => result.SelectedTransitions.Contains(t));

        _logger.LogInformation(
            "Branch resolution complete for activity '{ActivityId}' in workflow '{WorkflowId}': " +
            "{Selected} selected, {Skipped} skipped, {Errors} error(s).",
            activityId, workflow.Id,
            result.SelectedTransitions.Count,
            result.SkippedTransitions.Count,
            result.EvaluationErrors.Count);

        return await Task.FromResult(result);
    }

    /// <summary>
    /// Returns the target activities to execute after a completed activity,
    /// honouring conditional transition expressions.
    /// </summary>
    /// <param name="workflow">The workflow definition.</param>
    /// <param name="activityId">The ID of the completed activity.</param>
    /// <param name="context">Execution context used for expression evaluation.</param>
    /// <param name="cancellationToken">Token used to cancel in-progress evaluation.</param>
    /// <returns>
    /// Ordered list of <see cref="Activity"/> objects to execute next;
    /// empty when the activity has no outgoing transitions or no conditions match.
    /// </returns>
    public async Task<List<Activity>> GetNextActivitiesAsync(
        Workflow workflow,
        string activityId,
        ExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var branchingResult = await ResolveBranchesAsync(workflow, activityId, context, cancellationToken);

        var targetIds = branchingResult.SelectedTransitions
            .Select(t => t.ToActivityId)
            .ToHashSet(StringComparer.Ordinal);

        return workflow.Activities
            .Where(a => targetIds.Contains(a.Id))
            .ToList();
    }

    /// <summary>
    /// Validates all conditional transition expressions in a workflow without executing them.
    /// Call this at workflow-load time to surface syntax errors early.
    /// </summary>
    /// <param name="workflow">The workflow whose transitions should be validated.</param>
    /// <returns>
    /// A list of <see cref="TransitionEvaluationError"/> entries; empty when all expressions
    /// are syntactically valid.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="workflow"/> is <see langword="null"/>.
    /// </exception>
    public List<TransitionEvaluationError> ValidateTransitionExpressions(Workflow workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        var errors = new List<TransitionEvaluationError>();

        foreach (var transition in workflow.Transitions.Where(t => t.ConditionExpression != null))
        {
            if (!ExpressionEvaluator.ValidateExpression(transition.ConditionExpression!, out var expressionErrors))
            {
                foreach (var error in expressionErrors)
                {
                    errors.Add(new TransitionEvaluationError
                    {
                        TransitionId = transition.Id,
                        Expression   = transition.ConditionExpression!,
                        ErrorMessage = error
                    });

                    _logger.LogWarning(
                        "Transition '{TransitionId}' has an invalid condition expression " +
                        "'{Expression}': {Error}",
                        transition.Id, transition.ConditionExpression, error);
                }
            }
        }

        return errors;
    }

    // Wraps the synchronous ExpressionEvaluator to keep call-sites clean.
    private static bool EvaluateExpression(string expression, ExecutionContext context)
        => ExpressionEvaluator.Evaluate(expression, context);
}
