// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    public static T? SafeFirst<T>(this IEnumerable<T> collection, T? defaultValue = default) where T : class
    {
        return collection?.FirstOrDefault() ?? defaultValue;
    }

    /// <summary>
    /// Filters a collection to include only elements that are not null.
    /// Useful for cleaning up collections after nullable operations.
    /// </summary>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> collection) where T : class
    {
        return collection?.Where(x => x != null)! ?? Enumerable.Empty<T>();
    }

    /// <summary>
    /// Checks if a collection is null or empty without throwing an exception.
    /// </summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? collection)
    {
        return collection == null || !collection.Any();
    }

    /// <summary>
    /// Batches a collection into groups of a specified size.
    /// Example: [1,2,3,4,5].Batch(2) -> [[1,2], [3,4], [5]]
    /// </summary>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> collection, int batchSize)
    {
        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be greater than 0");

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
    public static Dictionary<TKey, T> ToSafeDictionary<T, TKey>(
        this IEnumerable<T> collection,
        Func<T, TKey> keySelector) where TKey : notnull
    {
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
    public static bool ContainsSameElements<T>(this IEnumerable<T> collection1, IEnumerable<T> collection2)
    {
        return collection1?.OrderBy(x => x)
            .SequenceEqual(collection2?.OrderBy(x => x) ?? Enumerable.Empty<T>()) ?? false;
    }

    /// <summary>
    /// Safely tries to get a value from a dictionary without throwing an exception.
    /// </summary>
    public static T? TryGetValue<TKey, T>(this Dictionary<TKey, T> dict, TKey key, T? defaultValue = default) where TKey : notnull
    {
        return dict != null && dict.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Merges multiple dictionaries into one. Later dictionaries override earlier ones.
    /// </summary>
    public static Dictionary<TKey, TValue> Merge<TKey, TValue>(
        this Dictionary<TKey, TValue> dict,
        params Dictionary<TKey, TValue>[] otherDictionaries) where TKey : notnull
    {
        var result = new Dictionary<TKey, TValue>(dict ?? new Dictionary<TKey, TValue>());

        foreach (var other in otherDictionaries ?? Array.Empty<Dictionary<TKey, TValue>>())
        {
            if (other != null)
            {
                foreach (var kvp in other)
                    result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    /// <summary>
    /// Flattens a nested collection into a single-level collection.
    /// Example: [[1,2], [3,4]].Flatten() -> [1,2,3,4]
    /// </summary>
    public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> collection)
    {
        return collection?.SelectMany(x => x) ?? Enumerable.Empty<T>();
    }

    /// <summary>
    /// Removes duplicate elements from a collection while preserving order.
    /// More efficient than distinct for small collections.
    /// </summary>
    public static IEnumerable<T> DistinctOrdered<T>(this IEnumerable<T> collection)
    {
        var seen = new HashSet<T>();
        foreach (var item in collection ?? Enumerable.Empty<T>())
        {
            if (seen.Add(item))
                yield return item;
        }
    }

    /// <summary>
    /// Adds an item to a collection and returns the collection (for chaining).
    /// </summary>
    public static ICollection<T> AddAndReturn<T>(this ICollection<T> collection, T item)
    {
        collection?.Add(item);
        return collection ?? new List<T> { item };
    }

    /// <summary>
    /// Splits a collection into two based on a predicate.
    /// Returns a tuple of (matching, notMatching) elements.
    /// </summary>
    public static (List<T> Matching, List<T> NotMatching) Partition<T>(
        this IEnumerable<T> collection,
        Func<T, bool> predicate)
    {
        var matching = new List<T>();
        var notMatching = new List<T>();

        foreach (var item in collection ?? Enumerable.Empty<T>())
        {
            if (predicate(item))
                matching.Add(item);
            else
                notMatching.Add(item);
        }

        return (matching, notMatching);
    }
}
