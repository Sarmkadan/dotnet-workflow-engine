# Audit service

The audit subsystem records workflow and activity events as `AuditLogEntry` objects. `AuditService` creates and queries entries, while `AuditRepository` provides the default in-memory implementation of `IAuditRepository`.

The types documented here are implemented in:

- `Services/AuditService.cs`
- `Models/AuditLogEntry.cs`
- `Data/Repositories/AuditRepository.cs`

## How writes are processed

`AuditService` does not persist an entry before a logging method returns. It enqueues the entry in a bounded channel with a capacity of 1,000 and a `DropOldest` overflow policy. A single background reader calls `IAuditRepository.AddAsync` for each dequeued entry. Repository exceptions are written to `Debug` output and are not propagated to the logging caller.

This design isolates workflow execution from slow or failing audit storage, but it has important consequences:

- A completed logging task means the entry was accepted by the channel, not that it was persisted.
- Queries performed immediately after a logging call may not include the new entry yet.
- Under sustained load, the channel can discard its oldest buffered entry.
- `GetDroppedEntryCount()` counts writes rejected during closure or cancellation and other enqueue failures. The channel accepts a new write when it automatically drops an old entry, so that automatic loss is not reflected reliably by this counter.
- `DisposeAsync()` cancels the reader and completes the channel. It waits for the background task, but cancellation can stop processing before every buffered entry is persisted.

Create one long-lived service per repository and call `DisposeAsync()` when it is no longer needed.

## AuditService API

### Construction

```csharp
public AuditService(IAuditRepository auditRepository)
```

A null repository throws `ArgumentNullException`. Construction starts the background writer immediately. `AuditService` implements `IAuditTrailQuery` and exposes `DisposeAsync()`, although the class declaration does not implement `IAsyncDisposable` directly.

### Event logging

All string arguments described as required reject null, empty, or whitespace values. Null values throw `ArgumentNullException`; empty or whitespace values throw `ArgumentException`.

| Method | Entry produced |
| --- | --- |
| `LogInstanceCreated(string instanceId, string createdBy)` | `InstanceCreated`, severity `Info`, with `Actor` set to `createdBy`. |
| `LogInstanceStarted(string instanceId)` | `InstanceStarted`, severity `Info`. |
| `LogInstanceCompleted(string instanceId)` | `InstanceCompleted`, severity `Info`. |
| `LogInstanceFailed(string instanceId, string errorMessage)` | `InstanceFailed`, severity `Error`; the description contains the error message. |
| `LogInstanceResumed(string instanceId)` | `InstanceResumed`, severity `Warning`. |
| `LogInstancePaused(string instanceId, string? reason = null)` | `InstancePaused`, severity `Warning`; a nonblank reason is appended to the description. |
| `LogActivityCompleted(string instanceId, string activityId, ActivityResult result)` | `ActivityCompleted`, severity `Info`, with execution time, attempt number, and output keys in `Details`. A null result throws `ArgumentNullException`. |
| `LogActivityFailed(string instanceId, string activityId, string errorMessage)` | `ActivityFailed`, severity `Error`, with the error and a UTC timestamp in `Details`. |
| `LogActivityRetry(string instanceId, string activityId, int attemptNumber, string? reason = null)` | `ActivityRetry`, severity `Warning`, with the attempt and reason in `Details`. The attempt number must be positive. |
| `LogCustomEvent(string instanceId, string eventType, string description, string severity = "Info", string? activityId = null)` | Uses the supplied event type, description, severity, and optional activity ID. The method is virtual. |

Severity is stored as a string. The service does not restrict custom severity values to `Info`, `Warning`, `Error`, or `Critical`.

### Instance queries and maintenance

```csharp
public Task<List<AuditLogEntry>> GetAuditLog(string instanceId)

public Task<List<AuditLogEntry>> GetAuditLog(
    string instanceId,
    DateTime? since = null,
    string? eventType = null)

public Task<List<AuditLogEntry>> GetRecentAuditLog(
    string instanceId,
    int count = 10)

public Task ClearAuditLog(string instanceId)
public Task<string> ExportAuditLogAsCsv(string instanceId)
```

