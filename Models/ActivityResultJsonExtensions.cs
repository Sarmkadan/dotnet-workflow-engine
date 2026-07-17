// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Models;

/// <summary>
/// Provides extension methods for serializing and deserializing <see cref="ActivityResult"/>
/// instances to and from JSON using System.Text.Json.
/// </summary>
public static class ActivityResultJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Converts the <see cref="ActivityResult"/> to a JSON string.
    /// </summary>
    /// <param name="value">The activity result to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the activity result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this ActivityResult value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions)
            {
                WriteIndented = true
            }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Parses an <see cref="ActivityResult"/> instance from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized activity result, or null if the JSON is null, empty, or whitespace.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static ActivityResult? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ActivityResult>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to parse an <see cref="ActivityResult"/> instance from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized activity result if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out ActivityResult? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<ActivityResult>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}