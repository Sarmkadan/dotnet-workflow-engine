// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System;
using System.Text.Json;

namespace DotNetWorkflowEngine.Examples;

/// <summary>
/// Provides JSON serialization helpers for <see cref="CustomActivityExample"/>.
/// </summary>
public static class CustomActivityExampleJsonExtensions
{
    // Cached options with camel‑case naming policy.
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Serializes the <see cref="CustomActivityExample"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The instance to serialize.</param>
    /// <param name="indented">If <c>true</c>, the output JSON will be indented.</param>
    /// <returns>A JSON representation of <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    public static string ToJson(this CustomActivityExample value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        // If indentation is requested, clone the cached options and enable WriteIndented.
        var options = indented ? new JsonSerializerOptions(_options) { WriteIndented = true } : _options;
        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string into a <see cref="CustomActivityExample"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="CustomActivityExample"/> instance, or <c>null</c> if the JSON represents a null value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <c>null</c>.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized into <see cref="CustomActivityExample"/>.</exception>
    public static CustomActivityExample? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize<CustomActivityExample>(json, _options);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a <see cref="CustomActivityExample"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">
    /// When this method returns, contains the deserialized <see cref="CustomActivityExample"/> if the operation succeeded;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <c>null</c>.</exception>
    public static bool TryFromJson(string json, out CustomActivityExample? value)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            value = JsonSerializer.Deserialize<CustomActivityExample>(json, _options);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}