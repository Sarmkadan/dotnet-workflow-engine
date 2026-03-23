// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetWorkflowEngine.Exceptions;

/// <summary>
/// Exception thrown when workflow or activity validation fails.
/// </summary>
public class ValidationException : WorkflowException
{
    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; }

    /// <summary>
    /// Gets the entity that failed validation.
    /// </summary>
    public string? EntityName { get; }

    /// <summary>
    /// Initializes a new instance of the ValidationException class.
    /// </summary>
    public ValidationException(string message)
        : base(message, "VALIDATION_ERROR")
    {
        ValidationErrors = new List<string>();
    }

    /// <summary>
    /// Initializes a new instance with validation errors.
    /// </summary>
    public ValidationException(string message, IEnumerable<string> errors, string? entityName = null)
        : base(message, "VALIDATION_ERROR")
    {
        ValidationErrors = errors.ToList();
        EntityName = entityName;
    }

    /// <summary>
    /// Initializes a new instance with single error.
    /// </summary>
    public ValidationException(string message, string error, string? entityName = null)
        : base(message, "VALIDATION_ERROR")
    {
        ValidationErrors = new List<string> { error };
        EntityName = entityName;
    }

    /// <summary>
    /// Returns a formatted message with all validation errors.
    /// </summary>
    public string GetDetailedMessage()
    {
        if (ValidationErrors.Count == 0)
            return Message;

        var errors = string.Join("; ", ValidationErrors);
        return $"{Message} Errors: {errors}";
    }
}
