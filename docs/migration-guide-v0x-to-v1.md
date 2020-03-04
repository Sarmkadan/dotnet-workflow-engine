# Migration Guide: v0.x XML DSL → v1.0 Fluent Builder API

This guide covers every breaking change introduced in v1.0 and provides side-by-side
examples to help you migrate workflows defined with the v0.x XML-based DSL to the v1.0
C# fluent builder API.

---

## Overview of What Changed

| Area | v0.x | v1.0 |
|------|------|------|
| Workflow definition format | XML (`*.workflow.xml`) | C# fluent builder (`WorkflowBuilder`) |
| Step identifiers | Sequential integers (`1`, `2`, `3`) | Auto-generated GUIDs or developer-supplied strings |
| Retry policy configuration | XML attributes on `<step>` | `RetryPolicyConfig` object with typed properties |
| Audit trail event names | `step.started`, `step.completed` | `ActivityStarted`, `ActivityCompleted` |
| Transition conditions | XPath expressions | C# lambda / string expressions via `ExpressionEvaluator` |
| Parallel execution | `<parallel>` XML element | `ExecutionMode.Fork` on an `Activity` |
| Service registration | Manual instantiation | `IServiceCollection` extension (`AddWorkflowEngine`) |

---

## 1. Workflow Definition

### v0.x — XML DSL

```xml
<?xml version="1.0" encoding="utf-8"?>
<workflow id="order-processing" name="Order Processing" version="1">
  <steps>
    <step id="1" name="Validate Order"   type="task" handler="ValidateOrderHandler" />
    <step id="2" name="Charge Payment"   type="task" handler="ChargePaymentHandler"
          retry-policy="exponential" max-retries="3" base-delay-ms="1000" />
    <step id="3" name="Send Confirmation" type="task" handler="SendEmailHandler" />
  </steps>
  <transitions>
    <transition from="1" to="2" />
    <transition from="2" to="3" />
  </transitions>
</workflow>
```

### v1.0 — Fluent Builder API

```csharp
var builder = new WorkflowBuilder("order-processing", "Order Processing", definitionService);

builder
    .AddTaskActivity("validate-order",    "Validate Order",    handlerType: nameof(ValidateOrderHandler))
    .AddTaskActivity("charge-payment",    "Charge Payment",    handlerType: nameof(ChargePaymentHandler))
    .AddTaskActivity("send-confirmation", "Send Confirmation", handlerType: nameof(SendEmailHandler))
    .AddTransition("validate-order",    "charge-payment")
    .AddTransition("charge-payment",    "send-confirmation")
    .WithStartActivity("validate-order")
    .WithEndActivity("send-confirmation");

var workflow = builder.Build();
```

> **Important:** v0.x step IDs were sequential integers (`1`, `2`, `3`).  
> In v1.0 they are developer-supplied strings (recommended) or auto-generated GUIDs.  
> Any external references to step IDs — audit queries, dashboards, stored correlations — must be updated.

---

## 2. Step / Activity Identifiers

v0.x used auto-incremented integers as step IDs. These appeared in:

* Audit log `ActivityId` fields
* Transition `from`/`to` attributes
* Stored workflow-instance state in persistent stores

### Migration steps

1. Assign explicit string IDs to every activity in the builder (`"validate-order"`, not `"1"`).
2. Update any audit queries that filter on `ActivityId` to use the new string IDs.
3. For existing workflow instances stored in a database, run a one-time migration script
   to rename integer IDs to their string equivalents before deploying v1.0.

```sql
-- Example: rename step IDs in an existing audit_log table
UPDATE audit_log SET activity_id = 'validate-order'   WHERE activity_id = '1';
UPDATE audit_log SET activity_id = 'charge-payment'   WHERE activity_id = '2';
UPDATE audit_log SET activity_id = 'send-confirmation' WHERE activity_id = '3';
```

---

## 3. Retry Policy Configuration

### v0.x — XML attributes

