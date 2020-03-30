// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetWorkflowEngine.Exceptions;

/// <summary>
/// Exception thrown when an invalid state transition is attempted.
/// </summary>
public class StateException : WorkflowException
{
    /// <summary>
    /// Gets the current state.
    /// </summary>
    public string CurrentState { get; }

    /// <summary>
    /// Gets the requested state transition.
    /// </summary>
    public string RequestedState { get; }

    /// <summary>
    /// Gets the ID of the entity that failed state transition.
    /// </summary>
    public string? EntityId { get; }

    /// <summary>
    /// Initializes a new instance of the StateException class.
    /// </summary>
    public StateException(string message, string currentState, string requestedState)
        : base(message, "STATE_TRANSITION_ERROR")
    {
        CurrentState = currentState;
        RequestedState = requestedState;
    }

    /// <summary>
    /// Initializes a new instance with entity information.
    /// </summary>
    public StateException(string message, string currentState, string requestedState, string entityId)
        : base(message, "STATE_TRANSITION_ERROR")
    {
        CurrentState = currentState;
        RequestedState = requestedState;
        EntityId = entityId;
    }

    /// <summary>
    /// Gets a detailed message describing the invalid transition.
    /// </summary>
    public string GetTransitionDetails()
    {
        var entity = string.IsNullOrEmpty(EntityId) ? "" : $" (Entity: {EntityId})";
        return $"Cannot transition from {CurrentState} to {RequestedState}{entity}";
    }
}
