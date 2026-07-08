// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace DotNetWorkflowEngine.Events;

/// <summary>
/// Represents a message received from an external system that can be correlated
/// to a waiting workflow instance.
/// </summary>
public interface IWorkflowMessage
{
    /// <summary>Gets the unique identifier for the message, used for correlation.</summary>
    string CorrelationKey { get; }

    /// <summary>Gets the name of the message (e.g., "PaymentConfirmed", "ApprovalGranted").</summary>
    string MessageName { get; }

    /// <summary>Gets any payload data associated with the message.</summary>
    Dictionary<string, object?> Payload { get; }
}

/// <summary>
/// Default concrete implementation of <see cref="IWorkflowMessage"/> for constructing
/// and dispatching correlation messages to the engine.
/// </summary>
public class WorkflowMessage : IWorkflowMessage
{
    /// <summary>Gets or sets the unique identifier for the message, used for correlation.</summary>
    public string CorrelationKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the message (e.g., "PaymentConfirmed", "ApprovalGranted").</summary>
    public string MessageName { get; set; } = string.Empty;

    /// <summary>Gets or sets any payload data associated with the message.</summary>
    public Dictionary<string, object?> Payload { get; set; } = new();
}

/// <summary>
/// Event published by the engine when an external message is received and
/// potentially correlated to a workflow instance.
/// </summary>
public class MessageReceivedEvent : IWorkflowEvent
{
    public string EventType => "message.received";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Gets the correlated workflow instance ID, if found.</summary>
    public string? InstanceId { get; set; }

    /// <summary>Gets the correlation key of the message.</summary>
    public string CorrelationKey { get; set; } = string.Empty;

    /// <summary>Gets the name of the message.</summary>
    public string MessageName { get; set; } = string.Empty;

    /// <summary>Gets the full message payload.</summary>
    public Dictionary<string, object?> Payload { get; set; } = new();

    /// <summary>Indicates if a waiting workflow instance was found and resumed.</summary>
    public bool WorkflowResumed { get; set; }
}
