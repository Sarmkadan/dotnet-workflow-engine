using System.Collections.Generic;

namespace DotNetWorkflowEngine.Cli;

/// <summary>
/// Represents the parsed context of a CLI command invocation, including the
/// command name, arguments, options, and output preferences.
/// </summary>
public class CommandContext
{
    /// <summary>Gets or sets the name of the command being executed.</summary>
    public string CommandName { get; set; } = string.Empty;

    /// <summary>Gets or sets the positional arguments passed to the command.</summary>
    public List<string> Arguments { get; set; } = new();

    /// <summary>Gets or sets the named options passed to the command, keyed by lower-cased option name.</summary>
    public Dictionary<string, string> Options { get; set; } = new();

    /// <summary>Gets or sets the desired output format (for example, "text" or "json").</summary>
    public string OutputFormat { get; set; } = "text";

    /// <summary>Gets or sets a value indicating whether verbose output is enabled.</summary>
    public bool IsVerbose { get; set; }

    /// <summary>Gets or sets the user executing the command, if known.</summary>
    public string? ExecutingUser { get; set; }

    /// <summary>
    /// Gets the value of the specified option, or <see langword="null"/> if it is not present.
    /// </summary>
    /// <param name="key">The option name to look up (matched case-insensitively).</param>
    /// <returns>The option value, or <see langword="null"/> if the option is not set.</returns>
    public string? GetOption(string key)
    {
        var normalizedKey = key.ToLowerInvariant();
        return Options.TryGetValue(normalizedKey, out var value) ? value : null;
    }

    /// <summary>
    /// Determines whether the specified flag is present and set to a truthy value.
    /// </summary>
    /// <param name="flagName">The name of the flag to check (matched case-insensitively).</param>
    /// <returns><see langword="true"/> if the flag exists and its value is "true", "1", empty string, or "yes"; otherwise, <see langword="false"/>.</returns>
    public bool HasFlag(string flagName)
    {
        var normalizedKey = flagName.ToLowerInvariant();
        if (!Options.TryGetValue(normalizedKey, out var value))
            return false;

        return value.ToLowerInvariant() is "true" or "1" or "" or "yes";
    }

    /// <summary>
    /// Validates that the command has at least the specified number of arguments.
    /// </summary>
    /// <param name="expectedCount">The minimum number of arguments required.</param>
    /// <returns><see langword="true"/> if the argument count meets or exceeds the expected count; otherwise, <see langword="false"/>.</returns>
    public bool ValidateArguments(int expectedCount)
    {
        return Arguments.Count >= expectedCount;
    }

    /// <summary>
    /// Returns a string representation of the command context for debugging purposes.
    /// </summary>
    /// <returns>A formatted string showing all properties of the command context.</returns>
    public override string ToString() => $"CommandContext {{ CommandName = {CommandName}, Arguments = {Arguments}, Options = {Options}, OutputFormat = {OutputFormat}, IsVerbose = {IsVerbose}, ExecutingUser = {ExecutingUser} }}";
}
