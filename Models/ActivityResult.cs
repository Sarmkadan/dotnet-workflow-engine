// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;
using Newtonsoft.Json;

namespace DotNetWorkflowEngine.Models;

/// <summary>
/// Represents the result of an activity execution.
/// </summary>
public class ActivityResult
{
    /// <summary>Gets or sets the ID of the activity that produced this result.</summary>
    public string ActivityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the status of the activity execution.</summary>
    public ActivityStatus Status { get; set; } = ActivityStatus.Pending;

    /// <summary>Gets or sets the output data from the activity.</summary>
    [JsonProperty]
    public Dictionary<string, object?> Output { get; set; } = new();

    /// <summary>Gets or sets any error message if execution failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the exception stack trace if an error occurred.</summary>
    public string? StackTrace { get; set; }

    /// <summary>Gets or sets when the activity started execution.</summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets when the activity completed.</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>Gets or sets the execution duration in milliseconds.</summary>
    public long ExecutionDurationMs { get; set; } = 0;

    /// <summary>Gets or sets the attempt number (for retries).</summary>
    public int AttemptNumber { get; set; } = 1;

    /// <summary>Gets or sets the total number of attempts made.</summary>
    public int TotalAttempts { get; set; } = 1;

    /// <summary>Gets or sets custom metadata.</summary>
    [JsonProperty]
    public Dictionary<string, object?> Metadata { get; set; } = new();

    /// <summary>
    /// Initializes a new activity result.
    /// </summary>
    public ActivityResult()
    {
    }

    /// <summary>
    /// Initializes a new activity result with activity ID.
    /// </summary>
    public ActivityResult(string activityId)
    {
        ActivityId = activityId;
    }

    /// <summary>
    /// Marks the activity as completed successfully.
    /// </summary>
    public void SetSuccess(Dictionary<string, object?> output)
    {
        Status = ActivityStatus.Completed;
        Output = output;
        EndTime = DateTime.UtcNow;
        ExecutionDurationMs = (long)(EndTime.Value - StartTime).TotalMilliseconds;
    }

    /// <summary>
    /// Marks the activity as failed.
    /// </summary>
    public void SetFailure(string errorMessage, string? stackTrace = null)
    {
        Status = ActivityStatus.Failed;
        ErrorMessage = errorMessage;
        StackTrace = stackTrace;
        EndTime = DateTime.UtcNow;
        ExecutionDurationMs = (long)(EndTime.Value - StartTime).TotalMilliseconds;
    }

    /// <summary>
    /// Marks the activity as skipped.
    /// </summary>
    public void SetSkipped(string reason)
    {
        Status = ActivityStatus.Skipped;
        ErrorMessage = reason;
        EndTime = DateTime.UtcNow;
        ExecutionDurationMs = (long)(EndTime.Value - StartTime).TotalMilliseconds;
    }

    /// <summary>
    /// Sets the activity status to waiting.
    /// </summary>
    public void SetWaiting(string reason)
    {
        Status = ActivityStatus.Waiting;
        ErrorMessage = reason;
    }

    /// <summary>
    /// Checks if the activity execution was successful.
    /// </summary>
    public bool IsSuccess()
    {
        return Status == ActivityStatus.Completed;
    }

    /// <summary>
    /// Checks if the activity execution failed.
    /// </summary>
    public bool IsFailed()
    {
        return Status == ActivityStatus.Failed;
    }

    /// <summary>
    /// Checks if the activity is waiting for external input.
    /// </summary>
    public bool IsWaiting()
    {
        return Status == ActivityStatus.Waiting;
    }

    /// <summary>
    /// Gets the output value for a specific key.
    /// </summary>
    public object? GetOutput(string key)
    {
        Output.TryGetValue(key, out var value);
        return value;
    }

    /// <summary>
    /// Gets the output value as a specific type.
    /// </summary>
    public T? GetOutput<T>(string key)
    {
        if (Output.TryGetValue(key, out var value))
        {
            if (value is T typed)
                return typed;
            return (T?)Convert.ChangeType(value, typeof(T));
        }
        return default;
    }
}
