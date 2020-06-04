# RetryPolicyConfigExtensions

Provides extension methods for configuring retry policies with linear backoff, custom retry configurations, and exception handling strategies.

## API

### `CreateLinearBackoff`
Creates a retry policy configuration with a linear backoff strategy.

- **Parameters**:
  - `initialDelay` (TimeSpan): The initial delay before the first retry.
  - `maxRetries` (int): The maximum number of retry attempts.
- **Returns**: A `RetryPolicyConfig` configured with linear backoff.
- **Throws**: `ArgumentOutOfRangeException` if `maxRetries` is negative or `initialDelay` is not positive.

### `CreateCustomRetry`
Creates a retry policy configuration with a custom delay sequence.

- **Parameters**:
  - `delays` (IEnumerable<TimeSpan>): The sequence of delays between retries.
  - `maxRetries` (int): The maximum number of retry attempts.
- **Returns**: A `RetryPolicyConfig` configured with the provided delays.
- **Throws**: `ArgumentNullException` if `delays` is null. `ArgumentOutOfRangeException` if `maxRetries` is negative.

### `Clone`
Creates a deep copy of the current retry policy configuration.

- **Returns**: A new `RetryPolicyConfig` with the same settings.
- **Throws**: None.

### `AddRetryableExceptions`
Adds exception types to the list of retryable exceptions.

- **Parameters**:
  - `exceptions` (params Type[]): The exception types to add.
- **Returns**: The same `RetryPolicyConfig` instance for method chaining.
- **Throws**: `ArgumentNullException` if `exceptions` is null.

### `WithRetryableExceptions`
Replaces the list of retryable exceptions with the provided types.

- **Parameters**:
  - `exceptions` (params Type[]): The exception types to set as retryable.
- **Returns**: The same `RetryPolicyConfig` instance for method chaining.
- **Throws**: `ArgumentNullException` if `exceptions` is null.

### `WithRetryOnTimeout`
Configures whether timeout exceptions should be treated as retryable.

- **Parameters**:
  - `retryOnTimeout` (bool): True to retry on timeout exceptions; otherwise, false.
- **Returns**: The same `RetryPolicyConfig` instance for method chaining.

### `WithMaxDelay`
Sets the maximum delay between retries.

- **Parameters**:
  - `maxDelay` (TimeSpan): The maximum delay allowed.
- **Returns**: The same `RetryPolicyConfig` instance for method chaining.
- **Throws**: `ArgumentOutOfRangeException` if `maxDelay` is not positive.

### `WithBackoffMultiplier`
Sets the multiplier applied to the backoff delay on each retry.

- **Parameters**:
  - `multiplier` (double): The multiplier value (must be >= 1.0).
- **Returns**: The same `RetryPolicyConfig` instance for method chaining.
- **Throws**: `ArgumentOutOfRangeException` if `multiplier` is less than 1.0.

### `WithJitter`
Enables or disables jitter (random variation) in retry delays.

- **Parameters**:
  - `enabled` (bool): True to enable jitter; otherwise, false.
- **Returns**: The same `RetryPolicyConfig` instance for method chaining.

## Usage

### Example 1: Linear Backoff Retry
