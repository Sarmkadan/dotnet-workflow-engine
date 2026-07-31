// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Enums;

/// <summary>
/// Extension methods for <see cref="WorkflowStatus"/>.
/// </summary>
public static class WorkflowStatusExtensions
{
    /// <summary>
    /// Checks if the status is a terminal state (cannot transition further).
    /// </summary>
    public static bool IsTerminal(this WorkflowStatus status)
    {
        return status == WorkflowStatus.Archived || status == WorkflowStatus.Cancelled;
    }

    /// <summary>
    /// Checks if a transition from the current status to the target status is allowed.
    /// </summary>
    public static bool CanTransitionTo(this WorkflowStatus currentStatus, WorkflowStatus targetStatus)
    {
        return WorkflowStatusMachine.IsValidTransition(currentStatus, targetStatus);
    }

    /// <summary>
    /// Returns a user-friendly display string for the status.
    /// </summary>
    public static string ToDisplayString(this WorkflowStatus status)
    {
        return status switch
        {
            WorkflowStatus.Draft => "Draft",
            WorkflowStatus.Active => "Active",
            WorkflowStatus.Deprecated => "Deprecated",
            WorkflowStatus.Archived => "Archived",
            WorkflowStatus.Suspended => "Suspended",
            WorkflowStatus.WaitingForMessage => "Waiting for Message",
            WorkflowStatus.Cancelled => "Cancelled",
            _ => status.ToString()
        };
    }
}
