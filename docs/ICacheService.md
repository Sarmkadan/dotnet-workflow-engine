# ICacheService

The `ICacheService` interface defines the contract for caching operations within the `dotnet-workflow-engine`, providing a unified abstraction for both in-memory and distributed caching strategies. It supports asynchronous retrieval, storage, and removal of typed objects, along with existence checks and lazy-loading patterns via `GetOrLoadAsync`. Implementations such as `MemoryCacheService` and `DistributedCacheService` adhere to this interface to ensure consistent cache interaction across different deployment environments while maintaining type safety and asynchronous execution flows.

## API

### `GetAsync<T>`
Retrieves a cached item of type `T` associated with a specific key.
- **Parameters**: Accepts a string key identifying the cache entry.
- **Return Value**: Returns a `Task<T?>` which resolves to the cached value if found, or `null` if the key does not exist or the value has expired.
- **Exceptions**: May throw exceptions if the underlying cache provider is unavailable or if serialization/deserialization fails in distributed implementations.

### `SetAsync<T>`
Stores an item of type `T` in the cache under a specified key.
- **Parameters**: Accepts a string key, the value of type `T`, and optionally a expiration configuration (depending on concrete implementation details).
- **Return Value**: Returns a `Task` that completes when the value is successfully written to the cache.
- **Exceptions**: Throws if the cache service is disconnected or if the value cannot be serialized.

### `RemoveAsync`
Deletes a specific item from the cache.
- **Parameters**: Accepts a string key corresponding to the item to remove.
- **Return Value**: Returns a `Task` that completes when the removal operation is finished.
- **Exceptions**: Generally does not throw if the key does not exist, but may propagate errors from the underlying provider if the connection is lost.

### `ExistsAsync`
Checks whether a specific key exists in the cache without retrieving the value.
- **Parameters**: Accepts a string key to check.
- **Return Value**: Returns a `Task<bool>` indicating `true` if the key exists and has not expired, otherwise `false`.
- **Exceptions**: May throw if the cache infrastructure is unreachable.

### `GetOrLoadAsync<T>`
Attempts to retrieve an item from the cache; if missing, invokes a factory function to load the data, caches it, and returns the result.
- **Parameters**: Accepts a string key and a `Func<Task<T>>` factory delegate used to load the data if the cache miss occurs.
- **Return Value**: Returns a `Task<T>` containing either the cached value or the newly loaded value.
- **Exceptions**: Propagates exceptions thrown by the factory delegate if loading fails. Concurrency issues may arise if multiple callers trigger the factory simultaneously depending on the implementation's locking strategy.

## Usage

### Example 1: Basic Caching Pattern
This example demonstrates storing a workflow state object and retrieving it later, handling potential cache misses gracefully.

```csharp
public class WorkflowProcessor
{
    private readonly ICacheService _cache;

    public WorkflowProcessor(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task ProcessWorkflowAsync(string workflowId, WorkflowState state)
    {
        // Store the state in cache with a 15-minute expiration (implementation dependent)
        await _cache.SetAsync($"workflow:{workflowId}", state);

        // Later retrieval
        var cachedState = await _cache.GetAsync<WorkflowState>($"workflow:{workflowId}");
        
        if (cachedState != null)
        {
            Console.WriteLine($"Resuming workflow {workflowId} from cache.");
        }
    }
}
```

### Example 2: Lazy Loading with GetOrLoadAsync
This example utilizes `GetOrLoadAsync` to ensure expensive configuration data is loaded only once and subsequently served from the cache.

```csharp
public class ConfigurationProvider
{
    private readonly ICacheService _cache;
    private readonly IConfigRepository _repository;

    public ConfigurationProvider(ICacheService cache, IConfigRepository repository)
    {
        _cache = cache;
        _repository = repository;
    }

    public async Task<EngineConfig> GetConfigAsync(string tenantId)
    {
        return await _cache.GetOrLoadAsync(
            $"config:{tenantId}", 
            async () => 
            {
                // This only executes if the key is missing
                return await _repository.FetchConfigAsync(tenantId);
            }
        );
    }
}
```

## Notes

- **Thread Safety**: Implementations like `MemoryCacheService` typically handle concurrent access internally, but callers should be aware that `GetOrLoadAsync` might invoke the factory delegate multiple times concurrently if the underlying cache does not implement request coalescing (double-checked locking).
- **Serialization**: When using `DistributedCacheService`, all types `T` stored must be serializable. Failure to serialize complex objects will result in runtime exceptions during `SetAsync` or `GetAsync`.
- **Null Handling**: A return value of `null` from `GetAsync<T>` indicates a cache miss or expiration; it does not distinguish between a stored `null` value and a missing key unless the specific implementation documents otherwise.
- **Expiration Policies**: The interface itself does not expose expiration parameters directly in the signature; expiration logic is often handled via default configurations in the concrete classes or overloaded methods not explicitly listed in the core interface definition.
- **Consistency**: In distributed scenarios, there may be a brief window of inconsistency between `ExistsAsync` returning `true` and `GetAsync` returning `null` due to network latency or eviction policies occurring between calls.