```xml
<step id="2" name="Charge Payment" type="task" handler="ChargePaymentHandler"
      retry-policy="exponential"
      max-retries="3"
      base-delay-ms="1000"
      max-delay-ms="30000" />
```

### v1.0 — `RetryPolicyConfig` object

```csharp
var retryConfig = RetryPolicyConfig.CreateExponentialBackoff(
    maxAttempts: 4,        // attempts = 1 original + 3 retries
    initialDelayMs: 1000,
    maxDelayMs: 30000,
    jitterFactor: 0.2      // ±20 % random spread to avoid thundering-herd
);

var activity = new Activity
{
    Id          = "charge-payment",
    Name        = "Charge Payment",
    HandlerType = nameof(ChargePaymentHandler),
    RetryPolicy = RetryPolicy.ExponentialBackoff,
    MaxRetries  = 3
};
```

#### Key differences

| v0.x attribute | v1.0 property | Notes |
|----------------|---------------|-------|
| `max-retries="3"` | `MaxAttempts = 4` | v1.0 counts the original attempt; set `MaxAttempts = retries + 1` |
| `base-delay-ms` | `InitialDelayMs` | Renamed |
| `max-delay-ms` | `MaxDelayMs` | Same semantics |
| *(none)* | `JitterFactor` | New in v1.0; defaults to `0.1` (10 %) |

---

## 4. Audit Trail Event Schema

Audit event names changed between versions.  If your dashboards, alerting rules, or
downstream consumers query by event type, update the field values.

| v0.x event name | v1.0 event name |
|-----------------|-----------------|
| `workflow.created` | `InstanceCreated` |
| `workflow.started` | `InstanceStarted` |
| `workflow.completed` | `InstanceCompleted` |
| `workflow.failed` | `InstanceFailed` |
| `step.started` | `ActivityStarted` |
| `step.completed` | `ActivityCompleted` |
| `step.failed` | `ActivityFailed` |
| `step.retry` | `ActivityRetry` |

The v1.0 audit log also adds `Severity`, `Actor`, `CorrelationId`, and a free-form
`Details` dictionary that replaces the flat XML attribute set.

---

## 5. Conditional Transitions

### v0.x — XPath condition

```xml
<transition from="2" to="3" condition="//context/paymentStatus = 'approved'" />
<transition from="2" to="4" condition="//context/paymentStatus = 'declined'" />
```

### v1.0 — string expression

```csharp
builder
    .AddTransition("charge-payment", "send-confirmation", condition: "paymentStatus == 'approved'")
    .AddTransition("charge-payment", "handle-decline",    condition: "paymentStatus == 'declined'");
```

Conditions are evaluated by `ExpressionEvaluator` against the workflow's `Context`
dictionary. Use dot-notation for nested keys (`order.total > 100`).

---

## 6. Parallel Execution (Fork / Join)

### v0.x — `<parallel>` element

```xml
<parallel id="5" name="Notify Parties">
  <branch>
    <step id="6" name="Email Customer" handler="EmailHandler" />
  </branch>
  <branch>
    <step id="7" name="Notify Warehouse" handler="WarehouseHandler" />
  </branch>
</parallel>
```

### v1.0 — `ExecutionMode.Fork` on a gateway activity

```csharp
builder
    .AddActivity(new Activity
    {
        Id            = "notify-fork",
        Name          = "Notify Parties",
        Type          = "Gateway",
        ExecutionMode = ExecutionMode.Fork
    })
    .AddTaskActivity("email-customer",    "Email Customer",    handlerType: nameof(EmailHandler))
    .AddTaskActivity("notify-warehouse",  "Notify Warehouse",  handlerType: nameof(WarehouseHandler))
    .AddTransition("notify-fork",        "email-customer")
    .AddTransition("notify-fork",        "notify-warehouse");
```

When all branches of a Fork gateway throw unhandled exceptions, the engine surfaces a
composite `AggregateException` instead of deadlocking — a regression that was fixed
in v1.0.

---

## 7. Message Correlation (New in v1.0)

v0.x had no built-in support for pausing a workflow until an external message arrived.
v1.0 introduces `MessageCatchEvent` — a BPMN 2.0-style intermediate catch event.

