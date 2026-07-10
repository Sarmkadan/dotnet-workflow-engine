// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotNetWorkflowEngine.Models;

/// <summary>
/// Extension methods for the Transition class providing additional functionality
/// for workflow transition operations.
/// </summary>
public static class TransitionExtensions
{
    /// <summary>
    /// Determines whether this transition is a conditional transition (has a condition expression).
    /// </summary>
    /// <param name="transition">The transition to check</param>
    /// <returns>True if the transition has a condition expression; otherwise, false</returns>
    public static bool IsConditional(this Transition transition)
    {
        ArgumentNullException.ThrowIfNull(transition);
        return !string.IsNullOrWhiteSpace(transition.ConditionExpression);
    }

    /// <summary>
    /// Determines whether this transition is an unconditional transition (no condition expression).
    /// </summary>
    /// <param name="transition">The transition to check</param>
    /// <returns>True if the transition has no condition expression; otherwise, false</returns>
    public static bool IsUnconditional(this Transition transition)
    {
        ArgumentNullException.ThrowIfNull(transition);
        return string.IsNullOrWhiteSpace(transition.ConditionExpression);
    }

    /// <summary>
    /// Gets a display-friendly label for the transition that combines the label, condition, and priority.
    /// </summary>
    /// <param name="transition">The transition</param>
    /// <returns>A formatted string representing the transition</returns>
    public static string GetDisplayLabel(this Transition transition)
    {
        ArgumentNullException.ThrowIfNull(transition);

        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(transition.Label))
        {
            parts.Add(transition.Label);
        }
        else
        {
            parts.Add($"{transition.FromActivityId} → {transition.ToActivityId}");
        }

        if (transition.IsConditional())
        {
            parts.Add($"[Condition: {transition.ConditionExpression}]");
        }

        if (transition.IsDefault)
        {
            parts.Add("[Default]");
        }
        else if (transition.Priority != 0)
        {
            parts.Add($"[Priority: {transition.Priority}]");
        }

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Creates a copy of this transition with updated properties.
    /// </summary>
    /// <param name="transition">The original transition to copy</param>
    /// <param name="id">New ID for the transition</param>
    /// <param name="fromActivityId">New source activity ID</param>
    /// <param name="toActivityId">New target activity ID</param>
    /// <param name="conditionExpression">New condition expression</param>
    /// <param name="label">New label</param>
    /// <param name="isDefault">Whether this is the default transition</param>
    /// <param name="priority">New priority value</param>
    /// <returns>A new transition with the specified properties</returns>
    public static Transition WithProperties(
        this Transition transition,
        string? id = null,
        string? fromActivityId = null,
        string? toActivityId = null,
        string? conditionExpression = null,
        string? label = null,
        bool? isDefault = null,
        int? priority = null)
    {
        ArgumentNullException.ThrowIfNull(transition);

        return new Transition
        {
            Id = id ?? transition.Id,
            FromActivityId = fromActivityId ?? transition.FromActivityId,
            ToActivityId = toActivityId ?? transition.ToActivityId,
            ConditionExpression = conditionExpression ?? transition.ConditionExpression,
            Label = label ?? transition.Label,
            IsDefault = isDefault ?? transition.IsDefault,
            Priority = priority ?? transition.Priority,
            CreatedAt = transition.CreatedAt
        };
    }

    /// <summary>
    /// Determines whether this transition points to a specific activity.
    /// </summary>
    /// <param name="transition">The transition to check</param>
    /// <param name="activityId">The activity ID to check against</param>
    /// <returns>True if the transition points to the specified activity; otherwise, false</returns>
    public static bool PointsTo(this Transition transition, string activityId)
    {
        ArgumentNullException.ThrowIfNull(transition);
        return transition.ToActivityId.Equals(activityId, StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines whether this transition originates from a specific activity.
    /// </summary>
    /// <param name="transition">The transition to check</param>
    /// <param name="activityId">The activity ID to check against</param>
    /// <returns>True if the transition originates from the specified activity; otherwise, false</returns>
    public static bool ComesFrom(this Transition transition, string activityId)
    {
        ArgumentNullException.ThrowIfNull(transition);
        return transition.FromActivityId.Equals(activityId, StringComparison.Ordinal);
    }
}