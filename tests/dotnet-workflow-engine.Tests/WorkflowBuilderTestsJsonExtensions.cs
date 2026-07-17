// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetWorkflowEngine.Tests;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="WorkflowBuilderTests"/>
/// to enable JSON serialization/deserialization of test workflows.
/// </summary>
/// <remarks>
/// This static utility class offers convenience methods for serializing and deserializing
/// <see cref="WorkflowBuilderTests"/> instances to and from JSON format, using camelCase property naming
/// and ignoring null values by default. The serialization preserves the structure of workflow
/// definitions including activities, transitions, and configuration for testing purposes.
/// </remarks>
public static class WorkflowBuilderTestsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    /// <summary>
    /// Serializes the <see cref="WorkflowBuilderTests"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The workflow builder tests instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the workflow builder tests.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this WorkflowBuilderTests value, bool indented = false) =>
        ToJsonAsync(value, indented).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronously serializes the <see cref="WorkflowBuilderTests"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The workflow builder tests instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the workflow builder tests.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static async Task<string> ToJsonAsync(this WorkflowBuilderTests value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, value, options).ConfigureAwait(false);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="WorkflowBuilderTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.
    /// Must be a valid JSON representation of a <see cref="WorkflowBuilderTests"/> instance.</param>
    /// <returns>The deserialized <see cref="WorkflowBuilderTests"/> instance, or null if the JSON is empty or whitespace.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized into a <see cref="WorkflowBuilderTests"/> instance.</exception>
    public static WorkflowBuilderTests? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<WorkflowBuilderTests>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="WorkflowBuilderTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.
    /// Can be null, empty, or whitespace to return true with a null output.</param>
    /// <param name="value">Receives the deserialized instance if successful, otherwise null.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out WorkflowBuilderTests? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        try
        {
            value = JsonSerializer.Deserialize<WorkflowBuilderTests>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}