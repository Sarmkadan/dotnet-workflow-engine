// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
//
// System.Text.Json serialization extensions for rate limiting middleware
// Provides JSON serialization/deserialization capabilities for rate limit configuration
// =============================================================================

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetWorkflowEngine.Middleware;

/// <summary>
/// Rate limit configuration DTO for serialization.
/// This mirrors the RateLimitConfig class from RateLimitingMiddleware.
/// </summary>
public class RateLimitConfig
{
    /// <summary>
    /// Maximum number of requests allowed in the time window.
    /// </summary>
    public int MaxRequests { get; set; } = 100;

    /// <summary>
    /// Time window in seconds for rate limiting.
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Seconds to wait before retrying after rate limit is exceeded.
    /// </summary>
    public int RetryAfterSeconds { get; set; } = 60;
}

/// <summary>
/// Provides System.Text.Json serialization extensions for rate limiting configuration.
/// Enables serialization and deserialization of rate limiting configuration.
/// </summary>
public static class RateLimitingMiddlewareJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Serializes the <see cref="RateLimitConfig"/> to a JSON string.
    /// </summary>
    /// <param name="value">The rate limit configuration to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this RateLimitConfig value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonSerializerOptions) { WriteIndented = true }
            : _jsonSerializerOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="RateLimitConfig"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A deserialized <see cref="RateLimitConfig"/> instance, or null if deserialization fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or whitespace.</exception>
    public static RateLimitConfig? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        try
        {
            return JsonSerializer.Deserialize<RateLimitConfig>(json, _jsonSerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="RateLimitConfig"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized instance if successful.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out RateLimitConfig? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            value = JsonSerializer.Deserialize<RateLimitConfig>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}