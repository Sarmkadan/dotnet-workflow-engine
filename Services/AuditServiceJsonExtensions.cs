// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System.Text.Json;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Services;

/// <summary>
/// Provides System.Text.Json serialization and deserialization extensions for audit-related models.
/// </summary>
public static class AuditServiceJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes an AuditLogEntry to a JSON string.
    /// </summary>
    /// <param name="value">The AuditLogEntry to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the AuditLogEntry.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the AuditLogEntry is null.</exception>
    public static string ToJson(this AuditLogEntry value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to an AuditLogEntry instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>An AuditLogEntry instance, or null if the JSON is empty or whitespace.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the JSON string is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static AuditLogEntry? FromJsonToAuditLogEntry(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<AuditLogEntry>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to an AuditLogEntry instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized AuditLogEntry instance, or null on failure.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the JSON string is null.</exception>
    public static bool TryFromJsonToAuditLogEntry(string json, out AuditLogEntry? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<AuditLogEntry>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Serializes a collection of AuditLogEntry objects to a JSON string.
    /// </summary>
    /// <param name="values">The collection of AuditLogEntry objects to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the AuditLogEntry collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the collection is null.</exception>
    public static string ToJson(this IEnumerable<AuditLogEntry> values, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(values);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(values, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a collection of AuditLogEntry instances.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A collection of AuditLogEntry instances, or empty collection if JSON is empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the JSON string is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static IReadOnlyList<AuditLogEntry> FromJsonToAuditLogEntries(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]")
        {
            return Array.Empty<AuditLogEntry>();
        }

        return JsonSerializer.Deserialize<AuditLogEntry[]>(json, _jsonOptions) ?? Array.Empty<AuditLogEntry>();
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a collection of AuditLogEntry instances.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="values">Receives the deserialized AuditLogEntry collection, or empty collection on failure.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the JSON string is null.</exception>
    public static bool TryFromJsonToAuditLogEntries(string json, out IReadOnlyList<AuditLogEntry> values)
    {
        ArgumentNullException.ThrowIfNull(json);

        values = Array.Empty<AuditLogEntry>();

        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]")
        {
            return true;
        }

        try
        {
            var result = JsonSerializer.Deserialize<AuditLogEntry[]>(json, _jsonOptions);
            values = result ?? Array.Empty<AuditLogEntry>();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}