// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;
using FluentAssertions;
using Xunit;

namespace DotNetWorkflowEngine.Tests;

/// <summary>
/// Provides extension methods for testing <see cref="RetryPolicyConfig"/> configurations and behaviors.
/// </summary>
public static class RetryPolicyConfigTestsExtensions
{
    /// <summary>
    /// Creates a retry policy config with exponential backoff starting at 100ms with multiplier of 2.0.
    /// </summary>
    /// <param name="_">Test context parameter (unused).</param>
    /// <param name="maxAttempts">Maximum number of retry attempts. Defaults to 5.</param>
    /// <param name="initialDelayMs">Initial delay in milliseconds. Defaults to 100.</param>
    /// <returns>A new <see cref="RetryPolicyConfig"/> configured for exponential backoff.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxAttempts"/> is less than 1.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialDelayMs"/> is less than 1.</exception>
    public static RetryPolicyConfig CreateExponentialBackoff(this RetryPolicyConfigTests _, int maxAttempts = 5, int initialDelayMs = 100)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(initialDelayMs, 1);

        return new RetryPolicyConfig
        {
            PolicyType = RetryPolicy.ExponentialBackoff,
            MaxAttempts = maxAttempts,
            InitialDelayMs = initialDelayMs,
            BackoffMultiplier = 2.0,
            JitterFactor = 0,
            MaxDelayMs = 60000
        };
    }

    /// <summary>
    /// Creates a retry policy config with fixed delay.
    /// </summary>
    /// <param name="_">Test context parameter (unused).</param>
    /// <param name="maxAttempts">Maximum number of retry attempts. Defaults to 3.</param>
    /// <param name="delayMs">Delay between attempts in milliseconds. Defaults to 500.</param>
    /// <returns>A new <see cref="RetryPolicyConfig"/> configured for fixed delay.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxAttempts"/> is less than 1.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="delayMs"/> is less than 1.</exception>
    public static RetryPolicyConfig CreateFixedDelay(this RetryPolicyConfigTests _, int maxAttempts = 3, int delayMs = 500)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(delayMs, 1);

        return RetryPolicyConfig.CreateFixedDelay(maxAttempts, delayMs);
    }

    /// <summary>
    /// Creates a retry policy config with no retry (max attempts = 1).
    /// </summary>
    /// <param name="_">Test context parameter (unused).</param>
    /// <param name="maxAttempts">Maximum number of retry attempts. Defaults to 1.</param>
    /// <returns>A new <see cref="RetryPolicyConfig"/> configured for no retry.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxAttempts"/> is less than 1.</exception>
    public static RetryPolicyConfig CreateNoRetry(this RetryPolicyConfigTests _, int maxAttempts = 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        return RetryPolicyConfig.CreateNoRetry();
    }

    /// <summary>
    /// Verifies that the retry policy correctly identifies when max attempts are exhausted.
    /// </summary>
    /// <param name="_">Test context parameter (unused).</param>
    /// <param name="config">The retry policy configuration to test.</param>
    /// <param name="exhaustedAttempt">The attempt number that exceeds <see cref="RetryPolicyConfig.MaxAttempts"/>.</param>
    public static void ShouldRetry_WhenMaxAttemptsExhausted_ShouldReturnFalse(this RetryPolicyConfigTests _, RetryPolicyConfig config, int exhaustedAttempt)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentOutOfRangeException.ThrowIfLessThan(exhaustedAttempt, 1);

        // Act
        var result = config.ShouldRetry(exhaustedAttempt);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that the retry policy correctly identifies retryable exception types.
    /// </summary>
    /// <param name="_">Test context parameter (unused).</param>
    /// <param name="exceptionTypeName">The name of the exception type to check.</param>
    public static void ShouldRetry_WithRetryableExceptionType_ShouldReturnTrue(this RetryPolicyConfigTests _, string exceptionTypeName)
    {
        ArgumentException.ThrowIfNullOrEmpty(exceptionTypeName);

        // Arrange
        var config = new RetryPolicyConfig
        {
            MaxAttempts = 5,
            InitialDelayMs = 100
        };
        config.RetryableExceptionTypes.Add("IOException");
        config.RetryableExceptionTypes.Add("SqlException");

        // Act
        var result = config.ShouldRetry(currentAttempt: 1, exceptionTypeName: exceptionTypeName);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Calculates the expected delay for exponential backoff at a specific attempt without jitter or max delay constraints.
    /// <para>This method provides the theoretical delay calculation for testing purposes.</para>
    /// </summary>
    /// <param name="_">Test context parameter (unused).</param>
    /// <param name="attempt">The attempt number (1-based).</param>
    /// <param name="initialDelayMs">The initial delay in milliseconds. Defaults to 1000.</param>
    /// <returns>The calculated delay in milliseconds.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="attempt"/> is less than 1.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialDelayMs"/> is less than 1.</exception>
    public static int CalculateExpectedExponentialDelay(this RetryPolicyConfigTests _, int attempt, int initialDelayMs = 1000)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(attempt, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(initialDelayMs, 1);

        return (int)(initialDelayMs * Math.Pow(2.0, attempt - 1));
    }

    /// <summary>
    /// Verifies that the delay calculation respects the max delay constraint.
    /// </summary>
    /// <param name="_">Test context parameter (unused).</param>
    /// <param name="config">The retry policy configuration to test.</param>
    /// <param name="attempt">The attempt number to calculate delay for.</param>
    /// <param name="expectedMaxDelay">The expected maximum delay in milliseconds.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
    public static void CalculateDelayMs_WithMaxDelayConstraint_ShouldNotExceedMax(this RetryPolicyConfigTests _, RetryPolicyConfig config, int attempt, int expectedMaxDelay)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentOutOfRangeException.ThrowIfLessThan(attempt, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(expectedMaxDelay, 1);

        // Arrange
        config.MaxDelayMs = expectedMaxDelay;

        // Act
        var delay = config.CalculateDelayMs(attempt);

        // Assert
        delay.Should().BeLessThanOrEqualTo(expectedMaxDelay);
    }
}