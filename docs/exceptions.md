# Exception Hierarchy

The `dotnet-workflow-engine` exposes a small hierarchy of exception types, all rooted at
[`WorkflowException`](WorkflowException.md), that carry structured, machine-readable context
(error codes, correlation IDs, entity identifiers, expected/actual versions) so callers can
handle failures programmatically rather than by string-matching messages.

## Hierarchy

```
System.Exception
└── WorkflowException
    ├── ActivityException
    ├── ConfigurationException
    ├── StateException
    │   └── WorkflowStatusTransitionException   (in DotNetWorkflowEngine.Models)
    ├── ValidationException
    └── WorkflowConcurrencyException
```

All exception types live in the `DotNetWorkflowEngine.Exceptions` namespace, with the single
exception of `WorkflowStatusTransitionException`, which lives in `DotNetWorkflowEngine.Models`
but derives from `StateException`.

## Base type: `WorkflowException`

The base class for every workflow-engine error. It adds two properties on top of `Exception`:

| Property | Type | Purpose |
| --- | --- | --- |
| `ErrorCode` | `string?` | Machine-readable code categorizing the failure (e.g. `"ACTIVITY_ERROR"`). `null` when not provided. |
| `CorrelationId` | `string?` | Identifier for tracing the exception back to a workflow instance or execution context. `null` when not provided. |

Constructors:

- `WorkflowException(string message)`
- `WorkflowException(string message, string errorCode)`
- `WorkflowException(string message, Exception innerException)`
- `WorkflowException(string message, string errorCode, string correlationId, Exception? innerException = null)`

### Extension methods

`WorkflowExceptionExtensions` provides helpers for working with `WorkflowException`:

- `WithCorrelationId(this WorkflowException, string correlationId)` — returns a new
  `WorkflowException` preserving the message, error code and inner exception but replacing the
  correlation ID.
- `ToDictionary(this WorkflowException)` — returns a dictionary of message, error code,
  correlation ID, stack trace and (when present) inner-exception details, useful for logging
  and telemetry.
- `IsCritical(this WorkflowException)` — returns `true` when the error code starts with
  `"CRIT"` (case-insensitive).

## `ActivityException`

Thrown when an activity fails during execution. Extends `WorkflowException` with the fixed
error code `"ACTIVITY_ERROR"`.

| Property | Type | Purpose |
| --- | --- | --- |
| `ActivityId` | `string` | ID of the activity that caused the exception. |
| `AttemptNumber` | `int` | Attempt number at which the failure occurred (defaults to `1`). |

Constructors:

- `ActivityException(string message, string activityId)` — sets `AttemptNumber` to `1`.
- `ActivityException(string message, string activityId, int attemptNumber, Exception? innerException = null)`
- `ActivityException(string message, string activityId, int attemptNumber, string correlationId, Exception? innerException = null)`

## `ConfigurationException`

Thrown when configuration-related errors occur. Extends `WorkflowException` with the default
error code `"CONFIGURATION_ERROR"`.

| Property | Type | Purpose |
| --- | --- | --- |
| `ConfigurationKey` | `string?` | Configuration key that caused the exception. |
| `ConfigurationValue` | `string?` | Configuration value that caused the exception. |

Constructors:

- `ConfigurationException(string message)`
- `ConfigurationException(string message, string configurationKey)`
- `ConfigurationException(string message, string configurationKey, string configurationValue)`
- `ConfigurationException(string message, Exception innerException)`
- `ConfigurationException(string message, string errorCode, string configurationKey, string? configurationValue = null, Exception? innerException = null)` — allows overriding the error code.

## `StateException`

Thrown when an invalid state transition is attempted. Extends `WorkflowException` with the
fixed error code `"STATE_TRANSITION_ERROR"`.

| Property | Type | Purpose |
| --- | --- | --- |
| `CurrentState` | `string` | State the entity was in when the invalid transition was attempted. |
| `RequestedState` | `string` | State that was requested but could not be transitioned to. |
| `EntityId` | `string?` | Identifier of the entity involved in the failed transition; `null` when not applicable. |

