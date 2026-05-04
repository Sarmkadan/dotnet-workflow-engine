// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetWorkflowEngine.Events;

/// <summary>
/// Central event bus for workflow engine events. Implements pub-sub pattern
/// allowing components to publish workflow events and subscribe to notifications.
/// Handles async event dispatching with error isolation (exceptions in one
/// subscriber don't prevent others from being notified).
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Subscribes to events of a specific type with an async handler.
    /// </summary>
    void Subscribe<T>(Func<T, Task> handler) where T : IWorkflowEvent;

    /// <summary>
    /// Unsubscribes from events, removing a previously registered handler.
    /// </summary>
    void Unsubscribe<T>(Func<T, Task> handler) where T : IWorkflowEvent;

    /// <summary>
    /// Publishes an event to all subscribed handlers.
    /// </summary>
    Task PublishAsync<T>(T @event) where T : IWorkflowEvent;
}

/// <summary>
/// Base interface for all workflow events.
/// </summary>
public interface IWorkflowEvent
{
    string EventType { get; }
    DateTime Timestamp { get; }
}

/// <summary>
/// Workflow lifecycle events.
/// </summary>
public class WorkflowStartedEvent : IWorkflowEvent
{
    public string EventType => "workflow.started";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? WorkflowId { get; set; }
    public string? InstanceId { get; set; }
    public Dictionary<string, object>? InputData { get; set; }
}

public class WorkflowCompletedEvent : IWorkflowEvent
{
    public string EventType => "workflow.completed";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? WorkflowId { get; set; }
    public string? InstanceId { get; set; }
    public Dictionary<string, object>? OutputData { get; set; }
    public long DurationMs { get; set; }
}

public class WorkflowFailedEvent : IWorkflowEvent
{
    public string EventType => "workflow.failed";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? WorkflowId { get; set; }
    public string? InstanceId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? FailedActivityId { get; set; }
}

/// <summary>
/// Activity-level events.
/// </summary>
public class ActivityStartedEvent : IWorkflowEvent
{
    public string EventType => "activity.started";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? InstanceId { get; set; }
    public string? ActivityId { get; set; }
    public Dictionary<string, object>? InputData { get; set; }
}

public class ActivityCompletedEvent : IWorkflowEvent
{
    public string EventType => "activity.completed";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? InstanceId { get; set; }
    public string? ActivityId { get; set; }
    public Dictionary<string, object>? OutputData { get; set; }
    public long DurationMs { get; set; }
}

public class ActivityFailedEvent : IWorkflowEvent
{
    public string EventType => "activity.failed";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? InstanceId { get; set; }
    public string? ActivityId { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryAttempt { get; set; }
}

/// <summary>
/// Implementation of the event bus with in-memory subscriber management.
/// </summary>
public class EventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
    private readonly ILogger<EventBus> _logger;
    private readonly object _lock = new();

    public EventBus(ILogger<EventBus> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Subscribes a handler to a specific event type.
    /// Handlers can subscribe to the same event type multiple times.
    /// </summary>
    public void Subscribe<T>(Func<T, Task> handler) where T : IWorkflowEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        lock (_lock)
        {
            var eventType = typeof(T);

            if (!_subscribers.ContainsKey(eventType))
                _subscribers[eventType] = new List<Delegate>();

            _subscribers[eventType].Add(handler);

            _logger.LogDebug(
                "Subscriber registered for event type {EventType}. Total subscribers: {Count}",
                eventType.Name,
                _subscribers[eventType].Count);
        }
    }

    /// <summary>
    /// Unsubscribes a handler from a specific event type.
    /// </summary>
    public void Unsubscribe<T>(Func<T, Task> handler) where T : IWorkflowEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        lock (_lock)
        {
            var eventType = typeof(T);

            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);

                if (handlers.Count == 0)
                    _subscribers.Remove(eventType);

                _logger.LogDebug(
                    "Subscriber unregistered for event type {EventType}. Remaining subscribers: {Count}",
                    eventType.Name,
                    handlers.Count);
            }
        }
    }

    /// <summary>
    /// Publishes an event to all subscribed handlers.
    /// Executes all handlers asynchronously but awaits their completion.
    /// If a handler throws, the exception is logged but doesn't prevent other handlers from executing.
    /// </summary>
    public async Task PublishAsync<T>(T @event) where T : IWorkflowEvent
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        var eventType = typeof(T);
        List<Delegate>? handlers = null;

        lock (_lock)
        {
            if (_subscribers.TryGetValue(eventType, out var h))
                handlers = new List<Delegate>(h);
        }

        if (handlers == null || handlers.Count == 0)
        {
            _logger.LogDebug("Event published with no subscribers: {EventType}", @event.EventType);
            return;
        }

        _logger.LogInformation(
            "Publishing event {EventType} to {SubscriberCount} subscribers",
            @event.EventType,
            handlers.Count);

        var tasks = handlers
            .Cast<Func<T, Task>>()
            .Select(handler => InvokeHandlerSafelyAsync(handler, @event))
            .ToList();

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Invokes a handler with exception safety - logs errors but doesn't rethrow.
    /// </summary>
    private async Task InvokeHandlerSafelyAsync<T>(Func<T, Task> handler, T @event) where T : IWorkflowEvent
    {
        try
        {
            await handler(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error in event handler for {EventType}. Handler: {HandlerType}",
                @event.EventType,
                handler.GetType().Name);
        }
    }
}
