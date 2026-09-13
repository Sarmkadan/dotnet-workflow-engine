<full file content here>
## RateLimitingMiddlewareTests
The `RateLimitingMiddlewareTests` class provides a set of tests for the rate limiting middleware. It tests various scenarios such as requests under the limit, requests over the limit, and exempt paths. Here is an example of how to use it:
```csharp
var tests = new RateLimitingMiddlewareTests();
await tests.InvokeAsync_RequestUnderLimit_PassesThrough();
await tests.InvokeAsync_RequestOverLimit_Returns429();
```

## ActivityService

`ActivityService` (in `Services/ActivityService.cs`) executes workflow activities and manages the handlers that run them. It lives in the `DotNetWorkflowEngine.Services` namespace.

### Purpose

The service is the execution core for a single activity within a workflow. Given an `Activity` and an `ExecutionContext`, it:

- Validates the activity configuration before running it.
- Resolves the registered handler for the activity's `HandlerType` and invokes it.
- Enforces a per-attempt timeout via `Activity.TimeoutSeconds` (a value of `0` disables the timeout).
- Applies the activity's retry policy (`FixedDelay`, `ExponentialBackoff`, `LinearBackoff`, or `NoRetry`) on failure.
- Short-circuits gateway activities and activities that don't require a handler.
- Evaluates a conditional skip expression (`Activity.ConditionExpression`) before execution.

### Public API

| Member | Description |
| --- | --- |
| `ActivityService(RetryPolicyService retryPolicyService)` | Constructor. Throws `ArgumentNullException` if the retry policy service is `null`. |
| `void RegisterHandler(string handlerType, IActivityHandler handler)` | Registers a handler for a given activity type. Throws `ArgumentException` for a null/empty type and `ArgumentNullException` for a null handler. |
| `Task<ActivityResult> ExecuteAsync(Activity activity, ExecutionContext context)` | Executes the activity with its configured retry policy. Throws `ValidationException` on invalid configuration and `ActivityException` if execution fails after all retries. |
| `List<string> GetRegisteredHandlerTypes()` | Returns the handler types currently registered. |
| `bool ValidateActivity(Activity activity, out List<string> errors)` | Validates an activity without executing it. |

The nested `IActivityHandler` interface defines the contract handlers must implement:

```csharp
public interface IActivityHandler
{
    Task<Dictionary<string, object?>> ExecuteAsync(Activity activity, ExecutionContext context);
}
```

### Usage example

```csharp
using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;

// 1. Create the retry policy service and the activity service.
var retryPolicyService = new RetryPolicyService();
var activityService = new ActivityService(retryPolicyService);

// 2. Register a handler for the activity type.
activityService.RegisterHandler("send-email", new EmailHandler());

// 3. Build the activity to execute.
var activity = new Activity
{
    Id = "send-welcome-email",
    Name = "Send welcome email",
    HandlerType = "send-email",
    RetryPolicy = RetryPolicy.ExponentialBackoff,
    MaxRetries = 3,
    TimeoutSeconds = 30
};

// 4. Execute it within a workflow context.
var context = new ExecutionContext();
var result = await activityService.ExecuteAsync(activity, context);

if (result.Succeeded)
{
    Console.WriteLine("Activity completed.");
}
else
{
    Console.WriteLine($"Activity failed: {result.ErrorMessage}");
}
```

A minimal handler implementation looks like this:

```csharp
public class EmailHandler : ActivityService.IActivityHandler
{
    public Task<Dictionary<string, object?>> ExecuteAsync(Activity activity, ExecutionContext context)
    {
        // Perform the work here.
        return Task.FromResult(new Dictionary<string, object?>
        {
            ["sent"] = true
        });
    }
}
```

## AuditService

`AuditService` (in `Services/AuditService.cs`) records workflow and activity lifecycle events as audit log entries and provides querying/export of those entries. It lives in the `DotNetWorkflowEngine.Services` namespace and implements `IAuditTrailQuery`.

### Purpose

The service is the audit trail for a workflow engine. It:

