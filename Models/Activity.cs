// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;

namespace DotNetWorkflowEngine.Models;

/// <summary>
/// Represents an activity/task within a workflow.
/// </summary>
public class Activity
{
    /// <summary>Gets or sets the unique identifier of the activity.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the activity.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the description of the activity.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the type of the activity (e.g., Task, Event, Gateway).</summary>
    public string Type { get; set; } = "Task";

    /// <summary>Gets or sets how this activity should be executed.</summary>
    public ExecutionMode ExecutionMode { get; set; } = ExecutionMode.Sequential;

    /// <summary>Gets or sets the handler/implementation type for this activity.</summary>
    public string? HandlerType { get; set; }

    /// <summary>Gets or sets input parameters for the activity.</summary>

    public Dictionary<string, object?> InputParameters { get; set; } = new();

    /// <summary>Gets or sets output mapping for the activity results.</summary>

    public Dictionary<string, string> OutputMapping { get; set; } = new();

    /// <summary>Gets or sets the retry policy for failed execution.</summary>
    public RetryPolicy RetryPolicy { get; set; } = RetryPolicy.NoRetry;

    /// <summary>Gets or sets maximum number of retries allowed.</summary>
    public int MaxRetries { get; set; } = 1;

    /// <summary>Gets or sets the timeout in seconds for activity execution.</summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>Gets or sets whether this activity is optional.</summary>
    public bool IsOptional { get; set; } = false;

    /// <summary>Gets or sets the condition expression for conditional execution.</summary>
    public string? ConditionExpression { get; set; }

    /// <summary>Gets or sets custom metadata associated with the activity.</summary>

    public Dictionary<string, object?> Metadata { get; set; } = new();

    /// <summary>Gets or sets when the activity was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Validates the activity configuration.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Id))
            errors.Add("Activity ID is required");

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Activity name is required");

        if (TimeoutSeconds <= 0)
            errors.Add("Timeout must be greater than zero");

        if (MaxRetries < 0)
            errors.Add("MaxRetries cannot be negative");

        if (MaxRetries > 0 && RetryPolicy == RetryPolicy.NoRetry)
            errors.Add("MaxRetries is set but RetryPolicy is NoRetry");

        return errors.Count == 0;
    }

    /// <summary>
    /// Sets an input parameter for this activity.
    /// </summary>
    public void SetInputParameter(string key, object? value)
    {
        InputParameters[key] = value;
    }

    /// <summary>
    /// Gets an input parameter value.
    /// </summary>
    public object? GetInputParameter(string key)
    {
        InputParameters.TryGetValue(key, out var value);
        return value;
    }

    /// <summary>
    /// Adds output mapping from activity output key to context key.
    /// </summary>
    public void AddOutputMapping(string activityOutputKey, string contextKey)
    {
        OutputMapping[activityOutputKey] = contextKey;
    }

    /// <summary>
    /// Checks if this activity is a gateway (fork/join).
    /// </summary>
    public bool IsGateway()
    {
        return ExecutionMode == ExecutionMode.Fork || ExecutionMode == ExecutionMode.Join;
    }

    /// <summary>
    /// Checks if this activity requires a handler implementation.
    /// </summary>
    public bool RequiresHandler()
    {
        return !IsGateway() && Type != "Event";
    }
}
