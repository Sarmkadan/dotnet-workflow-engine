// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Exceptions;

namespace DotNetWorkflowEngine.Models;

/// <summary>
/// Exception thrown when an invalid workflow status transition is attempted.
/// </summary>
public class WorkflowStatusTransitionException : StateException
{
    /// <summary>
    /// Gets the current workflow status.
    /// </summary>
    public WorkflowStatus CurrentStatus { get; }

    /// <summary>
    /// Gets the requested workflow status.
    /// </summary>
    public WorkflowStatus RequestedStatus { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowStatusTransitionException"/> class.
    /// </summary>
    /// <param name="currentStatus">The current workflow status.</param>
    /// <param name="requestedStatus">The requested workflow status that was rejected.</param>
    /// <param name="instanceId">The ID of the workflow instance.</param>
    public WorkflowStatusTransitionException(WorkflowStatus currentStatus, WorkflowStatus requestedStatus, string? instanceId = null)
        : base(
            GetErrorMessage(currentStatus, requestedStatus),
            currentStatus.ToString(),
            requestedStatus.ToString(),
            instanceId ?? "Unknown")
    {
        CurrentStatus = currentStatus;
        RequestedStatus = requestedStatus;
    }

    private static string GetErrorMessage(WorkflowStatus currentStatus, WorkflowStatus requestedStatus)
    {
        return $"Invalid workflow status transition from {currentStatus} to {requestedStatus}. " +
               "Allowed transitions must follow the explicit state machine rules.";
    }
}