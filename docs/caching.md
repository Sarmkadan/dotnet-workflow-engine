# Caching Documentation

This document describes the caching components in the `dotnet-workflow-engine`, including the unified `CacheService` interface and its implementations for in-memory, distributed, and no-operation caching scenarios.

## Overview

The caching subsystem provides a unified abstraction (`ICacheService`) that supports multiple cache backends:
- **In-memory caching** using `Microsoft.Extensions.Caching.Memory.IMemoryCache`
- **Distributed caching** using `Microsoft.Extensions.Caching.Distributed.IDistributedCache` (e.g., Redis)
- **No-operation caching** for disabling cache functionality in tests or specific environments

All implementations follow the same asynchronous interface and provide consistent behavior for cache operations including get, set, remove, exists, and get-or-load patterns.

## CacheService.cs

The `CacheService.cs` file contains the core caching abstractions and implementations:

### ICacheService Interface

Defines the contract for all cache operations:
- `GetAsync<T>(string key)` - Retrieve a cached value
- `SetAsync<T>(string key, T value, TimeSpan? expiration = null)` - Store a value with optional expiration
- `RemoveAsync(string key)` - Remove a specific cache entry
- `ExistsAsync(string key)` - Check if a key exists in cache
- `GetOrLoadAsync<T>(string key, Func<Task<T>> provider, TimeSpan? expiration = null)` - Retrieve cached value or load it via provider function

### MemoryCacheService

In-memory cache implementation using `IMemoryCache`:
- Fast, single-node caching suitable for development and simple deployments
- Automatic logging of cache hits/misses for monitoring
- Configurable default expiration (defaults to 1 hour)
- Thread-safe operations with proper argument validation

### DistributedCacheService

Distributed cache implementation using `IDistributedCache`:
- Suitable for multi-node deployments with Redis or other cache stores
- JSON serialization/deserialization for complex object storage
- Comprehensive error handling with logging for cache operations
- Automatic expiration handling matching the memory cache behavior

### NoOpCacheService

No-operation cache implementation that performs no actual caching:
- All operations complete successfully but do not store or retrieve data
- Useful for disabling caching in tests or environments where caching is not desired
- Returns default values (`null` for gets, `false` for exists) as appropriate
- Inherently thread-safe since no state is maintained

## Usage Examples

### Basic Caching Pattern

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
        // Store the state in cache with a 15-minute expiration
        await _cache.SetAsync($"workflow:{workflowId}", state, TimeSpan.FromMinutes(15));

        // Later retrieval
        var cachedState = await _cache.GetAsync<WorkflowState>($"workflow:{workflowId}");
        
        if (cachedState != null)
        {
            Console.WriteLine($"Resuming workflow {workflowId} from cache.");
        }
    }
}
```

### Lazy Loading with GetOrLoadAsync

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

### Using NoOpCacheService for Testing

```csharp
// In test scenarios where caching should be disabled
var cache = new NoOpCacheService();

// Operations complete but don't actually cache anything
await cache.SetAsync("test-key", "test-value");
var result = await cache.GetAsync<string>("test-key"); // Returns null
bool exists = await cache.ExistsAsync("test-key");     // Returns false
```

## Implementation Notes

### Thread Safety
- `MemoryCacheService`: Relies on the thread-safety of `IMemoryCache`
- `DistributedCacheService`: Thread-safety depends on the underlying `IDistributedCache` implementation
- `NoOpCacheService`: Inherently thread-safe as no state is maintained

### Serialization Requirements
- `DistributedCacheService`: All types `T` stored must be JSON serializable
- Complex objects may require custom serialization handling
- Serialization failures are caught and logged, returning null/default values

### Error Handling
- All implementations validate arguments and throw `ArgumentNullException`/`ArgumentException` for invalid inputs
- Distributed cache operations catch and log exceptions from the underlying cache provider
- Memory cache operations are generally exception-free for valid inputs

### Expiration Policies
- Both `MemoryCacheService` and `DistributedCacheService` support optional expiration parameters
- When not specified, a default expiration of 1 hour is applied
- Expiration is implemented as absolute expiration relative to now

### Logging
- All cache implementations include structured logging for monitoring cache performance
- Log levels: Debug for cache hits/misses, Information for cache misses triggering loads, Error for exceptions
- Logging helps with debugging cache-related issues and monitoring cache effectiveness

## Dependency Injection Registration

Cache services are typically registered in the application's dependency injection container:

```csharp
// For in-memory caching
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();

// For distributed caching (e.g., Redis)
builder.Services.AddStackExchangeRedisCache(options => 
{
    options.Configuration = "localhost:6379";
});
builder.Services.AddScoped<ICacheService, DistributedCacheService>();

// For disabling caching (e.g., in tests)
builder.Services.AddScoped<ICacheService, NoOpCacheService>();
```

## Performance Considerations

- **MemoryCacheService**: Lowest latency, highest throughput, limited by server memory
- **DistributedCacheService**: Higher latency due to network calls, but enables cache sharing across multiple instances
- **NoOpCacheService**: Minimal overhead, but results in every access being a cache miss

Choose the implementation based on your deployment architecture and performance requirements.