- Logs lifecycle events for workflow instances (created, started, completed, failed, resumed, paused).
- Logs activity outcomes (completed, failed, retried) and arbitrary custom events.
- Buffers writes through a bounded channel with a background writer so audit logging is non-blocking and failure-isolated.
- Drops the oldest entries on overflow (`BoundedChannelFullMode.DropOldest`) and tracks the drop count via `GetDroppedEntryCount()`.
- Swallows repository failures internally so audit problems never break workflow execution.
- Persists entries through `IAuditRepository` and exposes filtered/paginated queries, CSV export, and per-instance retrieval.

### Public API

| Member | Description |
| --- | --- |
| `AuditService(IAuditRepository auditRepository)` | Constructor. Throws `ArgumentNullException` if the repository is `null`. |
| `Task LogInstanceCreated(string instanceId, string createdBy)` | Logs that a workflow instance was created. |
| `Task LogInstanceStarted(string instanceId)` | Logs that a workflow instance started executing. |
| `Task LogInstanceCompleted(string instanceId)` | Logs that a workflow instance completed successfully. |
| `Task LogInstanceFailed(string instanceId, string errorMessage)` | Logs that a workflow instance failed. |
| `Task LogInstanceResumed(string instanceId)` | Logs that a workflow instance resumed from suspension. |
| `Task LogInstancePaused(string instanceId, string? reason = null)` | Logs that a workflow instance was paused. |
| `Task LogActivityCompleted(string instanceId, string activityId, ActivityResult result)` | Logs that an activity completed, including execution time, attempts, and output keys. |
| `Task LogActivityFailed(string instanceId, string activityId, string errorMessage)` | Logs that an activity failed. |
| `Task LogActivityRetry(string instanceId, string activityId, int attemptNumber, string? reason = null)` | Logs that an activity is being retried. |
| `Task LogCustomEvent(string instanceId, string eventType, string description, string severity = "Info", string? activityId = null)` | Logs a custom event. |
| `Task<List<AuditLogEntry>> GetAuditLog(string instanceId)` | Returns all audit entries for an instance. |
| `Task<List<AuditLogEntry>> GetAuditLog(string instanceId, DateTime? since = null, string? eventType = null)` | Returns filtered audit entries for an instance. |
| `Task<List<AuditLogEntry>> GetRecentAuditLog(string instanceId, int count = 10)` | Returns the most recent entries for an instance. |
| `Task ClearAuditLog(string instanceId)` | Clears the audit log for an instance. |
| `Task<string> ExportAuditLogAsCsv(string instanceId)` | Exports an instance's audit log as a CSV string. |
| `Task<(List<AuditLogEntry> Items, int Total)> GetFilteredAuditLogsAsync(...)` | Returns filtered, paginated entries across all workflows/instances. |
| `int GetDroppedEntryCount()` | Returns the number of entries dropped due to channel overflow. |
| `ValueTask DisposeAsync()` | Stops the background writer and flushes remaining entries. |

The `IAuditTrailQuery` interface (implemented by this service) exposes `QueryAsync`, `GetEventTypesAsync`, and `GetOutcomeSummaryAsync` for read-only audit queries.

### Usage example

```csharp
using DotNetWorkflowEngine.Services;

// 1. Create the audit service backed by a repository.
var auditService = new AuditService(myAuditRepository);

// 2. Record lifecycle events.
await auditService.LogInstanceCreated(instanceId, createdBy: "system");
await auditService.LogInstanceStarted(instanceId);

// 3. Record activity outcomes.
await auditService.LogActivityCompleted(instanceId, activityId, result);
await auditService.LogActivityFailed(instanceId, activityId, "Something went wrong");

// 4. Query and export the trail.
var entries = await auditService.GetAuditLog(instanceId);
var csv = await auditService.ExportAuditLogAsCsv(instanceId);

// 5. Shut down cleanly so buffered entries are flushed.
await auditService.DisposeAsync();
```

## WorkflowDefinitionService

