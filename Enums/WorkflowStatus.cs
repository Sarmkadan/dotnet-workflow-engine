// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetWorkflowEngine.Enums;

/// <summary>
/// Represents the lifecycle status of a workflow definition.
/// </summary>
public enum WorkflowStatus
{
    /// <summary>Workflow is in draft state, not yet published</summary>
    Draft = 0,

    /// <summary>Workflow is active and can be instantiated</summary>
    Active = 1,

    /// <summary>Workflow is deprecated but existing instances continue</summary>
    Deprecated = 2,

    /// <summary>Workflow is archived and no new instances allowed</summary>
    Archived = 3,

    /// <summary>Workflow is suspended and execution is paused</summary>
    Suspended = 4
}
