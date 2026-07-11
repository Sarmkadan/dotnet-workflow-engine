# WorkflowRepository

The `WorkflowRepository` provides asynchronous access to persisted `Workflow` entities, encapsulating all CRUD operations and common query patterns required by the workflow engine. It abstracts the underlying data store, allowing callers to work with workflows without concern for storage details.

## API

### GetByIdAsync
```csharp
Task<Workflow?> GetByIdAsync(Guid id);
```
Retrieves a single workflow by its unique identifier.  
- **Parameters**  
  - `id`: The `Guid` of the workflow to fetch.  
- **Return value**  
  - A `Task` that completes with the `Workflow` instance if found, or `null` when no matching record exists.  
- **Exceptions**  
  - `ArgumentException` if `id` is `Guid.Empty`.  

### GetAllAsync
```csharp
Task<List<Workflow>> GetAllAsync();
```
Returns every workflow stored in the repository.  
- **Parameters**  
  - None.  
- **Return value**  
  - A `Task` that completes with a list containing all `Workflow` objects. The list may be empty but never `null`.  
- **Exceptions**  
  - None defined; propagates any storage‑layer exceptions.

### AddAsync
```csharp
Task AddAsync(Workflow workflow);
```
Inserts a new workflow into the repository.  
- **Parameters**  
  - `workflow`: The workflow to add. Must not be `null` and must have a valid identifier (typically `Guid.Empty` or a value that the store can generate).  
- **Return value**  
  - A `Task` that completes when the insert operation finishes.  
- **Exceptions**  
  - `ArgumentNullException` if `workflow` is `null`.  
  - `InvalidOperationException` if the workflow already exists (based on its identifier).  

### UpdateAsync
```csharp
Task UpdateAsync(Workflow workflow);
```
Updates an existing workflow with the supplied instance.  
- **Parameters**  
  - `workflow`: The workflow containing updated values. Must not be `null` and must represent an existing record.  
- **Return value**  
  - A `Task` that completes when the update operation finishes.  
- **Exceptions**  
  - `ArgumentNullException` if `workflow` is `null`.  
  - `KeyNotFoundException` if no workflow with the given identifier exists.  

### DeleteAsync
```csharp
Task DeleteAsync(Guid id);
```
Removes the workflow identified by `id`.  
- **Parameters**  
  - `id`: The `Guid` of the workflow to delete.  
- **Return value**  
  - A `Task` that completes when the delete operation finishes.  
- **Exceptions**  
  - `ArgumentException` if `id` is `Guid.Empty`.  
  - `KeyNotFoundException` if no workflow with the specified identifier exists.  

### ExistsAsync
```csharp
Task<bool> ExistsAsync(Guid id);
```
Checks whether a workflow with the given identifier is present.  
- **Parameters**  
  - `id`: The `Guid` to test.  
- **Return value**  
  - A `Task` that completes with `true` if a matching workflow exists, otherwise `false`.  
- **Exceptions**  
  - `ArgumentException` if `id` is `Guid.Empty`.  

### CountAsync
```csharp
Task<int> CountAsync();
```
Returns the total number of workflows stored.  
- **Parameters**  
  - None.  
- **Return value**  
  - A `Task` that completes with the count of workflows as an `int`.  
- **Exceptions**  
  - None defined; propagates any storage‑layer exceptions.  

### GetPagedAsync
```csharp
Task<(List<Workflow> Items, int Total)> GetPagedAsync(int page, int pageSize, string? searchTerm = null);
```
Retrieves a subset of workflows with pagination support and optional filtering by name.  
- **Parameters**  
  - `page`: The 1‑based page number to fetch (must be >= 1).  
  - `pageSize`: The maximum number of items per page (must be >= 1).  
  - `searchTerm`: Optional string to filter workflows whose `Name` contains the term (case‑insensitive). Pass `null` for no filtering.  
- **Return value**  
  - A `Task` that completes with a tuple: `Items` is the list of workflows for the requested page, and `Total` is the total count of workflows matching the filter (ignoring pagination).  
- **Exceptions**  
  - `ArgumentOutOfRangeException` if `page` or `pageSize` is less than 1.  

### GetByStatusAsync
```csharp
Task<List<Workflow>> GetByStatusAsync(WorkflowStatus status);
```
Returns all workflows that currently have the specified status.  
- **Parameters**  
  - `status`: The `WorkflowStatus` enum value to filter by.  
- **Return value**  
  - A `Task` that completes with a list of workflows matching the status; the list may be empty but never `null`.  
- **Exceptions**  
  - `ArgumentException` if `status` is not a defined `WorkflowStatus` value.  

### GetActiveWorkflowsAsync
```csharp
Task<List<Workflow>> GetActiveWorkflowsAsync();
```
Returns workflows considered active (typically those with a status of `Running` or `Pending`).  
- **Parameters**  
  - None.  