The unfiltered audit log is ordered by ascending timestamp. The filtered overload delegates to the repository's filtered query and is ordered by descending timestamp. Recent entries are also newest first, and `count` must be positive.

`ClearAuditLog` removes every stored entry for the instance. `ExportAuditLogAsCsv` orders entries oldest first, emits a header plus one quoted row per entry, and doubles quotes only in `Description`. It returns the literal `No audit entries` when no entries exist; that value is not a CSV document with the normal header.

### Cross-instance queries

```csharp
public Task<(List<AuditLogEntry> Items, int Total)> GetFilteredAuditLogsAsync(
    string? workflowId = null,
    string? instanceId = null,
    string? activityId = null,
    string? eventType = null,
    string? severity = null,
    DateTime? fromDate = null,
    DateTime? toDate = null,
    string? actor = null,
    int skip = 0,
    int take = 100)
```

Filters are combined with AND. String comparisons are exact and case-sensitive except `workflowId`, which matches the start of `WorkflowInstanceId`. Date bounds are inclusive. Results are newest first. The service passes `skip` and `take` through without validating them.

The `IAuditTrailQuery` methods map their terminology to the same repository filters:

| Query parameter | Repository field |
| --- | --- |
| `workflowId` | `WorkflowInstanceId` prefix |
| `instanceId` | `WorkflowInstanceId` exact match |
| `stepName` | `ActivityId` |
| `activityType` | `EventType` |
| `outcome` | `Severity` |
| `actor` | `Actor` |

`QueryAsync(...)` returns the matching page and total. `GetEventTypesAsync()` returns distinct event types in alphabetical order. `GetOutcomeSummaryAsync(fromDate, toDate)` groups by `EventType`, despite its outcome-oriented name.

## AuditLogEntry model

`AuditLogEntry` is mutable. Its parameterless constructor initializes strings and dictionaries with defaults; it does not generate an ID. The three-argument constructor validates only that its arguments are not null or empty, generates a GUID string for `Id`, and retains the default UTC timestamp.

| Property | Default and purpose |
| --- | --- |
| `Id` | Empty string; unique entry ID when assigned by a constructor or factory. |
| `WorkflowInstanceId` | Empty string; owning workflow instance. |
| `EventType` | Empty string; event category. |
| `ActivityId` | `null`; related activity, if any. |
| `Description` | Empty string; human-readable event text. |
| `Severity` | `"Info"`; string severity value. |
| `Timestamp` | `DateTime.UtcNow`; event time. |
| `Actor` | `null`; user or system responsible. |
| `PreviousState` | Empty mutable dictionary. |
| `CurrentState` | Empty mutable dictionary. |
| `Details` | Empty mutable dictionary for event metadata. |
| `CorrelationId` | `null`; optional correlation value. |

The factory methods are:

```csharp
public static AuditLogEntry CreateActivityExecution(
    string workflowInstanceId,
    string activityId,
    string status)

public static AuditLogEntry CreateStateChange(
    string workflowInstanceId,
    string previousState,
    string currentState,
    string reason)

public static AuditLogEntry CreateError(
    string workflowInstanceId,
    string? activityId,
    string errorMessage,
    string? correlationId = null)
```

Each factory generates an ID. `CreateActivityExecution` creates an `ActivityExecution` event, `CreateStateChange` creates a `StateChange` event with `Warning` severity, and `CreateError` creates an `Error` event with `Error` severity. The state-change factory includes its string states in the description; it does not populate the `PreviousState` or `CurrentState` dictionaries.

`GetFormattedTimestamp()` formats `Timestamp` with `WorkflowConstants.AuditTimestampFormat`.

### Validation

Call `Validate()` explicitly when accepting externally constructed entries. It checks:

