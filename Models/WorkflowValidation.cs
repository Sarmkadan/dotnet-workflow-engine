// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
//
// Workflow validation utilities for comprehensive validation of Workflow instances
// =============================================================================

using System.Globalization;
using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Models;

/// <summary>
/// Provides comprehensive validation utilities for <see cref="Workflow"/> instances.
/// Contains methods to validate workflow definitions and check their validity state.
/// </summary>
/// <remarks>
/// <para>
/// This class implements the validation contract for workflows by checking:
/// <list type="bullet">
/// <item>Required string properties (Id, Name) for null/empty/whitespace</item>
/// <item>Version number validity (must be positive)</item>
/// <item>Status constraints based on workflow lifecycle</item>
/// <item>Collection validations (Activities, Transitions)</item>
/// <item>Date/time validations (CreatedAt, ModifiedAt)</item>
/// <item>Reference integrity (StartActivityId, EndActivityId, transitions)</item>
/// </list>
/// </para>
/// </remarks>
public static class WorkflowValidation
{
    /// <summary>
    /// Validates a workflow instance and returns a list of human-readable error messages.
    /// </summary>
    /// <param name="value">The workflow instance to validate.</param>
    /// <returns>An <see cref="IReadOnlyList{T}"/> of validation error messages. Empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this Workflow value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate required string properties
        ValidateRequiredString(value.Id, nameof(value.Id), errors);
        ValidateRequiredString(value.Name, nameof(value.Name), errors);

        // Validate version number
        if (value.Version <= 0)
        {
            errors.Add($"Workflow version must be positive, but was {value.Version}");
        }

        // Validate status constraints
        ValidateStatusConstraints(value.Status, errors);

        // Validate collections
        ValidateActivitiesCollection(value.Activities, errors);
        ValidateTransitionsCollection(value.Transitions, errors);

        // Validate reference integrity
        ValidateActivityReferences(value, errors);

        // Validate date/time fields
        ValidateDateTimeFields(value.CreatedAt, value.ModifiedAt, errors);

        // Validate optional string fields
        ValidateOptionalString(value.Description, nameof(value.Description), errors);
        ValidateOptionalString(value.StartActivityId, nameof(value.StartActivityId), errors);
        ValidateOptionalString(value.EndActivityId, nameof(value.EndActivityId), errors);
        ValidateOptionalString(value.CreatedBy, nameof(value.CreatedBy), errors);
        ValidateOptionalString(value.ModifiedBy, nameof(value.ModifiedBy), errors);

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Checks if a workflow instance is valid (has no validation errors).
    /// </summary>
    /// <param name="value">The workflow instance to check.</param>
    /// <returns><c>true</c> if the workflow is valid; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this Workflow value) => Validate(value).Count == 0;

