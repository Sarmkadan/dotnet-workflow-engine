# NoOpCacheService

A no‑op implementation of `ICacheService` that performs no actual caching. All operations complete successfully but do not store or retrieve data; they return default values or `false` as appropriate. This class is useful for disabling caching in tests or in environments where caching is not desired.

## API

### GetAsync<T>(string key)
- **Purpose:** Attempts to retrieve a value associated with the specified key.
- **Parameters:** `key` – the identifier of the item to retrieve.
- **Return value:** A `Task<T?>` that completes with the cached value if present; otherwise `default(T?)`. Because this is a no‑op cache, the method always returns `default(T?)`.
- **Exceptions:** Throws `ArgumentNullException` if `key` is `null`.

### SetAsync<T>(string key, T value)
- **Purpose:** Associates the specified value with the key in the cache.
- **Parameters:** `key` – the identifier under which to store the value; `value` – the object to store.
- **Return value:** A `Task` that completes when the operation finishes. The no‑op implementation does nothing and completes successfully.
- **Exceptions:** Throws `ArgumentNullException` if `key` is `null`.

### RemoveAsync(string key)
- **Purpose:** Removes the item identified by `key` from the cache.
- **Parameters:** `key` – the identifier of the item to remove.
- **Return value:** A `Task` that completes when the removal operation finishes. The no‑op implementation does nothing and completes successfully.
- **Exceptions:** Throws `ArgumentNullException` if `key` is `null`.

### ClearAsync()
- **Purpose:** Removes all items from the cache.
- **Parameters:** None.
- **Return value:** A `Task` that completes when the clear operation finishes. The no‑op implementation does nothing and completes successfully.
- **Exceptions:** None.

### ExistsAsync(string key)
- **Purpose:** Determines whether an item with the specified key exists in the cache.
- **Parameters:** `key` – the identifier to test.
- **Return value:** A `Task<bool>` that yields `true` if the item exists; otherwise `false`. The no‑op cache always returns `false`.
- **Exceptions:** Throws `ArgumentNullException` if `key` is `null`.

### GetOrLoadAsync<T>(string key, Func<Task<T>> loader)
- **Purpose:** Returns the cached value for `key` if present; otherwise invokes `loader` to obtain the value, caches it, and returns it.
- **Parameters:** `key` – the cache identifier; `loader` – an asynchronous factory that produces the value when the cache misses.
- **Return value:** A `Task<T>` that yields the value from the cache or from `loader`.
- **Exceptions:** Throws `ArgumentNullException` if `key` or `loader` is `null`. Any exception thrown by `loader` is propagated unchanged.

## Usage

```csharp
// Example 1: Basic get/set pattern with a NoOpCacheService
var cache = new NoOpCacheService();

await cache.SetAsync("user:42", new User { Id = 42, Name = "Ada" });
User? user = await cache.GetAsync<User>("user:42"); // user will be null
bool exists = await cache.ExistsAsync("user:42");   // exists will be false
```

```csharp
// Example 2: Using GetOrLoadAsync to lazily load data despite the no‑op cache
var cache = new NoOpCacheService();

async Task<Product> GetProductAsync(int id)
{
    // The loader will always be invoked because the cache never stores anything.
    return await cache.GetOrLoadAsync(
        $"product:{id}",
        async () =>
        {
            // Simulate a call to a database or external service.
            return await ProductRepository.LoadByIdAsync(id);
        });
}
```

## Notes

- Because the cache does not retain any state, all methods are inherently thread‑safe; concurrent calls will not interfere with each other.
- The methods perform only basic argument validation (null checks) and then return predetermined results, so they never throw exceptions other than `ArgumentNullException` for null arguments.
- Since no data is ever stored, `GetAsync<T>` always returns `default(T?)`, `ExistsAsync` always returns `false`, and `ClearAsync`/`RemoveAsync` have no effect.
- `GetOrLoadAsync<T>` will always invoke the supplied `loader` delegate, making it suitable for scenarios where the cache is intentionally disabled but the fallback loading logic must still run.
