// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;

namespace DotNetWorkflowEngine.Models;

/// <summary>
/// Configuration for activity retry behavior.
/// </summary>
public class RetryPolicyConfig
{
    /// <summary>Gets or sets the retry policy type.</summary>
    public RetryPolicy PolicyType { get; set; } = RetryPolicy.NoRetry;

    /// <summary>Gets or sets the maximum number of retry attempts.</summary>
    public int MaxAttempts { get; set; } = 1;

    /// <summary>Gets or sets the initial delay in milliseconds.</summary>
    public int InitialDelayMs { get; set; } = 1000;

    /// <summary>Gets or sets the maximum delay in milliseconds.</summary>
    public int MaxDelayMs { get; set; } = 300000; // 5 minutes

    /// <summary>Gets or sets the backoff multiplier for exponential/linear backoff.</summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>Gets or sets the jitter factor (0.0 to 1.0) to randomize delays.</summary>
    public double JitterFactor { get; set; } = 0.1;

    /// <summary>Gets or sets which exception types should trigger a retry.</summary>
    public List<string> RetryableExceptionTypes { get; set; } = new();

    /// <summary>Gets or sets whether to retry on timeout.</summary>
    public bool RetryOnTimeout { get; set; } = true;

    /// <summary>
    /// Calculates the delay for a given attempt number.
    /// </summary>
    public int CalculateDelayMs(int attemptNumber)
    {
        if (attemptNumber <= 1)
            return InitialDelayMs;

        var delay = PolicyType switch
        {
            RetryPolicy.FixedDelay => InitialDelayMs,
            RetryPolicy.ExponentialBackoff => (int)(InitialDelayMs * Math.Pow(BackoffMultiplier, attemptNumber - 1)),
            RetryPolicy.LinearBackoff => InitialDelayMs * attemptNumber,
            _ => InitialDelayMs
        };

        // Apply jitter
        if (JitterFactor > 0)
        {
            var random = new Random();
            var jitter = random.NextDouble() * JitterFactor;
            delay = (int)(delay * (1 + jitter));
        }

        // Cap at maximum delay
        return Math.Min(delay, MaxDelayMs);
    }

    /// <summary>
    /// Checks if another attempt should be made.
    /// </summary>
    public bool ShouldRetry(int currentAttempt, string? exceptionTypeName = null)
    {
        if (PolicyType == RetryPolicy.NoRetry)
            return false;

        if (currentAttempt >= MaxAttempts)
            return false;

        if (!string.IsNullOrEmpty(exceptionTypeName))
        {
            if (RetryableExceptionTypes.Count > 0 && !RetryableExceptionTypes.Contains(exceptionTypeName))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Creates a default no-retry configuration.
    /// </summary>
    public static RetryPolicyConfig CreateNoRetry()
    {
        return new RetryPolicyConfig
        {
            PolicyType = RetryPolicy.NoRetry,
            MaxAttempts = 1
        };
    }

    /// <summary>
    /// Creates a fixed delay retry configuration.
    /// </summary>
    public static RetryPolicyConfig CreateFixedDelay(int maxAttempts, int delayMs)
    {
        return new RetryPolicyConfig
        {
            PolicyType = RetryPolicy.FixedDelay,
            MaxAttempts = maxAttempts,
            InitialDelayMs = delayMs
        };
    }

    /// <summary>
    /// Creates an exponential backoff retry configuration.
    /// </summary>
    public static RetryPolicyConfig CreateExponentialBackoff(int maxAttempts, int initialDelayMs, int maxDelayMs)
    {
        return new RetryPolicyConfig
        {
            PolicyType = RetryPolicy.ExponentialBackoff,
            MaxAttempts = maxAttempts,
            InitialDelayMs = initialDelayMs,
            MaxDelayMs = maxDelayMs
        };
    }
}
