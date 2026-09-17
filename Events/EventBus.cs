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
    /// <summary>
    /// Gets the type identifier of the event (e.g. "workflow.started").
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// Gets the UTC timestamp when the event was created.
    /// </summary>
    DateTime Timestamp { get; }
}

/// <summary>
/// Workflow lifecycle events.
/// </summary>
/// <summary>
/// Raised when a workflow instance starts executing.
/// </summary>
public class WorkflowStartedEvent : IWorkflowEvent
{
    /// <summary>
    /// Gets the event type identifier.
    /// </summary>
    public string EventType => "workflow.started";

    /// <summary>
    /// Gets or sets the UTC timestamp when the event was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the identifier of the workflow definition.
    /// </summary>
    public string? WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the workflow instance.
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// Gets or sets the input data passed to the workflow.
    /// </summary>
    public Dictionary<string, object>? InputData { get; set; }
}

/// <summary>
/// Raised when a workflow instance completes successfully.
/// </summary>
public class WorkflowCompletedEvent : IWorkflowEvent
{
    /// <summary>
    /// Gets the event type identifier.
    /// </summary>
    public string EventType => "workflow.completed";

    /// <summary>
    /// Gets or sets the UTC timestamp when the event was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the identifier of the workflow definition.
    /// </summary>
    public string? WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the workflow instance.
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// Gets or sets the output data produced by the workflow.
    /// </summary>
    public Dictionary<string, object>? OutputData { get; set; }

    /// <summary>
    /// Gets or sets the total execution duration of the workflow in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }
}

/// <summary>
/// Raised when a workflow instance fails.
/// </summary>
public class WorkflowFailedEvent : IWorkflowEvent
{
    /// <summary>
    /// Gets the event type identifier.
    /// </summary>
    public string EventType => "workflow.failed";

    /// <summary>
    /// Gets or sets the UTC timestamp when the event was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the identifier of the workflow definition.
    /// </summary>
    public string? WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the workflow instance.
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// Gets or sets the error message describing the failure.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the activity that failed.
    /// </summary>
    public string? FailedActivityId { get; set; }
}

/// <summary>
/// Activity-level events.
/// </summary>
/// <summary>
/// Raised when an activity within a workflow instance starts executing.
/// </summary>
public class ActivityStartedEvent : IWorkflowEvent
{
    /// <summary>
    /// Gets the event type identifier.
    /// </summary>
    public string EventType => "activity.started";

    /// <summary>
    /// Gets or sets the UTC timestamp when the event was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the identifier of the workflow instance.
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the activity.
    /// </summary>
    public string? ActivityId { get; set; }

    /// <summary>
    /// Gets or sets the input data passed to the activity.
    /// </summary>
    public Dictionary<string, object>? InputData { get; set; }
}

/// <summary>
/// Raised when an activity within a workflow instance completes successfully.
/// </summary>
public class ActivityCompletedEvent : IWorkflowEvent
{
    /// <summary>
    /// Gets the event type identifier.
    /// </summary>
    public string EventType => "activity.completed";

    /// <summary>
    /// Gets or sets the UTC timestamp when the event was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the identifier of the workflow instance.
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the activity.
    /// </summary>
    public string? ActivityId { get; set; }

    /// <summary>
    /// Gets or sets the output data produced by the activity.
    /// </summary>
    public Dictionary<string, object>? OutputData { get; set; }

    /// <summary>
    /// Gets or sets the execution duration of the activity in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }
}

/// <summary>
/// Raised when an activity within a workflow instance fails.
/// </summary>
public class ActivityFailedEvent : IWorkflowEvent
{
    /// <summary>
    /// Gets the event type identifier.
    /// </summary>
    public string EventType => "activity.failed";

    /// <summary>
    /// Gets or sets the UTC timestamp when the event was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the identifier of the workflow instance.
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the activity.
    /// </summary>
    public string? ActivityId { get; set; }

    /// <summary>
    /// Gets or sets the error message describing the failure.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of retry attempts made for the activity.
    /// </summary>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBus"/> class.
    /// </summary>
    /// <param name="logger">The logger used to record event bus activity.</param>
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
