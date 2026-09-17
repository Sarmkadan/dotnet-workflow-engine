# Message events

The message event system enables workflow instances to wait for and resume from external signals. It consists of two core services: `MessageEventService` for publishing and correlating messages, and `MessageSubscriptionRegistry` for tracking which workflow instances are waiting for specific messages.

## Overview

Workflow instances can pause at `MessageCatchEvent` activities, declaring they are waiting for a specific message name and correlation key. External systems publish messages via `MessageEventService.PublishMessageAsync`, which:

1. Publishes a `MessageReceivedEvent` to the event bus
2. Looks for workflow instances waiting for that message name and correlation key
3. Resumes the first matching instance by calling `WorkflowExecutionService.ResumeFromMessageAsync`
4. Returns `true` if a workflow was resumed, `false` otherwise

The `MessageSubscriptionRegistry` maintains an in-memory lookup of `(correlationKey, messageName) → instanceId` so the engine can quickly find waiting instances.

## Dependencies

`MessageEventService` depends on:
- `IEventBus` – publishes `MessageReceivedEvent`
- `WorkflowExecutionService` – finds waiting instances and resumes them
- `AuditService` – logs success, warnings, and failures
- `MessageSubscriptionRegistry` – tracks waiting subscriptions

`MessageSubscriptionRegistry` has no dependencies; it uses `ConcurrentDictionary` for thread-safe storage.

## Key methods

### MessageEventService

| Method | Purpose |
| --- | --- |
| `PublishMessageAsync(message)` | Publishes an external message and attempts to resume a waiting workflow instance with the same message name and correlation key. Returns `true` if a matching instance was resumed. |

### MessageSubscriptionRegistry

| Method | Purpose |
| --- | --- |
| `RegisterSubscription(correlationKey, messageName, instanceId)` | Registers a workflow instance as waiting for a specific message. |
| `UnregisterSubscription(correlationKey, messageName, instanceId)` | Unregisters a workflow instance from waiting for a specific message. Returns `true` if found and removed. |
| `GetWaitingInstances(correlationKey, messageName)` | Gets all workflow instance IDs waiting for a message with the given correlation key and message name. |
| `ClearInstanceSubscriptions(instanceId)` | Clears all subscriptions for a specific workflow instance (called when an instance completes, fails, or is canceled). |

## Message correlation

A workflow instance declares it is waiting for a message by having these context variables set (typically by a `MessageCatchEvent` activity):
- `WaitingForMessageName` – the message name it is waiting for
- `WaitingForCorrelationKey` – the correlation key to match
- `WaitingActivityId` – the ID of the message catch activity (for logging/resume)

When `PublishMessageAsync` is called, it:
1. Validates the message has non-empty `MessageName` and `CorrelationKey`
2. Publishes a `MessageReceivedEvent` with the message details
3. Queries `WorkflowExecutionService.GetInstancesByCorrelation(message.CorrelationKey)` to get all instances with that correlation key
4. Filters for instances whose status is `WaitingForMessage` and whose `WaitingForMessageName` context variable matches `message.MessageName`
5. If a match is found, calls `WorkflowExecutionService.ResumeFromMessageAsync` to resume the instance
6. On success, updates the `MessageReceivedEvent` with the instance ID and sets `WorkflowResumed = true`
7. On failure, logs the error, marks the instance as failed, and re-throws the exception
8. If no match is found, logs a warning event

## Thread safety

- `MessageEventService` is stateless aside from its dependencies; thread safety depends on the injected services.
- `MessageSubscriptionRegistry` uses `ConcurrentDictionary` for all storage, making it safe for concurrent access from multiple threads.

## Example usage

### Waiting for a message (typically from a MessageCatchEvent activity)

```csharp
// In a workflow activity handler or execution context
var subscriptionRegistry = serviceProvider.GetRequiredService<MessageSubscriptionRegistry>();
subscriptionRegistry.RegisterSubscription(
    correlationKey: "order-1042",
    messageName: "PaymentConfirmed",
    instanceId: "wf-550e8400-e29b-41d4-a716-446655440000"
);
```

### Publishing an external message

```csharp
// From an external system (API handler, webhook, etc.)
var messageEventService = serviceProvider.GetRequiredService<MessageEventService>();
var message = new WorkflowMessage
{
    CorrelationKey = "order-1042",
    MessageName = "PaymentConfirmed",
    Payload = new Dictionary<string, object?>
    {
        ["paymentId"] = "pay_789",
        ["amount"] = 99.95
    }
};

bool wasResumed = await messageEventService.PublishMessageAsync(message);
// wasResumed == true if a waiting workflow instance was found and resumed
```

### Cleaning up subscriptions

```csharp
// When an instance completes, fails, or is canceled
var subscriptionRegistry = serviceProvider.GetRequiredService<MessageSubscriptionRegistry>();
subscriptionRegistry.ClearInstanceSubscriptions("wf-550e8400-e29b-41d4-a716-446655440000");
```

## Related types

- `IWorkflowMessage` – defines the contract for workflow messages (`CorrelationKey`, `MessageName`, `Payload`)
- `WorkflowMessage` – default implementation of `IWorkflowMessage`
- `MessageReceivedEvent` – event published when a message is received (includes `InstanceId` and `WorkflowResumed` when successful)
- `WorkflowStatus.WaitingForMessage` – instance status when waiting for a message
- `WorkflowExecutionService.GetInstancesByCorrelation` – finds instances by correlation key
- `WorkflowExecutionService.ResumeFromMessageAsync` – resumes a waiting instance with a message