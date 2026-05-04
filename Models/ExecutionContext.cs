// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Newtonsoft.Json;

namespace DotNetWorkflowEngine.Models;

/// <summary>
/// Contains the execution context for a workflow or activity execution.
/// </summary>
public class ExecutionContext
{
    /// <summary>Gets or sets the ID of the workflow instance.</summary>
    public string WorkflowInstanceId { get; set; } = string.Empty;

    /// <summary>Gets or sets the ID of the current activity.</summary>
    public string? ActivityId { get; set; }

    /// <summary>Gets or sets the correlation ID for tracking.</summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>Gets or sets variables stored during execution.</summary>
    [JsonProperty]
    public Dictionary<string, object?> Variables { get; set; } = new();

    /// <summary>Gets or sets execution state data.</summary>
    [JsonProperty]
    public Dictionary<string, object?> State { get; set; } = new();

    /// <summary>Gets or sets input data for the current activity.</summary>
    [JsonProperty]
    public Dictionary<string, object?> ActivityInput { get; set; } = new();

    /// <summary>Gets or sets output data from the current activity.</summary>
    [JsonProperty]
    public Dictionary<string, object?> ActivityOutput { get; set; } = new();

    /// <summary>Gets or sets when the execution started.</summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets when the execution completed.</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>Gets or sets the execution duration in milliseconds.</summary>
    public long ExecutionDurationMs { get; set; } = 0;

    /// <summary>Gets or sets whether execution is still active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets any error that occurred during execution.</summary>
    public string? ExecutionError { get; set; }

    /// <summary>Gets or sets custom metadata.</summary>
    [JsonProperty]
    public Dictionary<string, object?> Metadata { get; set; } = new();

    /// <summary>
    /// Sets a variable in the execution context.
    /// </summary>
    public void SetVariable(string key, object? value)
    {
        Variables[key] = value;
    }

    /// <summary>
    /// Gets a variable from the execution context.
    /// </summary>
    public object? GetVariable(string key)
    {
        Variables.TryGetValue(key, out var value);
        return value;
    }

    /// <summary>
    /// Gets a variable as a specific type.
    /// </summary>
    public T? GetVariable<T>(string key)
    {
        if (Variables.TryGetValue(key, out var value))
        {
            if (value is T typed)
                return typed;
            return (T?)Convert.ChangeType(value, typeof(T));
        }
        return default;
    }

    /// <summary>
    /// Sets activity input parameter.
    /// </summary>
    public void SetActivityInput(string key, object? value)
    {
        ActivityInput[key] = value;
    }

    /// <summary>
    /// Gets activity input parameter.
    /// </summary>
    public object? GetActivityInput(string key)
    {
        ActivityInput.TryGetValue(key, out var value);
        return value;
    }

    /// <summary>
    /// Sets activity output value.
    /// </summary>
    public void SetActivityOutput(string key, object? value)
    {
        ActivityOutput[key] = value;
    }

    /// <summary>
    /// Marks execution as completed.
    /// </summary>
    public void Complete()
    {
        EndTime = DateTime.UtcNow;
        ExecutionDurationMs = (long)(EndTime.Value - StartTime).TotalMilliseconds;
        IsActive = false;
    }

    /// <summary>
    /// Marks execution as failed.
    /// </summary>
    public void Fail(string errorMessage)
    {
        ExecutionError = errorMessage;
        EndTime = DateTime.UtcNow;
        ExecutionDurationMs = (long)(EndTime.Value - StartTime).TotalMilliseconds;
        IsActive = false;
    }

    /// <summary>
    /// Clears all execution data for a fresh state.
    /// </summary>
    public void Reset()
    {
        Variables.Clear();
        State.Clear();
        ActivityInput.Clear();
        ActivityOutput.Clear();
        ExecutionError = null;
        IsActive = true;
        StartTime = DateTime.UtcNow;
        EndTime = null;
        ExecutionDurationMs = 0;
    }
}
