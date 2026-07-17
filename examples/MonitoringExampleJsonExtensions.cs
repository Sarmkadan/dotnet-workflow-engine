using System;
using System.Text.Json;

/// <summary>
/// Provides JSON serialization helpers for <see cref="MonitoringExample"/>.
/// </summary>
public static class MonitoringExampleJsonExtensions
{
    // Cached options with camel‑case naming policy.
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Serializes the <see cref="MonitoringExample"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The instance to serialize.</param>
    /// <param name="indented">If <c>true</c>, the output JSON will be indented.</param>
    /// <returns>A JSON representation of <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    public static string ToJson(this MonitoringExample value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        // If indentation is requested, clone the cached options and enable WriteIndented.
        var options = indented
 ? new JsonSerializerOptions(_options) { WriteIndented = true }
 : _options;
        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string into a <see cref="MonitoringExample"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="MonitoringExample"/> instance, or <c>null</c> if deserialization returns <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <c>null</c>.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized into <see cref="MonitoringExample"/>.</exception>
    public static MonitoringExample? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize<MonitoringExample>(json, _options);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a <see cref="MonitoringExample"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">
    /// When this method returns, contains the deserialized <see cref="MonitoringExample"/> if deserialization succeeded;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <c>null</c>.</exception>
    public static bool TryFromJson(string json, out MonitoringExample? value)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            value = JsonSerializer.Deserialize<MonitoringExample>(json, _options);
            return value is not null;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