`WorkflowDefinitionService` (in `Services/WorkflowDefinitionService.cs`) manages workflow definitions with versioning support. It lives in the `DotNetWorkflowEngine.Services` namespace and is the component `WorkflowExecutionService` uses to look up the workflow a new instance is created from.

### Purpose

The service is the registry and authoring surface for workflow definitions. Workflow definitions are immutable once created — updates create new versions instead of mutating existing workflows, so in-flight instances always execute against the version they were created with. It:

- Creates new workflow definitions (`CreateWorkflow`) and registers already-constructed ones (`AddWorkflow`).
- Validates every workflow through `WorkflowValidator` before it is stored, throwing `ValidationException` on failure.
- Tracks all versions of a workflow and exposes the latest version, a specific version, or the full version history.
- Updates a workflow by cloning the latest version, incrementing the version, and applying an `Action<Workflow>` (`UpdateWorkflow`).
- Provides convenience mutation helpers (`AddActivity`, `AddTransition`, `SetStartActivity`, `SetEndActivity`, `PublishWorkflow`) that each create a new version.
- Supports cloning (`CloneWorkflow`), JSON export/import (`ExportWorkflowToJson`, `ImportWorkflowFromJson`), JSON validation (`ValidateWorkflowJson`), and deletion (`DeleteWorkflow`).

### Public API

| Member | Description |
| --- | --- |
| `Workflow CreateWorkflow(string id, string name, string? description = null)` | Creates a new workflow definition with version 1. Throws `ValidationException` when the workflow ID is invalid and `WorkflowException` when the ID already exists. |
| `void AddWorkflow(Workflow workflow)` | Registers an already-constructed workflow, overwriting any existing definition with the same ID. Throws `ValidationException` when the workflow is invalid. |
| `Workflow? GetWorkflow(string id)` | Gets a workflow definition by ID, returning the latest version. |
| `Workflow? GetWorkflowVersion(string id, int version)` | Gets a specific version of a workflow by ID and version number, or `null` if not found. |
| `List<Workflow> GetAllWorkflows()` | Gets all workflow definitions (latest versions only). |
| `List<Workflow> GetWorkflowVersions(string workflowId)` | Gets all versions of a workflow, ordered by version number. |
| `int GetLatestVersion(string workflowId)` | Gets the latest version number for a workflow, or `0` if not found. |
| `Workflow UpdateWorkflow(string workflowId, Action<Workflow> updateAction)` | Creates a new version by cloning the latest and applying the update. Throws `WorkflowException` when the workflow is not found and `ValidationException` when validation fails. |
| `void AddActivity(string workflowId, Activity activity)` | Adds an activity to the latest version, creating a new version. |
| `void AddTransition(string workflowId, Transition transition)` | Adds a transition between activities, creating a new version. |
| `void SetStartActivity(string workflowId, string activityId)` | Sets the start activity, creating a new version. |
| `void SetEndActivity(string workflowId, string activityId)` | Sets the end activity, creating a new version. |
| `void PublishWorkflow(string workflowId)` | Publishes the latest version to make it active, creating a new version. |
| `bool ValidateWorkflow(string workflowId, out List<string> errors)` | Validates a workflow without publishing it. |
| `List<Activity> GetActivities(string workflowId)` | Gets all activities in the latest version. |
| `Activity? GetActivity(string workflowId, string activityId)` | Gets a specific activity from the latest version. |
| `bool DeleteWorkflow(string workflowId)` | Deletes a workflow definition and all its versions. |
| `Workflow CloneWorkflow(string sourceWorkflowId, string newWorkflowId, string newName)` | Clones a workflow definition into a new workflow with a new ID and version 1. |
| `string ExportWorkflowToJson(string workflowId)` | Exports a workflow definition to a JSON string. |
| `Workflow ImportWorkflowFromJson(string workflowId, string workflowName, string jsonDefinition, bool overwriteExisting = false)` | Imports a workflow definition from JSON. |
| `bool ValidateWorkflowJson(string jsonDefinition, out List<string> errors)` | Validates a JSON workflow definition without importing it. |

### Usage example

