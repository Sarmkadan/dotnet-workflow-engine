# Workflow execution service

`WorkflowExecutionService` is the in-process coordinator for workflow instances. It creates instances from active workflow definitions, invokes activities through `ActivityService`, copies activity context and mapped outputs back to the instance, follows workflow transitions, records audit events, and exposes lifecycle and query operations.

The service keeps instances in a private `ConcurrentDictionary`. Its state is therefore scoped to the lifetime of the service and is not loaded from `WorkflowInstanceRepository`. Register it as a singleton, as `AddWorkflowEngine` does, when instances must be shared by callers in the same process. The dictionary makes lookup and enumeration safe, while the mutable fields of one `WorkflowInstance` are only partially synchronized; callers should avoid executing or changing the same instance concurrently except through the service's fork handling.

## Dependencies

The constructor requires:

- `WorkflowDefinitionService` to resolve workflow definitions.
- `AuditService` to record instance and activity events.
- `ActivityService` to execute registered activity handlers and apply retry policies.
- An optional `ILogger<WorkflowExecutionService>`; a null logger is used when none is supplied.

All constructor dependencies except the logger are required and produce `ArgumentNullException` when null.

## Key methods

| Method | Purpose |
| --- | --- |
| `CreateInstance(workflowId, correlationId, initiatedBy)` | Resolves an active definition, creates and stores a `Draft` instance, and writes an instance-created audit event. A missing correlation ID defaults to the generated instance ID. |
| `StartAsync(instanceId)` | Validates the instance, calls `WorkflowInstance.Start()`, audits the start, and executes the definition's start activity. See the start-state caveat below. |
| `ExecuteActivityAsync(instance, activityId)` | Executes one activity, propagates context variables and mapped outputs, records completion, selects outgoing transitions, and recursively executes their target activities. Any unhandled error is audited, changes the instance to `Suspended`, and is rethrown. |
| `CompleteInstance(instanceId)` | Transitions the instance to terminal `Archived` state and records completion. Execution does not call this automatically when an activity has no outgoing transition. |
| `FailInstance(instanceId, errorMessage)` | Stores the error, transitions the instance to `Suspended`, and records failure. In this model, failure is resumable rather than terminal. |
| `PauseInstance(instanceId, reason)` | Transitions a nonterminal instance to `Suspended` and records the pause. |
| `CancelInstance(instanceId, reason)` | Transitions a nonterminal instance to terminal `Cancelled` and writes a custom audit event. |
| `ResumeInstanceAsync(instanceId)` | Audits a resume and executes activities directly following `CurrentActivityId`. It does not itself transition `Suspended` to `Active`. |
| `ResumeFromMessageAsync(instanceId, messageName, correlationKey, payload)` | Validates a waiting instance and its stored message metadata, changes it to `Active`, audits receipt, and re-enters the waiting activity. See message events below. |
| `GetInstance(instanceId)` | Returns the stored instance, or `null` when the ID is unknown. |
| `GetInstancesByWorkflow(workflowId)` | Returns all in-memory instances for a definition ID. |
| `GetInstancesByCorrelation(correlationId)` | Returns all in-memory instances with an exact correlation-ID match. |
| `GetActiveInstances()` | Returns instances whose status is exactly `Active`; suspended and message-waiting instances are excluded. |
| `GetStatistics()` | Returns `(Total, Active, Completed, Failed)`. Completed means `Archived` with no error; failed means any instance with an error message, regardless of status. |

String identifiers and failure messages must be nonempty and non-whitespace. Mutation methods throw `WorkflowException` for unknown instances or definitions, and invalid lifecycle changes are rejected by `WorkflowStatusTransitionException` or `StateException` depending on the entry point.

## Activity progression

For a normal activity, `ExecuteActivityAsync`:

