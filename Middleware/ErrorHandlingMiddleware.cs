// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using DotNetWorkflowEngine.Exceptions;

namespace DotNetWorkflowEngine.Middleware;

/// <summary>
/// Global exception handling middleware that catches all unhandled exceptions
/// and converts them to appropriate HTTP responses with standardized error format.
/// This middleware ensures consistent error responses across all API endpoints.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware. Wraps the request processing in a try-catch block
    /// to handle any exceptions that occur during request processing.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Converts exceptions to appropriate HTTP responses based on exception type.
    /// Maps domain exceptions to correct HTTP status codes and response bodies.
    /// </summary>
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        switch (exception)
        {
            case ValidationException ex:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = ex.Message;
                response.ErrorCode = "VALIDATION_ERROR";
                break;

            case StateException ex:
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                response.Message = ex.Message;
                response.ErrorCode = "INVALID_STATE";
                break;

            case ActivityException ex:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Message = ex.Message;
                response.ErrorCode = "ACTIVITY_ERROR";
                response.Details = ex.ActivityId;
                break;

            case WorkflowException ex:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Message = ex.Message;
                response.ErrorCode = "WORKFLOW_ERROR";
                response.Details = ex.WorkflowId;
                break;

            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Message = "An unexpected error occurred";
                response.ErrorCode = "INTERNAL_SERVER_ERROR";
                break;
        }

        response.Timestamp = DateTime.UtcNow;
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        return context.Response.WriteAsJsonAsync(response, jsonOptions);
    }

    /// <summary>
    /// Standard error response format returned by all error handlers.
    /// Provides consistent structure for error messages across the API.
    /// </summary>
    private class ErrorResponse
    {
        public string ErrorCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
