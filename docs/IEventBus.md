# IEventBus

The `IEventBus` interface defines a contract for an event bus used within the `dotnet-workflow-engine` to publish and subscribe to workflow-related events. It provides a mechanism for decoupling event producers and consumers, enabling asynchronous communication between workflow components such as activities, transitions, and error handlers.

## API

### `Timestamp` (property)
A `DateTime` value representing the moment when the event was created or published. This timestamp is used for logging, auditing, and tracking the temporal order of events within a workflow instance.

### `WorkflowId` (property)
A `string` identifier for the workflow definition associated with the event. This allows consumers to filter or route events based on the specific workflow they pertain to.

### `InstanceId` (property)
A `string` identifier for a specific instance of a workflow execution. This enables tracking and correlating events that belong to the same workflow execution across its lifecycle.

### `InputData` (property)
A `Dictionary<string, object>?` containing the input data payload for the event. This dictionary may be `null` if no input data is provided. The keys are strings representing data field names, and the values are arbitrary objects representing the data.

### `OutputData` (property)
A `Dictionary<string, object>?` containing the output data payload resulting from an activity or workflow execution. This dictionary may be `null` if no output data is generated. The structure mirrors that of `InputData`, with keys representing field names and values as arbitrary objects.

### `DurationMs` (property)
A `long` value representing the duration, in milliseconds, of an activity or workflow execution. This is typically used for performance monitoring and benchmarking.

### `ErrorMessage` (property)
A `string?` containing an error message if the event represents a failure or error condition. This field is `null` when the event indicates a successful operation.

### `FailedActivityId` (property)
A `string?` identifier for the specific activity that failed, if applicable. This field is `null` for non-failure events or when the failure is not tied to a specific activity.

### `ActivityId` (property)
A `string?` identifier for the activity associated with the event. This field is used to correlate events with specific steps in a workflow execution.

## Usage

### Publishing a workflow-started event
```csharp
var eventBus = serviceProvider.GetRequiredService<IEventBus>();
var workflowEvent = new WorkflowEvent
{
    Timestamp = DateTime.UtcNow,
    WorkflowId = "order-processing-workflow",
    InstanceId = "order-12345",
    InputData = new Dictionary<string, object>
    {
        { "orderId", "12345" },
        { "customerId", "cust-67890" },
        { "items", new[] { "item-a", "item-b" } }
    }
};
await eventBus.PublishAsync(workflowEvent);
```

### Handling an activity-completed event
```csharp
public class ActivityCompletionHandler : IEventHandler<ActivityCompletedEvent>
{
    public Task HandleAsync(ActivityCompletedEvent @event)
    {
        Console.WriteLine($"Activity {@event.ActivityId} completed in {@event.DurationMs}ms");
        if (@event.OutputData != null)
        {
            foreach (var kvp in @event.OutputData)
            {
                Console.WriteLine($"{kvp.Key}: {kvp.Value}");
            }
        }
        return Task.CompletedTask;
    }
}
```

## Notes
- Events published via `IEventBus` are immutable after creation; consumers should treat them as read-only to avoid unintended side effects.
- The `Timestamp` property should be set to `DateTime.UtcNow` at the time of event creation to ensure consistency across distributed systems.
- The `InputData`, `OutputData`, and other dictionary-based properties may contain arbitrary objects; consumers must validate and handle type conversions appropriately.
- Thread safety is guaranteed by the event bus implementation, but consumers should ensure their event handlers are thread-safe if they maintain shared state.
- Events with `ErrorMessage` set should be treated as exceptional conditions; consumers may choose to retry or escalate based on the `FailedActivityId` and `ErrorMessage` content.
