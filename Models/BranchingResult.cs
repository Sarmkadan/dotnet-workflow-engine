// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetWorkflowEngine.Models;

/// <summary>
/// Describes the outcome of conditional branch resolution for a single activity.
/// Produced by <c>ConditionalBranchingService.ResolveBranchesAsync</c> after
/// evaluating all outgoing transition expressions from a completed activity.
/// </summary>
public class BranchingResult
{
    /// <summary>Gets or sets the ID of the activity whose outgoing transitions were evaluated.</summary>
    public string ActivityId { get; set; } = string.Empty;

    /// <summary>Gets the transitions whose conditions evaluated to <see langword="true"/>
    /// (or that have no condition expression) and will be followed.</summary>
    public List<Transition> SelectedTransitions { get; init; } = new();

    /// <summary>Gets the transitions that were evaluated but did not satisfy their condition.</summary>
    public List<Transition> SkippedTransitions { get; init; } = new();

    /// <summary>Gets errors encountered while evaluating individual transition expressions.</summary>
    public List<TransitionEvaluationError> EvaluationErrors { get; init; } = new();

    /// <summary>Gets or sets whether at least one conditional expression evaluated to <see langword="true"/>.</summary>
    public bool AnyConditionMatched { get; set; }

    /// <summary>Gets or sets whether the default fallback transition was used because no
    /// conditional branch matched.</summary>
    public bool UsedDefaultTransition { get; set; }

    /// <summary>Gets whether any transitions were selected for execution.</summary>
    public bool HasSelectedBranches => SelectedTransitions.Count > 0;

    /// <summary>Gets whether expression evaluation errors occurred during resolution.</summary>
    public bool HasEvaluationErrors => EvaluationErrors.Count > 0;

    /// <summary>
    /// Creates an empty result indicating the activity has no outgoing transitions.
    /// </summary>
    /// <param name="activityId">The ID of the completed activity.</param>
    /// <returns>An empty <see cref="BranchingResult"/>.</returns>
    public static BranchingResult Empty(string activityId) => new() { ActivityId = activityId };
}

/// <summary>
/// Describes a failure that occurred while evaluating a single transition expression.
/// </summary>
public class TransitionEvaluationError
{
    /// <summary>Gets or sets the ID of the transition whose expression could not be evaluated.</summary>
    public string TransitionId { get; set; } = string.Empty;

    /// <summary>Gets or sets the expression that caused the failure.</summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable error message.</summary>
    public string ErrorMessage { get; set; } = string.Empty;
}
