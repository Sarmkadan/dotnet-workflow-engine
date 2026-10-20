// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetWorkflowEngine.Exceptions;

/// <summary>
/// Base exception for all workflow engine related errors.
/// </summary>
public class WorkflowException : Exception
{
    /// <summary>
    /// Gets the error code associated with this exception.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Gets the correlation ID for tracking related exceptions.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Initializes a new instance of the WorkflowException class.
    /// </summary>
    public WorkflowException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the WorkflowException class with error code.
    /// </summary>
    public WorkflowException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the WorkflowException class with inner exception.
    /// </summary>
    public WorkflowException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance with complete information.
    /// </summary>
    public WorkflowException(string message, string errorCode, string correlationId, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        CorrelationId = correlationId;
    }

    /// <summary>
    /// Returns a string representation that includes contextual workflow data
    /// (error code and correlation ID) alongside the standard exception details.
    /// </summary>
    public override string ToString()
    {
        var builder = new System.Text.StringBuilder();

        builder.Append(GetType().FullName);
        builder.Append(": ");
        builder.Append(Message);

        if (!string.IsNullOrEmpty(ErrorCode))
        {
            builder.Append(" (ErrorCode: ");
            builder.Append(ErrorCode);
            builder.Append(')');
        }

        if (!string.IsNullOrEmpty(CorrelationId))
        {
            builder.Append(" [CorrelationId: ");
            builder.Append(CorrelationId);
            builder.Append(']');
        }

        if (InnerException is not null)
        {
            builder.Append(" ---> ");
            builder.Append(InnerException);
            builder.AppendLine();
            builder.Append("   --- End of inner exception stack trace ---");
        }

        if (StackTrace is not null)
        {
            builder.AppendLine();
            builder.Append(StackTrace);
        }

        return builder.ToString();
    }
}
