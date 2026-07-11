# RetryPolicyConfig

`RetryPolicyConfig` is a configuration class that defines retry behavior for operations, supporting fixed delay, exponential backoff, and no-retry policies. It encapsulates parameters for delay calculation, exception handling, and policy selection to enable resilient execution patterns.

## API

### `PolicyType`
- **Purpose**: Gets or sets the retry policy type.
- **Type**: `RetryPolicy`
- **Remarks**: Must be one of the values defined in the `RetryPolicy` enum (`NoRetry`, `FixedDelay`, `ExponentialBackoff`).

### `MaxAttempts`
- **Purpose**: Gets or sets the maximum number of retry attempts.
- **Type**: `int`
- **Remarks**: Must be a positive integer. Defaults to `1` (no retries) if not specified.

### `InitialDelayMs`
- **Purpose**: Gets or sets the initial delay in milliseconds before the first retry.
- **Type**: `int`
- **Remarks**: Must be a non-negative integer. Used as the base delay in fixed and exponential backoff policies.

### `MaxDelayMs`
- **Purpose**: Gets or sets the maximum delay in milliseconds for any retry attempt.
- **Type**: `int`
- **Remarks**: Must be a positive integer greater than or equal to `InitialDelayMs`. Caps the delay in exponential backoff policies.

### `BackoffMultiplier`
- **Purpose**: Gets or sets the multiplier applied to the delay between retry attempts in exponential backoff policies.
- **Type**: `double`
- **Remarks**: Must be a positive number. Defaults to `2.0` if not specified.

### `JitterFactor`
- **Purpose**: Gets or sets the jitter factor to randomize delays and avoid thundering herds.
- **Type**: `double`
- **Remarks**: Must be a value between `0.0` (no jitter) and `1.0` (max jitter). Applied as a fraction of the current delay.

### `RetryableExceptionTypes`
- **Purpose**: Gets the list of exception type names that should trigger a retry.
- **Type**: `List<string>`
- **Remarks**: Each entry must be a fully qualified exception type name (e.g., `"System.TimeoutException"`). Empty list implies no exception-based retry filtering.

### `RetryOnTimeout`
- **Purpose**: Gets or sets whether to retry on timeout exceptions.
- **Type**: `bool`
- **Remarks**: Defaults to `false`. Ignored if `RetryableExceptionTypes` is non-empty.

### `CalculateDelayMs(int attempt)`
- **Purpose**: Computes the delay in milliseconds for the given retry attempt.
- **Parameters**:
  - `attempt` – The current retry attempt number (1-based).
- **Returns**: The computed delay in milliseconds.
- **Throws**:
  - `InvalidOperationException` – If `PolicyType` is `NoRetry`.
  - `ArgumentOutOfRangeException` – If `attempt` is less than `1`.

### `ShouldRetry(Exception exception, int attempt)`
- **Purpose**: Determines whether a retry should be attempted for the given exception and attempt.
- **Parameters**:
  - `exception` – The exception that triggered the retry check.
  - `attempt` – The current retry attempt number (1-based).
- **Returns**: `true` if a retry should be performed; otherwise, `false`.
- **Throws**: `ArgumentNullException` – If `exception` is `null`.

### `CreateNoRetry()`
- **Purpose**: Creates a configuration for a policy that never retries.
- **Returns**: A `RetryPolicyConfig` instance with `PolicyType` set to `NoRetry` and default values for other properties.

### `CreateFixedDelay(int maxAttempts, int delayMs)`
- **Purpose**: Creates a configuration for a fixed delay retry policy.
- **Parameters**:
  - `maxAttempts` – The maximum number of retry attempts.
  - `delayMs` – The fixed delay in milliseconds between retries.
- **Returns**: A `RetryPolicyConfig` instance with `PolicyType` set to `FixedDelay`.
- **Throws**:
  - `ArgumentOutOfRangeException` – If `maxAttempts` is less than `1` or `delayMs` is negative.

### `CreateExponentialBackoff(int maxAttempts, int initialDelayMs, int maxDelayMs, double backoffMultiplier = 2.0, double jitterFactor = 0.1)`
- **Purpose**: Creates a configuration for an exponential backoff retry policy with optional jitter.
- **Parameters**:
  - `maxAttempts` – The maximum number of retry attempts.
  - `initialDelayMs` – The initial delay in milliseconds.
  - `maxDelayMs` – The maximum delay in milliseconds.
  - `backoffMultiplier` – The multiplier for exponential backoff (default: `2.0`).
  - `jitterFactor` – The jitter factor (default: `0.1`).
- **Returns**: A `RetryPolicyConfig` instance with `PolicyType` set to `ExponentialBackoff`.
- **Throws**:
  - `ArgumentOutOfRangeException` – If any numeric parameter is invalid (e.g., `maxAttempts < 1`, `initialDelayMs < 0`, `maxDelayMs <= 0`, `backoffMultiplier <= 0`, or `jitterFactor` outside `[0.0, 1.0]`).

## Usage

### Example 1: Fixed Delay Retry
