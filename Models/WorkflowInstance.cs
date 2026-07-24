// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using DotNetWorkflowEngine.Enums;
using System.Text.Json.Serialization;

namespace DotNetWorkflowEngine.Models;

/// <summary>
/// Represents a runtime instance of a workflow execution.
/// </summary>
public class WorkflowInstance
{
    /// <summary>Gets or sets the unique identifier of this instance.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the optimistic concurrency version number.</summary>
    public int Version { get; set; } = 0;

    /// <summary>
    /// Gets the version of the workflow definition that this instance was created from.
    /// This ensures the instance executes against the specific workflow version it started with,
    /// even if the workflow definition is later updated.
    /// </summary>
    public int DefinitionVersion { get; set; }

    /// <summary>Gets or sets the ID of the workflow definition.</summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>Gets or sets the current status of the instance.</summary>
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Draft;

    /// <summary>Gets or sets the ID of the activity currently executing.</summary>
    public string? CurrentActivityId { get; set; }

    /// <summary>Gets or sets the list of activities that have been executed.</summary>
    public List<string> ExecutedActivities { get; set; } = new();

    /// <summary>Gets or sets the list of activities currently executing (for parallel execution).</summary>
    public List<string> ActiveActivities { get; set; } = new();

    /// <summary>Gets or sets execution context containing variables and state.</summary>
    public Dictionary<string, object?> Context { get; set; } = new();

    /// <summary>Gets or sets when the instance was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets when the instance execution started.</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>Gets or sets when the instance execution completed.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Gets or sets the total execution time in milliseconds.</summary>
    public long ExecutionTimeMs { get; set; } = 0;

    /// <summary>
    /// Gets the elapsed duration of this instance.
    /// Returns <c>CompletedAt - StartedAt</c> for terminal instances, or
    /// <c>UtcNow - StartedAt</c> for instances that are still running.
    /// Returns <c>null</c> when the instance has not been started yet.
    /// </summary>
    public TimeSpan? Duration =>
        StartedAt.HasValue
            ? (CompletedAt ?? DateTime.UtcNow) - StartedAt.Value
            : null;

    /// <summary>Gets or sets the last error message if execution failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the correlation ID for tracking related instances.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Gets or sets custom metadata associated with the instance.</summary>
    public Dictionary<string, object?> Metadata { get; set; } = new();

