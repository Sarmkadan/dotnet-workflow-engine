# RetryPolicyService

The `RetryPolicyService` is a core utility within the workflow engine responsible for defining, managing, and executing retry logic for transient failures. It provides mechanisms to configure retry strategies such as fixed delays and exponential backoff, validate policy configurations, and determine at runtime whether an operation should be retried based on registered exception types and elapsed time. This service centralizes the decision-making process for fault tolerance, ensuring consistent behavior across workflow executions.

## API

### `CreatePolicy`
Registers a new retry policy configuration within the service.
*   **Parameters**: Accepts a `RetryPolicyConfig` instance defining the policy rules.
*   **Returns**: `void`.
*   **Throws**: May throw an exception if the provided configuration is invalid or if a policy with the same identifier already exists.

### `GetPolicy`
Retrieves an existing retry policy configuration by its identifier or criteria.
*   **Parameters**: Typically accepts a policy name or key (specific parameter signature depends on internal overload resolution).
*   **Returns**: `RetryPolicyConfig?`. Returns the configuration if found; otherwise, returns `null`.
*   **Throws**: Generally does not throw, returning `null` for missing policies.

### `CalculateRetryDelay`
Computes the specific wait time in milliseconds before the next retry attempt based on the current attempt number and the active policy.
*   **Parameters**: Accepts the current retry attempt count and the relevant `RetryPolicyConfig`.
*   **Returns**: `int`. The delay in milliseconds.
*   **Throws**: May throw if the policy configuration is malformed or if arithmetic overflow occurs during calculation.

### `ShouldRetry`
Determines whether an operation should be retried following a specific exception.
*   **Parameters**: Accepts the caught `Exception` instance and the current attempt count.
*   **Returns**: `bool`. `true` if the exception type is registered as retryable and the maximum attempt limit has not been reached; otherwise, `false`.
*   **Throws**: Does not typically throw; returns `false` for null exceptions or unregistered types.

### `CreateExponentialBackoffPolicy`
Factory method that generates a `RetryPolicyConfig` configured for exponential backoff.
*   **Parameters**: Accepts initial delay, growth factor, and maximum retry count.
*   **Returns**: `RetryPolicyConfig`. A new configuration instance ready for registration or simulation.
*   **Throws**: Throws if parameters are negative or if the calculated delay exceeds system limits.

### `CreateFixedDelayPolicy`
Factory method that generates a `RetryPolicyConfig` configured for a constant delay between retries.
*   **Parameters**: Accepts the fixed delay in milliseconds and the maximum retry count.
*   **Returns**: `RetryPolicyConfig`. A new configuration instance.
*   **Throws**: Throws if the delay or retry count is invalid (e.g., negative values).

### `CreateNoRetryPolicy`
Factory method that generates a `RetryPolicyConfig` which effectively disables retry logic.
*   **Parameters**: None (or optional metadata depending on overload).
*   **Returns**: `RetryPolicyConfig`. A configuration indicating zero retries.
*   **Throws**: Unlikely to throw under normal conditions.

### `SimulateRetryDelays`
Generates a sequence of delay intervals that would occur for a full retry cycle under a specific policy without actually waiting.
*   **Parameters**: Accepts a `RetryPolicyConfig` and the number of attempts to simulate.
*   **Returns**: `List<int>`. A list of delay values in milliseconds for each subsequent attempt.
*   **Throws**: Throws if the policy is null or if the simulation count is less than zero.

### `GetTotalRetryTimeMs`
Calculates the cumulative time required to exhaust all retry attempts for a given policy.
*   **Parameters**: Accepts a `RetryPolicyConfig`.
*   **Returns**: `long`. The total time in milliseconds.
*   **Throws**: Throws if the policy is null or if the total time exceeds `long` capacity.

### `ValidatePolicy`
Verifies the integrity and logical consistency of a `RetryPolicyConfig`.
*   **Parameters**: Accepts the `RetryPolicyConfig` to validate.
*   **Returns**: `bool`. `true` if the policy is valid; `false` otherwise.
*   **Throws**: Typically returns `false` rather than throwing, unless the input object is critically malformed.

