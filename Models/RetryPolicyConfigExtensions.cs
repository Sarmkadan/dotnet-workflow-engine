// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Enums;

namespace DotNetWorkflowEngine.Models;

/// <summary>
/// Provides extension methods for <see cref="RetryPolicyConfig"/> to configure and customize retry policies.
/// </summary>
/// <remarks>
/// This static class contains factory methods and fluent configuration methods for creating and modifying
/// <see cref="RetryPolicyConfig"/> instances with various retry strategies including linear backoff, exponential backoff,
/// fixed delay, and custom configurations.
/// </remarks>
public static class RetryPolicyConfigExtensions
{
    /// <summary>
    /// Creates a linear backoff retry configuration.
    /// </summary>
    /// <param name="maxAttempts">Maximum number of retry attempts. Must be greater than 0.</param>
    /// <param name="initialDelayMs">Initial delay in milliseconds. Must be greater than 0.</param>
    /// <param name="maxDelayMs">Maximum delay in milliseconds (optional). Default is 300000 (5 minutes).</param>
    /// <param name="jitterFactor">Jitter factor to randomize delays (optional). Must be between 0.0 and 1.0.</param>
    /// <returns>Configured retry policy with <see cref="RetryPolicy.LinearBackoff"/> policy type.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxAttempts"/> or <paramref name="initialDelayMs"/> is less than or equal to 0.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="jitterFactor"/> is less than 0.0 or greater than 1.0.</exception>
    public static RetryPolicyConfig CreateLinearBackoff(this int maxAttempts, int initialDelayMs, int maxDelayMs = 300000, double jitterFactor = 0.1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxAttempts, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(initialDelayMs, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(jitterFactor, 0.0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(jitterFactor, 1.0);

        return new RetryPolicyConfig
        {
            PolicyType = RetryPolicy.LinearBackoff,
            MaxAttempts = maxAttempts,
            InitialDelayMs = initialDelayMs,
            MaxDelayMs = maxDelayMs,
            JitterFactor = jitterFactor
        };
    }

    /// <summary>
    /// Creates a custom retry configuration with all parameters.
    /// </summary>
    /// <param name="policyType">The retry policy type.</param>
    /// <param name="maxAttempts">Maximum number of retry attempts. Must be greater than 0.</param>
    /// <param name="initialDelayMs">Initial delay in milliseconds. Must be greater than 0.</param>
    /// <param name="maxDelayMs">Maximum delay in milliseconds. Must be greater than 0.</param>
    /// <param name="backoffMultiplier">Backoff multiplier for exponential/linear backoff. Must be greater than 0.</param>
    /// <param name="jitterFactor">Jitter factor to randomize delays. Must be between 0.0 and 1.0.</param>
    /// <param name="retryableExceptionTypes">List of exception types that should trigger retry.</param>
    /// <param name="retryOnTimeout">Whether to retry on timeout.</param>
    /// <returns>Configured retry policy.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any numeric parameter is invalid.</exception>
    public static RetryPolicyConfig CreateCustomRetry(
        this RetryPolicy policyType,
        int maxAttempts = 3,
        int initialDelayMs = 1000,
        int maxDelayMs = 300000,
        double backoffMultiplier = 2.0,
        double jitterFactor = 0.1,
        List<string>? retryableExceptionTypes = null,
        bool retryOnTimeout = true)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxAttempts, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(initialDelayMs, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxDelayMs, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(backoffMultiplier, 0.0);
        ArgumentOutOfRangeException.ThrowIfLessThan(jitterFactor, 0.0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(jitterFactor, 1.0);

        return new RetryPolicyConfig
        {
            PolicyType = policyType,
            MaxAttempts = maxAttempts,
            InitialDelayMs = initialDelayMs,
            MaxDelayMs = maxDelayMs,
            BackoffMultiplier = backoffMultiplier,
            JitterFactor = jitterFactor,
            RetryableExceptionTypes = retryableExceptionTypes ?? new List<string>(),
            RetryOnTimeout = retryOnTimeout
        };
    }

    /// <summary>
    /// Clones the retry policy configuration.
    /// </summary>
    /// <param name="config">The retry policy configuration to clone.</param>
    /// <returns>A deep copy of the retry policy configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
    public static RetryPolicyConfig Clone(this RetryPolicyConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return new RetryPolicyConfig
        {
            PolicyType = config.PolicyType,
            MaxAttempts = config.MaxAttempts,
            InitialDelayMs = config.InitialDelayMs,
            MaxDelayMs = config.MaxDelayMs,
            BackoffMultiplier = config.BackoffMultiplier,
            JitterFactor = config.JitterFactor,
            RetryableExceptionTypes = new List<string>(config.RetryableExceptionTypes),
            RetryOnTimeout = config.RetryOnTimeout
        };
    }

    /// <summary>
    /// Adds exception types to the retryable exceptions list.
    /// </summary>
    /// <param name="config">The retry policy configuration.</param>
    /// <param name="exceptionTypes">Exception type names to add.</param>
    /// <returns>The same retry policy configuration for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
    public static RetryPolicyConfig AddRetryableExceptions(this RetryPolicyConfig config, params string[] exceptionTypes)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (exceptionTypes is { Length: > 0 })
        {
            config.RetryableExceptionTypes ??= new List<string>();
            config.RetryableExceptionTypes.AddRange(exceptionTypes);
        }

        return config;
    }

    /// <summary>
    /// Sets the retry policy to retry on specific exception types.
    /// </summary>
    /// <param name="config">The retry policy configuration.</param>
    /// <param name="exceptionTypes">Exception type names that should trigger retry.</param>
    /// <returns>The same retry policy configuration for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
    public static RetryPolicyConfig WithRetryableExceptions(this RetryPolicyConfig config, params string[] exceptionTypes)
    {
        ArgumentNullException.ThrowIfNull(config);

        config.RetryableExceptionTypes = new List<string>(exceptionTypes ?? Array.Empty<string>());
        return config;
    }

    /// <summary>
    /// Sets whether to retry on timeout.
    /// </summary>
    /// <param name="config">The retry policy configuration.</param>
    /// <param name="retryOnTimeout">Whether to retry on timeout.</param>
    /// <returns>The same retry policy configuration for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
    public static RetryPolicyConfig WithRetryOnTimeout(this RetryPolicyConfig config, bool retryOnTimeout)
    {
        ArgumentNullException.ThrowIfNull(config);

        config.RetryOnTimeout = retryOnTimeout;
        return config;
    }

    /// <summary>
    /// Sets the maximum delay for the retry policy.
    /// </summary>
    /// <param name="config">The retry policy configuration.</param>
    /// <param name="maxDelayMs">Maximum delay in milliseconds. Must be greater than 0.</param>
    /// <returns>The same retry policy configuration for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxDelayMs"/> is less than or equal to 0.</exception>
    public static RetryPolicyConfig WithMaxDelay(this RetryPolicyConfig config, int maxDelayMs)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxDelayMs, 0);

        config.MaxDelayMs = maxDelayMs;
        return config;
    }

    /// <summary>
    /// Sets the backoff multiplier for exponential or linear backoff policies.
    /// </summary>
    /// <param name="config">The retry policy configuration.</param>
    /// <param name="multiplier">Backoff multiplier value. Must be greater than 0.</param>
    /// <returns>The same retry policy configuration for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="multiplier"/> is less than or equal to 0.</exception>
    public static RetryPolicyConfig WithBackoffMultiplier(this RetryPolicyConfig config, double multiplier)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(multiplier, 0.0);

        config.BackoffMultiplier = multiplier;
        return config;
    }

    /// <summary>
    /// Sets the jitter factor for randomized delays.
    /// </summary>
    /// <param name="config">The retry policy configuration.</param>
    /// <param name="jitterFactor">Jitter factor (0.0 to 1.0).</param>
    /// <returns>The same retry policy configuration for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="jitterFactor"/> is less than 0.0 or greater than 1.0.</exception>
    public static RetryPolicyConfig WithJitter(this RetryPolicyConfig config, double jitterFactor)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentOutOfRangeException.ThrowIfLessThan(jitterFactor, 0.0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(jitterFactor, 1.0);

        config.JitterFactor = jitterFactor;
        return config;
    }
}