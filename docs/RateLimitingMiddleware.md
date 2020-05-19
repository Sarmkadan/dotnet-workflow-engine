# RateLimitingMiddleware

The `RateLimitingMiddleware` component provides a token-bucket style rate limiting mechanism for the workflow engine, controlling the throughput of incoming requests to prevent resource exhaustion. It tracks request consumption within a configurable time window, exposing metrics such as the current reset time and remaining capacity, while offering both automatic middleware invocation and manual consumption checks for flexible integration patterns.

## API

### Constructor
**`public RateLimitingMiddleware()`**
Initializes a new instance of the `RateLimitingMiddleware` class. Default configuration values for limits and windows are applied unless explicitly set via properties prior to use.

### Methods

**`public async Task InvokeAsync()`**
Executes the middleware logic for an incoming request. This method attempts to consume a token from the internal bucket. If the rate limit is exceeded, it may delay execution or throw an exception depending on the internal implementation strategy relative to `RetryAfterSeconds`.
*   **Parameters**: None (operates on the current context).
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Throws**: May throw an exception if the rate limit is strictly enforced and no tokens are available after the retry period.

**`public bool TryConsume()`**
Attempts to consume a single request token from the rate limit bucket without blocking.
*   **Parameters**: None.
*   **Return Value**: Returns `true` if a token was successfully consumed; otherwise, `false` if the limit has been reached for the current window.
*   **Throws**: Does not throw exceptions under normal operation.

### Properties

**`public int MaxRequests`**
Gets or sets the maximum number of requests allowed within the defined time window. Modifying this value affects subsequent window calculations.

**`public int WindowSeconds`**
Gets or sets the duration of the sliding or fixed time window in seconds during which `MaxRequests` are permitted.

**`public int RetryAfterSeconds`**
Gets or sets the suggested wait time in seconds for a client when a rate limit is exceeded. This value is often used to populate `Retry-After` headers in HTTP responses.

**`public DateTime ResetTime`**
Gets the specific point in time when the current rate limit window resets and the bucket refills. This value is read-only and updates dynamically as windows expire.

**`public RateLimitBucket`**
Gets the underlying `RateLimitBucket` instance responsible for storing the current state of token consumption. This exposes low-level metrics for monitoring or logging purposes.

## Usage

### Example 1: Manual Consumption Check
This example demonstrates how to manually check rate limits before executing a heavy operation, allowing for custom rejection logic without invoking the full middleware pipeline.

```csharp
var middleware = new RateLimitingMiddleware
{
    MaxRequests = 100,
    WindowSeconds = 60,
    RetryAfterSeconds = 5
};

if (middleware.TryConsume())
{
    // Proceed with workflow execution
    await ExecuteWorkflowStepAsync();
}
else
{
    // Handle rejection immediately
    Console.WriteLine($"Rate limit exceeded. Try again after {middleware.ResetTime}.");
}
```

### Example 2: Middleware Pipeline Integration
This example illustrates the standard usage where the middleware wraps the request processing pipeline, automatically managing throttling and reset times.

```csharp
var middleware = new RateLimitingMiddleware
{
    MaxRequests = 50,
    WindowSeconds = 10
};

try
{
    // Invoke the middleware logic for the current request context
    await middleware.InvokeAsync();
    
    // Continue processing if invocation succeeds
    ProcessRequest();
}
catch (Exception ex)
{
    // Log failure due to rate limiting
    Logger.LogWarning($"Request throttled. Reset occurs at {middleware.ResetTime}");
}
```

## Notes

*   **Thread Safety**: The presence of the `RateLimitBucket` property and the `TryConsume` method suggests that the internal state is shared. While `InvokeAsync` is asynchronous, callers should ensure that modifications to configuration properties (`MaxRequests`, `WindowSeconds`) are not performed concurrently with request processing to avoid race conditions in window calculation.
*   **Time Skew**: The `ResetTime` property returns a `DateTime` which should be treated as the authoritative source for window expiration. Clients relying on local clocks for retry logic may experience discrepancies if server time drifts.
*   **Zero-Window Edge Case**: Setting `WindowSeconds` to zero may result in undefined behavior or an immediate reset of the bucket depending on the `RateLimitBucket` implementation; it is recommended to maintain a positive integer value.
*   **Consumption Semantics**: `TryConsume` is non-blocking and returns a boolean immediately. In contrast, `InvokeAsync` may involve awaiting delays based on `RetryAfterSeconds` before completing or failing the task.
