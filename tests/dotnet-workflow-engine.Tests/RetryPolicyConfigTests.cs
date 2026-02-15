// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;
using FluentAssertions;
using Xunit;

namespace DotNetWorkflowEngine.Tests;

public class RetryPolicyConfigTests
{
    [Fact]
    public void ShouldRetry_WithNoRetryPolicy_ReturnsFalse()
    {
        // Arrange
        var config = RetryPolicyConfig.CreateNoRetry();

        // Act
        var result = config.ShouldRetry(currentAttempt: 1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRetry_WhenMaxAttemptsExhausted_ReturnsFalse()
    {
        // Arrange
        var config = RetryPolicyConfig.CreateFixedDelay(maxAttempts: 3, delayMs: 500);

        // Act — currentAttempt equals MaxAttempts, no retry allowed
        var result = config.ShouldRetry(currentAttempt: 3);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRetry_WithNonRetryableExceptionType_ReturnsFalse()
    {
        // Arrange
        var config = RetryPolicyConfig.CreateFixedDelay(maxAttempts: 5, delayMs: 200);
        config.RetryableExceptionTypes.Add("IOException");

        // Act — TimeoutException is not in the retryable list
        var result = config.ShouldRetry(currentAttempt: 1, exceptionTypeName: "TimeoutException");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CalculateDelayMs_ExponentialBackoff_GrowsExponentiallyWithNoJitter()
    {
        // Arrange
        var config = new RetryPolicyConfig
        {
            PolicyType = RetryPolicy.ExponentialBackoff,
            InitialDelayMs = 1000,
            BackoffMultiplier = 2.0,
            JitterFactor = 0,   // deterministic: no randomness
            MaxDelayMs = 60000
        };

        // Act
        var attempt1 = config.CalculateDelayMs(1);
        var attempt2 = config.CalculateDelayMs(2);
        var attempt3 = config.CalculateDelayMs(3);

        // Assert
        attempt1.Should().Be(1000);
        attempt2.Should().Be(2000);
        attempt3.Should().Be(4000);
    }
}