Constructors:

- `StateException(string message, string currentState, string requestedState)`
- `StateException(string message, string currentState, string requestedState, string entityId)`

Method:

- `GetTransitionDetails()` — returns a human-readable string such as
  `"Cannot transition from {CurrentState} to {RequestedState} (Entity: {EntityId})"`, omitting
  the entity portion when `EntityId` is empty.

### `WorkflowStatusTransitionException`

A specialized `StateException` in `DotNetWorkflowEngine.Models` thrown when a workflow status
transition violates the explicit state-machine rules. It adds typed status information:

| Property | Type | Purpose |
| --- | --- | --- |
| `CurrentStatus` | `WorkflowStatus` | Current workflow status. |
| `RequestedStatus` | `WorkflowStatus` | Requested workflow status that was rejected. |

Constructor:

- `WorkflowStatusTransitionException(WorkflowStatus currentStatus, WorkflowStatus requestedStatus, string? instanceId = null)`

### `StateExceptionValidation`

A static helper class providing validation for `StateException` instances:

- `Validate(this StateException)` — returns a read-only list of human-readable problems;
  empty when valid.
- `IsValid(this StateException)` — `true` when validation finds no problems.
- `EnsureValid(this StateException)` — throws `ArgumentException` listing all problems when
  the instance is invalid.

## `ValidationException`

Thrown when workflow or activity validation fails. Extends `WorkflowException` with the fixed
error code `"VALIDATION_ERROR"`.

| Property | Type | Purpose |
| --- | --- | --- |
| `ValidationErrors` | `IReadOnlyList<string>` | List of validation errors. |
| `EntityName` | `string?` | Entity that failed validation; `null` when not applicable. |

Constructors:

- `ValidationException(string message)` — initializes an empty `ValidationErrors` list.
- `ValidationException(string message, IEnumerable<string> errors, string? entityName = null)`
- `ValidationException(string message, string error, string? entityName = null)`

Method:

- `GetDetailedMessage()` — returns the message with all validation errors joined by `"; "`
  when any exist; otherwise returns the plain message.

## `WorkflowConcurrencyException`

Thrown when an optimistic concurrency check on a persisted entity's `Version` fails — the
caller's expected version no longer matches the stored version, indicating the record was
modified by another process between the caller's read and write. Extends `WorkflowException`
with the fixed error code `"CONCURRENCY_CONFLICT"`.

| Property | Type | Purpose |
| --- | --- | --- |
| `EntityId` | `string` | Identifier of the entity that failed the concurrency check. |
| `ExpectedVersion` | `int` | Version the caller expected to overwrite. |
| `ActualVersion` | `int` | Version actually stored at the time of the check. |

Constructor:

- `WorkflowConcurrencyException(string entityId, int expectedVersion, int actualVersion)` —
  throws `ArgumentException` when `entityId` is null or empty.

## Error codes at a glance

| Exception | Error code |
| --- | --- |
| `ActivityException` | `ACTIVITY_ERROR` |
| `ConfigurationException` | `CONFIGURATION_ERROR` (overridable) |
| `StateException` | `STATE_TRANSITION_ERROR` |
| `ValidationException` | `VALIDATION_ERROR` |
| `WorkflowConcurrencyException` | `CONCURRENCY_CONFLICT` |

## Usage

Catch the base type to handle any workflow-engine failure uniformly, then branch on the
concrete type or `ErrorCode` for specific handling:

```csharp
try
{
    await workflow.ExecuteAsync(instanceId);
}
catch (WorkflowConcurrencyException ex)
{
    // Reload the entity and retry with the fresh version.
    var fresh = await repository.GetAsync(ex.EntityId);
    await workflow.ExecuteAsync(fresh);
}
catch (ValidationException ex)
{
    logger.LogWarning("Validation failed: {Errors}", ex.GetDetailedMessage());
}
catch (WorkflowException ex)
{
    logger.LogError("Workflow error {Code} ({CorrelationId}): {Message}",
        ex.ErrorCode, ex.CorrelationId, ex.Message);
}
```