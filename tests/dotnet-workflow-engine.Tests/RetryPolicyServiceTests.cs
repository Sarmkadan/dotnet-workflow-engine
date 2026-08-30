// =============================================================================
// Author: Automated Generation
// =============================================================================

using System;
using System.Collections.Generic;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Constants;
using Xunit;

namespace DotNetWorkflowEngine.Tests
{
    /// <summary>
    /// Tests for <see cref="RetryPolicyService"/> covering the typical retry scenarios:
    ///   • Success on first attempt
    ///   • Transient failures followed by success
    ///   • Exhausted retries resulting in the last exception being thrown
    ///   • Back‑off delay calculations (logic, not wall‑clock time)
    ///   • Validation of policy configuration
    /// </summary>
    public class RetryPolicyServiceTests
    {
        private const string PolicyId = "testPolicy";

        // Simple custom exception used to simulate retryable failures
        private class TestException : Exception { }

        private RetryPolicyService CreateServiceWithExponentialPolicy(int maxRetries = 3)
        {
            var service = new RetryPolicyService();
            var policy = service.CreateExponentialBackoffPolicy(maxRetries);
            service.CreatePolicy(PolicyId, policy);
            service.RegisterRetryableException(PolicyId, typeof(TestException).FullName!);
            return service;
        }

        [Fact]
        public void SucceedsFirstTry_ShouldPerformOneAttempt()
        {
            var service = CreateServiceWithExponentialPolicy();

            int attempts = 0;
            // operation succeeds immediately
            while (true)
            {
                attempts++;
                try
                {
                    // no exception – success
                    break;
                }
                catch (Exception ex)
                {
                    // never reached
                    if (!service.ShouldRetry(PolicyId, attempts, ex.GetType().FullName))
                        throw;
                }
            }

            Assert.Equal(1, attempts);
        }

        [Fact]
        public void TransientFailuresThenSuccess_ShouldRetryUntilSuccess()
        {
            var service = CreateServiceWithExponentialPolicy();

            int attempts = 0;
            const int succeedOnAttempt = 3; // fail twice, succeed on third

            while (true)
            {
                attempts++;
                try
                {
                    if (attempts < succeedOnAttempt)
                        throw new TestException();

                    // success
                    break;
                }
                catch (Exception ex)
                {
                    // Verify that the exception type is considered retryable
                    Assert.True(service.ShouldRetry(PolicyId, attempts, ex.GetType().FullName));
                }
            }

            Assert.Equal(succeedOnAttempt, attempts);
        }

