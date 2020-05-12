// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System.Text.Json;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Utilities;

/// <summary>
/// Provides System.Text.Json serialization helpers for <see cref="WorkflowBuilder"/>.
/// </summary>
public static class WorkflowBuilderJsonExtensions
{
    // Cached options with camelCase naming policy, matching the application's default serialization settings.
    private static readonly JsonSerializerOptions _options = SerializationHelper.DefaultOptions;

    /// <summary>
    /// Serializes the <see cref="WorkflowBuilder"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The builder instance to serialize.</param>
    /// <param name="indented">If <c>true</c>, the output JSON will be indented for readability.</param>
    /// <returns>A JSON representation of the builder's workflow.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    public static string ToJson(this WorkflowBuilder value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_options) { WriteIndented = true }
            : _options;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string into a <see cref="WorkflowBuilder"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>
    /// A deserialized <see cref="WorkflowBuilder"/> instance, or <c>null</c> if the JSON represents a null value.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <c>null</c>.</exception>
    /// <exception cref="JsonException">
    /// Thrown when the JSON is invalid or cannot be deserialized into a <see cref="WorkflowBuilder"/>.
    /// </exception>
    public static WorkflowBuilder? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize<WorkflowBuilder>(json, _options);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a <see cref="WorkflowBuilder"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">
    /// When this method returns, contains the deserialized <see cref="WorkflowBuilder"/> if the operation succeeded;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <c>null</c>.</exception>
    public static bool TryFromJson(string json, out WorkflowBuilder? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            value = JsonSerializer.Deserialize<WorkflowBuilder>(json, _options);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}