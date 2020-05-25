// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
//
// Validation helpers for ValidationErrorResponse and related validation types.
// Provides comprehensive validation for filter responses and validation attributes.
// =============================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace DotNetWorkflowEngine.Filters;

/// <summary>
/// Provides validation extension methods for <see cref="ValidationErrorResponse"/> and validation-related types.
/// </summary>
public static class ValidationFilterValidation
{
    /// <summary>
    /// Validates a <see cref="ValidationErrorResponse"/> instance and returns any validation problems.
    /// </summary>
    /// <param name="value">The validation error response to validate.</param>
    /// <returns>A list of human-readable validation problems, or empty list if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ValidationErrorResponse? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate Message
        if (string.IsNullOrWhiteSpace(value.Message))
        {
            problems.Add("Message cannot be null, empty, or whitespace.");
        }

        // Validate Errors collection
        if (value.Errors == null)
        {
            problems.Add("Errors collection cannot be null.");
        }
        else if (value.Errors.Count == 0)
        {
            problems.Add("Errors collection cannot be empty.");
        }
        else
        {
            // Validate each error entry
            foreach (var error in value.Errors)
            {
                if (string.IsNullOrWhiteSpace(error.Key))
                {
                    problems.Add("Error entry key cannot be null, empty, or whitespace.");
                    break;
                }

                if (error.Value == null || error.Value.Length == 0)
                {
                    problems.Add($"Error entry for key '{error.Key}' has null or empty error messages array.");
                    break;
                }

                foreach (var message in error.Value)
                {
                    if (string.IsNullOrWhiteSpace(message))
                    {
                        problems.Add($"Error entry for key '{error.Key}' contains null, empty, or whitespace error message.");
                        break;
                    }
                }
            }
        }

        // Validate Timestamp (should not be default DateTime)
        if (value.Timestamp == default)
        {
            problems.Add("Timestamp cannot be default(DateTime).");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="ValidationErrorResponse"/> instance is valid.
    /// </summary>
    /// <param name="value">The validation error response to check.</param>
    /// <returns>True if the instance is valid; otherwise, false.</returns>
    public static bool IsValid(this ValidationErrorResponse? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that a <see cref="ValidationErrorResponse"/> instance is valid, throwing an exception if it is not.
    /// </summary>
    /// <param name="value">The validation error response to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid, containing a list of validation problems.</exception>
    public static void EnsureValid(this ValidationErrorResponse? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ValidationErrorResponse is not valid. Problems: {string.Join(" ", problems)}",
                nameof(value));
        }
    }

    /// <summary>
    /// Validates an <see cref="AllowedValuesAttribute"/> instance and returns any validation problems.
    /// </summary>
    /// <param name="attribute">The allowed values attribute to validate.</param>
    /// <returns>A list of human-readable validation problems, or empty list if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="attribute"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this AllowedValuesAttribute? attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        var problems = new List<string>();

        // Use reflection to access the private _allowedValues field
        var field = typeof(AllowedValuesAttribute).GetField(
            "_allowedValues",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field == null)
        {
            problems.Add("Could not access AllowedValuesAttribute._allowedValues field.");
            return problems.AsReadOnly();
        }

        var allowedValues = field.GetValue(attribute) as string[];

        if (allowedValues == null || allowedValues.Length == 0)
        {
            problems.Add("AllowedValuesAttribute must have at least one allowed value.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether an <see cref="AllowedValuesAttribute"/> instance is valid.
    /// </summary>
    /// <param name="attribute">The allowed values attribute to check.</param>
    /// <returns>True if the instance is valid; otherwise, false.</returns>
    public static bool IsValid(this AllowedValuesAttribute? attribute)
    {
        return attribute?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that an <see cref="AllowedValuesAttribute"/> instance is valid, throwing an exception if it is not.
    /// </summary>
    /// <param name="attribute">The allowed values attribute to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="attribute"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="attribute"/> is not valid, containing a list of validation problems.</exception>
    public static void EnsureValid(this AllowedValuesAttribute? attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        var problems = attribute.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"AllowedValuesAttribute is not valid. Problems: {string.Join(" ", problems)}",
                nameof(attribute));
        }
    }

    /// <summary>
    /// Validates a string value against <see cref="NotEmptyOrWhitespaceAttribute"/> rules.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <returns>A list of human-readable validation problems, or empty list if valid.</returns>
    public static IReadOnlyList<string> Validate(this string? value, string? fieldName = null)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(value))
        {
            problems.Add(fieldName == null
                ? "String value cannot be null, empty, or contain only whitespace."
                : $"Field '{fieldName}' cannot be null, empty, or contain only whitespace.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a string value is valid (not null, empty, or whitespace).
    /// </summary>
    /// <param name="value">The string value to check.</param>
    /// <returns>True if the string is valid; otherwise, false.</returns>
    public static bool IsValid(this string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Ensures that a string value is valid (not null, empty, or whitespace), throwing an exception if it is not.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid.</exception>
    public static void EnsureValid(this string? value, string? paramName = null)
    {
        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(problems[0], paramName ?? nameof(value));
        }
    }

    /// <summary>
    /// Validates a DateTime value against default value rules.
    /// </summary>
    /// <param name="value">The DateTime value to validate.</param>
    /// <param name="fieldName">Optional field name for error messages.</param>
    /// <returns>A list of human-readable validation problems, or empty list if valid.</returns>
    public static IReadOnlyList<string> Validate(this DateTime value, string? fieldName = null)
    {
        var problems = new List<string>();

        if (value == default)
        {
            problems.Add(fieldName == null
                ? "DateTime value cannot be default(DateTime)."
                : $"Field '{fieldName}' cannot be default(DateTime).");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a DateTime value is valid (not default).
    /// </summary>
    /// <param name="value">The DateTime value to check.</param>
    /// <returns>True if the DateTime is valid; otherwise, false.</returns>
    public static bool IsValid(this DateTime value)
    {
        return value != default;
    }

    /// <summary>
    /// Ensures that a DateTime value is valid (not default), throwing an exception if it is not.
    /// </summary>
    /// <param name="value">The DateTime value to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid.</exception>
    public static void EnsureValid(this DateTime value, string? paramName = null)
    {
        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(problems[0], paramName ?? nameof(value));
        }
    }

    /// <summary>
    /// Validates a collection of key-value pairs (errors format).
    /// </summary>
    /// <param name="errors">The errors collection to validate.</param>
    /// <returns>A list of human-readable validation problems, or empty list if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="errors"/> is null.</exception>
    public static IReadOnlyList<string> Validate(
        this IReadOnlyList<KeyValuePair<string, string[]>>? errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var problems = new List<string>();

        if (errors.Count == 0)
        {
            problems.Add("Errors collection cannot be empty.");
        }
        else
        {
            foreach (var error in errors)
            {
                if (string.IsNullOrWhiteSpace(error.Key))
                {
                    problems.Add("Error entry key cannot be null, empty, or whitespace.");
                    break;
                }

                if (error.Value == null || error.Value.Length == 0)
                {
                    problems.Add($"Error entry for key '{error.Key}' has null or empty error messages array.");
                    break;
                }

                foreach (var message in error.Value)
                {
                    if (string.IsNullOrWhiteSpace(message))
                    {
                        problems.Add($"Error entry for key '{error.Key}' contains null, empty, or whitespace error message.");
                        break;
                    }
                }
            }
        }

        return problems.AsReadOnly();
    }
}