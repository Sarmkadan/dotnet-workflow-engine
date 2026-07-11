// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DotNetWorkflowEngine.Utilities;

/// <summary>
/// Provides extension methods for string manipulation operations commonly used
/// throughout the workflow engine, including case conversion, validation,
/// truncation, and specialized parsing utilities.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts a string to PascalCase (UpperCamelCase).
    /// Example: "hello-world" -> "HelloWorld", "hello_world" -> "HelloWorld"
    /// </summary>
    /// <param name="input">The string to convert.</param>
    /// <returns>The PascalCase representation of the input string, or empty string if input is null or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is null.</exception>
    public static string ToPascalCase(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

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
    /// <param name="input">The string to convert.</param>
    /// <returns>The snake_case representation of the input string, or empty string if input is null or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is null.</exception>
    public static string ToSnakeCase(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrEmpty(input))
            return input;

        var result = Regex.Replace(input, "([a-z0-9])([A-Z])", "$1_$2");
        return result.Replace("-", "_").ToLowerInvariant();
    }

    /// <summary>
    /// Converts a string to kebab-case.
    /// Example: "HelloWorld" -> "hello-world", "hello_world" -> "hello-world"
    /// </summary>
    /// <param name="input">The string to convert.</param>
    /// <returns>The kebab-case representation of the input string, or empty string if input is null or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is null.</exception>
    public static string ToKebabCase(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

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
    /// <param name="input">The string to truncate.</param>
    /// <param name="maxLength">The maximum length of the resulting string.</param>
    /// <param name="suffix">Optional suffix to append when truncation occurs. Default is empty string.</param>
    /// <returns>The truncated string, or the original string if it's shorter than maxLength.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxLength"/> is negative.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is null.</exception>
    public static string Truncate(this string input, int maxLength, string suffix = "")
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

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
    /// <param name="input">The string to validate.</param>
    /// <returns>True if the string appears to be a valid email format; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is null.</exception>
    public static bool IsValidEmail(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

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
    /// <param name="input">The string to validate.</param>
    /// <returns>True if the string is a valid absolute URI; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is null.</exception>
    public static bool IsValidUrl(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return Uri.TryCreate(input, UriKind.Absolute, out var _);
    }

    /// <summary>
    /// Removes all whitespace from a string including spaces, tabs, and newlines.
    /// Example: "hello world" -> "helloworld"
    /// </summary>
    /// <param name="input">The string to process.</param>
    /// <returns>A new string with all whitespace removed, or empty string if input is null.</returns>
    public static string RemoveWhitespace(this string input)
    {
        return input is null
            ? string.Empty
            : Regex.Replace(input, @"\s+", string.Empty);
    }

    /// <summary>
    /// Normalizes whitespace in a string by replacing multiple spaces/tabs/newlines
    /// with single spaces and trimming leading/trailing whitespace.
    /// </summary>
    /// <param name="input">The string to normalize.</param>
    /// <returns>The normalized string, or empty string if input is null or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is null.</exception>
    public static string NormalizeWhitespace(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrEmpty(input))
            return input;

        return Regex.Replace(input.Trim(), @"\s+", " ");
    }

    /// <summary>
    /// Repeats a string N times.
    /// Example: "ab".Repeat(3) -> "ababab"
    /// </summary>
    /// <param name="input">The string to repeat.</param>
    /// <param name="count">The number of times to repeat the string.</param>
    /// <returns>A new string containing the input repeated count times.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is negative.</exception>
    public static string Repeat(this string input, int count)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (count == 0)
            return string.Empty;

        return string.Concat(Enumerable.Repeat(input, count));
    }

    /// <summary>
    /// Safely extracts a substring without throwing an exception if indices are out of bounds.
    /// </summary>
    /// <param name="input">The source string.</param>
    /// <param name="startIndex">The zero-based starting character position of the substring.</param>
    /// <param name="length">The number of characters in the substring. Use -1 for the rest of the string.</param>
    /// <returns>The extracted substring, or empty string if extraction is not possible.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is null.</exception>
    public static string SafeSubstring(this string input, int startIndex, int length = -1)
    {
        ArgumentNullException.ThrowIfNull(input);

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
    /// <param name="input">The source string to search within.</param>
    /// <param name="startDelimiter">The starting delimiter to search for.</param>
    /// <param name="endDelimiter">The ending delimiter to search for.</param>
    /// <returns>The extracted content between delimiters, or null if delimiters are not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
    public static string? ExtractBetween(this string input, string startDelimiter, string endDelimiter)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(startDelimiter);
        ArgumentNullException.ThrowIfNull(endDelimiter);

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
    /// <param name="input">The string to split.</param>
    /// <param name="delimiter">The delimiter to split by.</param>
    /// <returns>An enumerable of split string segments.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> or <paramref name="delimiter"/> is null.</exception>
    public static IEnumerable<string> SmartSplit(this string input, string delimiter)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(delimiter);

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