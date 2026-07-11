# AuditService

Central service for recording and querying audit events within the workflow engine. It persists lifecycle events (instance creation, start, completion, failure), activity outcomes, retries, custom events, and exposes query capabilities to retrieve, filter, summarize, and export audit data.

## API

### `AuditService`

Initializes a new audit service with the required storage abstraction.

| | |
|---|---|
| **Parameters** | `IAuditStore store` – persistence layer used to write and read audit entries. |

---

### `LogInstanceCreated`

Persists an event indicating that a workflow instance has been created.

| | |
|---|---|
| **Parameters** | `string instanceId`, `string workflowName`, `string? correlationId = null`, `Dictionary<string, object>? customData = null` |
| **Returns** | `Task` – completes when the entry is written. |
| **Throws** | `ArgumentNullException` if `instanceId` or `workflowName` is null. |
| **Remarks** | Custom data is stored as JSON and may be used for downstream reporting. |

---

### `LogInstanceStarted`

Persists an event indicating that a workflow instance has started execution.

| | |
|---|---|
| **Parameters** | `string instanceId`, `string workflowName`, `string? correlationId = null`, `Dictionary<string, object>? customData = null` |
| **Returns** | `Task` – completes when the entry is written. |
| **Throws** | `ArgumentNullException` if `instanceId` or `workflowName` is null. |

---
### `LogInstanceCompleted`

Persists an event indicating that a workflow instance completed successfully.

| | |
|---|---|
| **Parameters** | `string instanceId`, `string workflowName`, `string? correlationId = null`, `Dictionary<string, object>? customData = null` |
| **Returns** | `Task` – completes when the entry is written. |
| **Throws** | `ArgumentNullException` if `instanceId` or `workflowName` is null. |

---
### `LogInstanceFailed`

Persists an event indicating that a workflow instance terminated with a failure.

| | |
|---|---|
| **Parameters** | `string instanceId`, `string workflowName`, `string failureReason`, `string? correlationId = null`, `Dictionary<string, object>? customData = null` |
| **Returns** | `Task` – completes when the entry is written. |
| **Throws** | `ArgumentNullException` if `instanceId`, `workflowName`, or `failureReason` is null. |

---
### `LogInstanceResumed`

Persists an event indicating that a previously suspended workflow instance has resumed execution.

| | |
|---|---|
| **Parameters** | `string instanceId`, `string workflowName`, `string? correlationId = null`, `Dictionary<string, object>? customData = null` |
| **Returns** | `Task` – completes when the entry is written. |
| **Throws** | `ArgumentNullException` if `instanceId` or `workflowName` is null. |

---
### `LogActivityCompleted`

Persists an event indicating that an activity within a workflow completed successfully.

| | |
|---|---|
| **Parameters** | `string instanceId`, `string activityName`, `string? correlationId = null`, `Dictionary<string, object>? customData = null` |
| **Returns** | `Task` – completes when the entry is written. |
| **Throws** | `ArgumentNullException` if `instanceId` or `activityName` is null. |

---
### `LogActivityFailed`

Persists an event indicating that an activity within a workflow terminated with a failure.

| | |
|---|---|
| **Parameters** | `string instanceId`, `string activityName`, `string failureReason`, `string? correlationId = null`, `Dictionary<string, object>? customData = null` |
| **Returns** | `Task` – completes when the entry is written. |
| **Throws** | `ArgumentNullException` if `instanceId`, `activityName`, or `failureReason` is null. |

---
### `LogActivityRetry`

Persists an event indicating that an activity is being retried after a transient failure.

| | |
|---|---|
| **Parameters** | `string instanceId`, `string activityName`, `int retryCount`, `string? correlationId = null`, `Dictionary<string, object>? customData = null` |
| **Returns** | `Task` – completes when the entry is written. |
| **Throws** | `ArgumentNullException` if `instanceId` or `activityName` is null. |
| **Remarks** | `retryCount` must be ≥ 0. |

---
### `LogCustomEvent`

Persists a user-defined audit event with arbitrary payload.

| | |
|---|---|
| **Parameters** | `string instanceId`, `string eventType`, `Dictionary<string, object>? customData = null`, `string? correlationId = null` |
| **Returns** | `Task` – completes when the entry is written. |
| **Throws** | `ArgumentNullException` if `instanceId` or `eventType` is null. |

---
### `GetAuditLog`

Retrieves all audit entries for a given workflow instance.

| | |
|---|---|
| **Parameters** | `string instanceId` |
| **Returns** | `Task<List<AuditLogEntry>>` – ordered chronologically ascending. |
| **Throws** | `ArgumentNullException` if `instanceId` is null. |

---
### `GetRecentAuditLog`

Retrieves the most recent audit entries across all instances, limited by count.

| | |
|---|---|
| **Parameters** | `int maxCount` |
| **Returns** | `Task<List<AuditLogEntry>>` – ordered chronologically descending. |
| **Throws** | `ArgumentOutOfRangeException` if `maxCount` ≤ 0. |

---
### `ClearAuditLog`

Removes all audit entries from storage.

| | |
|---|---|
| **Parameters** | – |
| **Returns** | `Task` – completes when deletion is finished. |
| **Throws** | – |

---
### `ExportAuditLogAsCsv`

Exports the entire audit trail as a CSV-formatted byte array.

| | |
|---|---|
| **Parameters** | – |
| **Returns** | `Task<string>` – CSV content encoded as UTF-8. |
| **Throws** | – |

---
### `GetFilteredAuditLogsAsync`

Retrieves audit entries matching the supplied filter criteria with pagination.

| | |
|---|---|
| **Parameters** | `string? instanceId = null`, `string? activityName = null`, `string? eventType = null`, `DateTime? from = null`, `DateTime? to = null`, `int page = 1`, `int pageSize = 100` |
| **Returns** | `Task<(List<AuditLogEntry> Items, int Total)>` – total count of matching entries across all pages. |
| **Throws** | `ArgumentOutOfRangeException` if `page` < 1 or `pageSize` < 1. |

---
### `QueryAsync`

Retrieves audit entries using a free-form query string interpreted by the underlying store.

| | |
|---|---|
| **Parameters** | `string query`, `int page = 1`, `int pageSize = 100` |
| **Returns** | `Task<(List<AuditLogEntry> Items, int Total)>` – total count of matching entries across all pages. |
| **Throws** | `ArgumentNullException` if `query` is null; `ArgumentOutOfRangeException` if `page` < 1 or `pageSize` < 1. |

---
### `GetEventTypesAsync`

Returns the set of distinct event types present in the audit store.

| | |
|---|---|
| **Parameters** | – |
| **Returns** | `Task<IReadOnlyList<string>>` – alphabetically sorted list. |
| **Throws** | – |

---
### `GetOutcomeSummaryAsync`

Returns a summary of workflow outcomes (completed, failed, etc.) grouped by outcome type.

| | |
|---|---|
| **Parameters** | – |
| **Returns** | `Task<IReadOnlyDictionary<string, int>>` – keys are outcome names, values are counts. |
| **Throws** | – |

## Usage
