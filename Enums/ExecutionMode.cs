// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetWorkflowEngine.Enums;

/// <summary>
/// Defines how activities are executed within a workflow.
/// </summary>
public enum ExecutionMode
{
    /// <summary>Activities execute sequentially, one after another</summary>
    Sequential = 0,

    /// <summary>Activities execute in parallel when possible</summary>
    Parallel = 1,

    /// <summary>Activities execute based on conditional expressions</summary>
    Conditional = 2,

    /// <summary>Activity is a fork that splits into multiple parallel branches</summary>
    Fork = 3,

    /// <summary>Activity is a join that waits for multiple parallel branches</summary>
    Join = 4,

    /// <summary>Activity repeats until a condition is met</summary>
    Loop = 5
}