```csharp
builder
    .AddTaskActivity("place-order",  "Place Order")
    .AddMessageCatchEvent(
        id:                  "wait-payment",
        name:                "Wait for Payment Confirmation",
        messageName:         "PaymentConfirmed",
        correlationProperty: "orderId"          // key in the workflow Context
    )
    .AddTaskActivity("ship-order",   "Ship Order")
    .AddTransition("place-order",   "wait-payment")
    .AddTransition("wait-payment",  "ship-order");
```

To resume the workflow, call `IWorkflowMessageBus.PublishMessageAsync`:

```csharp
await messageBus.PublishMessageAsync(new WorkflowMessage
{
    MessageName    = "PaymentConfirmed",
    CorrelationKey = "order-abc-123",
    Payload        = new Dictionary<string, object?> { ["transactionId"] = "TX-9001" }
});
```

---

## 8. Service Registration

### v0.x — manual instantiation

```csharp
var definitionService = new WorkflowDefinitionService();
var auditService      = new AuditService(new InMemoryAuditRepository());
var executionService  = new WorkflowExecutionService(definitionService, auditService, activityService);
```

### v1.0 — dependency injection

```csharp
builder.Services.AddWorkflowEngine(options =>
{
    options.DefaultRetryPolicy = RetryPolicy.ExponentialBackoff;
    options.MaxParallelBranches = 10;
});
```

All services (`WorkflowDefinitionService`, `WorkflowExecutionService`, `AuditService`,
`MessageEventService`, etc.) are registered automatically with appropriate lifetimes.

---

## 9. Silent Behavioural Differences to Watch For

These changes are **not caught at compile time** but affect runtime behaviour:

1. **Step IDs are strings, not integers.** Code that parses `ActivityId` as `int` will
   throw at runtime.
2. **`MaxAttempts` includes the original attempt.** A policy with `MaxRetries = 3` needs
   `MaxAttempts = 4` in v1.0 or you will get one fewer retry than expected.
3. **`WorkflowStatus.Suspended` vs `WaitingForMessage`.** A workflow paused at a
   `MessageCatchEvent` reports `WaitingForMessage`, not `Suspended`. Update status checks
   accordingly.
4. **Audit timestamps are UTC.** v0.x used local time in some environments. Ensure any
   comparison logic uses UTC.
5. **Jitter is applied by default.** `RetryPolicyConfig.CreateExponentialBackoff` now
   sets `JitterFactor = 0.1`. If you need deterministic delays (e.g. in tests), pass
   `jitterFactor: 0` explicitly.

---

## 10. Step-by-Step Migration Checklist

- [ ] Replace each `*.workflow.xml` file with a `WorkflowBuilder` class.
- [ ] Assign explicit string IDs to all activities (avoid relying on GUID generation).
- [ ] Update retry configurations: rename `base-delay-ms` → `InitialDelayMs`; add
      `jitterFactor`; adjust `MaxAttempts = retries + 1`.
- [ ] Update audit queries: replace v0.x event name strings with v1.0 equivalents.
- [ ] Replace XPath conditions with string expressions understood by `ExpressionEvaluator`.
- [ ] Replace `<parallel>` blocks with `ExecutionMode.Fork` gateway activities.
- [ ] Migrate `<message-catch>` patterns (if any custom implementation existed) to the
      built-in `MessageCatchEvent` DSL node.
- [ ] Switch service wiring to `AddWorkflowEngine(...)` in `Program.cs`.
- [ ] Run a database migration script to rename integer step IDs to string IDs in stored
      audit and instance state.
- [ ] Update monitoring / alerting rules that reference v0.x event type strings.

---

## Additional Resources

- [Workflow Patterns](workflow-patterns.md) — Common patterns in the v1.0 fluent API
- [Configuration Reference](configuration.md) — All engine configuration options
- [API Reference](api-reference.md) — REST API documentation
- [Troubleshooting](troubleshooting.md) — Common issues and solutions
