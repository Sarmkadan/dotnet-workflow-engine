// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Generic;

namespace DotNetWorkflowEngine.Cli;

/// <summary>
/// Encapsulates the execution context for a CLI command, including arguments,
/// options, and output configuration. This abstraction allows commands to be
/// executed in different environments (console, REST API, testing).
/// </summary>
public class CommandContext
{
    /// <summary>
    /// The command being executed (e.g., "create-workflow", "execute-instance").
    /// </summary>
    public string CommandName { get; set; } = string.Empty;

    /// <summary>
    /// Positional arguments after the command name.
    /// </summary>
    public List<string> Arguments { get; set; } = new();

    /// <summary>
    /// Named options/flags passed via --key value or --flag syntax.
    /// </summary>
    public Dictionary<string, string> Options { get; set; } = new();

    /// <summary>
    /// Output format (json, csv, text). Defaults to text for console compatibility.
    /// </summary>
    public string OutputFormat { get; set; } = "text";

    /// <summary>
    /// Indicates whether verbose/debug output is requested.
    /// </summary>
    public bool IsVerbose { get; set; }

    /// <summary>
    /// The user/identity executing the command. Used for audit logging.
    /// </summary>
    public string? ExecutingUser { get; set; }

    /// <summary>
    /// Retrieves an option value by key, returning null if not present.
    /// This method provides case-insensitive access to options.
    /// </summary>
    public string? GetOption(string key)
    {
        var normalizedKey = key.ToLowerInvariant();
        return Options.TryGetValue(normalizedKey, out var value) ? value : null;
    }

    /// <summary>
    /// Checks if a flag option is present and not explicitly set to false.
    /// </summary>
    public bool HasFlag(string flagName)
    {
        var normalizedKey = flagName.ToLowerInvariant();
        if (!Options.TryGetValue(normalizedKey, out var value))
            return false;

        return value.ToLowerInvariant() is "true" or "1" or "" or "yes";
    }

    /// <summary>
    /// Validates that all required arguments are present.
    /// </summary>
    public bool ValidateArguments(int expectedCount)
    {
        return Arguments.Count >= expectedCount;
    }
}