    /// <summary>
    /// Ensures that a workflow instance is valid, throwing an exception if it is not.
    /// </summary>
    /// <param name="value">The workflow instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the workflow has validation errors.</exception>
    public static void EnsureValid(this Workflow value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"Workflow validation failed with {errors.Count} error(s):{Environment.NewLine}- ".Replace("\n", "\n- ") +
                string.Join(Environment.NewLine + "- ", errors),
                nameof(value));
        }
    }

    /// <summary>
    /// Validates that a string is not null, empty, or whitespace.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="propertyName">The name of the property being validated.</param>
    /// <param name="errors">The collection to add error messages to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    private static void ValidateRequiredString(string value, string propertyName, List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{propertyName} is required and cannot be null, empty, or whitespace");
        }
    }

    /// <summary>
    /// Validates that an optional string is valid if provided.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="propertyName">The name of the property being validated.</param>
    /// <param name="errors">The collection to add error messages to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    private static void ValidateOptionalString(string? value, string propertyName, List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (value != null && string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{propertyName} cannot be empty or whitespace when provided");
        }
    }

    /// <summary>
    /// Validates workflow status constraints.
    /// </summary>
    /// <param name="status">The workflow status to validate.</param>
    /// <param name="errors">The collection to add error messages to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    private static void ValidateStatusConstraints(WorkflowStatus status, List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        // No specific constraints on status values themselves
        // The status enum has valid values by design
    }

    /// <summary>
    /// Validates the Activities collection.
    /// </summary>
    /// <param name="activities">The activities collection to validate.</param>
    /// <param name="errors">The collection to add error messages to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    private static void ValidateActivitiesCollection(List<Activity> activities, List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (activities == null)
        {
            errors.Add("Activities collection cannot be null");
            return;
        }

        if (activities.Count == 0)
        {
            errors.Add("Workflow must have at least one activity");
        }

        // Validate each activity individually
        for (int i = 0; i < activities.Count; i++)
        {
            var activity = activities[i];
            if (activity == null)
            {
                errors.Add($"Activities[{i}] cannot be null");
                continue;
            }

            var activityErrors = new List<string>();
            if (!activity.Validate(out activityErrors))
            {
                errors.AddRange(activityErrors.Select(e => $"Activity '{activity.Id}' validation failed: {e}"));
            }
        }
    }

    /// <summary>
    /// Validates the Transitions collection.
    /// </summary>
    /// <param name="transitions">The transitions collection to validate.</param>
    /// <param name="errors">The collection to add error messages to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    private static void ValidateTransitionsCollection(List<Transition> transitions, List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (transitions == null)
        {
            errors.Add("Transitions collection cannot be null");
            return;
        }

        // Validate each transition individually
        for (int i = 0; i < transitions.Count; i++)
        {
            var transition = transitions[i];
            if (transition == null)
            {
                errors.Add($"Transitions[{i}] cannot be null");
                continue;
            }

            var transitionErrors = new List<string>();
            if (!transition.Validate(out transitionErrors))
            {
                errors.AddRange(transitionErrors.Select(e => $"Transition '{transition.Id}' validation failed: {e}"));
            }
        }
    }

    /// <summary>
    /// Validates all activity references within the workflow.
    /// </summary>
    /// <param name="workflow">The workflow to validate.</param>
    /// <param name="errors">The collection to add error messages to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    private static void ValidateActivityReferences(Workflow workflow, List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        // Validate StartActivityId references existing activity
        if (!string.IsNullOrWhiteSpace(workflow.StartActivityId))
        {
            if (workflow.Activities.All(a => a.Id != workflow.StartActivityId))
            {
                errors.Add($"Start activity '{workflow.StartActivityId}' does not exist in Activities collection");
            }
        }

        // Validate EndActivityId references existing activity
        if (!string.IsNullOrWhiteSpace(workflow.EndActivityId))
        {
            if (workflow.Activities.All(a => a.Id != workflow.EndActivityId))
            {
                errors.Add($"End activity '{workflow.EndActivityId}' does not exist in Activities collection");
            }
        }

        // Validate all transition references
        foreach (var transition in workflow.Transitions)
        {
            // FromActivityId must reference existing activity
            if (workflow.Activities.All(a => a.Id != transition.FromActivityId))
            {
                errors.Add($"Transition '{transition.Id}' references non-existent FromActivity: '{transition.FromActivityId}'");
            }

            // ToActivityId must reference existing activity
            if (workflow.Activities.All(a => a.Id != transition.ToActivityId))
            {
                errors.Add($"Transition '{transition.Id}' references non-existent ToActivity: '{transition.ToActivityId}'");
            }

            // Transition cannot point to itself
            if (transition.FromActivityId == transition.ToActivityId)
            {
                errors.Add($"Transition '{transition.Id}' cannot point from and to the same activity: '{transition.FromActivityId}'");
            }
        }
    }

    /// <summary>
    /// Validates date/time fields for logical consistency.
    /// </summary>
    /// <param name="createdAt">The creation timestamp.</param>
    /// <param name="modifiedAt">The modification timestamp.</param>
    /// <param name="errors">The collection to add error messages to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    private static void ValidateDateTimeFields(DateTime createdAt, DateTime modifiedAt, List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        // Validate that dates are not default (MinValue)
        if (createdAt == default)
        {
            errors.Add("CreatedAt cannot be default DateTime value");
        }

        if (modifiedAt == default)
        {
            errors.Add("ModifiedAt cannot be default DateTime value");
        }

        // Validate that modified date is not before created date
        if (modifiedAt < createdAt)
        {
            errors.Add("ModifiedAt cannot be earlier than CreatedAt");
        }

        // Validate that dates are in the past or very recent (within last minute for clock skew)
        var now = DateTime.UtcNow;
        if (createdAt > now.AddMinutes(1))
        {
            errors.Add("CreatedAt cannot be in the future");
        }

        if (modifiedAt > now.AddMinutes(1))
        {
            errors.Add("ModifiedAt cannot be in the future");
        }
    }
}