// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetWorkflowEngine.Enums;

/// <summary>
/// Represents the execution status of an activity within a workflow instance.
/// </summary>
public enum ActivityStatus
{
    /// <summary>Activity is pending execution</summary>
    Pending = 0,

    /// <summary>Activity is currently running</summary>
    Running = 1,

    /// <summary>Activity completed successfully</summary>
    Completed = 2,

    /// <summary>Activity failed with an error</summary>
    Failed = 3,

    /// <summary>Activity was skipped due to conditional logic</summary>
    Skipped = 4,

    /// <summary>Activity is waiting for callback or timer</summary>
    Waiting = 5,

    /// <summary>Activity execution was cancelled</summary>
    Cancelled = 6
}
