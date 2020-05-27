# WorkflowInstanceRepository

The `WorkflowInstanceRepository` provides asynchronous persistence operations for `WorkflowInstance` entities, enabling retrieval, creation, update, deletion, and querying of workflow execution records in a storage-backed repository.

## API

### `Task<WorkflowInstance?> GetByIdAsync(Guid id)`

Retrieves a single workflow instance by its unique identifier. Returns `null` if no matching instance exists. Throws `ArgumentNullException` if `id` is `Guid.Empty`.

### `Task<List<WorkflowInstance>> GetAllAsync()`

Returns all workflow instances stored in the repository. The result is never `null`; an empty list is returned if no instances exist.

### `Task AddAsync(WorkflowInstance instance)`

Persists a new workflow instance. Throws `ArgumentNullException` if `instance` is `null` or if `instance.Id` is `Guid.Empty`. Throws `InvalidOperationException` if an instance with the same `Id` already exists.

### `Task UpdateAsync(WorkflowInstance instance)`

Updates an existing workflow instance. Throws `ArgumentNullException` if `instance` is `null`. Throws `InvalidOperationException` if `instance.Id` is `Guid.Empty` or if no instance with the matching `Id` exists.

### `Task DeleteAsync(Guid id)`

Removes the workflow instance with the specified identifier. Does nothing if no instance with the given `id` exists. Throws `ArgumentNullException` if `id` is `Guid.Empty`.

### `Task<bool> ExistsAsync(Guid id)`

Checks whether a workflow instance with the specified identifier exists. Returns `false` if no matching instance exists. Throws `ArgumentNullException` if `id` is `Guid.Empty`.

### `Task<int> CountAsync()`

Returns the total number of workflow instances stored in the repository.

### `Task<(List<WorkflowInstance> Items, int Total)> GetPagedAsync(int skip, int take)`

Retrieves a page of workflow instances with pagination support. The `skip` parameter specifies the number of items to skip; `take` specifies the maximum number of items to return. Returns a tuple containing the page items and the total count of all instances. Throws `ArgumentOutOfRangeException` if `skip` or `take` is negative.

### `Task<List<WorkflowInstance>> GetByWorkflowIdAsync(Guid workflowId)`

Retrieves all workflow instances associated with a specific workflow definition identifier. Returns an empty list if no instances exist for the given `workflowId`.

### `Task<List<WorkflowInstance>> GetByStatusAsync(WorkflowStatus status)`

Retrieves all workflow instances matching the specified execution status. Returns an empty list if no instances match the given `status`.

### `Task<List<WorkflowInstance>> GetActiveInstancesAsync()`

Retrieves all workflow instances currently in an active state (e.g., running, suspended). Returns an empty list if no active instances exist.

### `Task<List<WorkflowInstance>> GetByCorrelationIdAsync(string correlationId)`

Retrieves all workflow instances associated with a specific correlation identifier. Returns an empty list if no instances match the given `correlationId`.

### `Task<List<WorkflowInstance>> GetByDateRangeAsync(DateTime fromUtc, DateTime toUtc)`

Retrieves all workflow instances whose creation or modification timestamps fall within the specified UTC date range (inclusive). Returns an empty list if no instances fall within the range. Throws `ArgumentOutOfRangeException` if `fromUtc` is later than `toUtc`.

### `Task<List<WorkflowInstance>> GetFailedInstancesAsync()`

Retrieves all workflow instances that have failed execution. Returns an empty list if no failed instances exist.

### `Task<(int Total, int Active, int Completed, int Failed)> GetStatisticsAsync()`

Returns aggregate statistics about stored workflow instances, including the total count, number of active instances, completed instances, and failed instances.

### `Task ClearAsync()`

Removes all workflow instances from the repository. This operation is irreversible.

## Usage

```csharp
// Example 1: Basic CRUD operations
var repo = new WorkflowInstanceRepository(storageProvider);

var instance = new WorkflowInstance
{
    Id = Guid.NewGuid(),
    WorkflowId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    Status = WorkflowStatus.Running,
    CorrelationId = "order-123",
    CreatedAt = DateTime.UtcNow
};

await repo.AddAsync(instance);

var retrieved = await repo.GetByIdAsync(instance.Id);
Console.WriteLine($"Retrieved: {retrieved?.Id}");

await repo.UpdateAsync(new WorkflowInstance
{
    Id = instance.Id,
    Status = WorkflowStatus.Completed
});

await repo.DeleteAsync(instance.Id);
```

```csharp
// Example 2: Querying and aggregation
var repo = new WorkflowInstanceRepository(storageProvider);

var active = await repo.GetActiveInstancesAsync();
Console.WriteLine($"Active instances: {active.Count}");

var stats = await repo.GetStatisticsAsync();
Console.WriteLine($"Total: {stats.Total}, Active: {stats.Active}, Completed: {stats.Completed}, Failed: {stats.Failed}");

var paged = await repo.GetPagedAsync(skip: 0, take: 10);
Console.WriteLine($"Page 1 of {paged.Total} items: {paged.Items.Count}");
```

## Notes

- All methods are thread-safe and may be called concurrently from multiple threads.
- Pagination parameters (`skip`, `take`) must be non-negative; negative values will throw `ArgumentOutOfRangeException`.
- Date range queries use inclusive bounds; ensure `fromUtc` ≤ `toUtc` to avoid exceptions.
- The `ClearAsync` method is destructive and should be used with caution in production environments.
- Identity checks (`ExistsAsync`, `GetByIdAsync`, `DeleteAsync`) treat `Guid.Empty` as invalid input and throw `ArgumentNullException`.
- Update operations require the target instance to exist; otherwise, `InvalidOperationException` is thrown.
