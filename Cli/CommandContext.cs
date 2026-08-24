using System.Collections.Generic;

namespace DotNetWorkflowEngine.Cli;

public class CommandContext
{
    public string CommandName { get; set; } = string.Empty;
    public List<string> Arguments { get; set; } = new();
    public Dictionary<string, string> Options { get; set; } = new();
    public string OutputFormat { get; set; } = "text";
    public bool IsVerbose { get; set; }
    public string? ExecutingUser { get; set; }

    public string? GetOption(string key)
    {
        var normalizedKey = key.ToLowerInvariant();
        return Options.TryGetValue(normalizedKey, out var value) ? value : null;
    }

    public bool HasFlag(string flagName)
    {
        var normalizedKey = flagName.ToLowerInvariant();
        if (!Options.TryGetValue(normalizedKey, out var value))
            return false;

        return value.ToLowerInvariant() is "true" or "1" or "" or "yes";
    }

    public bool ValidateArguments(int expectedCount)
    {
        return Arguments.Count >= expectedCount;
    }

    public override string ToString() => $"CommandContext {{ CommandName = {CommandName}, Arguments = {Arguments}, Options = {Options}, OutputFormat = {OutputFormat}, IsVerbose = {IsVerbose}, ExecutingUser = {ExecutingUser} }}";
}
