// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;

namespace DotNetWorkflowEngine.Models;

/// <summary>
/// Represents a runtime instance of a workflow execution.
/// </summary>
public class WorkflowInstance
{
    /// <summary>Gets or sets the unique identifier of this instance.</summary>
    public string Id { get; set; } = string.Empty;

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

    /// <summary>Gets or sets the last error message if execution failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the correlation ID for tracking related instances.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Gets or sets custom metadata associated with the instance.</summary>

    public Dictionary<string, object?> Metadata { get; set; } = new();

    /// <summary>Gets or sets the ID of the user who initiated this instance.</summary>
    public string? InitiatedBy { get; set; }

    /// <summary>
    /// Initializes a new instance with required information.
    /// </summary>
    public WorkflowInstance(string workflowId, string? correlationId = null)
    {
        Id = Guid.NewGuid().ToString();
        WorkflowId = workflowId;
        CorrelationId = correlationId ?? Id;
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
        StartedAt = DateTime.UtcNow;
        Status = WorkflowStatus.Active;
    }

    /// <summary>
    /// Marks the instance as completed successfully.
    /// </summary>
    public void Complete()
    {
        CompletedAt = DateTime.UtcNow;
        Status = WorkflowStatus.Archived;
        ExecutionTimeMs = (long)(CompletedAt.Value - (StartedAt ?? CreatedAt)).TotalMilliseconds;
    }

    /// <summary>
    /// Marks the instance as failed with error message.
    /// </summary>
    public void Fail(string errorMessage)
    {
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        Status = WorkflowStatus.Suspended;
        ExecutionTimeMs = (long)(CompletedAt.Value - (StartedAt ?? CreatedAt)).TotalMilliseconds;
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
}
