// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DotNetWorkflowEngine.Filters;

/// <summary>
/// Extension methods for ValidationFilter to provide additional validation utilities.
/// </summary>
public static class ValidationFilterExtensions
{
    /// <summary>
    /// Adds a custom validation error to the context with the specified message.
    /// </summary>
    /// <param name="filter">The validation filter instance</param>
    /// <param name="context">The action executing context</param>
    /// <param name="errorMessage">The error message to add</param>
    /// <param name="propertyName">Optional property name to associate the error with</param>
    public static void AddValidationError(
        this ValidationFilter filter,
        ActionExecutingContext context,
        string errorMessage,
        string? propertyName = null)
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray())
            .ToList();

        if (errors.Count == 0 && propertyName != null)
        {
            errors.Add(new KeyValuePair<string, string[]>(propertyName, new[] { errorMessage }));
        }
        else if (propertyName != null)
        {
            var existing = errors.FirstOrDefault(e => e.Key == propertyName);
            if (existing.Key != null)
            {
                errors.Remove(existing);
                errors.Add(new KeyValuePair<string, string[]>(propertyName, existing.Value.Concat(new[] { errorMessage }).ToArray()));
            }
            else
            {
                errors.Add(new KeyValuePair<string, string[]>(propertyName, new[] { errorMessage }));
            }
        }

        context.Result = new BadRequestObjectResult(new ValidationErrorResponse
        {
            Message = "Validation failed",
            Errors = errors,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Validates a single value against validation attributes and returns validation errors.
    /// </summary>
    /// <param name="filter">The validation filter instance</param>
    /// <param name="value">The value to validate</param>
    /// <param name="propertyName">The name of the property being validated</param>
    /// <returns>List of validation errors, or empty list if valid</returns>
    public static List<string> ValidateValue(
        this ValidationFilter filter,
        object? value,
        string propertyName)
    {
        var errors = new List<string>();

        if (value != null)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(value) { MemberName = propertyName };

            if (!Validator.TryValidateObject(value, validationContext, validationResults, true))
            {
                errors.AddRange(validationResults
                    .SelectMany(v => v.MemberNames)
                    .Distinct()
                    .SelectMany(mn => validationResults
                        .Where(v => v.MemberNames.Contains(mn))
                        .Select(v => v.ErrorMessage ?? "Invalid")
                        .Distinct())
                    .ToList());
            }
        }

        return errors;
    }

    /// <summary>
    /// Combines multiple validation errors into a single error message with a custom prefix.
    /// </summary>
    /// <param name="filter">The validation filter instance</param>
    /// <param name="errors">The list of validation errors</param>
    /// <param name="prefix">Optional prefix to prepend to the combined message</param>
    /// <returns>Combined error message</returns>
    public static string CombineErrors(
        this ValidationFilter filter,
        List<KeyValuePair<string, string[]>>? errors,
        string prefix = "Validation errors detected:")
    {
        if (errors == null || errors.Count == 0)
        {
            return "No validation errors";
        }

        var allErrors = errors
            .SelectMany(e => e.Value.Select(v => $"{e.Key}: {v}"))
            .ToList();

        return $"{prefix} {string.Join(" | ", allErrors)}";
    }

    /// <summary>
    /// Checks if the validation context has any errors for the specified property.
    /// </summary>
    /// <param name="filter">The validation filter instance</param>
    /// <param name="context">The action executing context</param>
    /// <param name="propertyName">The property name to check</param>
    /// <returns>True if the property has validation errors, false otherwise</returns>
    public static bool HasPropertyErrors(
        this ValidationFilter filter,
        ActionExecutingContext context,
        string propertyName)
    {
        return context.ModelState.TryGetValue(propertyName, out var entry)
            && entry?.Errors.Count > 0;
    }
}