- **Return value**  
  - A `Task` that completes with a list of active workflows; the list may be empty but never `null`.  
- **Exceptions**  
  - None defined; propagates any storage‑layer exceptions.  

### SearchByNameAsync
```csharp
Task<List<Workflow>> SearchByNameAsync(string name);
```
Finds workflows whose `Name` property matches the supplied string (exact match, case‑insensitive).  
- **Parameters**  
  - `name`: The name to search for; must not be `null` or whitespace.  
- **Return value**  
  - A `Task` that completes with a list of matching workflows; the list may be empty but never `null`.  
- **Exceptions**  
  - `ArgumentNullException` if `name` is `null`.  
  - `ArgumentException` if `name` consists only of whitespace.  

### GetCreatedSinceAsync
```csharp
Task<List<Workflow>> GetCreatedSinceAsync(DateTimeOffset since);
```
Returns workflows created on or after the specified point in time.  
- **Parameters**  
  - `since`: The lower bound `DateTimeOffset` for the creation timestamp.  
- **Return value**  
  - A `Task` that completes with a list of workflows meeting the date criterion; the list may be empty but never `null`.  
- **Exceptions**  
  - `ArgumentOutOfRangeException` if `since` is later than `DateTimeOffset.UtcNow`.  

### GetWithActivityCountAsync
```csharp
Task<List<(Workflow Workflow, int ActivityCount)>> GetWithActivityCountAsync();
```
Retrieves each workflow together with the number of activities it defines.  
- **Parameters**  
  - None.  
- **Return value**  
  - A `Task` that completes with a list of tuples. Each tuple contains the `Workflow` instance and an `int` indicating how many child activities are associated with that workflow. The list may be empty but never `null`.  
- **Exceptions**  
  - None defined; propagates any storage‑layer exceptions.  

### ClearAsync
```csharp
Task ClearAsync();
```
Removes all workflows from the repository.  
- **Parameters**  
  - None.  
- **Return value**  
  - A `Task` that completes when the clear operation finishes.  
- **Exceptions**  
  - None defined; propagates any storage‑layer exceptions.  

## Usage

### Example 1: Basic CRUD workflow
```csharp
var repo = new WorkflowRepository(); // assuming a parameter‑less constructor or DI

// Create
var newWf = new Workflow { Name = "Order Processing", Status = WorkflowStatus.Draft };
await repo.AddAsync(newWf);

// Read
var fetched = await repo.GetByIdAsync(newWf.Id);
if (fetched == null) throw new InvalidOperationException("Workflow not found after insert.");

// Update
fetched.Status = WorkflowStatus.Running;
await repo.UpdateAsync(fetched);

// Delete
await repo.DeleteAsync(fetched.Id);
```

### Example 2: Paged search with status filter
```csharp
var repo = new WorkflowRepository();

// Get the first 20 active workflows, sorted implicitly by the repository
var (page, pageSize) = (1, 20);
var result = await repo.GetPagedAsync(page, pageSize);
var activeWorkflows = result.Items.Where(w => w.Status == WorkflowStatus.Running).ToList();

Console.WriteLine($"Page {page} contains {activeWorkflows.Count} active workflows (total {result.Total}).");
```

## Notes

- All methods are asynchronous and return `Task` or `Task<T>`. Callers should `await` them to avoid blocking threads.  
- The repository does **not** enforce any locking; concurrent calls to mutating methods (`AddAsync`, `UpdateAsync`, `DeleteAsync`, `ClearAsync`) may result in race conditions depending on the underlying storage implementation. Consumers requiring serialized access should coordinate externally (e.g., with a `SemaphoreSlim` or by using a transactional scope).  
- Read‑only methods (`GetByIdAsync`, `GetAllAsync`, `GetByStatusAsync`, etc.) are safe to invoke concurrently; they return snapshot data that reflects the state of the store at the moment the operation completes.  
- Methods that accept identifiers (`GetByIdAsync`, `ExistsAsync`, `DeleteAsync`) treat `Guid.Empty` as invalid and will throw an `ArgumentException`.  
- `GetPagedAsync` uses 1‑based page numbering; supplying a page number less than 1 or a page size less than 1 results in an `ArgumentOutOfRangeException`. The total count returned reflects the filtered set before pagination is applied.  
- `ClearAsync` irreversibly removes all persisted workflows; there is no undo operation. Use with caution, typically only in test or maintenance scenarios.  
- The repository does not automatically validate business rules beyond basic argument checks; it is the caller’s responsibility to ensure that a `Workflow` instance is in a valid state before persisting it.  
- Implementations may throw storage‑specific exceptions (e.g., `DbUpdateException`, `TimeoutException`) which are not caught or wrapped by the repository; callers should handle those as appropriate for their data access technology.
