# RetryPolicyConfigTests

Unit tests for the retry policy configuration logic, verifying behavior of retry attempts, delay calculations, and exception handling under different policy settings.

## API

### `ShouldRetry_WithNoRetryPolicy_ReturnsFalse()`

Verifies that the retry policy correctly identifies no retry should occur when no retry policy is configured.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: Does not throw under any scenario.

### `ShouldRetry_WhenMaxAttemptsExhausted_ReturnsFalse()`

Ensures that retry logic returns `false` once the maximum allowed retry attempts have been exhausted.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: Does not throw under any scenario.

### `ShouldRetry_WithNonRetryableExceptionType_ReturnsFalse()`

Confirms that exceptions of non-retryable types are not retried, even if retry attempts remain.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: Does not throw under any scenario.

### `CalculateDelayMs_ExponentialBackoff_GrowsExponentiallyWithNoJitter()`

Validates that the delay between retry attempts grows exponentially according to the configured backoff strategy, with no random jitter applied.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: Does not throw under any scenario.

## Usage
