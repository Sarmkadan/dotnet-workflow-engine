// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetWorkflowEngine.Utilities;

namespace DotNetWorkflowEngine.Formatters;

/// <summary>
/// Formats workflow data as JSON. Supports both single objects and collections.
/// Uses standardized JSON options for consistency across the application.
/// </summary>
public class JsonOutputFormatter : IOutputFormatter
{
    private readonly ILogger<JsonOutputFormatter> _logger;
    private readonly bool _prettyPrint;

    public string Format => "json";
    public string ContentType => "application/json";

    public JsonOutputFormatter(ILogger<JsonOutputFormatter> logger, bool prettyPrint = true)
    {
        _logger = logger;
        _prettyPrint = prettyPrint;
    }

    /// <summary>
    /// Formats a single object to JSON.
    /// </summary>
    public Task<string> FormatAsync<T>(T obj) where T : class
    {
        try
        {
            _logger.LogDebug("Formatting single object of type {Type} to JSON", typeof(T).Name);

            var options = _prettyPrint
                ? SerializationHelper.PrettyOptions
                : SerializationHelper.DefaultOptions;

            var json = JsonSerializer.Serialize(obj, options);
            return Task.FromResult(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting object to JSON");
            throw new FormatException($"Failed to format object to JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Formats a collection of objects to JSON array.
    /// </summary>
    public Task<string> FormatAsync<T>(IEnumerable<T> items) where T : class
    {
        try
        {
            _logger.LogDebug("Formatting collection of {Type} to JSON array", typeof(T).Name);

            var options = _prettyPrint
                ? SerializationHelper.PrettyOptions
                : SerializationHelper.DefaultOptions;

            var json = JsonSerializer.Serialize(items, options);
            return Task.FromResult(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting collection to JSON");
            throw new FormatException($"Failed to format collection to JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Formats a dictionary to JSON object.
    /// </summary>
    public Task<string> FormatAsync(Dictionary<string, object> data)
    {
        try
        {
            _logger.LogDebug("Formatting dictionary to JSON");

            var options = _prettyPrint
                ? SerializationHelper.PrettyOptions
                : SerializationHelper.DefaultOptions;

            var json = JsonSerializer.Serialize(data, options);
            return Task.FromResult(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting dictionary to JSON");
            throw new FormatException($"Failed to format dictionary to JSON: {ex.Message}", ex);
        }
    }
}
