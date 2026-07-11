// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetWorkflowEngine.Utilities;

/// <summary>
/// Extension methods for collections (lists, dictionaries, enumerables).
/// Provides common operations like filtering, transformation, and safe access.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Safely retrieves the first element of a collection or returns a default value
    /// without throwing an exception if the collection is empty.
    /// </summary>
    /// <param name="collection">The collection to retrieve the first element from.</param>
    /// <param name="defaultValue">The default value to return if the collection is null or empty.</param>
    /// <returns>The first element or default value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if collection is null and defaultValue is null.</exception>
    public static T? SafeFirst<T>(this IEnumerable<T>? collection, T? defaultValue = default) where T : class
    {
        if (collection == null)
        {
            if (defaultValue == null)
            {
                throw new ArgumentNullException(nameof(collection), "Collection cannot be null when defaultValue is null");
            }
            return defaultValue;
        }

        return collection.FirstOrDefault() ?? defaultValue;
    }

    /// <summary>
    /// Filters a collection to include only elements that are not null.
    /// Useful for cleaning up collections after nullable operations.
    /// </summary>
    /// <param name="collection">The collection to filter.</param>
    /// <returns>Filtered collection without null elements.</returns>
    /// <exception cref="ArgumentNullException">Thrown if collection is null.</exception>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?>? collection) where T : class
    {
        ArgumentNullException.ThrowIfNull(collection);
        return collection.Where(x => x != null)!;
    }

    /// <summary>
    /// Checks if a collection is null or empty without throwing an exception.
    /// </summary>
    /// <param name="collection">The collection to check.</param>
    /// <returns>True if collection is null or empty; otherwise false.</returns>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? collection)
    {
        return collection == null || !collection.Any();
    }

    /// <summary>
    /// Batches a collection into groups of a specified size.
    /// Example: [1,2,3,4,5].Batch(2) -> [[1,2], [3,4], [5]]
    /// </summary>
    /// <param name="collection">The collection to batch.</param>
    /// <param name="batchSize">The size of each batch.</param>
    /// <returns>Batched collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown if collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown if batchSize is less than or equal to 0.</exception>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T>? collection, int batchSize)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(batchSize, 0);

        var batch = new List<T>();
        foreach (var item in collection)
        {
            batch.Add(item);
            if (batch.Count == batchSize)
            {
                yield return batch;
                batch = new List<T>();
            }
        }

        if (batch.Count > 0)
            yield return batch;
    }

    /// <summary>
    /// Converts a collection to a dictionary using a key selector function.
    /// Throws if duplicate keys are encountered.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <param name="collection">The collection to convert.</param>
    /// <param name="keySelector">The function to extract keys from elements.</param>
    /// <returns>Dictionary with extracted keys and original elements.</returns>
    /// <exception cref="ArgumentNullException">Thrown if collection or keySelector is null.</exception>
    /// <exception cref="ArgumentException">Thrown if duplicate keys are encountered.</exception>
    public static Dictionary<TKey, T> ToSafeDictionary<T, TKey>(
        this IEnumerable<T>? collection,
        Func<T, TKey>? keySelector) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(keySelector);

        var dict = new Dictionary<TKey, T>();
        foreach (var item in collection)
        {
            var key = keySelector(item);
            if (dict.ContainsKey(key))
                throw new ArgumentException($"Duplicate key encountered: {key}");

            dict[key] = item;
        }

        return dict;
    }

    /// <summary>
    /// Checks if two collections contain the same elements (order-independent).
    /// </summary>
    /// <param name="collection1">The first collection to compare.</param>
    /// <param name="collection2">The second collection to compare.</param>
    /// <returns>True if collections contain the same elements; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if collection1 is null.</exception>
    public static bool ContainsSameElements<T>(this IEnumerable<T>? collection1, IEnumerable<T>? collection2)
    {
        ArgumentNullException.ThrowIfNull(collection1);

        return collection1.OrderBy(x => x)
            .SequenceEqual(collection2?.OrderBy(x => x) ?? Enumerable.Empty<T>());
    }

    /// <summary>
    /// Safely tries to get a value from a dictionary without throwing an exception.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="T">The type of values in the dictionary.</typeparam>
    /// <param name="dict">The dictionary to search.</param>
    /// <param name="key">The key to find.</param>
    /// <param name="defaultValue">The default value to return if key is not found.</param>
    /// <returns>The found value or default value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if dict is null.</exception>
    public static T? TryGetValue<TKey, T>(this Dictionary<TKey, T>? dict, TKey key, T? defaultValue = default) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dict);
        return dict.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Merges multiple dictionaries into one. Later dictionaries override earlier ones.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionaries.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionaries.</typeparam>
    /// <param name="dict">The primary dictionary.</param>
    /// <param name="otherDictionaries">Additional dictionaries to merge.</param>
    /// <returns>Merged dictionary.</returns>
    /// <exception cref="ArgumentNullException">Thrown if dict is null.</exception>
    public static Dictionary<TKey, TValue> Merge<TKey, TValue>(
        this Dictionary<TKey, TValue>? dict,
        params Dictionary<TKey, TValue>?[] otherDictionaries) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dict);

        var result = new Dictionary<TKey, TValue>(dict);

        foreach (var other in otherDictionaries ?? Array.Empty<Dictionary<TKey, TValue>?>())
        {
            if (other != null)
            {
                foreach (var kvp in other)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Flattens a nested collection into a single-level collection.
    /// Example: [[1,2], [3,4]].Flatten() -> [1,2,3,4]
    /// </summary>
    /// <param name="collection">The nested collection to flatten.</param>
    /// <returns>Flattened collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown if collection is null.</exception>
    public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>>? collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        return collection.SelectMany(x => x);
    }

    /// <summary>
    /// Removes duplicate elements from a collection while preserving order.
    /// More efficient than distinct for small collections.
    /// </summary>
    /// <param name="collection">The collection to process.</param>
    /// <returns>Collection with duplicates removed, preserving original order.</returns>
    /// <exception cref="ArgumentNullException">Thrown if collection is null.</exception>
    public static IEnumerable<T> DistinctOrdered<T>(this IEnumerable<T>? collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        var seen = new HashSet<T>();
        foreach (var item in collection)
        {
            if (seen.Add(item))
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Adds an item to a collection and returns the collection (for chaining).
    /// </summary>
    /// <param name="collection">The collection to add to.</param>
    /// <param name="item">The item to add.</param>
    /// <returns>The collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if collection is null.</exception>
    public static ICollection<T> AddAndReturn<T>(this ICollection<T>? collection, T item)
    {
        ArgumentNullException.ThrowIfNull(collection);
        collection.Add(item);
        return collection;
    }

    /// <summary>
    /// Splits a collection into two based on a predicate.
    /// Returns a tuple of (matching, notMatching) elements.
    /// </summary>
    /// <param name="collection">The collection to partition.</param>
    /// <param name="predicate">The predicate to split by.</param>
    /// <returns>Tuple containing matching and not matching elements.</returns>
    /// <exception cref="ArgumentNullException">Thrown if collection or predicate is null.</exception>
    public static (List<T> Matching, List<T> NotMatching) Partition<T>(
        this IEnumerable<T>? collection,
        Func<T, bool>? predicate)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(predicate);

        var matching = new List<T>();
        var notMatching = new List<T>();

        foreach (var item in collection)
        {
            if (predicate(item))
                matching.Add(item);
            else
                notMatching.Add(item);
        }

        return (matching, notMatching);
    }
}