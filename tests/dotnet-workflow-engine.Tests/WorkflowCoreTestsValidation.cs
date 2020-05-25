// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.ObjectModel;

namespace DotNetWorkflowEngine.Tests;

/// <summary>
/// Provides validation helpers for <see cref="WorkflowCoreTests"/> instances.
/// </summary>
public static class WorkflowCoreTestsValidation
{
    /// <summary>
    /// Validates a <see cref="WorkflowCoreTests"/> instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>A read-only list of validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this WorkflowCoreTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // WorkflowCoreTests is a test class with no instance state to validate
        // All validation is compile-time (the class itself is always valid)

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="WorkflowCoreTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this WorkflowCoreTests? value)
    {
        return value is not null && Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that a <see cref="WorkflowCoreTests"/> instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is invalid, listing all problems.</exception>
    public static void EnsureValid(this WorkflowCoreTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"WorkflowCoreTests instance is invalid. Problems: {string.Join("; ", problems)}",
            nameof(value));
    }
}
