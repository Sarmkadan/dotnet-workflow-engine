// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
//
// Extension methods for ErrorHandlingMiddleware providing additional functionality
// for error handling and response generation scenarios.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace DotNetWorkflowEngine.Middleware;

/// <summary>
/// Provides extension methods for ErrorHandlingMiddleware to enhance error handling
/// capabilities with additional common scenarios and response formatting options.
/// </summary>
public static class ErrorHandlingMiddlewareExtensions
{
    /// <summary>
    /// Creates a standardized error response from the given exception using the middleware's
    /// error response format. Useful for manually generating error responses outside of
    /// the HTTP pipeline.
    /// </summary>
    /// <param name="exception">The exception to convert to an error response.</param>
    /// <returns>A tuple containing the error code, message, details (if any), and timestamp.</returns>
    /// <exception cref="ArgumentNullException">Thrown when exception is null.</exception>
    public static (string ErrorCode, string Message, string? Details, DateTime Timestamp)
        ToErrorResponse(this Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        string errorCode = "INTERNAL_SERVER_ERROR";
        string message = "An unexpected error occurred";
        string? details = null;
        DateTime timestamp = DateTime.UtcNow;

        switch (exception)
        {
            case DotNetWorkflowEngine.Exceptions.ValidationException ex:
                errorCode = "VALIDATION_ERROR";
                message = ex.Message;
                break;

            case DotNetWorkflowEngine.Exceptions.StateException ex:
                errorCode = "INVALID_STATE";
                message = ex.Message;
                break;

            case DotNetWorkflowEngine.Exceptions.ActivityException ex:
                errorCode = "ACTIVITY_ERROR";
                message = ex.Message;
                details = ex.ActivityId;
                break;

            case DotNetWorkflowEngine.Exceptions.WorkflowException ex:
                errorCode = ex.ErrorCode ?? "WORKFLOW_ERROR";
                message = ex.Message;
                details = ex.CorrelationId;
                break;
        }

        return (errorCode, message, details, timestamp);
    }

    /// <summary>
    /// Creates an HTTP response from the error response tuple. Useful for manually
    /// constructing error responses in controllers or other middleware components.
    /// </summary>
    /// <param name="context">The HTTP context to write the response to.</param>
    /// <param name="errorResponse">The error response tuple from ToErrorResponse.</param>
    /// <param name="statusCode">Optional HTTP status code to use (defaults to 500).</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when context or errorResponse is invalid.</exception>
    public static async Task WriteErrorResponseAsync(
        this HttpContext context,
        (string ErrorCode, string Message, string? Details, DateTime Timestamp) errorResponse,
        int statusCode = StatusCodes.Status500InternalServerError)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new
        {
            errorCode = errorResponse.ErrorCode,
            message = errorResponse.Message,
            details = errorResponse.Details,
            timestamp = errorResponse.Timestamp.ToString("o", CultureInfo.InvariantCulture)
        };

        await context.Response.WriteAsJsonAsync(response);
    }

    /// <summary>
    /// Creates a standardized error response with additional context information.
    /// Useful for adding correlation IDs, request IDs, or other diagnostic information
    /// to error responses.
    /// </summary>
    /// <param name="context">The HTTP context containing request information.</param>
    /// <param name="exception">The exception to convert to an error response.</param>
    /// <param name="additionalContext">Optional dictionary of additional context to include in the response.</param>
    /// <returns>A tuple containing the error code, message, details (if any), and timestamp.</returns>
    /// <exception cref="ArgumentNullException">Thrown when context or exception is null.</exception>
    public static (string ErrorCode, string Message, string? Details, DateTime Timestamp,
        IReadOnlyDictionary<string, object> Context)
        ToErrorResponseWithContext(
            this HttpContext context,
            Exception exception,
            IReadOnlyDictionary<string, object>? additionalContext = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exception);

        var baseResponse = exception.ToErrorResponse();
        var contextDict = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["requestId"] = context.TraceIdentifier,
            ["timestamp"] = baseResponse.Timestamp.ToString("o", CultureInfo.InvariantCulture)
        };

        if (additionalContext != null)
        {
            foreach (var kvp in additionalContext)
            {
                contextDict[kvp.Key] = kvp.Value;
            }
        }

        return (baseResponse.ErrorCode, baseResponse.Message, baseResponse.Details,
            baseResponse.Timestamp, contextDict);
    }
}