# Architecture

This document describes how dotnet-workflow-engine actually works, module by module,
and records the design decisions behind it. Everything here is grounded in the current
code - if a section disagrees with the source, the source wins and this file needs a fix.

## Overview

The engine is a single library project (`DotNetWorkflowEngine.csproj`, `net10.0`) with a
console demo entry point in `Program.cs`. A workflow is a directed graph:

- **`Workflow`** (Models/Workflow.cs) - the definition. String `Id`, `Version`, a list of
  `Activity` nodes and `Transition` edges, a `StartActivityId`, and a `WorkflowStatus`
  lifecycle (`Draft` -> `Active` via `Publish()`). `Validate()` checks that every transition
  endpoint references an existing activity before publishing is allowed.
- **`Activity`** - a node. Carries a `Type` (e.g. `"Task"`, `"MessageCatchEvent"`), an
  optional `HandlerType` binding it to user code, retry settings, input parameters,
  output mapping, an optional `ConditionExpression`, and an `ExecutionMode`
  (`Sequential` or `Fork` for parallel splits).
- **`Transition`** - an edge. Optional `ConditionExpression`, `IsDefault` flag and
  `Priority` for routing decisions.
- **`WorkflowInstance`** - a running (or finished) execution of a definition. Holds the
  mutable `Context` variable bag, status, executed/active activity lists, timing and
  error info.

## Module breakdown

| Directory | What lives there |
|---|---|
| `Models/` | `Workflow`, `Activity`, `Transition`, `WorkflowInstance`, `ExecutionContext`, `ActivityResult`, `AuditLogEntry`, `RetryPolicyConfig` |
| `Services/` | The engine proper: `WorkflowDefinitionService`, `WorkflowExecutionService`, `ActivityService`, `AuditService`, `RetryPolicyService`, `ConditionalBranchingService`, `MessageEventService` |
| `Utilities/` | `WorkflowBuilder` (fluent construction), `WorkflowValidator`, `ExpressionEvaluator`, serialization/reflection helpers |
| `Data/` | `DatabaseContext` facade over `WorkflowRepository`, `WorkflowInstanceRepository`, `AuditRepository` (all in-memory, see limitations) |
| `Events/` | `IEventBus`/`EventBus` pub-sub and the workflow event types (`WorkflowStartedEvent`, message events, ...) |
| `Configuration/` | `AddWorkflowEngine` DI extensions, `DotnetWorkflowEngineOptions` + FluentValidation validator |
| `Exceptions/` | Typed exception hierarchy: `WorkflowException` (with error codes), `StateException`, `ActivityException`, `ValidationException`, `ConfigurationException` |
| `Caching/`, `Monitoring/`, `Formatters/`, `Filters/` | Supporting pieces: cache service + no-op fallback, metrics, JSON/CSV output formatters |
| `tests/`, `examples/`, `dotnet-workflow-engine.Benchmarks/` | Excluded from the library compile via `<Compile Remove>` in the csproj |

Note: `Controllers/**`, the middleware pipeline files, `Cli/WorkflowCommand.cs`,
`Integration/HttpClientFactory.cs` and `Configuration/DependencyInjection.cs` are also
**excluded from compilation** in the csproj. They document an intended ASP.NET Core
hosting layer but are not part of the built assembly today. Don't design against them.

## Execution flow

1. **Define**: build a `Workflow` by hand, via `WorkflowDefinitionService.CreateWorkflow`,
   or with `WorkflowBuilder` (fluent `AddTaskActivity` / `AddTransition` /
   `WithStartActivity`; `CreateSerial` wires a linear chain in one call).
   `Build()` validates, `BuildAndRegister()` also stores it in the definition service.
2. **Publish**: `WorkflowDefinitionService.PublishWorkflow` flips status to `Active`.
   Instances can only be created from `Active` definitions.
3. **Instantiate**: `WorkflowExecutionService.CreateInstance(workflowId, correlationId,
   initiatedBy)` - the instance is stored in a `ConcurrentDictionary` keyed by instance id.
4. **Run**: `StartAsync` executes the start activity; `ExecuteActivityAsync` then walks
   the graph *recursively*: execute activity -> copy handler-set variables and
   `OutputMapping` values back onto the instance context -> resolve outgoing transitions ->
   recurse into targets.
5. **Route**: `ResolveNextActivities` (private, in `WorkflowExecutionService`) evaluates
   each outgoing transition's `ConditionExpression` against the instance context via
   `ExpressionEvaluator`. Rules: conditional transitions fire only when true,
   unconditional ones always fire, and an `IsDefault` transition (highest `Priority`
   wins) fires only when nothing else matched.
6. **Fork**: if the completed activity has `ExecutionMode.Fork`, all next activities run
   concurrently with `Task.WhenAll`. Branch exceptions are collected into an
   `AggregateException` rather than lost, so a failed branch surfaces as a composite
   fault instead of a hung join.
7. **Finish**: when there are no outgoing transitions the recursion unwinds. Failures
   mark the instance failed, log to audit, and rethrow.

### Activity execution (ActivityService)

`ActivityService.ExecuteAsync` owns the retry loop. Per attempt it:

- short-circuits gateways (`activity.IsGateway()`) - they succeed with empty output;
- skips the activity if its own `ConditionExpression` evaluates false (`SetSkipped`);
- looks up the registered `IActivityHandler` by `activity.HandlerType` and awaits it;
- on exception, consults `RetryPolicyConfig.ShouldRetry` and waits
  `CalculateDelayMs(attempt)` (fixed delay or exponential backoff, capped) before retrying;
  exhausted retries throw `ActivityException` with attempt count and correlation id.

