// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using DotNetWorkflowEngine.Enums;

namespace DotNetWorkflowEngine.Models;

/// <summary>
/// Represents a validated state machine for workflow instance status transitions.
/// This class enforces explicit transition rules and ensures only valid state changes are allowed.
/// </summary>
public static class WorkflowStatusMachine
{
    /// <summary>
    /// Represents a transition rule that defines valid status transitions.
    /// </summary>
    /// <param name="From">The source status.</param>
    /// <param name="To">The target status.</param>
    private readonly record struct TransitionRule(WorkflowStatus From, WorkflowStatus To);

    /// <summary>
    /// Set of all valid transitions in the workflow state machine.
    /// </summary>
    private static readonly HashSet<TransitionRule> ValidTransitions = new()
    {
        // Draft -> Active: Instance created and ready to start
        new(WorkflowStatus.Draft, WorkflowStatus.Active),

        // Active -> WaitingForMessage: MessageCatchEvent encountered
        new(WorkflowStatus.Active, WorkflowStatus.WaitingForMessage),

        // Active -> Suspended: Manual pause requested
        new(WorkflowStatus.Active, WorkflowStatus.Suspended),

        // Active -> Archived: Normal completion
        new(WorkflowStatus.Active, WorkflowStatus.Archived),

        // Active -> Cancelled: Manual cancellation requested
        new(WorkflowStatus.Active, WorkflowStatus.Cancelled),

        // WaitingForMessage -> Active: Message received and workflow resumed
        new(WorkflowStatus.WaitingForMessage, WorkflowStatus.Active),

        // WaitingForMessage -> Suspended: Manual pause while waiting for message
        new(WorkflowStatus.WaitingForMessage, WorkflowStatus.Suspended),

        // WaitingForMessage -> Cancelled: Manual cancellation while waiting for message
        new(WorkflowStatus.WaitingForMessage, WorkflowStatus.Cancelled),

        // Suspended -> Active: Manual resume requested
        new(WorkflowStatus.Suspended, WorkflowStatus.Active),

        // Suspended -> WaitingForMessage: MessageCatchEvent encountered while suspended
        new(WorkflowStatus.Suspended, WorkflowStatus.WaitingForMessage),

        // Suspended -> Archived: Completion after resume
        new(WorkflowStatus.Suspended, WorkflowStatus.Archived),

        // Suspended -> Cancelled: Manual cancellation after suspend
        new(WorkflowStatus.Suspended, WorkflowStatus.Cancelled),

        // Archived -> (no transitions allowed - terminal state)

        // Cancelled -> (no transitions allowed - terminal state)
    };

    /// <summary>
    /// Validates whether a transition from one status to another is allowed.
    /// </summary>
    /// <param name="from">The current status.</param>
    /// <param name="to">The target status.</param>
    /// <returns>True if the transition is valid; false otherwise.</returns>
    public static bool IsValidTransition(WorkflowStatus from, WorkflowStatus to)
    {
        // Terminal states (Archived, Cancelled) cannot transition to any other state
        if (from == WorkflowStatus.Archived || from == WorkflowStatus.Cancelled)
        {
            return false;
        }

        // Draft can only transition to Active
        if (from == WorkflowStatus.Draft && to != WorkflowStatus.Active)
        {
            return false;
        }

        // Check if the specific transition is in the valid transitions set
        return ValidTransitions.Contains(new TransitionRule(from, to));
    }

    /// <summary>
    /// Gets all valid transitions from a given status.
    /// </summary>
    /// <param name="status">The source status.</param>
    /// <returns>Collection of valid target statuses.</returns>
    public static IEnumerable<WorkflowStatus> GetValidTransitionsFrom(WorkflowStatus status)
    {
        // Terminal states have no valid transitions
        if (status == WorkflowStatus.Archived || status == WorkflowStatus.Cancelled)
        {
            yield break;
        }

        // Draft can only transition to Active
        if (status == WorkflowStatus.Draft)
        {
            yield return WorkflowStatus.Active;
            yield break;
        }

        // Check all valid transitions from this status
        foreach (var transition in ValidTransitions)
        {
            if (transition.From == status)
            {
                yield return transition.To;
            }
        }
    }

    /// <summary>
    /// Gets a string representation of all valid transitions.
    /// </summary>
    public static string GetTransitionMatrix()
    {
        var transitions = ValidTransitions
            .GroupBy(t => t.From)
            .OrderBy(g => g.Key)
            .Select(g => $"{g.Key} -> {string.Join(", ", g.Select(t => t.To))}");

        return string.Join("\n", transitions);
    }
}