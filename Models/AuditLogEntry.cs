// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

namespace DotNetWorkflowEngine.Models;

/// <summary>
/// Represents an audit log entry for tracking workflow events and changes.
/// </summary>
public class AuditLogEntry
{
    /// <summary>Gets or sets the unique identifier of the audit entry.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the ID of the workflow instance this entry relates to.</summary>
    public string WorkflowInstanceId { get; set; } = string.Empty;

    /// <summary>Gets or sets the type of event recorded.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Gets or sets the activity ID involved in this event.</summary>
    public string? ActivityId { get; set; }

    /// <summary>Gets or sets a human-readable description of the event.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the severity level of the event.</summary>
    public string Severity { get; set; } = "Info"; // Info, Warning, Error, Critical

    /// <summary>Gets or sets when this event occurred.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the user who triggered this event.</summary>
    public string? Actor { get; set; }

    /// <summary>Gets or sets the previous state before this event.</summary>
    public Dictionary<string, object?> PreviousState { get; set; } = new();

    /// <summary>Gets or sets the current state after this event.</summary>
    public Dictionary<string, object?> CurrentState { get; set; } = new();

    /// <summary>Gets or sets details specific to the event.</summary>
    public Dictionary<string, object?> Details { get; set; } = new();

    /// <summary>Gets or sets the correlation ID for tracking related events.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Initializes a new audit log entry.
    /// </summary>
    public AuditLogEntry()
    {
    }

    /// <summary>
    /// Initializes a new audit log entry with required information.
    /// </summary>
    public AuditLogEntry(string workflowInstanceId, string eventType, string description)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowInstanceId);
        ArgumentException.ThrowIfNullOrEmpty(eventType);
        ArgumentException.ThrowIfNullOrEmpty(description);
        Id = Guid.NewGuid().ToString();
        WorkflowInstanceId = workflowInstanceId;
        EventType = eventType;
        Description = description;
    }

    /// <summary>
    /// Creates an audit entry for activity execution.
    /// </summary>
    public static AuditLogEntry CreateActivityExecution(string workflowInstanceId, string activityId, string status)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowInstanceId);
        ArgumentException.ThrowIfNullOrEmpty(activityId);
        ArgumentException.ThrowIfNullOrEmpty(status);
        return new AuditLogEntry
        {
            Id = Guid.NewGuid().ToString(),
            WorkflowInstanceId = workflowInstanceId,
            EventType = "ActivityExecution",
            ActivityId = activityId,
            Description = $"Activity {activityId} execution: {status}"
        };
    }

    /// <summary>
    /// Creates an audit entry for state change.
    /// </summary>
    public static AuditLogEntry CreateStateChange(string workflowInstanceId, string previousState, string currentState, string reason)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowInstanceId);
        ArgumentException.ThrowIfNullOrEmpty(previousState);
        ArgumentException.ThrowIfNullOrEmpty(currentState);
        ArgumentException.ThrowIfNullOrEmpty(reason);
        return new AuditLogEntry
        {
            Id = Guid.NewGuid().ToString(),
            WorkflowInstanceId = workflowInstanceId,
            EventType = "StateChange",
            Description = $"Workflow state changed from {previousState} to {currentState}: {reason}",
            Severity = "Warning"
        };
    }

    /// <summary>
    /// Creates an audit entry for an error.
    /// </summary>
    public static AuditLogEntry CreateError(string workflowInstanceId, string? activityId, string errorMessage, string? correlationId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowInstanceId);
        ArgumentException.ThrowIfNullOrEmpty(errorMessage);
        return new AuditLogEntry
        {
            Id = Guid.NewGuid().ToString(),
            WorkflowInstanceId = workflowInstanceId,
            ActivityId = activityId,
            EventType = "Error",
            Description = errorMessage,
            Severity = "Error",
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Gets a formatted timestamp string.
    /// </summary>
    public string GetFormattedTimestamp()
    {
        return Timestamp.ToString(Constants.WorkflowConstants.AuditTimestampFormat);
    }

    /// <summary>
    /// Validates the audit log entry for security and consistency.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(WorkflowInstanceId))
        {
            throw new ArgumentException(
                "WorkflowInstanceId cannot be null or whitespace.",
                nameof(WorkflowInstanceId));
        }

        if (WorkflowInstanceId.Length > Constants.WorkflowConstants.MaxWorkflowInstanceIdLength)
        {
            throw new ArgumentException(
                $"WorkflowInstanceId exceeds maximum length of {Constants.WorkflowConstants.MaxWorkflowInstanceIdLength} characters.",
                nameof(WorkflowInstanceId));
        }

        // Validate for path-unsafe or control characters that could be used in path traversal
        if (ContainsUnsafePathCharacters(WorkflowInstanceId))
        {
            throw new ArgumentException(
                "WorkflowInstanceId contains path-unsafe or control characters.",
                nameof(WorkflowInstanceId));
        }

        if (string.IsNullOrWhiteSpace(EventType))
        {
            throw new ArgumentException(
                "EventType cannot be null or whitespace.",
                nameof(EventType));
        }

        if (EventType.Length > Constants.WorkflowConstants.MaxEventTypeLength)
        {
            throw new ArgumentException(
                $"EventType exceeds maximum length of {Constants.WorkflowConstants.MaxEventTypeLength} characters.",
                nameof(EventType));
        }

        if (Description.Length > Constants.WorkflowConstants.MaxAuditFieldLength)
        {
            throw new ArgumentException(
                $"Description exceeds maximum length of {Constants.WorkflowConstants.MaxAuditFieldLength} characters.",
                nameof(Description));
        }

        if (Severity.Length > Constants.WorkflowConstants.MaxAuditFieldLength)
        {
            throw new ArgumentException(
                $"Severity exceeds maximum length of {Constants.WorkflowConstants.MaxAuditFieldLength} characters.",
                nameof(Severity));
        }

        if (Actor?.Length > Constants.WorkflowConstants.MaxAuditFieldLength)
        {
            throw new ArgumentException(
                $"Actor exceeds maximum length of {Constants.WorkflowConstants.MaxAuditFieldLength} characters.",
                nameof(Actor));
        }

        if (ActivityId?.Length > Constants.WorkflowConstants.MaxActivityIdLength)
        {
            throw new ArgumentException(
                $"ActivityId exceeds maximum length of {Constants.WorkflowConstants.MaxActivityIdLength} characters.",
                nameof(ActivityId));
        }

        if (Id.Length > Constants.WorkflowConstants.MaxAuditFieldLength)
        {
            throw new ArgumentException(
                $"Id exceeds maximum length of {Constants.WorkflowConstants.MaxAuditFieldLength} characters.",
                nameof(Id));
        }
    }

    /// <summary>
    /// Checks if a string contains path-unsafe or control characters that could be used in path traversal attacks.
    /// </summary>
    private static bool ContainsUnsafePathCharacters(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        foreach (char c in input)
        {
            // Check for path separators and control characters
            if (c == '/' || c == '\\' || c == ':' || c == '*' || c == '?' ||
                c == '"' || c == '<' || c == '>' || c == '|' ||
                c < 32) // Control characters (ASCII 0-31)
            {
                return true;
            }
        }

        return false;
    }
}