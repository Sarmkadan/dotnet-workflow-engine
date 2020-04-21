// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Models;

/// <summary>
/// Extension methods for <see cref="RetryPolicyConfig"/> to provide additional retry policy utilities.
/// </summary>
public static class RetryPolicyConfigExtensions
{
    /// <summary>
    /// Creates a linear backoff retry configuration.
    /// </summary>
    /// <param name="maxAttempts">Maximum number of retry attempts.</param>
    /// <param name="initialDelayMs">Initial delay in milliseconds.</param>
    /// <param name="maxDelayMs">Maximum delay in milliseconds (optional).</param>
    /// <param name="jitterFactor">Jitter factor to randomize delays (optional).</param>
    /// <returns>Configured retry policy.</returns>
    public static RetryPolicyConfig CreateLinearBackoff(this int maxAttempts, int initialDelayMs, int maxDelayMs = 300000, double jitterFactor = 0.1)
    {
        return new RetryPolicyConfig
        {
            PolicyType = Enums.RetryPolicy.LinearBackoff,
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
    /// <param name="maxAttempts">Maximum number of retry attempts.</param>
    /// <param name="initialDelayMs">Initial delay in milliseconds.</param>
    /// <param name="maxDelayMs">Maximum delay in milliseconds.</param>
    /// <param name="backoffMultiplier">Backoff multiplier for exponential/linear backoff.</param>
    /// <param name="jitterFactor">Jitter factor to randomize delays.</param>
    /// <param name="retryableExceptionTypes">List of exception types that should trigger retry.</param>
    /// <param name="retryOnTimeout">Whether to retry on timeout.</param>
    /// <returns>Configured retry policy.</returns>
    public static RetryPolicyConfig CreateCustomRetry(
        this Enums.RetryPolicy policyType,
        int maxAttempts = 3,
        int initialDelayMs = 1000,
        int maxDelayMs = 300000,
        double backoffMultiplier = 2.0,
        double jitterFactor = 0.1,
        List<string>? retryableExceptionTypes = null,
        bool retryOnTimeout = true)
    {
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
    public static RetryPolicyConfig Clone(this RetryPolicyConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

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
    public static RetryPolicyConfig AddRetryableExceptions(this RetryPolicyConfig config, params string[] exceptionTypes)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        if (exceptionTypes != null && exceptionTypes.Length > 0)
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
    public static RetryPolicyConfig WithRetryableExceptions(this RetryPolicyConfig config, params string[] exceptionTypes)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        config.RetryableExceptionTypes = new List<string>(exceptionTypes ?? Array.Empty<string>());
        return config;
    }

    /// <summary>
    /// Sets whether to retry on timeout.
    /// </summary>
    /// <param name="config">The retry policy configuration.</param>
    /// <param name="retryOnTimeout">Whether to retry on timeout.</param>
    /// <returns>The same retry policy configuration for method chaining.</returns>
    public static RetryPolicyConfig WithRetryOnTimeout(this RetryPolicyConfig config, bool retryOnTimeout)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        config.RetryOnTimeout = retryOnTimeout;
        return config;
    }

    /// <summary>
    /// Sets the maximum delay for the retry policy.
    /// </summary>
    /// <param name="config">The retry policy configuration.</param>
    /// <param name="maxDelayMs">Maximum delay in milliseconds.</param>
    /// <returns>The same retry policy configuration for method chaining.</returns>
    public static RetryPolicyConfig WithMaxDelay(this RetryPolicyConfig config, int maxDelayMs)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        config.MaxDelayMs = maxDelayMs;
        return config;
    }

    /// <summary>
    /// Sets the backoff multiplier for exponential or linear backoff policies.
    /// </summary>
    /// <param name="config">The retry policy configuration.</param>
    /// <param name="multiplier">Backoff multiplier value.</param>
    /// <returns>The same retry policy configuration for method chaining.</returns>
    public static RetryPolicyConfig WithBackoffMultiplier(this RetryPolicyConfig config, double multiplier)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        config.BackoffMultiplier = multiplier;
        return config;
    }

    /// <summary>
    /// Sets the jitter factor for randomized delays.
    /// </summary>
    /// <param name="config">The retry policy configuration.</param>
    /// <param name="jitterFactor">Jitter factor (0.0 to 1.0).</param>
    /// <returns>The same retry policy configuration for method chaining.</returns>
    public static RetryPolicyConfig WithJitter(this RetryPolicyConfig config, double jitterFactor)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        config.JitterFactor = jitterFactor;
        return config;
    }
}