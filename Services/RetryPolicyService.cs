// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Services;

/// <summary>
/// Service for managing and calculating retry policies.
/// </summary>
public class RetryPolicyService
{
    private readonly Dictionary<string, RetryPolicyConfig> _policies = new();

    /// <summary>
    /// Creates and registers a retry policy.
    /// </summary>
    public void CreatePolicy(string policyId, RetryPolicyConfig config)
    {
        _policies[policyId] = config;
    }

    /// <summary>
    /// Gets a retry policy by ID.
    /// </summary>
    public RetryPolicyConfig? GetPolicy(string policyId)
    {
        _policies.TryGetValue(policyId, out var policy);
        return policy;
    }

    /// <summary>
    /// Calculates the next retry delay.
    /// </summary>
    public int CalculateRetryDelay(string policyId, int attemptNumber)
    {
        var policy = GetPolicy(policyId);
        if (policy == null)
            return Constants.WorkflowConstants.DefaultRetryDelayMs;

        return policy.CalculateDelayMs(attemptNumber);
    }

    /// <summary>
    /// Determines if a retry should be attempted.
    /// </summary>
    public bool ShouldRetry(string policyId, int currentAttempt, string? exceptionTypeName = null)
    {
        var policy = GetPolicy(policyId);
        if (policy == null)
            return false;

        return policy.ShouldRetry(currentAttempt, exceptionTypeName);
    }

    /// <summary>
    /// Creates a default exponential backoff policy.
    /// </summary>
    public RetryPolicyConfig CreateExponentialBackoffPolicy(int maxRetries = 3)
    {
        return RetryPolicyConfig.CreateExponentialBackoff(
            maxRetries,
            Constants.WorkflowConstants.DefaultRetryDelayMs,
            Constants.WorkflowConstants.MaxBackoffDelayMs
        );
    }

    /// <summary>
    /// Creates a fixed delay retry policy.
    /// </summary>
    public RetryPolicyConfig CreateFixedDelayPolicy(int maxRetries = 3, int delayMs = 1000)
    {
        return RetryPolicyConfig.CreateFixedDelay(maxRetries, delayMs);
    }

    /// <summary>
    /// Creates a no-retry policy.
    /// </summary>
    public RetryPolicyConfig CreateNoRetryPolicy()
    {
        return RetryPolicyConfig.CreateNoRetry();
    }

    /// <summary>
    /// Simulates retry delays for analysis.
    /// </summary>
    public List<int> SimulateRetryDelays(string policyId, int maxAttempts)
    {
        var delays = new List<int>();
        for (int i = 1; i <= maxAttempts; i++)
        {
            delays.Add(CalculateRetryDelay(policyId, i));
        }
        return delays;
    }

    /// <summary>
    /// Gets total estimated time for all retry attempts.
    /// </summary>
    public long GetTotalRetryTimeMs(string policyId)
    {
        var policy = GetPolicy(policyId);
        if (policy == null)
            return 0;

        long total = 0;
        for (int i = 1; i < policy.MaxAttempts; i++)
        {
            total += policy.CalculateDelayMs(i);
        }
        return total;
    }

    /// <summary>
    /// Validates a retry policy configuration.
    /// </summary>
    public bool ValidatePolicy(RetryPolicyConfig config, out List<string> errors)
    {
        errors = new List<string>();

        if (config.MaxAttempts <= 0)
            errors.Add("MaxAttempts must be greater than 0");

        if (config.InitialDelayMs <= 0)
            errors.Add("InitialDelayMs must be greater than 0");

        if (config.MaxDelayMs < config.InitialDelayMs)
            errors.Add("MaxDelayMs must be greater than or equal to InitialDelayMs");

        if (config.BackoffMultiplier <= 1.0)
            errors.Add("BackoffMultiplier must be greater than 1.0");

        if (config.JitterFactor < 0 || config.JitterFactor > 1)
            errors.Add("JitterFactor must be between 0 and 1");

        return errors.Count == 0;
    }

    /// <summary>
    /// Registers a retryable exception type for a policy.
    /// </summary>
    public void RegisterRetryableException(string policyId, string exceptionTypeName)
    {
        var policy = GetPolicy(policyId);
        if (policy != null && !policy.RetryableExceptionTypes.Contains(exceptionTypeName))
        {
            policy.RetryableExceptionTypes.Add(exceptionTypeName);
        }
    }

    /// <summary>
    /// Clears all policies.
    /// </summary>
    public void ClearPolicies()
    {
        _policies.Clear();
    }
}
