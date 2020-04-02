# Workflow Patterns

This document describes common workflow patterns supported by the engine and how to implement them.

## Sequential Workflow

The simplest pattern - activities execute one after another in a linear chain.

```
[Start] -> [Validate] -> [Process] -> [Notify] -> [End]
```

```csharp
var workflow = new Workflow { Id = "seq-1", Name = "Sequential Order" };

workflow.Activities.Add(new Activity { Id = "validate", Name = "Validate Input" });
workflow.Activities.Add(new Activity { Id = "process", Name = "Process Order" });
workflow.Activities.Add(new Activity { Id = "notify", Name = "Send Notification" });

workflow.Transitions.Add(new Transition { FromActivityId = "validate", ToActivityId = "process" });
workflow.Transitions.Add(new Transition { FromActivityId = "process", ToActivityId = "notify" });

workflow.StartActivityId = "validate";
workflow.EndActivityId = "notify";
```

## Conditional Branching

Use `ConditionExpression` on activities and the `ConditionalBranchingService` to route execution based on runtime data.

```
                    ┌─[Approve]─┐
[Review] -> [Gate] ─┤           ├─> [Complete]
                    └─[Reject]──┘
```

```csharp
var gate = new Activity
{
    Id = "gate",
    Name = "Decision Gate",
    ExecutionMode = ExecutionMode.Fork
};

var approve = new Activity
{
    Id = "approve",
    Name = "Approve",
    ConditionExpression = "context.Score >= 80"
};

var reject = new Activity
{
    Id = "reject",
    Name = "Reject",
    ConditionExpression = "context.Score < 80"
};
```

## Parallel Execution (Fork/Join)

Split work across multiple branches and synchronize at a join point.

```
              ┌─[Task A]─┐
[Fork] ───────┤          ├───> [Join] -> [Aggregate]
              └─[Task B]─┘
```

```csharp
var fork = new Activity
{
    Id = "fork",
    Name = "Split Work",
    ExecutionMode = ExecutionMode.Fork
};

var join = new Activity
{
    Id = "join",
    Name = "Synchronize",
    ExecutionMode = ExecutionMode.Join
};
```

## Retry with Backoff

Configure activities to retry on transient failures using the built-in retry policies.

```csharp
var activity = new Activity
{
    Id = "call-api",
    Name = "External API Call",
    RetryPolicy = RetryPolicy.ExponentialBackoff,
    MaxRetries = 5,
    TimeoutSeconds = 30
};
```

Available retry policies:
- `RetryPolicy.NoRetry` - fail immediately on error
- `RetryPolicy.FixedDelay` - retry after a constant delay
- `RetryPolicy.ExponentialBackoff` - retry with increasing delays (recommended for network calls)
- `RetryPolicy.LinearBackoff` - retry with linearly increasing delays

## Human-in-the-Loop

Create a workflow that pauses for human input by suspending the instance at a specific activity.

```csharp
// 1. Start the workflow - it will reach the approval activity
var instance = executionService.CreateInstance("approval-workflow");
await executionService.StartAsync(instance.Id);

// 2. Instance suspends at the manual approval step
// ... time passes, human reviews ...

// 3. Set the decision and resume
instance.SetContextVariable("approved", true);
await executionService.ResumeInstanceAsync(instance.Id);
```

## Correlation-Based Grouping

Use correlation IDs to track related workflow instances across different workflow definitions.

```csharp
var orderId = "ORD-12345";

var paymentInstance = executionService.CreateInstance(
    "payment-workflow", correlationId: orderId);
var shippingInstance = executionService.CreateInstance(
    "shipping-workflow", correlationId: orderId);

// Query all instances related to this order
var related = executionService.GetInstancesByCorrelation(orderId);
```