```csharp
using DotNetWorkflowEngine.Services;

// 1. Create the definition service.
var definitionService = new WorkflowDefinitionService();

// 2. Create a workflow definition (version 1).
var workflow = definitionService.CreateWorkflow("onboarding", "Onboarding");

// 3. Add activities and transitions, each producing a new version.
definitionService.AddActivity(workflow.Id, new Activity { Id = "start", Name = "Start" });
definitionService.AddActivity(workflow.Id, new Activity { Id = "complete", Name = "Complete" });
definitionService.AddTransition(workflow.Id, new Transition
{
    Id = "t1",
    FromActivityId = "start",
    ToActivityId = "complete"
});
definitionService.SetStartActivity(workflow.Id, "start");
definitionService.SetEndActivity(workflow.Id, "complete");

// 4. Publish the latest version so it can be executed.
definitionService.PublishWorkflow(workflow.Id);

// 5. Retrieve the published definition for execution.
var published = definitionService.GetWorkflow(workflow.Id);
```

## WorkflowExecutionService

`WorkflowExecutionService` (in `Services/WorkflowExecutionService.cs`) is the core execution engine for workflows. It manages the full lifecycle of workflow instances — creation, execution, suspension, resumption, completion, and failure handling — and implements `IWorkflowInstanceQuery` for read-only access. It lives in the `DotNetWorkflowEngine.Services` namespace.

### Execution flow

A workflow instance moves through the following sequence:

1. **Create** — `CreateInstance(workflowId, correlationId, initiatedBy)` looks up the published workflow definition, verifies it is `Active`, and creates a `WorkflowInstance` in an idle state. The instance is stored in a thread-safe `ConcurrentDictionary` keyed by instance ID and an `InstanceCreated` audit event is logged. It must be explicitly started via `StartAsync`.

2. **Start** — `StartAsync(instanceId)` validates the instance is active, marks it started, and runs the workflow's start activity via `ExecuteActivityAsync`. The instance is then driven forward by following the transitions defined in the workflow graph.

3. **Execute an activity** — `ExecuteActivityAsync(instance, activityId)` resolves the activity from the workflow definition, records it as the current/active activity, and seeds an `ExecutionContext` with the instance's current context variables plus the activity's input parameters. It delegates to `ActivityService.ExecuteAsync`, which applies the activity's retry policy and timeout. On success the handler's output variables are persisted back onto the instance (both directly and via `OutputMapping`), the activity execution is recorded, and the next activities are resolved.

4. **Resolve next activities** — `ResolveNextActivities` evaluates the outgoing transitions of the completed activity against the instance's context variables. Conditional transitions are followed only when their `ConditionExpression` evaluates to true, unconditional transitions are always followed, and a default transition (`Transition.IsDefault`) is used only when nothing else matched.

5. **Branch execution** — if the activity's `ExecutionMode` is `Fork`, all resolved next activities run concurrently via `Task.WhenAll` and every branch exception is captured into a composite `AggregateException` so the join barrier never hangs. Otherwise the next activities run sequentially.

6. **Complete or fail** — when no more transitions remain the instance completes (`CompleteInstance`). Any unhandled exception marks the instance as failed (`FailInstance`), logs an `ActivityFailed`/`InstanceFailed` audit event, and propagates.

### Suspension and resumption

- **MessageCatchEvent** — when an activity of type `MessageCatchEvent` is reached, the instance is suspended (`WaitingForMessage`). The message name, correlation key, and waiting activity ID are stored in the instance context, and a `WorkflowSuspended` audit event is logged.
- **Resume from message** — `ResumeFromMessageAsync(instanceId, messageName, correlationKey, messagePayload)` validates the instance is waiting and that the incoming message name and correlation key match what was stored. It clears the waiting metadata, transitions the instance back to `Active`, injects the message payload into the execution context as `MessagePayload.*` inputs, and re-executes the waiting activity (which now acts as a no-op and continues the workflow).
- **Resume** — `ResumeInstanceAsync(instanceId)` continues execution from the instance's current activity by resolving and running its next activities.
- **Pause** — `PauseInstance(instanceId, reason)` suspends the instance and prevents further execution.

