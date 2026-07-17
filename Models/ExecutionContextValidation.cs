// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotNetWorkflowEngine.Models;

/// <summary>
/// Provides validation helpers for <see cref="ExecutionContext"/> instances.
/// </summary>
public static class ExecutionContextValidation
{
    /// <summary>
    /// Validates an execution context and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The execution context to validate.</param>
    /// <returns>A list of validation problems; empty if the context is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ExecutionContext value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate WorkflowInstanceId
        if (string.IsNullOrWhiteSpace(value.WorkflowInstanceId))
        {
            problems.Add("WorkflowInstanceId must not be null or whitespace.");
        }

        // Validate ActivityId (optional but must be valid if set)
        if (value.ActivityId is not null && string.IsNullOrWhiteSpace(value.ActivityId))
        {
            problems.Add("ActivityId must not be empty if set.");
        }

        // Validate CorrelationId
        if (string.IsNullOrWhiteSpace(value.CorrelationId))
        {
            problems.Add("CorrelationId must not be null or whitespace.");
        }

        // Validate dictionaries are not null
        if (value.Variables is null)
        {
            problems.Add("Variables dictionary must not be null.");
        }

        if (value.State is null)
        {
            problems.Add("State dictionary must not be null.");
        }

        if (value.ActivityInput is null)
        {
            problems.Add("ActivityInput dictionary must not be null.");
        }

        if (value.ActivityOutput is null)
        {
            problems.Add("ActivityOutput dictionary must not be null.");
        }

        if (value.Metadata is null)
        {
            problems.Add("Metadata dictionary must not be null.");
        }

        // Validate StartTime is not default (Unix epoch)
        if (value.StartTime == default)
        {
            problems.Add("StartTime must be set to a valid DateTime.");
        }

        // Validate EndTime (if set, should be after StartTime)
        if (value.EndTime.HasValue)
        {
            if (value.EndTime.Value < value.StartTime)
            {
                problems.Add("EndTime must be after StartTime when set.");
            }
        }

        // Validate ExecutionDurationMs
        if (value.EndTime.HasValue && !value.IsActive)
        {
            // If execution is complete, duration should be positive
            if (value.ExecutionDurationMs <= 0)
            {
                problems.Add("ExecutionDurationMs must be positive when execution is complete.");
            }
        }

        // Validate IsActive consistency with EndTime
        if (value.EndTime.HasValue && value.IsActive)
        {
            problems.Add("IsActive must be false when EndTime is set.");
        }

        if (!value.EndTime.HasValue && !value.IsActive)
        {
            problems.Add("IsActive must be true when EndTime is not set.");
        }

        // Validate ExecutionError (if set, execution should be inactive)
        if (value.ExecutionError is not null && value.IsActive)
        {
            problems.Add("ExecutionError can only be set when execution is inactive.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether an execution context is valid.
    /// </summary>
    /// <param name="value">The execution context to check.</param>
    /// <returns>True if the context is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this ExecutionContext value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that an execution context is valid, throwing an exception if it is not.
    /// </summary>
    /// <param name="value">The execution context to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the context contains validation problems.</exception>
    public static void EnsureValid(this ExecutionContext value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ExecutionContext is invalid. Problems:\n{string.Join("\n", problems)}");
        }
    }
}