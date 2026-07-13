// ... (rest of the README content remains unchanged)

## RetryPolicyConfigExtensions

The `RetryPolicyConfigExtensions` class provides a set of extension methods for creating and modifying retry policy configurations. These extensions simplify the process of defining retry behaviors for workflow activities.

### Usage Example

Here's an example of using `RetryPolicyConfigExtensions` to create a custom retry policy:

```csharp
var retryConfig = RetryPolicyConfigExtensions.CreateLinearBackoff(
    maxAttempts: 5,
    initialDelayMs: 2000,
    backoffMultiplier: 1.5
);

var policyWithJitter = retryConfig.WithJitter(0.1);
var policyWithMaxDelay = policyWithJitter.WithMaxDelay(TimeSpan.FromMinutes(5));

var executedWithRetry = await ExecuteWithRetryAsync(
    action: () => DoWork(),
    retryPolicy: policyWithMaxDelay
);
```

The extension methods available on `RetryPolicyConfigExtensions` include:

* `CreateLinearBackoff`: Creates a retry policy with linear backoff.
* `CreateCustomRetry`: Creates a custom retry policy with specified parameters.
* `Clone`: Creates a deep copy of a retry policy configuration.
* `AddRetryableExceptions`: Adds additional exception types to retry on.
* `WithRetryableExceptions`: Sets the retryable exception types.
* `WithRetryOnTimeout`: Configures retry on timeout.
* `WithMaxDelay`: Sets the maximum delay between retries.
* `WithBackoffMultiplier`: Sets the backoff multiplier for exponential backoff.
* `WithJitter`: Adds random jitter to the retry delay.