### Public API

| Member | Description |
| --- | --- |
| `WorkflowExecutionService(WorkflowDefinitionService, AuditService, ActivityService, ILogger<WorkflowExecutionService>? = null)` | Constructor. Throws `ArgumentNullException` if any required dependency is `null`. |
| `WorkflowInstance CreateInstance(string workflowId, string? correlationId = null, string? initiatedBy = null)` | Creates an idle instance from an active workflow definition. Throws `WorkflowException` if the workflow is not found or not active. |
| `Task<WorkflowInstance> StartAsync(string instanceId)` | Starts an instance and runs its start activity. |
| `Task ExecuteActivityAsync(WorkflowInstance instance, string activityId)` | Executes a single activity and follows its transitions. |
| `void CompleteInstance(string instanceId)` | Marks an instance as completed. |
| `void FailInstance(string instanceId, string errorMessage)` | Marks an instance as failed with an error message. |
| `WorkflowInstance? GetInstance(string instanceId)` | Gets a single instance by ID. |
| `List<WorkflowInstance> GetInstancesByWorkflow(string workflowId)` | Gets all instances for a workflow. |
| `List<WorkflowInstance> GetInstancesByCorrelation(string correlationId)` | Gets instances by correlation ID. |
| `List<WorkflowInstance> GetActiveInstances()` | Gets all active instances. |
| `Task ResumeInstanceAsync(string instanceId)` | Resumes a suspended instance from its current activity. |
| `Task ResumeFromMessageAsync(string instanceId, string messageName, string correlationKey, Dictionary<string, object?> messagePayload)` | Resumes a message-waiting instance, injecting the payload and re-running the waiting activity. |
| `(int Total, int Active, int Completed, int Failed) GetStatistics()` | Returns aggregate instance statistics. |
| `void CancelInstance(string instanceId, string? reason = null)` | Cancels an instance and prevents further execution. |
| `Task PauseInstance(string instanceId, string? reason = null)` | Suspends an instance and prevents further execution. |

### Usage example

```csharp
using DotNetWorkflowEngine.Services;

// 1. Create the execution service with its dependencies.
var executionService = new WorkflowExecutionService(
    definitionService, auditService, activityService);

// 2. Create and start an instance of a published workflow.
var instance = executionService.CreateInstance("onboarding", correlationId: "user-42");
await executionService.StartAsync(instance.Id);

// 3. Inspect the outcome.
var stats = executionService.GetStatistics();
Console.WriteLine($"Active: {stats.Active}, Completed: {stats.Completed}, Failed: {stats.Failed}");

// 4. Resume an instance that was suspended waiting for a message.
await executionService.ResumeFromMessageAsync(
    instance.Id, "UserApproved", "user-42", new Dictionary<string, object?>
    {
        ["ApprovedBy"] = "admin"
    });
```

## MessageEventService

`MessageEventService` (in `Services/MessageEventService.cs`) handles external messages, correlates them with waiting workflow instances, and resumes those instances. It lives in the `DotNetWorkflowEngine.Services` namespace.

### Purpose

The service processes incoming messages and attempts to correlate them with workflow instances waiting for those messages. It:

- Publishes a `MessageReceivedEvent` to the event bus for every incoming message
- Attempts to find a waiting workflow instance matching the message's correlation key and message name
- Resumes the waiting instance using `WorkflowExecutionService.ResumeFromMessageAsync` if a match is found
- Logs appropriate audit events for successful correlations, uncorrelated messages, and resume failures
- Handles error cases by logging failures and marking instances as failed when resume operations fail

### Public API

| Member | Description |
| --- | --- |
| `MessageEventService(IEventBus eventBus, WorkflowExecutionService workflowExecutionService, AuditService auditService, MessageSubscriptionRegistry subscriptionRegistry)` | Constructor. Throws `ArgumentNullException` if any dependency is null. |
| `Task<bool> PublishMessageAsync(IWorkflowMessage message)` | Publishes an external message to the workflow engine, attempting to correlate it with a waiting workflow instance and resume its execution. Returns true if a workflow was successfully correlated and resumed, false otherwise. Throws `ArgumentNullException` when message is null. Throws `ValidationException` when message name or correlation key is empty. Throws `WorkflowException` when message processing fails. |