- `WorkflowInstanceId` is present, within `MaxWorkflowInstanceIdLength`, and contains no path separators, reserved path characters, or ASCII control characters.
- `EventType` is present and within `MaxEventTypeLength`.
- `Description`, `Severity`, `Actor`, and `Id` satisfy `MaxAuditFieldLength` where applicable.
- `ActivityId` satisfies `MaxActivityIdLength` when present.

Validation throws `ArgumentException`. It does not validate severity against a fixed set, dictionary contents, correlation ID length, or timestamp kind. `AuditRepository.AddAsync` performs only its own required-field checks and does not call `Validate()`.

## AuditRepository API

`AuditRepository` keeps entries in process in a global list and a second dictionary keyed by workflow instance ID. Data is lost when the repository is discarded or the process exits. Its methods return tasks but execute synchronously, and its mutable collections are not protected for concurrent access.

### General repository operations

| Method | Behavior |
| --- | --- |
| `GetByIdAsync(string id)` | Returns the first matching entry or `null`; rejects a blank ID. |
| `GetAllAsync()` | Returns a new list in insertion order. Entry objects are not cloned. |
| `AddAsync(AuditLogEntry entity)` | Adds the same object to both indexes. Null throws `ArgumentNullException`; missing instance ID, event type, or description throws `ValidationException`. |
| `UpdateAsync(AuditLogEntry entity)` | No-op; it performs no validation or lookup. Mutating a stored object is nevertheless visible because entries are held by reference. |
| `DeleteAsync(string id)` | Removes the first matching entry from both indexes; a missing ID is a no-op. |
| `ExistsAsync(string id)` | Tests for an exact ID match; rejects a blank ID. |
| `CountAsync()` | Returns the total number of entries. |
| `GetPagedAsync(int pageNumber, int pageSize)` | Returns a newest-first page and the unpaged total. Both arguments must be positive. |

### Audit-specific operations

| Method | Behavior |
| --- | --- |
| `GetByInstanceIdAsync(string instanceId)` | Exact instance match, oldest first. |
| `GetByEventTypeAsync(string eventType)` | Exact, case-sensitive event-type match in insertion order. |
| `GetBySeverityAsync(string severity)` | Exact, case-sensitive severity match in insertion order. |
| `GetErrorsAsync()` | Equivalent to `GetBySeverityAsync("Error")`. |
| `GetByDateRangeAsync(DateTime from, DateTime to)` | Inclusive range in insertion order; rejects `from > to`. |
| `GetRecentForInstanceAsync(string instanceId, int count = 10)` | Exact instance match, newest first; count must be positive. |
| `GetByActivityIdAsync(string activityId)` | Exact activity match in insertion order. |
| `ClearInstanceAsync(string instanceId)` | Removes an instance and all of its entries from both indexes. |
| `ClearAsync()` | Removes every entry. |
| `GetFilteredAndPagedAsync(...)` | Applies the filters described above, then orders newest first and uses zero-based `skip`/`take`. |

Except where noted, required string identifiers reject null, empty, or whitespace with `ArgumentException`. `GetFilteredAndPagedAsync` does not validate negative `skip` or `take`, nor does it validate the relationship between its date bounds.

## Example

```csharp
using DotNetWorkflowEngine.Data.Repositories;
using DotNetWorkflowEngine.Services;

var repository = new AuditRepository();
var audit = new AuditService(repository);

await audit.LogInstanceCreated("order-42", "checkout-api");
await audit.LogActivityRetry("order-42", "charge-card", 2, "Gateway timeout");

// Logging is buffered. This query can run before the background writer persists
// the entries, so consumers that require read-after-write consistency need a
// repository or coordination strategy that provides it.
List<DotNetWorkflowEngine.Models.AuditLogEntry> recent =
    await audit.GetRecentAuditLog("order-42", 10);

foreach (var entry in recent)
{
    Console.WriteLine($"{entry.GetFormattedTimestamp()} {entry.EventType}");
}

await audit.DisposeAsync();
```

For production audit retention, supply an `IAuditRepository` implementation backed by durable storage and designed for the application's concurrency requirements.
