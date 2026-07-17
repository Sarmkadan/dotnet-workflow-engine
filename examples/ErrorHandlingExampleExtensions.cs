// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotNetWorkflowEngine.Examples;

/// <summary>
/// Provides useful extension methods for <see cref="ErrorHandlingExample"/> to enhance error handling workflows.
/// </summary>
public static class ErrorHandlingExampleExtensions
{
    /// <summary>
    /// Validates the processing request and ensures required fields are present.
    /// </summary>
    /// <param name="example">The <see cref="ErrorHandlingExample"/> instance.</param>
    /// <param name="request">The processing request to validate.</param>
    /// <returns>True if the request is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="example"/> or <paramref name="request"/> is null.</exception>
    public static bool ValidateProcessingRequest(this ErrorHandlingExample example, ProcessingRequest request)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentNullException.ThrowIfNull(request);

        return !string.IsNullOrWhiteSpace(request.DataSourceUrl) &&
               request.ProcessingRules?.Any() == true;
    }

    /// <summary>
    /// Creates a standardized error response for workflow failures.
    /// </summary>
    /// <param name="example">The <see cref="ErrorHandlingExample"/> instance.</param>
    /// <param name="instanceId">The workflow instance ID.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="retryCount">The number of retry attempts made.</param>
    /// <returns>An <see cref="ActionResult"/> representing the error response.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="example"/> or <paramref name="errorMessage"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="instanceId"/> is <see cref="Guid.Empty"/>.</exception>
    public static ActionResult CreateErrorResponse(
        this ErrorHandlingExample example,
        Guid instanceId,
        string errorMessage,
        int retryCount = 0)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentNullException.ThrowIfNull(errorMessage);
        ArgumentException.ThrowIfDefault(instanceId);

        var errorResponse = new
        {
            InstanceId = instanceId,
            Error = errorMessage,
            Timestamp = DateTime.UtcNow,
            RetryAttempts = retryCount,
            RecoverySuggested = retryCount < 3 ? "Retry with exponential backoff" : "Use fallback path"
        };

        return new BadRequestObjectResult(errorResponse);
    }

    /// <summary>
    /// Extracts retry policy information from processing rules.
    /// </summary>
    /// <param name="example">The <see cref="ErrorHandlingExample"/> instance.</param>
    /// <param name="processingRules">The processing rules dictionary.</param>
    /// <returns>A dictionary containing retry policy information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="example"/> or <paramref name="processingRules"/> is null.</exception>
    public static Dictionary<string, object> GetRetryPolicyInfo(
        this ErrorHandlingExample example,
        Dictionary<string, object> processingRules)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentNullException.ThrowIfNull(processingRules);

        var retryInfo = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            { "maxRetries", 3 },
            { "retryDelayMs", 1000 },
            { "backoffFactor", 2.0 },
            { "timeoutMs", 30000 }
        };

        if (processingRules.TryGetValue("MaxRetries", out var maxRetries))
        {
            if (maxRetries is int maxRetriesInt)
            {
                retryInfo["maxRetries"] = maxRetriesInt;
            }
            else if (maxRetries is string maxRetriesStr && int.TryParse(maxRetriesStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedMax))
            {
                retryInfo["maxRetries"] = parsedMax;
            }
        }

        if (processingRules.TryGetValue("RetryDelayMs", out var retryDelay))
        {
            if (retryDelay is int delayInt)
            {
                retryInfo["retryDelayMs"] = delayInt;
            }
            else if (retryDelay is string delayStr && int.TryParse(delayStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedDelay))
            {
                retryInfo["retryDelayMs"] = parsedDelay;
            }
        }

        return retryInfo;
    }

    /// <summary>
    /// Creates a comprehensive error report from workflow execution context.
    /// </summary>
    /// <param name="example">The <see cref="ErrorHandlingExample"/> instance.</param>
    /// <param name="context">The execution context containing error information.</param>
    /// <returns>A dictionary containing detailed error report information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="example"/> or <paramref name="context"/> is null.</exception>
    public static Dictionary<string, object> CreateErrorReport(
        this ErrorHandlingExample example,
        ExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentNullException.ThrowIfNull(context);

        var errorReport = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            { "instanceId", context.InstanceId },
            { "workflowId", context.WorkflowId },
            { "timestamp", DateTime.UtcNow },
            { "status", context.Status.ToString() },
            { "retryCount", context.Variables?.TryGetValue("RetryAttempts", out var retryCount) == true ? retryCount : 0 },
            { "fallbackUsed", context.Variables?.TryGetValue("FallbackUsed", out var fallbackUsed) == true && fallbackUsed is bool b && b }
        };

        if (context.Variables?.TryGetValue("LastError", out var lastError) == true && lastError is string errorMessage)
        {
            errorReport["errorMessage"] = errorMessage;
        }

        if (context.Variables?.TryGetValue("RecoveryMethod", out var recoveryMethod) == true)
        {
            errorReport["recoveryMethod"] = recoveryMethod;
        }

        if (context.Variables?.TryGetValue("StartTime", out var startTime) == true && startTime is DateTime startDt)
        {
            errorReport["durationMs"] = (DateTime.UtcNow - startDt).TotalMilliseconds;
        }

        return errorReport;
    }

    /// <summary>
    /// Validates workflow instance status and determines appropriate recovery action.
    /// </summary>
    /// <param name="example">The <see cref="ErrorHandlingExample"/> instance.</param>
    /// <param name="instance">The workflow instance to validate.</param>
    /// <returns>A tuple containing the validation result and suggested recovery action.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="example"/> or <paramref name="instance"/> is null.</exception>
    public static (bool IsValid, string RecoveryAction) ValidateWorkflowInstance(
        this ErrorHandlingExample example,
        WorkflowInstance instance)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentNullException.ThrowIfNull(instance);

        var isValid = instance.Status == WorkflowStatus.Completed ||
                      instance.Status == WorkflowStatus.Active;

        var recoveryAction = instance.Status switch
        {
            WorkflowStatus.Failed => "Review error details and retry",
            WorkflowStatus.Terminated => "Investigate termination cause",
            WorkflowStatus.Suspended => "Resume workflow execution",
            _ => "Continue monitoring"
        };

        return (isValid, recoveryAction);
    }
}