### Usage example

```csharp
using DotNetWorkflowEngine.Services;
using DotNetWorkflowEngine.Events;

// 1. Create dependencies (typically via dependency injection)
var eventBus = new EventBus(); // or your implementation
var workflowExecutionService = new WorkflowExecutionService(definitionService, auditService, activityService);
var auditService = new AuditService(auditRepository);
var subscriptionRegistry = new MessageSubscriptionRegistry();

// 2. Create the message event service
var messageEventService = new MessageEventService(
    eventBus, 
    workflowExecutionService, 
    auditService, 
    subscriptionRegistry);

// 3. Create and publish a message
var message = new WorkflowMessage
{
    MessageName = "UserApproved",
    CorrelationKey = "user-42",
    Payload = new Dictionary<string, object?>
    {
        ["ApprovedBy"] = "admin"
    }
};

bool wasHandled = await messageEventService.PublishMessageAsync(message);
if (wasHandled)
{
    Console.WriteLine("Message was successfully correlated and workflow resumed.");
}
else
{
    Console.WriteLine("Message was received but no waiting workflow instance was found.");
}
```

### Message Handling Flow

1. **Validation** - Checks that the message and its required properties (MessageName, CorrelationKey) are not null or empty
2. **Event Publishing** - Publishes a `MessageReceivedEvent` to the event bus with the message details
3. **Correlation Attempt** - Searches for workflow instances with matching correlation key that are waiting for the specific message name
4. **Resume Operation** - If a waiting instance is found, attempts to resume it using `ResumeFromMessageAsync`
5. **Audit Logging** - Logs appropriate events for success, failure, or uncorrelated messages
6. **Error Handling** - If resume fails, logs the error, marks the instance as failed, and re-throws the exception

## ConditionalBranchingService

`ConditionalBranchingService` (in `Services/ConditionalBranchingService.cs`) evaluates conditional expressions on workflow transitions to determine which branches to activate after an activity completes. It lives in the `DotNetWorkflowEngine.Services` namespace.

### Purpose

The service implements the branching logic for workflow transitions. It:

- Evaluates conditional transitions based on expressions in the execution context
- Processes transitions in priority order (highest priority first)
- Selects all matching conditional transitions, followed by unconditional transitions
- Falls back to default transitions only when no other transitions match
- Handles expression validation and evaluation errors gracefully
- Provides detailed branching results including selected/skipped transitions and any errors

### Public API

| Member | Description |
| --- | --- |
| `ConditionalBranchingService(ILogger<ConditionalBranchingService> logger)` | Constructor. Throws `ArgumentNullException` if logger is `null`. |
| `Task<BranchingResult> ResolveBranchesAsync(Workflow workflow, string activityId, ExecutionContext context, CancellationToken cancellationToken = default)` | Resolves which transitions to follow from a completed activity based on condition expressions. Returns selected transitions, skipped transitions, and any evaluation errors. |
| `Task<List<Activity>> GetNextActivitiesAsync(Workflow workflow, string activityId, ExecutionContext context, CancellationToken cancellationToken = default)` | Returns the target activities to execute after a completed activity, honoring conditional transition expressions. |
| `List<TransitionEvaluationError> ValidateTransitionExpressions(Workflow workflow)` | Validates all conditional transition expressions in a workflow without executing them. Useful for early error detection during workflow loading. |

### Usage example

