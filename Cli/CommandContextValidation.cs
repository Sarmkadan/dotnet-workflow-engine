// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
//
// Validation helpers for CommandContext to ensure data integrity before execution.
// Provides comprehensive validation with human-readable error messages.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotNetWorkflowEngine.Cli;

/// <summary>
/// Provides validation extensions for <see cref="CommandContext"/> to validate
/// command execution contexts before processing.
/// </summary>
public static class CommandContextValidation
{
    /// <summary>
    /// Validates the command context and returns a list of human-readable problems.
    /// Returns an empty list if the context is valid.
    /// </summary>
    /// <param name="value">The command context to validate.</param>
    /// <returns>List of validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static IReadOnlyList<string> Validate(this CommandContext value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate CommandName
        if (string.IsNullOrWhiteSpace(value.CommandName))
        {
            problems.Add("CommandName cannot be null, empty, or whitespace.");
        }
        else if (value.CommandName.Length > 100)
        {
            problems.Add("CommandName exceeds maximum length of 100 characters.");
        }

        // Validate Arguments
        if (value.Arguments is null)
        {
            problems.Add("Arguments collection cannot be null.");
        }
        else
        {
            for (int i = 0; i < value.Arguments.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(value.Arguments[i]))
                {
                    problems.Add($"Arguments[{i}] cannot be null, empty, or whitespace.");
                }
                else if (value.Arguments[i].Length > 1000)
                {
                    problems.Add($"Arguments[{i}] exceeds maximum length of 1000 characters.");
                }
            }
        }

        // Validate Options
        if (value.Options is null)
        {
            problems.Add("Options dictionary cannot be null.");
        }
        else
        {
            foreach (var kvp in value.Options)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                {
                    problems.Add("Options dictionary contains a null or empty key.");
                    break;
                }

                if (kvp.Key.Length > 100)
                {
                    problems.Add($"Options key '{kvp.Key}' exceeds maximum length of 100 characters.");
                    break;
                }

                if (string.IsNullOrWhiteSpace(kvp.Value))
                {
                    problems.Add($"Options['{kvp.Key}'] cannot be null or empty.");
                }
                else if (kvp.Value.Length > 1000)
                {
                    problems.Add($"Options['{kvp.Key}'] exceeds maximum length of 1000 characters.");
                }
            }
        }

        // Validate OutputFormat
        if (!string.IsNullOrEmpty(value.OutputFormat))
        {
            var validFormats = new[] { "text", "json", "csv", "xml" };
            if (Array.IndexOf(validFormats, value.OutputFormat.ToLowerInvariant()) < 0)
            {
                problems.Add($"OutputFormat '{value.OutputFormat}' is not a valid format. Valid values: text, json, csv, xml.");
            }
        }

        // Validate ExecutingUser
        if (value.ExecutingUser is not null)
        {
            if (string.IsNullOrWhiteSpace(value.ExecutingUser))
            {
                problems.Add("ExecutingUser cannot be empty or whitespace when set.");
            }
            else if (value.ExecutingUser.Length > 100)
            {
                problems.Add("ExecutingUser exceeds maximum length of 100 characters.");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the command context is valid.
    /// </summary>
    /// <param name="value">The command context to check.</param>
    /// <returns>True if valid; false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static bool IsValid(this CommandContext value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures the command context is valid, throwing an <see cref="ArgumentException"/>
    /// with a detailed message listing all validation problems if invalid.
    /// </summary>
    /// <param name="value">The command context to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    /// <exception cref="ArgumentException">Thrown if value is invalid with detailed problems.</exception>
    public static void EnsureValid(this CommandContext value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();

        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"CommandContext is invalid. Problems: {problems.Count}. Details:\n- {
                string.Join("\n- ", problems)
            }");
    }
}