    /// <summary>Gets or sets the ID of the user who initiated this instance.</summary>
    public string? InitiatedBy { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowInstance"/> class.
    /// </summary>
    /// <param name="workflowId">The unique identifier of the workflow definition.</param>
    /// <param name="correlationId">Optional correlation ID for tracking related instances.</param>
    /// <param name="definitionVersion">The version of the workflow definition to pin this instance to.</param>
    public WorkflowInstance(string workflowId, string? correlationId = null, int definitionVersion = 1)
    {
        Id = Guid.NewGuid().ToString();
        WorkflowId = workflowId;
        CorrelationId = correlationId ?? Id;
        DefinitionVersion = definitionVersion;
    }

    /// <summary>
    /// Parameterless constructor for deserialization.
    /// </summary>
    public WorkflowInstance()
    {
    }

    /// <summary>
    /// Marks the instance as started.
    /// </summary>
    public void Start()
    {
        TransitionTo(WorkflowStatus.Active);
    }

    /// <summary>
    /// Marks the instance as completed successfully.
    /// </summary>
    public void Complete()
    {
        TransitionTo(WorkflowStatus.Archived);
    }

    /// <summary>
    /// Marks the instance as failed with error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    public void Fail(string errorMessage)
    {
        ErrorMessage = errorMessage;
        TransitionTo(WorkflowStatus.Suspended);
    }

    /// <summary>
    /// Sets a context variable.
    /// </summary>
    public void SetContextVariable(string key, object? value)
    {
        Context[key] = value;
    }

    /// <summary>
    /// Gets a context variable.
    /// </summary>
    public object? GetContextVariable(string key)
    {
        Context.TryGetValue(key, out var value);
        return value;
    }

    /// <summary>
    /// Checks if an activity has been executed.
    /// </summary>
    public bool HasActivityBeenExecuted(string activityId)
    {
        return ExecutedActivities.Contains(activityId);
    }

    /// <summary>
    /// Records that an activity has been executed.
    /// </summary>
    public void RecordActivityExecution(string activityId)
    {
        if (!ExecutedActivities.Contains(activityId))
            ExecutedActivities.Add(activityId);
    }

    /// <summary>
    /// Checks if the instance is currently active.
    /// </summary>
    public bool IsActive()
    {
        return Status == WorkflowStatus.Active;
    }

    /// <summary>
    /// Checks if the instance is completed or cancelled.
    /// </summary>
    public bool IsCompletedOrCancelled()
    {
        return Status == WorkflowStatus.Archived || Status == WorkflowStatus.Cancelled;
    }

    /// <summary>
    /// Checks if the instance is suspended.
    /// </summary>
    public bool IsSuspended()
    {
        return Status == WorkflowStatus.Suspended;
    }

    /// <summary>
    /// Marks the instance as suspended.
    /// </summary>
    /// <param name="reason">Optional reason for suspension.</param>
    public void Suspend(string? reason = null)
    {
        TransitionTo(WorkflowStatus.Suspended, reason);
    }

    /// <summary>
    /// Marks the instance as cancelled.
    /// </summary>
    public void Cancel()
    {
        TransitionTo(WorkflowStatus.Cancelled);
    }

    /// <summary>
    /// Creates an independent deep copy of this instance, including its own copies of
    /// <see cref="ExecutedActivities"/>, <see cref="ActiveActivities"/>, <see cref="Context"/> and
    /// <see cref="Metadata"/>. Repositories return clones from reads so that callers can only affect
    /// persisted state by going through a save path that enforces the <see cref="Version"/>
    /// compare-and-swap - mutating a clone in place never bypasses the concurrency check.
    /// </summary>
    /// <returns>A new <see cref="WorkflowInstance"/> with the same field values as this one.</returns>
    public WorkflowInstance Clone() => new()
    {
        Id = Id,
        Version = Version,
        DefinitionVersion = DefinitionVersion,
        WorkflowId = WorkflowId,
        Status = Status,
        CurrentActivityId = CurrentActivityId,
        ExecutedActivities = new List<string>(ExecutedActivities),
        ActiveActivities = new List<string>(ActiveActivities),
        Context = new Dictionary<string, object?>(Context),
        CreatedAt = CreatedAt,
        StartedAt = StartedAt,
        CompletedAt = CompletedAt,
        ExecutionTimeMs = ExecutionTimeMs,
        ErrorMessage = ErrorMessage,
        CorrelationId = CorrelationId,
        Metadata = new Dictionary<string, object?>(Metadata),
        InitiatedBy = InitiatedBy
    };

    /// <summary>
    /// Validates and performs a state transition to the specified status.
    /// This method enforces the explicit state machine rules and ensures only valid transitions are allowed.
    /// </summary>
    /// <param name="newStatus">The target status to transition to.</param>
    /// <param name="reason">Optional reason for the status change.</param>
    /// <exception cref="WorkflowStatusTransitionException">Thrown when the transition is invalid.</exception>
    public void TransitionTo(WorkflowStatus newStatus, string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(newStatus);

        if (!WorkflowStatusMachine.IsValidTransition(Status, newStatus))
        {
            throw new WorkflowStatusTransitionException(Status, newStatus, Id);
        }

        // Perform the transition with optimistic concurrency
        Version++;

        switch (newStatus)
        {
            case WorkflowStatus.Active:
                StartedAt = DateTime.UtcNow;
                break;

            case WorkflowStatus.Archived:
                CompletedAt = DateTime.UtcNow;
                ExecutionTimeMs = (long)(CompletedAt.Value - (StartedAt ?? CreatedAt)).TotalMilliseconds;
                break;

            case WorkflowStatus.Suspended:
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    ErrorMessage = reason;
                }
                break;

            case WorkflowStatus.Cancelled:
                CompletedAt = DateTime.UtcNow;
                ExecutionTimeMs = (long)(CompletedAt.Value - (StartedAt ?? CreatedAt)).TotalMilliseconds;
                break;

            case WorkflowStatus.WaitingForMessage:
                // Clear waiting message metadata if transitioning away from waiting state
                if (Status == WorkflowStatus.WaitingForMessage)
                {
                    Context.Remove("WaitingForMessageName");
                    Context.Remove("WaitingForCorrelationKey");
                    Context.Remove("WaitingActivityId");
                }
                break;
        }

        Status = newStatus;
    }
}