```csharp
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;

// 1. Create the conditional branching service.
var branchingService = new ConditionalBranchingService(logger);

// 2. Define a workflow with conditional transitions.
var workflow = new Workflow
{
    Id = "approval-workflow",
    Activities = new List<Activity>
    {
        new Activity { Id = "start", Name = "Start" },
        new Activity { Id = "approve", Name = "Approval" },
        new Activity { Id = "escalate", Name = "Escalate" },
        new Activity { Id = "complete", Name = "Complete" }
    },
    Transitions = new List<Transition>
    {
        // Conditional transition - only if amount > 1000
        new Transition
        {
            Id = "t1",
            FromActivityId = "start",
            ToActivityId = "approve",
            ConditionExpression = "${amount} > 1000",
            Priority = 1
        },
        // Conditional transition - only if amount <= 1000
        new Transition
        {
            Id = "t2",
            FromActivityId = "start",
            ToActivityId = "escalate",
            ConditionExpression = "${amount} <= 1000",
            Priority = 1
        },
        // Default transition (fallback)
        new Transition
        {
            Id = "t3",
            FromActivityId = "start",
            ToActivityId = "complete",
            IsDefault = true,
            Priority = 0
        }
    }
};

// 3. Create execution context with variables.
var context = new ExecutionContext
{
    Variables = new Dictionary<string, object?>
    {
        ["amount"] = 1500
    }
};

// 4. Resolve branches after the start activity completes.
var result = await branchingService.ResolveBranchesAsync(
    workflow, 
    "start", 
    context);

// 5. Check the result.
if (result.SelectedTransitions.Count > 0)
{
    var selectedTransition = result.SelectedTransitions.First();
    Console.WriteLine($"Selected transition: {selectedTransition.Id} -> {selectedTransition.ToActivityId}");
    // In this example, t1 should be selected (amount=1500 > 1000)
}
else
{
    Console.WriteLine("No transitions selected.");
}

// 6. Get the next activities to execute.
var nextActivities = await branchingService.GetNextActivitiesAsync(
    workflow,
    "start",
    context);

foreach (var activity in nextActivities)
{
    Console.WriteLine($"Next activity: {activity.Id} - {activity.Name}");
}
```

### Transition Resolution Order

When resolving branches from a completed activity, the service follows this order:

1. **Conditional transitions** (those with a `ConditionExpression`) - evaluated in descending `Priority` order; all expressions that evaluate to `true` are selected
2. **Unconditional transitions** (no expression, not marked as default) - always selected regardless of context
3. **Default transitions** (`Transition.IsDefault = true`) - selected only when no conditional or unconditional transitions were selected

If multiple default transitions exist, the one with the highest `Priority` is chosen.

## RetryPolicyService

`RetryPolicyService` (in `Services/RetryPolicyService.cs`) manages named retry policies and computes the delay to wait before each retry attempt. It lives in the `DotNetWorkflowEngine.Services` namespace and is the component `ActivityService` uses to apply an activity's retry policy on failure.

### Purpose

The service is a registry plus a delay calculator for retry behavior. It:

- Stores named policies in a dictionary keyed by `policyId` (`CreatePolicy` / `GetPolicy`).
- Computes the next retry delay for a given attempt via `CalculateRetryDelay`.
- Optionally adds bounded random jitter via `CalculateRetryDelayWithJitter`.
- Decides whether another attempt should be made via `ShouldRetry`.
- Provides factory helpers for common policy shapes (exponential backoff, fixed delay, no-retry).
- Supports simulation (`SimulateRetryDelays`), total-time estimation (`GetTotalRetryTimeMs`), and validation (`ValidatePolicy`).

### Retry policies

A policy is modeled by `RetryPolicyConfig` (in `Models/RetryPolicyConfig.cs`), whose `PolicyType` selects the delay formula:

| PolicyType | Delay formula (attempt `n`, `n > 1`) |
| --- | --- |
| `FixedDelay` | `InitialDelayMs` (constant) |
| `ExponentialBackoff` | `InitialDelayMs * BackoffMultiplier^(n - 1)` |
| `LinearBackoff` | `InitialDelayMs * n` |
| `NoRetry` | Never retries; `ShouldRetry` always returns `false` |

Key `RetryPolicyConfig` fields: `MaxAttempts`, `InitialDelayMs`, `MaxDelayMs` (cap, default 5 minutes), `BackoffMultiplier` (default `2.0`), `JitterFactor` (default `0.1`), `RetryableExceptionTypes`, and `RetryOnTimeout`.

