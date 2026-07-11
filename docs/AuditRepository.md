# AuditRepository

The `AuditRepository` class provides an asynchronous data access layer for managing audit logs within the workflow engine. It encapsulates all persistence operations related to `AuditLogEntry` entities, supporting full CRUD capabilities, specialized querying by workflow instance, event type, severity, and activity, as well as pagination and filtering mechanisms. This repository ensures that audit trails for workflow executions are stored, retrieved, and maintained efficiently, enabling robust tracking, debugging, and compliance reporting.

## API

### GetByIdAsync
Retrieves a single audit log entry by its unique identifier.
- **Parameters**: `id` (The unique identifier of the audit entry).
- **Returns**: `Task<AuditLogEntry?>` containing the entry if found, or `null` otherwise.
- **Throws**: Throws an exception if the underlying data store is unavailable or the query fails.

### GetAllAsync
Retrieves all audit log entries stored in the repository.
- **Parameters**: None.
- **Returns**: `Task<List<AuditLogEntry>>` containing the complete list of entries.
- **Throws**: Throws an exception if the data retrieval operation fails.

### AddAsync
Persists a new audit log entry to the storage.
- **Parameters**: `entry` (The `AuditLogEntry` instance to add).
- **Returns**: `Task` that completes when the operation finishes.
- **Throws**: Throws an exception if the entry is invalid, duplicates a unique constraint, or the write operation fails.

### UpdateAsync
Updates an existing audit log entry in the storage.
- **Parameters**: `entry` (The modified `AuditLogEntry` instance).
- **Returns**: `Task` that completes when the update is committed.
- **Throws**: Throws an exception if the entry does not exist or the update operation fails.

### DeleteAsync
Removes a specific audit log entry from the storage.
- **Parameters**: `entry` or `id` (The identifier or instance to delete).
- **Returns**: `Task` that completes when the deletion is committed.
- **Throws**: Throws an exception if the deletion operation fails.

### ExistsAsync
Checks whether an audit log entry with a specific identifier exists.
- **Parameters**: `id` (The unique identifier to check).
- **Returns**: `Task<bool>` indicating existence (`true` if found, `false` otherwise).
- **Throws**: Throws an exception if the check cannot be performed due to storage errors.

### CountAsync
Returns the total number of audit log entries currently stored.
- **Parameters**: None.
- **Returns**: `Task<int>` representing the total count.
- **Throws**: Throws an exception if the count operation fails.

### GetPagedAsync
Retrieves a specific page of audit log entries.
- **Parameters**: `pageNumber`, `pageSize` (Pagination parameters).
- **Returns**: `Task<(List<AuditLogEntry> Items, int Total)>` containing the list of items for the current page and the total count of all items.
- **Throws**: Throws an exception if pagination parameters are invalid or the query fails.

### GetByInstanceIdAsync
Retrieves all audit log entries associated with a specific workflow instance.
- **Parameters**: `instanceId` (The unique identifier of the workflow instance).
- **Returns**: `Task<List<AuditLogEntry>>` containing matching entries.
- **Throws**: Throws an exception if the query fails.

### GetByEventTypeAsync
Retrieves audit log entries filtered by a specific event type.
- **Parameters**: `eventType` (The type of event to filter by).
- **Returns**: `Task<List<AuditLogEntry>>` containing matching entries.
- **Throws**: Throws an exception if the query fails.

### GetBySeverityAsync
Retrieves audit log entries filtered by a specific severity level.
- **Parameters**: `severity` (The severity level to filter by).
- **Returns**: `Task<List<AuditLogEntry>>` containing matching entries.
- **Throws**: Throws an exception if the query fails.

### GetErrorsAsync
Retrieves all audit log entries marked as errors.
- **Parameters**: None.
- **Returns**: `Task<List<AuditLogEntry>>` containing entries with error severity.
- **Throws**: Throws an exception if the query fails.

### GetByDateRangeAsync
Retrieves audit log entries occurring within a specified time window.
- **Parameters**: `startDate`, `endDate` (The inclusive time range).
- **Returns**: `Task<List<AuditLogEntry>>` containing matching entries.
- **Throws**: Throws an exception if the date range is invalid or the query fails.

### GetRecentForInstanceAsync
Retrieves the most recent audit log entries for a specific workflow instance.
- **Parameters**: `instanceId`, `count` (The instance ID and the number of recent entries to retrieve).
- **Returns**: `Task<List<AuditLogEntry>>` containing the latest entries sorted by timestamp.
- **Throws**: Throws an exception if the query fails.

