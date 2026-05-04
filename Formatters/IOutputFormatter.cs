// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotNetWorkflowEngine.Formatters;

/// <summary>
/// Interface for output formatters that convert workflow data to various formats.
/// Implementations should be stateless and thread-safe.
/// </summary>
public interface IOutputFormatter
{
    /// <summary>
    /// Gets the format identifier (e.g., "json", "csv", "xml").
    /// </summary>
    string Format { get; }

    /// <summary>
    /// Gets the MIME type for this format (e.g., "application/json").
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// Formats a single object to the target format.
    /// </summary>
    Task<string> FormatAsync<T>(T obj) where T : class;

    /// <summary>
    /// Formats a collection of objects to the target format.
    /// </summary>
    Task<string> FormatAsync<T>(IEnumerable<T> items) where T : class;

    /// <summary>
    /// Formats raw dictionary data to the target format.
    /// Useful for untyped or dynamic data.
    /// </summary>
    Task<string> FormatAsync(Dictionary<string, object> data);
}
