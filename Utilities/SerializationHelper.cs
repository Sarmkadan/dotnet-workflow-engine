// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetWorkflowEngine.Utilities;

/// <summary>
/// Helper class for serialization/deserialization operations. Provides convenient
/// methods for working with JSON, with standardized options for consistency across
/// the application.
/// </summary>
public static class SerializationHelper
{
    /// <summary>
    /// Default JSON serialization options used throughout the application.
    /// Configured for consistency: camelCase properties, ignore nulls, preserve references.
    /// </summary>
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// JSON serialization options with pretty-printing (indentation) enabled.
    /// Useful for logging and API responses that should be human-readable.
    /// </summary>
    public static readonly JsonSerializerOptions PrettyOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Serializes an object to JSON string using default options.
    /// </summary>
    public static string ToJson<T>(T? obj) where T : class
    {
        return JsonSerializer.Serialize(obj, DefaultOptions);
    }

    /// <summary>
    /// Serializes an object to JSON string with pretty-printing.
    /// </summary>
    public static string ToJsonPretty<T>(T? obj) where T : class
    {
        return JsonSerializer.Serialize(obj, PrettyOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to an object of type T.
    /// Returns null if deserialization fails or input is null/empty.
    /// </summary>
    public static T? FromJson<T>(string? json) where T : class
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }
        catch (JsonException ex)
        {
            throw new SerializationException($"Failed to deserialize JSON to {typeof(T).Name}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deserializes a JSON string to a Dictionary, useful for untyped data.
    /// </summary>
    public static Dictionary<string, object>? FromJsonToDict(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json, DefaultOptions);
        }
        catch (JsonException ex)
        {
            throw new SerializationException($"Failed to deserialize JSON to dictionary: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Safely deserializes JSON, returning null on any error (instead of throwing).
    /// Useful when parsing user input where invalid JSON is expected.
    /// </summary>
    public static T? TryFromJson<T>(string? json) where T : class
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Clones an object by serializing to JSON and deserializing back.
    /// Useful for creating deep copies of complex objects.
    /// </summary>
    public static T? DeepClone<T>(T? obj) where T : class
    {
        var json = ToJson(obj);
        return FromJson<T>(json);
    }

    /// <summary>
    /// Merges two objects by serializing to dictionaries and combining.
    /// Later object's values override earlier object's values.
    /// </summary>
    public static T? Merge<T>(T? obj1, T? obj2) where T : class
    {
        if (obj1 == null)
            return obj2;

        if (obj2 == null)
            return obj1;

        var dict1 = JsonSerializer.Deserialize<Dictionary<string, object>>(ToJson(obj1), DefaultOptions) ?? new();
        var dict2 = JsonSerializer.Deserialize<Dictionary<string, object>>(ToJson(obj2), DefaultOptions) ?? new();

        foreach (var kvp in dict2)
            dict1[kvp.Key] = kvp.Value;

        var mergedJson = ToJson(dict1);
        return FromJson<T>(mergedJson);
    }

    /// <summary>
    /// Converts a JSON element to a specific type. Useful when working with JsonDocument.
    /// </summary>
    public static T? FromJsonElement<T>(JsonElement element) where T : class
    {
        var json = element.GetRawText();
        return FromJson<T>(json);
    }

    /// <summary>
    /// Converts an object to a JsonElement for manipulation using JsonDocument APIs.
    /// </summary>
    public static JsonElement ToJsonElement<T>(T? obj) where T : class
    {
        var json = ToJson(obj);
        using (var doc = JsonDocument.Parse(json))
        {
            return doc.RootElement.Clone();
        }
    }

    /// <summary>
    /// Validates if a string is valid JSON without deserializing to a specific type.
    /// </summary>
    public static bool IsValidJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return false;

        try
        {
            using (var doc = JsonDocument.Parse(json))
            {
                return true;
            }
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Pretty-prints JSON string (formats with indentation).
    /// Useful for making log output more readable.
    /// </summary>
    public static string PrettyPrintJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return json;

        try
        {
            using (var doc = JsonDocument.Parse(json))
            {
                return JsonSerializer.Serialize(doc.RootElement, PrettyOptions);
            }
        }
        catch
        {
            return json; // Return original if prettification fails
        }
    }

    /// <summary>
    /// Minifies JSON string (removes unnecessary whitespace).
    /// Useful for reducing payload size in API responses.
    /// </summary>
    public static string MinifyJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return json;

        try
        {
            using (var doc = JsonDocument.Parse(json))
            {
                return JsonSerializer.Serialize(doc.RootElement, DefaultOptions);
            }
        }
        catch
        {
            return json; // Return original if minification fails
        }
    }
}

/// <summary>
/// Custom exception for serialization/deserialization errors.
/// </summary>
public class SerializationException : Exception
{
    public SerializationException(string message) : base(message) { }
    public SerializationException(string message, Exception innerException) : base(message, innerException) { }
}
