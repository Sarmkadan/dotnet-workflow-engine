// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DotNetWorkflowEngine.Filters;

/// <summary>
/// ASP.NET Core action filter that validates model state before request processing.
/// Returns a standardized validation error response if model validation fails.
/// Applied globally to all controllers to ensure consistent validation handling.
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    private readonly ILogger<ValidationFilter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationFilter"/> class.
    /// </summary>
    /// <param name="logger">The logger used to record validation activity.</param>
    public ValidationFilter(ILogger<ValidationFilter> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Called before action execution. Checks model state and returns
    /// early with validation errors if model is invalid.
    /// </summary>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        _logger.LogInformation(
            "Validating model state for action {Action}",
            context.ActionDescriptor.DisplayName);

        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray())
                .ToList();

            _logger.LogWarning(
                "Model validation failed for action {Action}. Errors: {ErrorCount}",
                context.ActionDescriptor.DisplayName,
                errors.Count);

            context.Result = new BadRequestObjectResult(new ValidationErrorResponse
            {
                Message = "Validation failed",
                Errors = errors
            });

            return;
        }

        await next();
    }
}

/// <summary>
/// Model validation filter that uses data annotation validators.
/// Validates objects before they are processed by actions.
/// </summary>
public class DataAnnotationValidationFilter : IAsyncActionFilter
{
    private readonly ILogger<DataAnnotationValidationFilter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataAnnotationValidationFilter"/> class.
    /// </summary>
    /// <param name="logger">The logger used to record validation activity.</param>
    public DataAnnotationValidationFilter(ILogger<DataAnnotationValidationFilter> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        _logger.LogInformation(
            "Validating action arguments for action {Action}",
            context.ActionDescriptor.DisplayName);

        // Validate all action arguments that have validation attributes
        foreach (var argument in context.ActionArguments)
        {
            var validationResults = new List<ValidationResult>();

            if (argument.Value != null)
            {
                var vc = new ValidationContext(argument.Value);
                if (!Validator.TryValidateObject(argument.Value, vc, validationResults, true))
                {
                    _logger.LogWarning(
                        "Validation failed for parameter {ParameterName}. Errors: {ErrorCount}",
                        argument.Key,
                        validationResults.Count);

                    var errors = validationResults
                        .GroupBy(v => v.MemberNames.FirstOrDefault() ?? argument.Key)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(v => v.ErrorMessage ?? "Invalid").ToArray())
                        .ToList();

                    context.Result = new BadRequestObjectResult(new ValidationErrorResponse
                    {
                        Message = "Validation failed",
                        Errors = errors
                    });

                    return;
                }
            }
        }

        await next();
    }
}

/// <summary>
/// Response format for validation errors.
/// </summary>
public class ValidationErrorResponse
{
    /// <summary>
    /// Gets or sets the top-level validation error message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the collection of validation errors keyed by member name.
    /// </summary>
    public List<KeyValuePair<string, string[]>>? Errors { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the validation error response was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Returns a string representation of the validation error response.
    /// </summary>
    /// <returns>A string containing the message, errors, and timestamp.</returns>
    public override string ToString() => $"ValidationErrorResponse {{ Message = {Message}, Errors = {Errors}, Timestamp = {Timestamp} }}";
}

/// <summary>
/// Custom attribute for validating that a property value is not empty or whitespace.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class NotEmptyOrWhitespaceAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return new ValidationResult($"The {validationContext.DisplayName} field cannot be empty or contain only whitespace.");
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Custom attribute for validating URL format.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ValidUrlAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
                return new ValidationResult($"The {validationContext.DisplayName} field is not a valid URL.");
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Custom attribute for validating that a property is within an allowed set of values.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class AllowedValuesAttribute : ValidationAttribute
{
    private readonly string[] _allowedValues;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllowedValuesAttribute"/> class
    /// with the set of allowed values.
    /// </summary>
    /// <param name="allowedValues">The values that the validated property is allowed to take.</param>
    public AllowedValuesAttribute(params string[] allowedValues)
    {
        _allowedValues = allowedValues;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string str && !_allowedValues.Contains(str, StringComparer.OrdinalIgnoreCase))
            return new ValidationResult(
                $"The {validationContext.DisplayName} field value must be one of: {string.Join(", ", _allowedValues)}");

        return ValidationResult.Success;
    }
}
