// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

namespace DotNetWorkflowEngine.Constants;

/// <summary>
/// Contains constant values used throughout the workflow engine.
/// </summary>
public static class WorkflowConstants
{
    /// <summary>Maximum number of activity retries allowed</summary>
    public const int MaxRetries = 10;

    /// <summary>Default retry delay in milliseconds</summary>
    public const int DefaultRetryDelayMs = 1000;

    /// <summary>Maximum workflow execution timeout in minutes</summary>
    public const int MaxExecutionTimeoutMinutes = 1440; // 24 hours

    /// <summary>Default workflow execution timeout in minutes</summary>
    public const int DefaultExecutionTimeoutMinutes = 60;

    /// <summary>Exponential backoff multiplier</summary>
    public const double BackoffMultiplier = 2.0;

    /// <summary>Maximum exponential backoff delay in milliseconds</summary>
    public const int MaxBackoffDelayMs = 300000; // 5 minutes

    /// <summary>Activity state key prefix for context storage</summary>
    public const string ActivityStateKeyPrefix = "activity_state_";

    /// <summary>Workflow state key prefix for context storage</summary>
    public const string WorkflowStateKeyPrefix = "workflow_state_";

    /// <summary>Audit log entry timestamp format</summary>
    public const string AuditTimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";

    /// <summary>Maximum audit log entries retained per workflow instance</summary>
    public const int MaxAuditEntriesPerInstance = 10000;

    /// <summary>Default page size for query results</summary>
    public const int DefaultPageSize = 100;

    /// <summary>Maximum page size allowed</summary>
    public const int MaxPageSize = 1000;

    /// <summary>Maximum length for audit log entry fields to prevent memory amplification</summary>
    public const int MaxAuditFieldLength = 4096; // 4 KB per field

    /// <summary>Maximum length for workflow instance ID to prevent path traversal</summary>
    public const int MaxWorkflowInstanceIdLength = 255; // Reasonable limit for IDs used in paths

    /// <summary>Maximum length for workflow ID to prevent path traversal</summary>
    public const int MaxWorkflowIdLength = 255; // Reasonable limit for IDs used in paths

    /// <summary>Maximum length for activity ID to prevent path traversal</summary>
    public const int MaxActivityIdLength = 255; // Reasonable limit for IDs used in paths

    /// <summary>Maximum length for event type to prevent memory amplification</summary>
    public const int MaxEventTypeLength = 128; // Reasonable limit for event types
}