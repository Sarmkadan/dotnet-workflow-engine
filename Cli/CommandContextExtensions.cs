// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetWorkflowEngine.Cli;

/// <summary>
/// Extension methods for CommandContext providing convenient utility methods
/// for common CLI operations and argument parsing.
/// </summary>
public static class CommandContextExtensions
{
    /// <summary>
    /// Gets the first argument at the specified index, or null if not present.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="index">The zero-based argument index.</param>
    /// <returns>The argument value or null if the index is out of range.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> is null.</exception>
    public static string? GetArgument(this CommandContext context, int index)
    {
        ArgumentNullException.ThrowIfNull(context);

        return index >= 0 && index < context.Arguments.Count
            ? context.Arguments[index]
            : null;
    }

    /// <summary>
    /// Gets all arguments starting from the specified index as a space-separated string.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="startIndex">The starting index (inclusive). Must be non-negative.</param>
    /// <returns>Space-separated arguments from the starting index, or empty string if startIndex is invalid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="startIndex"/> is negative.</exception>
    public static string GetArgumentsFrom(this CommandContext context, int startIndex)
    {
        ArgumentNullException.ThrowIfNull(context);

        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);

        if (startIndex >= context.Arguments.Count)
            return string.Empty;

        return string.Join(" ", context.Arguments.Skip(startIndex));
    }

    /// <summary>
    /// Gets an option value with a default fallback if the option is not present.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="key">The option key.</param>
    /// <param name="defaultValue">The default value to return if option is missing.</param>
    /// <returns>The option value or the specified default value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> is null.</exception>
    public static string GetOptionOrDefault(this CommandContext context, string key, string defaultValue = "")
    {
        ArgumentNullException.ThrowIfNull(context);

        ArgumentException.ThrowIfNullOrEmpty(key);

        var value = context.GetOption(key);
        return value ?? defaultValue;
    }

    /// <summary>
    /// Validates that required arguments are present and returns a formatted error message if validation fails.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="expectedCount">The minimum number of required arguments.</param>
    /// <param name="argumentNames">Descriptive names of the arguments for error messages.</param>
    /// <returns>Error message if validation fails, otherwise null.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="argumentNames"/> is null.</exception>
    public static string? ValidateRequiredArguments(this CommandContext context, int expectedCount, params string[] argumentNames)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(argumentNames);

        if (context.ValidateArguments(expectedCount))
            return null;

        var missingCount = expectedCount - context.Arguments.Count;
        var missingNames = argumentNames.Length >= missingCount
            ? argumentNames.Take(missingCount).ToArray()
            : argumentNames;

        return $"Missing required arguments: {string.Join(", ", missingNames)}";
    }
}