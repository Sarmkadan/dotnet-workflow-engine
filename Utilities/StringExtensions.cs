// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DotNetWorkflowEngine.Utilities;

/// <summary>
/// String extension methods providing common text manipulation operations
/// used throughout the workflow engine. Includes case conversion, validation,
/// truncation, and specialized parsing utilities.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts a string to PascalCase (UpperCamelCase).
    /// Example: "hello-world" -> "HelloWorld", "hello_world" -> "HelloWorld"
    /// </summary>
    public static string ToPascalCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var parts = Regex.Split(input, @"[-_\s]+");
        return string.Concat(parts.Select(part =>
            part.Length > 0
                ? char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()
                : string.Empty));
    }

    /// <summary>
    /// Converts a string to snake_case.
    /// Example: "HelloWorld" -> "hello_world", "hello-world" -> "hello_world"
    /// </summary>
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = Regex.Replace(input, "([a-z0-9])([A-Z])", "$1_$2");
        return result.Replace("-", "_").ToLowerInvariant();
    }

    /// <summary>
    /// Converts a string to kebab-case.
    /// Example: "HelloWorld" -> "hello-world", "hello_world" -> "hello-world"
    /// </summary>
    public static string ToKebabCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = Regex.Replace(input, "([a-z0-9])([A-Z])", "$1-$2");
        return result.Replace("_", "-").ToLowerInvariant();
    }

    /// <summary>
    /// Truncates a string to a maximum length, optionally adding an ellipsis suffix.
    /// Example: "Hello World".Truncate(5) -> "Hello"
    /// Example: "Hello World".Truncate(5, "...") -> "He..."
    /// </summary>
    public static string Truncate(this string input, int maxLength, string suffix = "")
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input;

        var truncated = input[..maxLength];
        return suffix.Length > 0 && truncated.Length >= suffix.Length
            ? truncated[..(maxLength - suffix.Length)] + suffix
            : truncated;
    }

    /// <summary>
    /// Checks if a string is a valid email address using a simple regex pattern.
    /// Note: This is a pragmatic check, not RFC-compliant validation.
    /// </summary>
    public static bool IsValidEmail(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        try
        {
            var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(input, pattern);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a string is a valid URL format.
    /// </summary>
    public static bool IsValidUrl(this string input)
    {
        return Uri.TryCreate(input, UriKind.Absolute, out var _);
    }

    /// <summary>
    /// Removes all whitespace from a string including spaces, tabs, and newlines.
    /// Example: "hello world" -> "helloworld"
    /// </summary>
    public static string RemoveWhitespace(this string input)
    {
        return Regex.Replace(input ?? string.Empty, @"\s+", string.Empty);
    }

    /// <summary>
    /// Normalizes whitespace in a string by replacing multiple spaces/tabs/newlines
    /// with single spaces and trimming leading/trailing whitespace.
    /// </summary>
    public static string NormalizeWhitespace(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return Regex.Replace(input.Trim(), @"\s+", " ");
    }

    /// <summary>
    /// Repeats a string N times.
    /// Example: "ab".Repeat(3) -> "ababab"
    /// </summary>
    public static string Repeat(this string input, int count)
    {
        if (count <= 0)
            return string.Empty;

        var builder = new StringBuilder();
        for (int i = 0; i < count; i++)
            builder.Append(input);

        return builder.ToString();
    }

    /// <summary>
    /// Safely extracts a substring without throwing an exception if indices are out of bounds.
    /// </summary>
    public static string SafeSubstring(this string input, int startIndex, int length = -1)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        if (startIndex >= input.Length)
            return string.Empty;

        if (startIndex < 0)
            startIndex = 0;

        if (length < 0)
            length = input.Length - startIndex;
        else
            length = Math.Min(length, input.Length - startIndex);

        return input.Substring(startIndex, length);
    }

    /// <summary>
    /// Extracts the content between two delimiters.
    /// Example: "prefix[content]suffix".ExtractBetween("[", "]") -> "content"
    /// </summary>
    public static string? ExtractBetween(this string input, string startDelimiter, string endDelimiter)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(startDelimiter) || string.IsNullOrEmpty(endDelimiter))
            return null;

        var startIndex = input.IndexOf(startDelimiter, StringComparison.Ordinal);
        if (startIndex < 0)
            return null;

        startIndex += startDelimiter.Length;

        var endIndex = input.IndexOf(endDelimiter, startIndex, StringComparison.Ordinal);
        if (endIndex < 0)
            return null;

        return input[startIndex..endIndex];
    }

    /// <summary>
    /// Splits a string by a delimiter while respecting quoted sections.
    /// Example: "a,b,\"c,d\",e".SmartSplit(",") -> ["a", "b", "\"c,d\"", "e"]
    /// </summary>
    public static IEnumerable<string> SmartSplit(this string input, string delimiter)
    {
        if (string.IsNullOrEmpty(input))
            yield break;

        var current = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '"')
            {
                inQuotes = !inQuotes;
                current.Append(input[i]);
            }
            else if (!inQuotes && input.Substring(i).StartsWith(delimiter, StringComparison.Ordinal))
            {
                yield return current.ToString();
                current.Clear();
                i += delimiter.Length - 1;
            }
            else
            {
                current.Append(input[i]);
            }
        }

        yield return current.ToString();
    }
}
