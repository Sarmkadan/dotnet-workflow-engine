# Retry policy service

`RetryPolicyService` is an in-memory registry for named `RetryPolicyConfig` objects. It calculates retry delays, decides whether a registered policy permits another attempt, creates common policy configurations, and provides validation and reporting helpers.

The service is implemented in `Services/RetryPolicyService.cs` in the `DotNetWorkflowEngine.Services` namespace. Policies are stored by string ID for the lifetime of the service. Registering the same ID again replaces the previous configuration; `ClearPolicies` removes every registration.

## RetryPolicy values

`RetryPolicy`, defined in `Enums/RetryPolicy.cs`, selects the delay strategy used by `RetryPolicyConfig`.

| Value | Numeric value | Behavior |
| --- | ---: | --- |
| `NoRetry` | `0` | `ShouldRetry` always returns `false`. If a delay is calculated directly, it falls back to `InitialDelayMs`. |
| `FixedDelay` | `1` | Uses `InitialDelayMs` for every attempt. |
| `ExponentialBackoff` | `2` | For attempts after the first, calculates `InitialDelayMs * BackoffMultiplier^(attemptNumber - 1)`. |
| `LinearBackoff` | `3` | For attempts after the first, calculates `InitialDelayMs * attemptNumber`. |
| `Custom` | `4` | Reserved for custom retry logic. `RetryPolicyConfig.CalculateDelayMs` currently falls back to `InitialDelayMs`. |

`RetryPolicyConfig` also controls `MaxAttempts`, `MaxDelayMs`, `BackoffMultiplier`, `JitterFactor`, retryable exception type names, and timeout behavior. `RetryOnTimeout` is configuration data; `RetryPolicyService.ShouldRetry` does not inspect it directly.

## CalculateRetryDelay algorithm

`CalculateRetryDelay(string policyId, int attemptNumber)` performs these steps:

1. Rejects a null or empty `policyId` with `ArgumentException` and a negative `attemptNumber` with `ArgumentOutOfRangeException`. Attempt number `0` is accepted.
2. Looks up the registered policy. If the ID is unknown, returns `WorkflowConstants.DefaultRetryDelayMs` (`1000` ms).
3. Calls `RetryPolicyConfig.CalculateDelayMs(attemptNumber)` for a known policy.
4. Clamps the returned value to a minimum of zero.

`RetryPolicyConfig.CalculateDelayMs` supplies the strategy-specific part of the calculation:

1. For attempt `0` or `1`, it returns `InitialDelayMs` immediately. This early return does not apply configuration jitter or the `MaxDelayMs` cap.
2. For attempts greater than `1`, it calculates the base delay from `PolicyType`:

   ```text
   FixedDelay:          InitialDelayMs
   ExponentialBackoff:  InitialDelayMs * BackoffMultiplier^(attemptNumber - 1)
   LinearBackoff:       InitialDelayMs * attemptNumber
   NoRetry or Custom:   InitialDelayMs
   ```

3. When `JitterFactor > 0`, it adds a random value from the symmetric range `[-delay * JitterFactor, +delay * JitterFactor)`, converts the result to `int`, and floors it at `1` ms.
4. It caps the result at `MaxDelayMs`.

Because jitter uses `Random.Shared`, a configuration with nonzero `JitterFactor` can return a different delay on each call. For deterministic calculations or simulations, set `JitterFactor` to `0`.

`CalculateRetryDelayWithJitter` first runs the algorithm above, then applies a second symmetric random variation using its own `jitterFactor` argument (default `0.2`). That argument must be between `0` and `1`, inclusive. The final result is clamped to the range `0` through `int.MaxValue`.

### Example calculations

For an exponential policy with `InitialDelayMs = 1000`, `BackoffMultiplier = 2`, `MaxDelayMs = 5000`, and `JitterFactor = 0`, attempts `1` through `4` produce `1000`, `2000`, `4000`, and `5000` ms. The fourth uncapped value is `8000` ms, so the maximum-delay cap reduces it to `5000` ms.

## Public API

| Member | Description |
| --- | --- |
| `CreatePolicy(string policyId, RetryPolicyConfig config)` | Adds or replaces a policy. A null config throws `ArgumentNullException`; a null or empty ID throws `ArgumentException`. |
| `GetPolicy(string policyId)` | Returns the registered configuration by reference, or `null` when the ID is unknown. |
| `CalculateRetryDelay(string policyId, int attemptNumber)` | Calculates a nonnegative delay, or returns the default delay for an unknown policy. |
| `CalculateRetryDelayWithJitter(string policyId, int attemptNumber, double jitterFactor = 0.2)` | Applies additional bounded jitter to the calculated delay. |
| `ShouldRetry(string policyId, int currentAttempt, string? exceptionTypeName = null)` | Returns `false` for an unknown or `NoRetry` policy, when `currentAttempt >= MaxAttempts`, or when a nonempty retryable-type list does not contain the supplied type name. |
| `CreateExponentialBackoffPolicy(int maxRetries = 3)` | Creates an exponential policy using the engine's default delay (`1000` ms), maximum delay (`300000` ms), and the config factory's default jitter (`0.1`). |
| `CreateFixedDelayPolicy(int maxRetries = 3, int delayMs = 1000)` | Creates a fixed-delay policy. |
| `CreateNoRetryPolicy()` | Creates a `NoRetry` configuration with one maximum attempt. |
| `SimulateRetryDelays(string policyId, int maxAttempts)` | Calculates delays for attempt numbers `1` through `maxAttempts`. A nonpositive count returns an empty list. |
| `GetTotalRetryTimeMs(string policyId)` | Sums configured delays for attempts `1` through `MaxAttempts - 1`; an unknown policy returns `0`. |
| `ValidatePolicy(RetryPolicyConfig config, out List<string> errors)` | Validates positive attempts and initial delay, maximum delay ordering, a multiplier greater than `1`, and jitter in `[0, 1]`. |
| `RegisterRetryableException(string policyId, string exceptionTypeName)` | Adds a type name if the policy exists and the exact string is not already registered. |
| `ClearPolicies()` | Removes all policies from the service. |

The registry uses a regular `Dictionary<string, RetryPolicyConfig>` and exposes the stored mutable configurations. Concurrent reads, writes, or configuration mutation are not synchronized by this service.

## Usage

```csharp
using DotNetWorkflowEngine.Services;

var retryPolicies = new RetryPolicyService();
var policy = retryPolicies.CreateExponentialBackoffPolicy(maxRetries: 4);

// Disable configuration-level jitter when exact delays are required.
policy.JitterFactor = 0;
retryPolicies.CreatePolicy("payments", policy);
retryPolicies.RegisterRetryableException(
    "payments",
    typeof(TimeoutException).FullName!);

var attempt = 1;
if (retryPolicies.ShouldRetry(
        "payments",
        attempt,
        typeof(TimeoutException).FullName))
{
    var delayMs = retryPolicies.CalculateRetryDelay("payments", attempt);
    await Task.Delay(delayMs);
}
```

`MaxAttempts` is interpreted as the total attempt limit by `ShouldRetry`: it permits another attempt only while `currentAttempt < MaxAttempts`.
