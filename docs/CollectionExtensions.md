# CollectionExtensions

Provides a set of static utility methods for common operations on collections, enumerables, and dictionaries. These extensions simplify null-safe enumeration, batching, partitioning, dictionary merging, and other transformations that reduce boilerplate in workflow and general-purpose .NET code.

## API

### SafeFirst\<T\>
```csharp
public static T? SafeFirst<T>(this IEnumerable<T> source)
```
Returns the first element of a sequence, or `default(T)` if the sequence is empty. Does not throw when the source contains no elements. If `source` is `null`, an `ArgumentNullException` is thrown.

### WhereNotNull\<T\>
```csharp
public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
```
Filters a sequence of nullable reference types, returning only those elements that are not `null`. The resulting sequence is typed as non-nullable `T`. Throws `ArgumentNullException` if `source` is `null`.

### IsNullOrEmpty\<T\>
```csharp
public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
```
Returns `true` if the sequence is `null` or contains no elements; otherwise `false`. Does not throw.

### Batch\<T\>
```csharp
public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int size)
```
Partitions the source sequence into batches of the specified `size`. The final batch may be smaller if the total count is not evenly divisible. `size` must be greater than zero; otherwise an `ArgumentOutOfRangeException` is thrown. Throws `ArgumentNullException` if `source` is `null`.

### ToSafeDictionary\<T, TKey\>
```csharp
public static Dictionary<TKey, T> ToSafeDictionary<T, TKey>(
    this IEnumerable<T> source,
    Func<T, TKey> keySelector)
```
Creates a `Dictionary<TKey, T>` from the source sequence using the provided key selector. If duplicate keys are encountered, subsequent values silently overwrite earlier ones rather than throwing. Throws `ArgumentNullException` if `source` or `keySelector` is `null`.

### ContainsSameElements\<T\>
```csharp
public static bool ContainsSameElements<T>(
    this IEnumerable<T> first,
    IEnumerable<T> second)
```
Determines whether two sequences contain exactly the same elements, ignoring order and duplicate counts. Returns `true` if both sequences have the same set of distinct elements; otherwise `false`. Throws `ArgumentNullException` if either argument is `null`.

### TryGetValue\<TKey, T\>
```csharp
public static T? TryGetValue<TKey, T>(
    this IDictionary<TKey, T> dictionary,
    TKey key)
```
Attempts to retrieve a value from the dictionary by key. Returns the value if found, or `default(T)` if the key is not present. Throws `ArgumentNullException` if `dictionary` is `null`.

### Merge\<TKey, TValue\>
```csharp
public static Dictionary<TKey, TValue> Merge<TKey, TValue>(
    this IDictionary<TKey, TValue> first,
    IDictionary<TKey, TValue> second)
```
Combines two dictionaries into a new `Dictionary<TKey, TValue>`. Entries from `second` overwrite entries with the same key from `first`. Both input dictionaries remain unchanged. Throws `ArgumentNullException` if either argument is `null`.

### Flatten\<T\>
```csharp
public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> source)
```
Flattens a sequence of sequences into a single, flat sequence of elements. Throws `ArgumentNullException` if `source` is `null`. Inner sequences that are `null` are skipped.

### DistinctOrdered\<T\>
```csharp
public static IEnumerable<T> DistinctOrdered<T>(this IEnumerable<T> source)
```
Returns distinct elements from the source sequence while preserving their original order. Uses the default equality comparer for `T`. Throws `ArgumentNullException` if `source` is `null`.

### AddAndReturn\<T\>
```csharp
public static ICollection<T> AddAndReturn<T>(this ICollection<T> collection, T item)
```
Adds the specified `item` to the collection and returns the same collection instance, enabling fluent chaining. Throws `ArgumentNullException` if `collection` is `null`.

### Partition\<T\>
```csharp
public static (List<T> Matching, List<T> NotMatching) Partition<T>(
    this IEnumerable<T> source,
    Func<T, bool> predicate)
```
Splits the source sequence into two lists: elements that satisfy the predicate (`Matching`) and those that do not (`NotMatching`). Both lists are always returned, even if one or both are empty. Throws `ArgumentNullException` if `source` or `predicate` is `null`.

## Usage

### Example 1: Batching and Partitioning Workflow Items
```csharp
var items = workflowService.GetPendingItems();
if (items.IsNullOrEmpty())
    return;

// Process in batches of 50
foreach (var batch in items.Batch(50))
{
    var (priority, normal) = batch.Partition(i => i.Priority > 3);

    ProcessPriorityItems(priority);
    ProcessNormalItems(normal);
}
```

### Example 2: Safe Dictionary Building and Merging
```csharp
var configOverrides = new Dictionary<string, string>
{
    ["host"] = "production.example.com",
    ["timeout"] = "30"
};

var baseConfig = settingsReader.LoadBaseConfiguration()
    .ToSafeDictionary(s => s.Key);

// Merge with overrides taking precedence
var finalConfig = baseConfig.Merge(configOverrides);

var host = finalConfig.TryGetValue("host") ?? "localhost";
```

## Notes

- All methods that accept `IEnumerable<T>` or collection parameters throw `ArgumentNullException` when passed `null` arguments, unless explicitly documented otherwise (`IsNullOrEmpty` accepts `null`).
- `ToSafeDictionary` silently resolves key collisions by keeping the last value encountered. This differs from LINQ's `ToDictionary`, which throws on duplicate keys.
- `Batch` returns deferred iterators. Enumerating the outer sequence multiple times re-evaluates the source; materialize with `ToList()` if multiple passes are needed.
- `ContainsSameElements` uses set-based comparison and ignores element frequency. Two sequences with different duplicate counts but the same distinct values are considered equal.
- `Flatten` skips `null` inner sequences rather than throwing, which differs from `SelectMany` behavior when an inner sequence is `null`.
- None of these methods guarantee thread safety. If the underlying collections or sequences are modified during enumeration, behavior follows standard .NET enumeration rules (typically resulting in `InvalidOperationException`).
