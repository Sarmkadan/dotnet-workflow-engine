# RetryPolicyConfigTestsExtensions

Provides factory methods for creating common `RetryPolicyConfig` instances and a set of static assertion helpers used to verify retry policy behaviour in unit tests. The type is designed exclusively for test projects referencing the `dotnet-workflow-engine` and is not intended for production use.

## API

### `CreateExponentialBackoff`

```csharp
public static RetryPolicyConfig CreateExponentialBackoff()
```

Creates a `RetryPolicyConfig` pre-configured with exponential backoff semantics. The returned configuration uses a base delay that doubles after each attempt, subject to an internal maximum delay cap.

**Returns:** A new `RetryPolicyConfig` instance with exponential backoff enabled.

**Throws:** Never throws.

---

### `CreateFixedDelay`

```csharp
public static RetryPolicyConfig CreateFixedDelay()
```

Creates a `RetryPolicyConfig` pre-configured with a constant delay between retry attempts. Every retry waits the same amount of time regardless of the attempt number.

**Returns:** A new `RetryPolicyConfig` instance with fixed-delay semantics.

**Throws:** Never throws.

---

### `CreateNoRetry`

```csharp
public static RetryPolicyConfig CreateNoRetry()
```

Creates a `RetryPolicyConfig` that disables retries entirely. Any operation governed by this configuration will execute exactly once and surface the original exception on failure.

**Returns:** A new `RetryPolicyConfig` instance with zero permitted retries.

**Throws:** Never throws.

---

### `ShouldRetry_WhenMaxAttemptsExhausted_ShouldReturnFalse`

```csharp
public static void ShouldRetry_WhenMaxAttemptsExhausted_ShouldReturnFalse()
```

Asserts that the retry policy correctly returns `false` from its `ShouldRetry` evaluation when the configured maximum number of attempts has been reached. Typically invoked as a standalone test method or called from a test orchestrator.

**Parameters:** None.

**Returns:** Void. Throws an assertion exception on failure.

**Throws:** An assertion exception if the policy indicates a retry should occur after the maximum attempts are exhausted.

---

### `ShouldRetry_WithRetryableExceptionType_ShouldReturnTrue`

```csharp
public static void ShouldRetry_WithRetryableExceptionType_ShouldReturnTrue()
```

Asserts that the retry policy correctly returns `true` from its `ShouldRetry` evaluation when the thrown exception matches one of the retryable exception types defined in the configuration.

**Parameters:** None.

**Returns:** Void. Throws an assertion exception on failure.

**Throws:** An assertion exception if the policy declines to retry a recognised retryable exception.

---

### `CalculateExpectedExponentialDelay`

```csharp
public static int CalculateExpectedExponentialDelay(int attempt, int baseDelayMs, int maxDelayMs)
```

Computes the expected delay in milliseconds for a given attempt number under an exponential backoff strategy. The formula doubles the base delay for each subsequent attempt and clamps the result to `maxDelayMs`.

| Parameter      | Type  | Description                                      |
|----------------|-------|--------------------------------------------------|
| `attempt`      | `int` | The one-based attempt number.                    |
| `baseDelayMs`  | `int` | The initial delay in milliseconds (attempt 1).   |
| `maxDelayMs`   | `int` | The absolute ceiling for the computed delay.     |

**Returns:** The expected delay in milliseconds after applying exponential growth and the maximum constraint.

**Throws:** May throw `ArgumentOutOfRangeException` if `attempt` is less than 1, or if `baseDelayMs` / `maxDelayMs` are negative (validation depends on the underlying implementation).

---

### `CalculateDelayMs_WithMaxDelayConstraint_ShouldNotExceedMax`

```csharp
public static void CalculateDelayMs_WithMaxDelayConstraint_ShouldNotExceedMax()
```

Asserts that the delay calculation logic respects the configured maximum delay. It verifies that even for a high attempt number the computed delay never exceeds the specified ceiling.

**Parameters:** None.

**Returns:** Void. Throws an assertion exception on failure.

**Throws:** An assertion exception if the computed delay surpasses the maximum allowed value.

## Usage

### Example 1: Verifying exponential backoff delay clamping

```csharp
using Xunit;

public class RetryPolicyTests
{
    [Fact]
    public void ExponentialDelay_ShouldBeClampedAtMax()
    {
        // The helper computes the expected delay; the assertion method validates clamping.
        int delay = RetryPolicyConfigTestsExtensions.CalculateExpectedExponentialDelay(
            attempt: 10,
            baseDelayMs: 1000,
            maxDelayMs: 30000);

        Assert.True(delay <= 30000);

        // Alternatively, use the built-in assertion which performs the same check.
        RetryPolicyConfigTestsExtensions.CalculateDelayMs_WithMaxDelayConstraint_ShouldNotExceedMax();
    }
}
```

### Example 2: Testing retry policy behaviour with a fixed-delay configuration

```csharp
using Xunit;

public class FixedDelayRetryTests
{
    [Fact]
    public void ShouldRetry_WhenExceptionIsRetryable_AndAttemptsRemain()
    {
        RetryPolicyConfig config = RetryPolicyConfigTestsExtensions.CreateFixedDelay();

        // The assertion methods internally construct a policy from the config,
        // simulate attempts, and verify ShouldRetry outcomes.
        RetryPolicyConfigTestsExtensions.ShouldRetry_WithRetryableExceptionType_ShouldReturnTrue();
    }

    [Fact]
    public void ShouldNotRetry_WhenMaxAttemptsExhausted()
    {
        RetryPolicyConfigTestsExtensions.ShouldRetry_WhenMaxAttemptsExhausted_ShouldReturnFalse();
    }
}
```

## Notes

- All factory methods (`CreateExponentialBackoff`, `CreateFixedDelay`, `CreateNoRetry`) return independent `RetryPolicyConfig` instances; modifying one does not affect another.
- The assertion methods (`ShouldRetry_*`, `CalculateDelayMs_WithMaxDelayConstraint_ShouldNotExceedMax`) are self-contained and do not expose internal state. They are safe to call concurrently from different test classes, provided the underlying test framework supports parallel execution.
- `CalculateExpectedExponentialDelay` is a pure static function with no side effects or shared state. It is inherently thread-safe.
- The exact default values embedded in the factory methods (maximum attempts, base delay, maximum delay) are implementation details of the test extensions and may change across versions. Production code should construct `RetryPolicyConfig` explicitly rather than relying on these test-oriented defaults.
- The assertion methods throw framework-specific assertion exceptions (e.g., `Xunit.Sdk.XunitException` or equivalent) and are not designed to be caught; they signal test failure.