1. Sets `CurrentActivityId` and adds the activity to `ActiveActivities`.
2. Seeds a new execution context from the instance context and the activity's input parameters.
3. Invokes `ActivityService.ExecuteAsync`.
4. Copies handler-created variables and configured output mappings into the instance context.
5. Records the activity as executed and writes its audit event.
6. Evaluates outgoing conditional transitions against the updated instance context. Unconditional, non-default transitions are also selected. If none is selected, the highest-priority default transition is used.
7. Executes selected targets sequentially, or concurrently when the current activity uses `ExecutionMode.Fork`.

Fork branch failures are collected into an `AggregateException`; the instance is failed and the aggregate is rethrown. A join is handled by `ActivityService`, but this class does not maintain a separate durable join state.

Encountering a `MessageCatchEvent` stores `WaitingForMessageName`, `WaitingForCorrelationKey`, and `WaitingActivityId` in the instance context, transitions the instance to `WaitingForMessage`, audits the suspension, and returns without invoking an activity handler. The current implementation of `ResumeFromMessageAsync` puts the payload into a temporary execution context that is not passed onward, clears the waiting values, and then calls `ExecuteActivityAsync` for the same catch event; that method treats it as a catch again. Consumers should account for this current behavior rather than assuming the payload was persisted or the next activity has run when the resume task returns.

## State transitions

`WorkflowInstance` enforces the following transition graph:

```text
Draft ------------> Active
Active -----------> WaitingForMessage | Suspended | Archived | Cancelled
WaitingForMessage -> Active | Suspended | Cancelled
Suspended --------> Active | WaitingForMessage | Archived | Cancelled
Archived ---------> (terminal)
Cancelled --------> (terminal)
```

The service uses those states as follows:

- `CreateInstance` produces `Draft`.
- `CompleteInstance` produces `Archived` and records completion time.
- `FailInstance`, activity exceptions, and `PauseInstance` produce `Suspended`.
- A message catch produces `WaitingForMessage`; a matching message changes it to `Active` before re-entering the catch activity.
- `CancelInstance` produces `Cancelled` and records completion time.

There is an important implementation constraint around `StartAsync`: it first requires `instance.IsActive()`, then calls `instance.Start()`, which attempts an `Active -> Active` transition. A newly created `Draft` instance fails the precondition, while an `Active` instance fails the state-machine transition. Until that implementation is changed, `StartAsync` cannot successfully start an instance through the public lifecycle as written.

## Example usage

The following example uses the service's current behavior. It creates an active definition with a handler-free event, creates an instance, explicitly moves it through the state model, executes the activity, and completes it. Direct activity execution does not enforce an active-state precondition.

```csharp
using DotNetWorkflowEngine.Configuration;
using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using Microsoft.Extensions.DependencyInjection;

var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
services.AddWorkflowEngine("Data Source=workflow.db");

await using var provider = services.BuildServiceProvider();
var definitions = provider.GetRequiredService<WorkflowDefinitionService>();
var execution = provider.GetRequiredService<WorkflowExecutionService>();

definitions.AddWorkflow(new Workflow
{
    Id = "order-processing",
    Name = "Order processing",
    Status = WorkflowStatus.Active,
    StartActivityId = "received",
    EndActivityId = "received",
    Activities =
    {
        new Activity
        {
            Id = "received",
            Name = "Order received",
            Type = "Event"
        }
    }
});

var instance = execution.CreateInstance(
    "order-processing",
    correlationId: "order-1042",
    initiatedBy: "orders-api");

instance.TransitionTo(WorkflowStatus.Active);
await execution.ExecuteActivityAsync(instance, "received");
execution.CompleteInstance(instance.Id);

var completed = execution.GetInstance(instance.Id);
Console.WriteLine($"{completed?.Id}: {completed?.Status}"); // Archived
```

For handler-backed tasks, register an `ActivityService.IActivityHandler` under the activity's `HandlerType` before executing the instance. Use `SetContextVariable` before execution to supply workflow data used by handlers, transition conditions, or message correlation properties.
