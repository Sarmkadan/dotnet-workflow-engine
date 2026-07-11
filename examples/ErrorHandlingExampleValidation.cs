// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DotNetWorkflowEngine.Examples;

/// <summary>
/// Provides validation helpers for error handling workflow data models.
/// </summary>
public static class ErrorHandlingExampleValidation
{
    /// <summary>
    /// Validates a <see cref="ProcessingRequest"/> instance.
    /// </summary>
    /// <param name="value">The processing request to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ProcessingRequest value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate DataSourceUrl
        if (string.IsNullOrWhiteSpace(value.DataSourceUrl))
        {
            problems.Add("DataSourceUrl must not be null or whitespace.");
        }
        else if (!Uri.IsWellFormedUriString(value.DataSourceUrl, UriKind.Absolute))
        {
            problems.Add("DataSourceUrl must be a well-formed absolute URI.");
        }

        // Validate ProcessingRules
        if (value.ProcessingRules == null)
        {
            problems.Add("ProcessingRules must not be null.");
        }
        else if (value.ProcessingRules.Count == 0)
        {
            problems.Add("ProcessingRules must contain at least one rule.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="ProcessingRequest"/> is valid.
    /// </summary>
    /// <param name="value">The processing request to check.</param>
    /// <returns>True if the request is valid; otherwise, false.</returns>
    public static bool IsValid(this ProcessingRequest value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="ProcessingRequest"/> is valid.
    /// </summary>
    /// <param name="value">The processing request to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not valid.
    /// The exception message contains all validation problems.</exception>
    public static void EnsureValid(this ProcessingRequest value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ProcessingRequest is not valid. Problems: {string.Join(" ", problems)}");
        }
    }

    /// <summary>
    /// Validates a <see cref="ProcessingRequest"/> instance with additional context.
    /// </summary>
    /// <param name="value">The processing request to validate.</param>
    /// <param name="maxDataSourceUrlLength">Maximum allowed length for DataSourceUrl.</param>
    /// <param name="maxProcessingRulesCount">Maximum allowed count for ProcessingRules.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(
        this ProcessingRequest value,
        int maxDataSourceUrlLength,
        int maxProcessingRulesCount)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate DataSourceUrl
        if (string.IsNullOrWhiteSpace(value.DataSourceUrl))
        {
            problems.Add("DataSourceUrl must not be null or whitespace.");
        }
        else if (value.DataSourceUrl.Length > maxDataSourceUrlLength)
        {
            problems.Add(
                $"DataSourceUrl length must not exceed {maxDataSourceUrlLength} characters. Current: {value.DataSourceUrl.Length}.");
        }
        else if (!Uri.IsWellFormedUriString(value.DataSourceUrl, UriKind.Absolute))
        {
            problems.Add("DataSourceUrl must be a well-formed absolute URI.");
        }

        // Validate ProcessingRules
        if (value.ProcessingRules == null)
        {
            problems.Add("ProcessingRules must not be null.");
        }
        else if (value.ProcessingRules.Count == 0)
        {
            problems.Add("ProcessingRules must contain at least one rule.");
        }
        else if (value.ProcessingRules.Count > maxProcessingRulesCount)
        {
            problems.Add(
                $"ProcessingRules count must not exceed {maxProcessingRulesCount}. Current: {value.ProcessingRules.Count}.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates individual processing rules within a <see cref="ProcessingRequest"/>.
    /// </summary>
    /// <param name="value">The processing request containing rules to validate.</param>
    /// <returns>A list of human-readable validation problems for processing rules; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> ValidateProcessingRules(this ProcessingRequest value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (value.ProcessingRules == null)
        {
            return problems.AsReadOnly();
        }

        foreach (var (key, ruleValue) in value.ProcessingRules)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                problems.Add("Processing rule key must not be null or whitespace.");
            }

            if (ruleValue == null)
            {
                problems.Add($"Processing rule '{key}' must not be null.");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates that a processing rule value is of the expected type.
    /// </summary>
    /// <param name="processingRules">The processing rules dictionary.</param>
    /// <param name="ruleName">The name of the rule to validate.</param>
    /// <param name="expectedType">The expected type of the rule value.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="processingRules"/> is null.</exception>
    public static IReadOnlyList<string> ValidateRuleType(
        this IReadOnlyDictionary<string, object> processingRules,
        string ruleName,
        Type expectedType)
    {
        ArgumentNullException.ThrowIfNull(processingRules);
        ArgumentException.ThrowIfNullOrEmpty(ruleName);
        ArgumentNullException.ThrowIfNull(expectedType);

        var problems = new List<string>();

        if (!processingRules.TryGetValue(ruleName, out var ruleValue))
        {
            problems.Add($"Processing rule '{ruleName}' not found.");
            return problems.AsReadOnly();
        }

        if (ruleValue?.GetType() != expectedType)
        {
            problems.Add(
                $"Processing rule '{ruleName}' must be of type {expectedType.Name}. Found: {ruleValue?.GetType().Name ?? "null"}.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates that a processing rule value is a positive integer.
    /// </summary>
    /// <param name="processingRules">The processing rules dictionary.</param>
    /// <param name="ruleName">The name of the rule to validate.</param>
    /// <param name="minValue">Minimum allowed value (inclusive).</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="processingRules"/> is null.</exception>
    public static IReadOnlyList<string> ValidatePositiveIntegerRule(
        this IReadOnlyDictionary<string, object> processingRules,
        string ruleName,
        int minValue = 1)
    {
        ArgumentNullException.ThrowIfNull(processingRules);
        ArgumentException.ThrowIfNullOrEmpty(ruleName);

        var problems = new List<string>();

        if (!processingRules.TryGetValue(ruleName, out var ruleValue))
        {
            problems.Add($"Processing rule '{ruleName}' not found.");
            return problems.AsReadOnly();
        }

        int intValue;
        switch (ruleValue)
        {
            case int i:
                intValue = i;
                break;

            case string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed):
                intValue = parsed;
                break;

            case null:
                problems.Add($"Processing rule '{ruleName}' must not be null.");
                return problems.AsReadOnly();

            default:
                problems.Add(
                    $"Processing rule '{ruleName}' must be an integer or string parseable to integer. Found: {ruleValue?.GetType().Name ?? "null"}.");
                return problems.AsReadOnly();
        }

        if (intValue < minValue)
        {
            problems.Add(
                $"Processing rule '{ruleName}' must be at least {minValue}. Found: {intValue}.");
        }

        return problems.AsReadOnly();
    }
}
