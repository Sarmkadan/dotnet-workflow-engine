# IWorkflowMessage

A marker interface used by the workflow engine to represent messages that can trigger or resume workflow instances. It carries the minimal information required to correlate an incoming message with an existing workflow instance and to route the message to the correct handler.

## API

### `string CorrelationKey`
Gets or sets the correlation key used to match the message with a workflow instance. The correlation key must uniquely identify the workflow instance that should process this message.

### `string MessageName`
Gets or sets the name of the message, which determines the workflow activity or handler that will process it. This value is used by the engine to route the message to the appropriate handler within the workflow definition.

### `Dictionary<string, object?> Payload`
Gets or sets the payload data carried by the message. The payload is a dictionary of named values that are passed to the workflow activity or handler. The values can be of any type, including `null`.

### `DateTime Timestamp`
Gets or sets the time at which the message was created or sent. This value is used by the engine for logging, auditing, and timing purposes.

### `string? InstanceId`
Gets or sets the optional identifier of the workflow instance that should process this message. If this value is set, it takes precedence over the `CorrelationKey` for instance resolution.

### `bool WorkflowResumed`
Gets or sets a value indicating whether the workflow should resume execution after processing this message. If `true`, the workflow engine will continue executing the workflow from the point where it was paused. If `false` or unset, the workflow will remain in a paused state after processing. Defaults to `false`.

## Usage

```csharp
// Example 1: Sending a message to resume a workflow
var message = new WorkflowMessage
{
    CorrelationKey = "order-12345",
    MessageName = "ProcessPayment",
    Payload = new Dictionary<string, object?>
    {
        ["PaymentId"] = Guid.NewGuid(),
        ["Amount"] = 99.99m,
        ["Currency"] = "USD"
    },
    Timestamp = DateTime.UtcNow,
    InstanceId = "workflow-789",
    WorkflowResumed = true
};

workflowEngine.SendMessage(message);
```

```csharp
// Example 2: Handling a message in a workflow activity
public class ProcessPaymentActivity : IWorkflowActivity
{
    public async Task Execute(IWorkflowContext context, IWorkflowMessage message)
    {
        if (message.Payload.TryGetValue("Amount", out var amountObj) && amountObj is decimal amount)
        {
            // Process payment logic
        }

        if (message.WorkflowResumed)
        {
            await context.ResumeWorkflowAsync();
        }
    }
}
```

## Notes

- The `CorrelationKey` and `InstanceId` properties are mutually exclusive in practice; setting both may lead to undefined behavior depending on the engine’s implementation.
- The `Payload` dictionary may contain `null` values, which should be handled gracefully by consumers.
- The `Timestamp` is expected to be in UTC; no timezone conversion is performed by the engine.
- The `WorkflowResumed` flag is advisory; the engine may override it based on workflow state or configuration.
- This interface is not thread-safe by design; concurrent modifications to its properties are unsupported and may lead to race conditions.