### How CalculateRetryDelay works

`CalculateRetryDelay(policyId, attemptNumber)`:

1. Looks up the policy by ID. If no policy is registered, it returns `Constants.WorkflowConstants.DefaultRetryDelayMs` (1000 ms).
2. Otherwise it calls `RetryPolicyConfig.CalculateDelayMs(attemptNumber)`:
   - Attempt `1` (or less) returns `InitialDelayMs` immediately.
   - Attempts `> 1` apply the formula above for the policy's `PolicyType`.
   - If `JitterFactor > 0`, a symmetric ±jitter spread (`delay * JitterFactor`) is applied using `Random.Shared`, floored at 1 ms.
   - The result is capped at `MaxDelayMs`.
3. `CalculateRetryDelay` clamps the result to be non-negative (`Math.Max(0, delay)`).

`CalculateRetryDelayWithJitter(policyId, attemptNumber, jitterFactor = 0.2)` layers an additional bounded jitter on top of `CalculateRetryDelay` and clamps the final value to `[0, int.MaxValue]`. It throws `ArgumentOutOfRangeException` if `jitterFactor` is outside `[0, 1]`.

### Public API

| Member | Description |
| --- | --- |
| `void CreatePolicy(string policyId, RetryPolicyConfig config)` | Registers a policy by ID. Throws `ArgumentNullException` for a null config and `ArgumentException` for a null/empty ID. |
| `RetryPolicyConfig? GetPolicy(string policyId)` | Returns the policy for an ID, or `null` if not registered. |
| `int CalculateRetryDelay(string policyId, int attemptNumber)` | Returns the delay in ms for the given attempt. Throws `ArgumentOutOfRangeException` for a negative attempt number. |
| `int CalculateRetryDelayWithJitter(string policyId, int attemptNumber, double jitterFactor = 0.2)` | Returns the delay with bounded random jitter applied. |
| `bool ShouldRetry(string policyId, int currentAttempt, string? exceptionTypeName = null)` | Returns whether another attempt should be made, honoring `MaxAttempts` and `RetryableExceptionTypes`. |
| `RetryPolicyConfig CreateExponentialBackoffPolicy(int maxRetries = 3)` | Creates an exponential backoff policy using the workflow defaults. |
| `RetryPolicyConfig CreateFixedDelayPolicy(int maxRetries = 3, int delayMs = 1000)` | Creates a fixed-delay policy. |
| `RetryPolicyConfig CreateNoRetryPolicy()` | Creates a no-retry policy. |
| `List<int> SimulateRetryDelays(string policyId, int maxAttempts)` | Returns the delays for attempts `1..maxAttempts`. |
| `long GetTotalRetryTimeMs(string policyId)` | Returns the sum of delays for attempts `1..MaxAttempts - 1`. |
| `bool ValidatePolicy(RetryPolicyConfig config, out List<string> errors)` | Validates a policy configuration, returning a list of error messages. |
| `void RegisterRetryableException(string policyId, string exceptionTypeName)` | Adds an exception type that should trigger a retry for a policy. |
| `void ClearPolicies()` | Removes all registered policies. |

### Usage example

```csharp
using DotNetWorkflowEngine.Services;

// 1. Create the service and register an exponential backoff policy.
var retryPolicyService = new RetryPolicyService();
var policy = retryPolicyService.CreateExponentialBackoffPolicy(maxRetries: 3);
retryPolicyService.CreatePolicy("send-email", policy);

// 2. Compute the delay before each retry attempt.
int firstRetryDelay = retryPolicyService.CalculateRetryDelay("send-email", attemptNumber: 1);
int secondRetryDelay = retryPolicyService.CalculateRetryDelay("send-email", attemptNumber: 2);

// 3. Decide whether to retry after a failure.
if (retryPolicyService.ShouldRetry("send-email", currentAttempt: 1))
{
    await Task.Delay(retryPolicyService.CalculateRetryDelayWithJitter("send-email", attemptNumber: 2));
}
```