**User code plugs in via `ActivityService.IActivityHandler`** - one method,
`ExecuteAsync(Activity, ExecutionContext) -> Dictionary<string, object?>`. Handlers are
registered per handler-type string with `RegisterHandler`. This is the primary
extension point of the whole engine.

### Long-running workflows: message correlation

`MessageCatchEvent` activities implement BPMN-style intermediate catch events:

- When execution reaches one, the instance is suspended (`WorkflowStatus.WaitingForMessage`)
  and the expected message name + correlation key (read from a context variable named by
  `CorrelationProperty`) are stashed *in the instance context itself*
  (`WaitingForMessageName` / `WaitingForCorrelationKey` / `WaitingActivityId`).
- `ResumeFromMessageAsync(instanceId, messageName, correlationKey, payload)` validates
  the match, injects the payload as activity inputs, and re-executes the waiting
  activity to continue the graph. Mismatches are audited and rejected with
  `MESSAGE_CORRELATION_MISMATCH`.
- `MessageEventService` sits on top and uses `IEventBus` to route published message
  events to waiting instances by correlation.

Storing the wait-state in the ordinary context bag is deliberate: it keeps
`WorkflowInstance` serializable as-is with no side tables, at the cost of reserving
three variable names.

## Key design decisions

**In-memory state everywhere.** Definitions live in a `Dictionary` inside
`WorkflowDefinitionService`; instances in a `ConcurrentDictionary` inside
`WorkflowExecutionService`; audit entries in `AuditRepository`'s lists.
`DatabaseContext.InitializeAsync`/`SaveChangesAsync` are intentional no-ops. Trade-off:
zero infrastructure to try the engine and dead-simple tests, but no durability - a
process restart loses everything. The repository layer (`IRepository<T>`,
`IAuditRepository`) exists precisely so a SQL-backed implementation can be swapped in
without touching the services.

**Recursive, in-process execution.** `ExecuteActivityAsync` recurses through the graph
on the caller's task rather than scheduling steps on a queue. This makes the happy path
easy to reason about and debug (one stack = one workflow run), but means very deep
workflows consume stack, and a crashed process cannot resume mid-run. The options class
caps `MaxWorkflowDepth` for this reason.

**Retry lives in ActivityService, not in handlers.** Handlers stay trivially simple;
retry policy is declared on the `Activity` (`RetryPolicy` enum + `MaxRetries`) and
enforced centrally with the delays computed by `RetryPolicyConfig`. Consequence:
handlers must be idempotent, because the engine will happily re-invoke them.

**String-based expression language.** `ExpressionEvaluator` handles literals,
`${variable}` references, comparisons and boolean combinators - it is a small
hand-rolled evaluator, not Roslyn scripting. Rationale: no dynamic compilation, no
sandboxing concerns, negligible startup cost. Trade-off: limited expressiveness; complex
routing belongs in a handler that sets a context variable, with a cheap expression on
the transition.

**Typed exceptions with machine-readable codes.** Every failure path throws a subclass
of `WorkflowException` carrying an error code string (`WORKFLOW_NOT_FOUND`,
`MESSAGE_CORRELATION_MISMATCH`, ...). Callers can branch on codes without parsing
messages; `StateException` additionally carries current vs expected state.

**Error isolation in the event bus.** `EventBus.PublishAsync` catches per-subscriber
exceptions so one bad handler cannot starve the others - correct default for a
fire-and-forget notification channel; subscribers who care about failures must handle
their own.

**Singleton services via `AddWorkflowEngine`.** All engine services are registered as
singletons because they *are* the state store (see in-memory decision above). Scoped
registration would silently give each scope its own universe of workflows.

## Extension points

- `ActivityService.IActivityHandler` - your business logic per activity type.
- `IEventBus` - replace `EventBus` with a distributed bus for cross-process messaging.
- `IAuditRepository` / `IRepository<T>` - swap the in-memory persistence for a real store.
- `IAuditTrailQuery` - read-side abstraction over the audit trail (implemented by `AuditService`).
- `IOutputFormatter` (`Formatters/`) - JSON and CSV exist; add your own for exports.
- `RetryPolicyConfig` factory methods - `CreateNoRetry`, `CreateFixedDelay`,
  `CreateExponentialBackoff` cover the built-in policies.

## Known limitations

- **No durability.** All state is process-local memory. `System.Data.SqlClient` and the
  Redis caching package are referenced in the csproj but nothing uses them yet.
- **No true join barrier.** `Fork` splits execute branches in parallel, but a converging
  activity is executed once per incoming branch rather than waiting for all of them;
  gateways succeed as no-ops in `ActivityService.HandleGateway`.
- **Sync-over-async spots.** `CreateInstance`, `CompleteInstance` and `FailInstance` call
  `_auditService....GetAwaiter().GetResult()` because their public signatures are
  synchronous. Safe in console/service contexts, a deadlock hazard if wired into a
  synchronization-context-bound host.
- **`GetStatistics` counts by convention.** "Completed" means `Archived` with no error
  message; anything with an `ErrorMessage` counts as failed regardless of status.
- **Excluded web layer.** Controllers, middleware and the CLI command are reference-only
  (see module table); there is no hosted HTTP API in the compiled assembly.
- **Options are only partially enforced.** `DotnetWorkflowEngineOptions` exposes many
  flags (circuit breaker, Prometheus, rate limiting) that the current code does not act
  on; the FluentValidation validator checks their ranges but the engine ignores most of
  them. Treat them as roadmap, not behavior.