        [Fact]
        public void ExhaustedRetries_ShouldThrowLastException()
        {
            var service = CreateServiceWithExponentialPolicy(maxRetries: 3);

            int attempts = 0;
            Exception? caught = null;

            try
            {
                while (true)
                {
                    attempts++;
                    try
                    {
                        // always fail with a retryable exception
                        throw new TestException();
                    }
                    catch (Exception ex)
                    {
                        // If the policy says we should not retry any more, rethrow
                        if (!service.ShouldRetry(PolicyId, attempts, ex.GetType().FullName))
                            throw;
                    }
                }
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            Assert.NotNull(caught);
            Assert.IsType<TestException>(caught);
            // The number of attempts should equal MaxAttempts (3) before the exception is re‑thrown
            Assert.Equal(3, attempts);
        }

        [Fact]
        public void BackoffDelay_Calculation_ShouldRespectMultiplierAndCap()
        {
            var service = new RetryPolicyService();

            // Create a custom exponential backoff policy with known parameters
            var customPolicy = new RetryPolicyConfig
            {
                MaxAttempts = 4,
                InitialDelayMs = 1000,
                MaxDelayMs = 5000,
                BackoffMultiplier = 2.0,
                JitterFactor = 0.0,
                PolicyType = RetryPolicy.ExponentialBackoff,
                RetryableExceptionTypes = new List<string>()
            };
            service.CreatePolicy(PolicyId, customPolicy);

            // Expected delays: 1s, 2s, 4s, 5s (capped at MaxDelayMs)
            var expected = new[] { 1000, 2000, 4000, 5000 };
            for (int i = 1; i <= expected.Length; i++)
            {
                int delay = service.CalculateRetryDelay(PolicyId, i);
                Assert.Equal(expected[i - 1], delay);
            }
        }

        [Fact]
        public void ValidatePolicy_InvalidConfiguration_ShouldReturnErrors()
        {
            var service = new RetryPolicyService();

            var invalidPolicy = new RetryPolicyConfig
            {
                MaxAttempts = 0,               // invalid
                InitialDelayMs = -5,           // invalid (<=0)
                MaxDelayMs = -10,              // less than InitialDelayMs (-10 < -5)
                BackoffMultiplier = 1.0,      // not greater than 1.0
                JitterFactor = 1.5,            // out of range (>1)
                RetryableExceptionTypes = new List<string>()
            };

            bool isValid = service.ValidatePolicy(invalidPolicy, out List<string> errors);

            Assert.False(isValid);
            Assert.Contains("MaxAttempts must be greater than 0", errors);
            Assert.Contains("InitialDelayMs must be greater than 0", errors);
            Assert.Contains("MaxDelayMs must be greater than or equal to InitialDelayMs", errors);
            Assert.Contains("BackoffMultiplier must be greater than 1.0", errors);
            Assert.Contains("JitterFactor must be between 0 and 1", errors);
        }

        // New tests for the requested coverage

        [Fact]
        public void CreatePolicy_NullConfig_ThrowsArgumentNullException()
        {
            var service = new RetryPolicyService();
            Assert.Throws<ArgumentNullException>(() => service.CreatePolicy("test", null!));
        }

        [Fact]
        public void CreatePolicy_EmptyPolicyId_ThrowsArgumentException()
        {
            var service = new RetryPolicyService();
            var config = new RetryPolicyConfig();
            Assert.Throws<ArgumentException>(() => service.CreatePolicy(string.Empty, config));
        }

        [Fact]
        public void GetPolicy_ExistingPolicy_ReturnsPolicy()
        {
            var service = new RetryPolicyService();
            var config = new RetryPolicyConfig();
            service.CreatePolicy("test", config);
            var result = service.GetPolicy("test");
            Assert.NotNull(result);
            Assert.Same(config, result);
        }

        [Fact]
        public void GetPolicy_NonExistingPolicy_ReturnsNull()
        {
            var service = new RetryPolicyService();
            var result = service.GetPolicy("unknown");
            Assert.Null(result);
        }

        [Fact]
        public void CalculateRetryDelay_UnknownPolicy_ReturnsDefaultRetryDelayMs()
        {
            var service = new RetryPolicyService();
            var delay = service.CalculateRetryDelay("unknown", 1);
            Assert.Equal(Constants.WorkflowConstants.DefaultRetryDelayMs, delay);
        }

        [Fact]
        public void ShouldRetry_UnknownPolicy_ReturnsFalse()
        {
            var service = new RetryPolicyService();
            var result = service.ShouldRetry("unknown", 1, "System.Exception");
            Assert.False(result);
        }

        [Fact]
        public void ShouldRetry_KnownPolicy_ReturnsTrueWhenAttemptLessThanMaxAndExceptionMatches()
        {
            var service = new RetryPolicyService();
            var policy = service.CreateExponentialBackoffPolicy(maxRetries: 3);
            service.CreatePolicy("test", policy);
            service.RegisterRetryableException("test", typeof(TestException).FullName!);
            var result = service.ShouldRetry("test", 1, typeof(TestException).FullName!);
            Assert.True(result);
        }

        [Fact]
        public void ShouldRetry_KnownPolicy_ReturnsFalseWhenAttemptExceedsMax()
        {
            var service = new RetryPolicyService();
            var policy = service.CreateExponentialBackoffPolicy(maxRetries: 3);
            service.CreatePolicy("test", policy);
            service.RegisterRetryableException("test", typeof(TestException).FullName!);
            var result = service.ShouldRetry("test", 4, typeof(TestException).FullName!); // attempt 4 > maxRetries 3
            Assert.False(result);
        }

        [Fact]
        public void ShouldRetry_KnownPolicy_ReturnsTrueWhenExceptionNotRetryableButListEmpty()
        {
            var service = new RetryPolicyService();
            var policy = service.CreateExponentialBackoffPolicy(maxRetries: 3);
            service.CreatePolicy("test", policy);
            // not registering any exception -> empty list
            var result = service.ShouldRetry("test", 1, typeof(TestException).FullName!);
            Assert.True(result);
        }

        [Fact]
        public void ShouldRetry_KnownPolicy_ReturnsFalseWhenExceptionNotRetryableAndListNotEmpty()
        {
            var service = new RetryPolicyService();
            var policy = service.CreateExponentialBackoffPolicy(maxRetries: 3);
            service.CreatePolicy("test", policy);
            // register a different exception
            service.RegisterRetryableException("test", typeof(ArgumentException).FullName!);
            var result = service.ShouldRetry("test", 1, typeof(TestException).FullName!);
            Assert.False(result);
        }
    }
}