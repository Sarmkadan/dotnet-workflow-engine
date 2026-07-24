// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Services;

/// <summary>
/// Provides System.Text.Json serialization and deserialization extensions for audit-related models.
/// </summary>
public static class AuditServiceJsonExtensions
{
    /// <summary>
    /// Maximum allowed JSON input size in bytes to prevent memory exhaustion attacks.
    /// Default: 1 MB (1024 * 1024 bytes)
    /// </summary>
    public static int MaxJsonInputSizeBytes { get; set; } = 1024 * 1024; // 1 MB

    /// <summary>
    /// Maximum allowed nesting depth for JSON to prevent stack overflow attacks.
    /// Default: 128 levels (significantly higher than typical JSON but prevents DoS)
    /// </summary>
    public static int MaxJsonNestingDepth { get; set; } = 128;

    private static JsonSerializerOptions CreateJsonSerializerOptions(bool indented)
    {
        return new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = indented,
            Converters = { new JsonStringEnumConverter() },
            MaxDepth = MaxJsonNestingDepth
        };
    }

    private static readonly JsonSerializerOptions _jsonOptionsCompact = CreateJsonSerializerOptions(false);
    private static readonly JsonSerializerOptions _jsonOptionsIndented = CreateJsonSerializerOptions(true);

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

        return JsonSerializer.Serialize(value, indented ? _jsonOptionsIndented : _jsonOptionsCompact);
    }

    /// <summary>
    /// Validates JSON input size to prevent memory exhaustion attacks.
    /// </summary>
    /// <param name="json">The JSON string to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the JSON size exceeds the maximum allowed limit.</exception>
    private static void ValidateJsonInputSize(string json)
    {
        if (json.Length > MaxJsonInputSizeBytes)
        {
            throw new ArgumentException(
                $"JSON input size ({json.Length} bytes) exceeds maximum allowed size ({MaxJsonInputSizeBytes} bytes). " +
                "This may indicate a potential denial-of-service attack through large payloads.",
                nameof(json));
        }
    }

    /// <summary>
    /// Deserializes a JSON string to an AuditLogEntry instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>An AuditLogEntry instance, or null if the JSON is empty or whitespace.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the JSON string is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the JSON size exceeds the maximum allowed limit.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static AuditLogEntry? FromJsonToAuditLogEntry(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        ValidateJsonInputSize(json);

        return JsonSerializer.Deserialize<AuditLogEntry>(json, _jsonOptionsCompact);
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
            ValidateJsonInputSize(json);
            value = JsonSerializer.Deserialize<AuditLogEntry>(json, _jsonOptionsCompact);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
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

        return JsonSerializer.Serialize(values, indented ? _jsonOptionsIndented : _jsonOptionsCompact);
    }

    /// <summary>
    /// Deserializes a JSON string to a collection of AuditLogEntry instances.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A collection of AuditLogEntry instances, or empty collection if JSON is empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the JSON string is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the JSON size exceeds the maximum allowed limit.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static IReadOnlyList<AuditLogEntry> FromJsonToAuditLogEntries(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]")
        {
            return Array.Empty<AuditLogEntry>();
        }

        ValidateJsonInputSize(json);

        return JsonSerializer.Deserialize<AuditLogEntry[]>(json, _jsonOptionsCompact) ?? Array.Empty<AuditLogEntry>();
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
            ValidateJsonInputSize(json);
            var result = JsonSerializer.Deserialize<AuditLogEntry[]>(json, _jsonOptionsCompact);
            values = result ?? Array.Empty<AuditLogEntry>();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }
}