### `RegisterRetryableException`
Adds a specific exception type to the list of errors that trigger a retry.
*   **Parameters**: Accepts a `Type` object representing the exception class.
*   **Returns**: `void`.
*   **Throws**: May throw if the type is not derived from `System.Exception` or is null.

### `ClearPolicies`
Removes all registered retry policies from the service memory.
*   **Parameters**: None.
*   **Returns**: `void`.
*   **Throws**: Does not throw; safe to call even if no policies exist.

## Usage

### Example 1: Configuring and Validating an Exponential Backoff Strategy
This example demonstrates creating a policy, validating it, and simulating the resulting delays to ensure they meet operational constraints before applying them to a workflow.

```csharp
var service = new RetryPolicyService();

// Create an exponential backoff policy: 1s start, 2x growth, max 5 retries
var policy = service.CreateExponentialBackoffPolicy(
    initialDelayMs: 1000,
    growthFactor: 2.0,
    maxRetries: 5
);

// Validate the configuration
if (!service.ValidatePolicy(policy))
{
    throw new InvalidOperationException("Generated policy is invalid.");
}

// Simulate delays to inspect timing behavior
var delays = service.SimulateRetryDelays(policy, 5);
// Expected output: [1000, 2000, 4000, 8000, 16000]

var totalWaitTime = service.GetTotalRetryTimeMs(policy);
Console.WriteLine($"Total worst-case wait time: {totalWaitTime} ms");

// Register the policy for use by the workflow engine
service.CreatePolicy(policy);
```

### Example 2: Runtime Retry Decision Logic
This example shows how to use the service within a catch block to determine if a failed operation should be retried based on registered exception types and attempt counts.

```csharp
var service = new RetryPolicyService();

// Register specific transient exceptions that warrant a retry
service.RegisterRetryableException(typeof(SqlException));
service.RegisterRetryableException(typeof(TimeoutException));

// Assume a policy named "Default" was previously created
var policy = service.GetPolicy("Default");

int currentAttempt = 3;
Exception caughtEx = new SqlException("Connection timeout");

// Determine if the workflow should schedule another attempt
if (policy != null && service.ShouldRetry(caughtEx, currentAttempt))
{
    int delayMs = service.CalculateRetryDelay(currentAttempt, policy);
    
    Console.WriteLine($"Retrying in {delayMs}ms...");
    Thread.Sleep(delayMs);
    
    // Execute retry logic here
}
else
{
    Console.WriteLine("Max retries reached or exception type not retryable. Failing workflow.");
    throw;
}
```

## Notes

*   **Thread Safety**: The method signatures suggest a mutable internal state (e.g., `CreatePolicy`, `ClearPolicies`, `RegisterRetryableException`). While read operations like `ShouldRetry` and `CalculateRetryDelay` may be stateless regarding the specific call, they depend on the current registry state. It is recommended to treat `RetryPolicyService` as not thread-safe for write operations. Concurrent calls to `ClearPolicies` while other threads are evaluating `ShouldRetry` may result in inconsistent behavior or race conditions. External synchronization or initialization during a single-threaded startup phase is advised.
*   **Null Handling**: `GetPolicy` explicitly returns a nullable `RetryPolicyConfig?`. Callers must handle the `null` case where a policy identifier is not found. Similarly, `ShouldRetry` likely returns `false` immediately if the provided exception is null or not in the registered list, preventing unnecessary processing.
*   **Overflow Risks**: Methods calculating time (`CalculateRetryDelay`, `GetTotalRetryTimeMs`) return `int` and `long` respectively. When configuring aggressive exponential backoff policies with high retry counts, developers must ensure the calculated delays do not exceed the bounds of these types, which would trigger runtime exceptions.
*   **Validation Scope**: `ValidatePolicy` performs logical checks but does not guarantee that a policy will succeed in a live environment (e.g., it cannot predict network conditions). It strictly validates configuration parameters such as negative delays or illogical retry counts.