### GetByActivityIdAsync
Retrieves audit log entries associated with a specific activity within a workflow.
- **Parameters**: `activityId` (The unique identifier of the activity).
- **Returns**: `Task<List<AuditLogEntry>>` containing matching entries.
- **Throws**: Throws an exception if the query fails.

### ClearInstanceAsync
Removes all audit log entries associated with a specific workflow instance.
- **Parameters**: `instanceId` (The unique identifier of the workflow instance).
- **Returns**: `Task` that completes when the deletion is committed.
- **Throws**: Throws an exception if the bulk deletion operation fails.

### ClearAsync
Removes all audit log entries from the repository.
- **Parameters**: None.
- **Returns**: `Task` that completes when the repository is emptied.
- **Throws**: Throws an exception if the bulk deletion operation fails.

### GetFilteredAndPagedAsync
Retrieves a page of audit log entries based on complex filtering criteria.
- **Parameters**: `filter` (Object containing filter properties), `pageNumber`, `pageSize`.
- **Returns**: `Task<(List<AuditLogEntry> Items, int Total)>` containing the filtered page items and the total count matching the filter.
- **Throws**: Throws an exception if the filter is invalid or the query fails.

## Usage

### Example 1: Retrieving Error Logs for a Specific Instance
This example demonstrates how to fetch recent error-level audit entries for a specific workflow instance to diagnose a failure.

```csharp
public async Task DiagnoseInstanceFailure(IAuditRepository repository, string instanceId)
{
    // Retrieve the most recent 50 entries for the instance
    var recentLogs = await repository.GetRecentForInstanceAsync(instanceId, 50);
    
    // Filter locally for errors if specific severity filtering isn't combined in the call
    var errors = recentLogs.Where(x => x.Severity == AuditSeverity.Error).ToList();

    if (errors.Any())
    {
        Console.WriteLine($"Found {errors.Count} errors for instance {instanceId}");
        foreach (var error in errors)
        {
            Console.WriteLine($"[{error.Timestamp}] {error.Message}");
        }
    }
    else
    {
        Console.WriteLine("No recent errors found for this instance.");
    }
}
```

### Example 2: Paginated Audit Report with Total Count
This example shows how to generate a paginated view of all audit logs for an administrative dashboard, including the total record count for UI pagination controls.

```csharp
public async Task<AuditPageDto> GetAuditPage(IAuditRepository repository, int page, int pageSize)
{
    // Fetch the specific page and total count in a single optimized call
    var result = await repository.GetPagedAsync(page, pageSize);
    
    return new AuditPageDto
    {
        CurrentPage = page,
        PageSize = pageSize,
        TotalRecords = result.Total,
        TotalPages = (int)Math.Ceiling((double)result.Total / pageSize),
        Items = result.Items.Select(x => new AuditLogSummaryDto 
        { 
            Id = x.Id, 
            EventType = x.EventType, 
            Timestamp = x.Timestamp 
        }).ToList()
    };
}
```

## Notes

- **Thread Safety**: As all public methods return `Task` objects and rely on asynchronous I/O, the `AuditRepository` is designed to be thread-safe for concurrent read operations. However, concurrent write operations (e.g., `AddAsync`, `UpdateAsync`, `DeleteAsync`) targeting the same entity or performing bulk clears (`ClearAsync`) may depend on the underlying database's transaction isolation levels to prevent race conditions.
- **Null Handling**: `GetByIdAsync` explicitly returns `null` if an entity is not found, rather than throwing an exception. Callers must handle nullable return values appropriately.
- **Bulk Operations**: Methods like `ClearAsync` and `ClearInstanceAsync` perform bulk deletions. On large datasets, these operations may be resource-intensive and could potentially time out depending on the configured command timeout of the underlying data provider.
- **Pagination Consistency**: The `Total` count returned by `GetPagedAsync` and `GetFilteredAndPagedAsync` represents the state of the data at the moment the query was executed. In highly concurrent environments where entries are frequently added or deleted, the total count may change between page requests.
- **Empty Results**: Query methods returning lists (e.g., `GetByEventTypeAsync`, `GetErrorsAsync`) will return an empty `List<AuditLogEntry>` rather than `null` if no matches are found, simplifying consumer logic by eliminating null checks for collections.
