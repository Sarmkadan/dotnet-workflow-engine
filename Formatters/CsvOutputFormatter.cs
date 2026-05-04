// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkflowEngine.Utilities;

namespace DotNetWorkflowEngine.Formatters;

/// <summary>
/// Formats workflow data as CSV (Comma-Separated Values). Supports collections
/// by flattening properties into columns. Handles escaping of special characters
/// and CRLF line endings per RFC 4180.
/// </summary>
public class CsvOutputFormatter : IOutputFormatter
{
    private readonly ILogger<CsvOutputFormatter> _logger;
    private readonly string _delimiter;

    public string Format => "csv";
    public string ContentType => "text/csv";

    public CsvOutputFormatter(ILogger<CsvOutputFormatter> logger, string delimiter = ",")
    {
        _logger = logger;
        _delimiter = delimiter;
    }

    /// <summary>
    /// Formats a single object to CSV (returns single row).
    /// </summary>
    public async Task<string> FormatAsync<T>(T obj) where T : class
    {
        try
        {
            _logger.LogDebug("Formatting single object to CSV");
            return await FormatAsync(new[] { obj });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting object to CSV");
            throw new FormatException($"Failed to format object to CSV: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Formats a collection of objects to CSV with headers.
    /// Each object becomes a row, properties become columns.
    /// </summary>
    public Task<string> FormatAsync<T>(IEnumerable<T> items) where T : class
    {
        try
        {
            _logger.LogDebug("Formatting collection to CSV");

            var itemList = items.ToList();
            if (!itemList.Any())
                return Task.FromResult(string.Empty);

            var builder = new StringBuilder();
            var itemType = typeof(T);
            var properties = itemType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            // Write header row
            var headers = properties.Select(p => EscapeValue(p.Name));
            builder.AppendLine(string.Join(_delimiter, headers));

            // Write data rows
            foreach (var item in itemList)
            {
                var values = properties.Select(p =>
                {
                    try
                    {
                        var value = p.GetValue(item);
                        return EscapeValue(FormatValue(value));
                    }
                    catch
                    {
                        return "ERROR";
                    }
                });

                builder.AppendLine(string.Join(_delimiter, values));
            }

            return Task.FromResult(builder.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting collection to CSV");
            throw new FormatException($"Failed to format collection to CSV: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Formats a dictionary to CSV (single row).
    /// </summary>
    public Task<string> FormatAsync(Dictionary<string, object> data)
    {
        try
        {
            _logger.LogDebug("Formatting dictionary to CSV");

            var builder = new StringBuilder();

            // Header row
            var headers = data.Keys.Select(EscapeValue);
            builder.AppendLine(string.Join(_delimiter, headers));

            // Data row
            var values = data.Values.Select(v => EscapeValue(FormatValue(v)));
            builder.AppendLine(string.Join(_delimiter, values));

            return Task.FromResult(builder.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting dictionary to CSV");
            throw new FormatException($"Failed to format dictionary to CSV: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Escapes a CSV value by wrapping in quotes if it contains special characters,
    /// and escaping internal quotes per RFC 4180.
    /// </summary>
    private string EscapeValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // If value contains delimiter, quotes, or newlines, wrap in quotes and escape internal quotes
        if (value.Contains(_delimiter) || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    /// <summary>
    /// Formats an object value for CSV output, handling nulls and special types.
    /// </summary>
    private string FormatValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateTime dt => dt.ToString("O"), // ISO 8601 format
            bool b => b ? "true" : "false",
            IEnumerable<object> list => $"[{string.Join(", ", list.Select(FormatValue))}]",
            _ => value.ToString() ?? string.Empty
        };
    }
}
