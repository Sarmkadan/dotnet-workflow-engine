// =============================================================================
// Author: Automated Generation
// =============================================================================

using System;
using System.Collections.Generic;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
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

            var exception = Assert.Throws<TestException>(() =>
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
            });

            // The number of attempts should equal MaxAttempts (3) before the exception is re‑thrown
            Assert.Equal(3, attempts);
            Assert.IsType<TestException>(exception);
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
                InitialDelayMs = -10,          // invalid
                MaxDelayMs = 5,                // less than InitialDelayMs
                BackoffMultiplier = 1.0,      // not greater than 1.0
                JitterFactor = 1.5,            // out of range
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
    }
}
