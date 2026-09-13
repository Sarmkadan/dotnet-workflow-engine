// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Services;

/// <summary>
/// Provides registration, evaluation, validation, and delay calculations for retry policies.
/// </summary>
public class RetryPolicyService
{
    private static readonly Random JitterRandom = Random.Shared;
    private readonly Dictionary<string, RetryPolicyConfig> _policies = new();

    /// <summary>
    /// Creates or replaces the retry policy registered with the specified identifier.
    /// </summary>
    /// <param name="policyId">The identifier used to register the policy.</param>
    /// <param name="config">The retry policy configuration to register.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="policyId"/> is <see langword="null"/> or empty.
    /// </exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is <see langword="null"/>.</exception>
    public void CreatePolicy(string policyId, RetryPolicyConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrEmpty(policyId);
        _policies[policyId] = config;
    }

    /// <summary>
    /// Gets the retry policy registered with the specified identifier.
    /// </summary>
    /// <param name="policyId">The identifier of the policy to retrieve.</param>
    /// <returns>The registered policy, or <see langword="null"/> when no matching policy exists.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="policyId"/> is <see langword="null"/> or empty.
    /// </exception>
    public RetryPolicyConfig? GetPolicy(string policyId)
    {
        ArgumentException.ThrowIfNullOrEmpty(policyId);
        _policies.TryGetValue(policyId, out var policy);
        return policy;
    }

    /// <summary>
    /// Calculates the delay before the specified retry attempt.
    /// </summary>
    /// <param name="policyId">The identifier of the policy used to calculate the delay.</param>
    /// <param name="attemptNumber">The zero-based or positive attempt number.</param>
    /// <returns>
    /// The non-negative delay in milliseconds, or the default retry delay when the policy is not registered.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="policyId"/> is <see langword="null"/> or empty.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="attemptNumber"/> is negative.</exception>
    public int CalculateRetryDelay(string policyId, int attemptNumber)
    {
        ArgumentException.ThrowIfNullOrEmpty(policyId);
        if (attemptNumber < 0)
            throw new ArgumentOutOfRangeException(nameof(attemptNumber), "Attempt number cannot be negative.");
        var policy = GetPolicy(policyId);
        if (policy == null)
            return Constants.WorkflowConstants.DefaultRetryDelayMs;

        var delay = policy.CalculateDelayMs(attemptNumber);
        return Math.Max(0, delay);
    }

    /// <summary>
    /// Calculates the next retry delay with bounded random jitter.
    /// </summary>
    /// <param name="policyId">The identifier of the policy used to calculate the delay.</param>
    /// <param name="attemptNumber">The zero-based or positive attempt number.</param>
    /// <param name="jitterFactor">
    /// The maximum proportional variation applied above or below the calculated delay, from <c>0</c> through <c>1</c>.
    /// </param>
    /// <returns>The jittered delay in milliseconds, clamped to the range from zero through <see cref="int.MaxValue"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="policyId"/> is <see langword="null"/> or empty.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="attemptNumber"/> is negative, or when <paramref name="jitterFactor"/> is not between
    /// <c>0</c> and <c>1</c>, inclusive.
    /// </exception>
    public int CalculateRetryDelayWithJitter(string policyId, int attemptNumber, double jitterFactor = 0.2)
    {
        if (double.IsNaN(jitterFactor) || jitterFactor < 0 || jitterFactor > 1)
            throw new ArgumentOutOfRangeException(nameof(jitterFactor), "Jitter factor must be between 0 and 1.");

        var delay = CalculateRetryDelay(policyId, attemptNumber);
        var jitter = delay * jitterFactor * (JitterRandom.NextDouble() * 2 - 1);
        return (int)Math.Clamp(delay + jitter, 0, int.MaxValue);
    }

    /// <summary>
    /// Determines whether another attempt should be made for a registered policy.
    /// </summary>
    /// <param name="policyId">The identifier of the policy to evaluate.</param>
    /// <param name="currentAttempt">The current attempt number.</param>
    /// <param name="exceptionTypeName">The optional exception type name that caused the current attempt to fail.</param>
    /// <returns>
    /// <see langword="true"/> when the policy permits another attempt; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="policyId"/> is <see langword="null"/> or empty.
    /// </exception>
    public bool ShouldRetry(string policyId, int currentAttempt, string? exceptionTypeName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(policyId);
        var policy = GetPolicy(policyId);
        if (policy == null)
            return false;

        return policy.ShouldRetry(currentAttempt, exceptionTypeName);
    }

    /// <summary>
    /// Creates a default exponential backoff policy.
    /// </summary>
    /// <param name="maxRetries">The maximum number of attempts allowed by the policy.</param>
    /// <returns>An exponential backoff retry policy using the engine's default and maximum delay values.</returns>
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
    /// <param name="maxRetries">The maximum number of attempts allowed by the policy.</param>
    /// <param name="delayMs">The delay between attempts, in milliseconds.</param>
    /// <returns>A fixed-delay retry policy with the specified settings.</returns>
    public RetryPolicyConfig CreateFixedDelayPolicy(int maxRetries = 3, int delayMs = 1000)
    {
        return RetryPolicyConfig.CreateFixedDelay(maxRetries, delayMs);
    }

    /// <summary>
    /// Creates a no-retry policy.
    /// </summary>
    /// <returns>A policy configuration that does not permit retries.</returns>
    public RetryPolicyConfig CreateNoRetryPolicy()
    {
        return RetryPolicyConfig.CreateNoRetry();
    }

    /// <summary>
    /// Simulates retry delays for analysis.
    /// </summary>
    /// <param name="policyId">The identifier of the policy whose delays are simulated.</param>
    /// <param name="maxAttempts">The number of attempts to simulate.</param>
    /// <returns>
    /// A list containing the calculated delay for each attempt from one through <paramref name="maxAttempts"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="policyId"/> is <see langword="null"/> or empty.
    /// </exception>
    public List<int> SimulateRetryDelays(string policyId, int maxAttempts)
    {
        ArgumentException.ThrowIfNullOrEmpty(policyId);
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
    /// <param name="policyId">The identifier of the policy to estimate.</param>
    /// <returns>
    /// The total delay in milliseconds for all attempts after the initial attempt, or zero when the policy is not registered.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="policyId"/> is <see langword="null"/> or empty.
    /// </exception>
    public long GetTotalRetryTimeMs(string policyId)
    {
        ArgumentException.ThrowIfNullOrEmpty(policyId);
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
    /// <param name="config">The retry policy configuration to validate.</param>
    /// <param name="errors">When this method returns, contains descriptions of any validation errors.</param>
    /// <returns><see langword="true"/> when the configuration is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is <see langword="null"/>.</exception>
    public bool ValidatePolicy(RetryPolicyConfig config, out List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(config);
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
    /// <param name="policyId">The identifier of the policy to update.</param>
    /// <param name="exceptionTypeName">The exception type name to add to the policy.</param>
    /// <remarks>
    /// This method has no effect when the policy is not registered or the exception type is already present.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="policyId"/> is <see langword="null"/> or empty.
    /// </exception>
    public void RegisterRetryableException(string policyId, string exceptionTypeName)
    {
        var policy = GetPolicy(policyId);
        if (policy != null && !policy.RetryableExceptionTypes.Contains(exceptionTypeName))
        {
            policy.RetryableExceptionTypes.Add(exceptionTypeName);
        }
    }

    /// <summary>
    /// Removes all registered retry policies.
    /// </summary>
    public void ClearPolicies()
    {
        _policies.Clear();
    }
}
