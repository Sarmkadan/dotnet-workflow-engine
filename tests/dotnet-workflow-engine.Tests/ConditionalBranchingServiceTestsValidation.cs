// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using DotNetWorkflowEngine.Services;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Tests;

/// <summary>
/// Provides validation helpers for <see cref="ConditionalBranchingService"/> instances.
/// </summary>
public static class ConditionalBranchingServiceTestsValidation
{
    /// <summary>
    /// Validates that a <see cref="ConditionalBranchingService"/> instance is in a valid state.
    /// </summary>
    /// <param name="value">The service instance to validate.</param>
    /// <returns>
    /// An <see cref="IReadOnlyList{T}"/> of human-readable validation problems;
    /// empty when the instance is valid.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> is <see langword="null"/>.
    /// </exception>
    public static IReadOnlyList<string> Validate(this ConditionalBranchingService value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // ConditionalBranchingService only has a logger dependency which is validated in constructor
        // No additional validation needed beyond null check for the service instance itself

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="ConditionalBranchingService"/> instance is valid.
    /// </summary>
    /// <param name="value">The service instance to check.</param>
    /// <returns>
    /// <see langword="true"/> when the instance is valid; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsValid(this ConditionalBranchingService value)
        => value?.Validate().Count == 0;

    /// <summary>
    /// Ensures that a <see cref="ConditionalBranchingService"/> instance is valid,
    /// throwing an <see cref="ArgumentException"/> with a detailed message when it is not.
    /// </summary>
    /// <param name="value">The service instance to validate.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the instance is invalid, containing a list of all validation problems.
    /// </exception>
    public static void EnsureValid(this ConditionalBranchingService value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ConditionalBranchingService is invalid:{Environment.NewLine}" +
                string.Join(Environment.NewLine, problems.Select(p => $" - {p}")),
                nameof(value));
        }
    }
}