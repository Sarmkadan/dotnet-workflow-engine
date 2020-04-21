// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;
using FluentAssertions;
using Xunit;

namespace DotNetWorkflowEngine.Tests;

public static class RetryPolicyConfigTestsExtensions
{
    /// <summary>
    /// Creates a retry policy config with exponential backoff starting at 100ms with multiplier of 2.0
    /// </summary>
    public static RetryPolicyConfig CreateExponentialBackoff(this RetryPolicyConfigTests _, int maxAttempts = 5, int initialDelayMs = 100)
    {
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
    /// Creates a retry policy config with fixed delay
    /// </summary>
    public static RetryPolicyConfig CreateFixedDelay(this RetryPolicyConfigTests _, int maxAttempts = 3, int delayMs = 500)
    {
        return RetryPolicyConfig.CreateFixedDelay(maxAttempts, delayMs);
    }

    /// <summary>
    /// Creates a retry policy config with no retry (max attempts = 1)
    /// </summary>
    public static RetryPolicyConfig CreateNoRetry(this RetryPolicyConfigTests _, int maxAttempts = 1)
    {
        return RetryPolicyConfig.CreateNoRetry();
    }

    /// <summary>
    /// Verifies that the retry policy correctly identifies when max attempts are exhausted
    /// </summary>
    public static void ShouldRetry_WhenMaxAttemptsExhausted_ShouldReturnFalse(this RetryPolicyConfigTests _, RetryPolicyConfig config, int exhaustedAttempt)
    {
        // Act
        var result = config.ShouldRetry(exhaustedAttempt);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that the retry policy correctly identifies retryable exception types
    /// </summary>
    public static void ShouldRetry_WithRetryableExceptionType_ShouldReturnTrue(this RetryPolicyConfigTests _, string exceptionTypeName)
    {
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
    /// Calculates the expected delay for exponential backoff at a specific attempt
    /// </summary>
    public static int CalculateExpectedExponentialDelay(this RetryPolicyConfigTests _, int attempt, int initialDelayMs = 1000)
    {
        return (int)(initialDelayMs * Math.Pow(2.0, attempt - 1));
    }

    /// <summary>
    /// Verifies that the delay calculation respects the max delay constraint
    /// </summary>
    public static void CalculateDelayMs_WithMaxDelayConstraint_ShouldNotExceedMax(this RetryPolicyConfigTests _, RetryPolicyConfig config, int attempt, int expectedMaxDelay)
    {
        // Arrange
        config.MaxDelayMs = expectedMaxDelay;

        // Act
        var delay = config.CalculateDelayMs(attempt);

        // Assert
        delay.Should().BeLessThanOrEqualTo(expectedMaxDelay);
    }
}