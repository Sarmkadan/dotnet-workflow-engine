// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DotNetWorkflowEngine.Middleware;

/// <summary>
/// Request/response logging middleware that captures and logs all HTTP traffic
/// for debugging, monitoring, and compliance purposes. Logs include method, path,
/// status code, response time, and request/response bodies when configured.
/// </summary>
public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;
    private readonly bool _logRequestBody;
    private readonly bool _logResponseBody;

    public LoggingMiddleware(
        RequestDelegate next,
        ILogger<LoggingMiddleware> logger,
        bool logRequestBody = false,
        bool logResponseBody = false)
    {
        _next = next;
        _logger = logger;
        _logRequestBody = logRequestBody;
        _logResponseBody = logResponseBody;
    }

    /// <summary>
    /// Invokes the middleware, capturing request/response details and measuring
    /// request processing time. This is called for every HTTP request.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        var requestMethod = context.Request.Method;
        var requestPath = context.Request.Path;
        var requestBody = await ReadRequestBodyAsync(context.Request);

        var originalBodyStream = context.Response.Body;
        using (var responseBody = new MemoryStream())
        {
            context.Response.Body = responseBody;

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                var responseBodyContent = await ReadResponseBodyAsync(context.Response);

                // Log the complete request/response
                _logger.LogInformation(
                    "HTTP {Method} {Path} -> {StatusCode} ({ResponseTime}ms) | User: {User}",
                    requestMethod,
                    requestPath,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    context.User?.Identity?.Name ?? "anonymous");

                // Log request body if configured and applicable
                if (_logRequestBody && !string.IsNullOrEmpty(requestBody) && IsLoggableContentType(context.Request.ContentType))
                {
                    _logger.LogDebug("Request body: {RequestBody}", requestBody);
                }

                // Log response body if configured and applicable
                if (_logResponseBody && !string.IsNullOrEmpty(responseBodyContent))
                {
                    _logger.LogDebug("Response body: {ResponseBody}", responseBodyContent);
                }

                // Warn if response time exceeds threshold (e.g., 5 seconds)
                if (stopwatch.ElapsedMilliseconds > 5000)
                {
                    _logger.LogWarning(
                        "Slow request detected: {Method} {Path} took {ResponseTime}ms",
                        requestMethod,
                        requestPath,
                        stopwatch.ElapsedMilliseconds);
                }

                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
    }

    /// <summary>
    /// Reads the request body from the stream while ensuring the stream can be
    /// re-read by the request handler (by seeking back to position 0).
    /// </summary>
    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (!request.ContentLength.HasValue || request.ContentLength == 0)
            return string.Empty;

        var originalPosition = request.Body.Position;
        request.Body.Seek(0, SeekOrigin.Begin);

        using (var streamReader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
        {
            var body = await streamReader.ReadToEndAsync();
            request.Body.Seek(originalPosition, SeekOrigin.Begin);
            return body;
        }
    }

    /// <summary>
    /// Reads the response body from the memory stream. This must be done before
    /// the response is sent to the client.
    /// </summary>
    private async Task<string> ReadResponseBodyAsync(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        using (var streamReader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true))
        {
            var body = await streamReader.ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);
            return body;
        }
    }

    /// <summary>
    /// Determines whether a content type should be logged. Avoids logging
    /// large binary content like images, videos, files, etc.
    /// </summary>
    private bool IsLoggableContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        var nonLoggableTypes = new[] { "image/", "video/", "audio/", "application/octet-stream", "multipart/form-data" };

        return !Array.Exists(nonLoggableTypes, type => contentType.Contains(type, StringComparison.OrdinalIgnoreCase));
    }
}
