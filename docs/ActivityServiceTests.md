# ActivityServiceTests

Unit tests for the `ActivityService` class, verifying correct handling of activity execution, handler registration, error conditions, retry policies, conditional execution, and context propagation within the workflow engine.

## API

### `RegisterHandler_AddsHandlerToRegistry`
Verifies that registering a handler via `ActivityService.RegisterHandler` adds the handler to the internal registry. No parameters or return value; throws if the handler is not found in the registry after registration.

### `ExecuteAsync_GatewayActivity_ReturnsSuccessWithoutHandler`
Ensures that executing a gateway-type activity succeeds without requiring a handler. No parameters; returns a completed task. Does not throw.

### `ExecuteAsync_InvalidActivity_ThrowsValidationException`
Validates that passing an invalid activity (e.g., null or malformed) results in a `ValidationException`. No parameters; throws if the activity is invalid.

### `ExecuteAsync_NoHandlerRegistered_ThrowsActivityException`
Confirms that attempting to execute an activity with no registered handler throws an `ActivityException`. No parameters; throws if no handler is registered.

### `ExecuteAsync_HandlerExecutes_ReturnsSuccess`
Asserts that when a handler is registered and executes successfully, the service returns a successful result. No parameters; returns a completed task. Does not throw.

### `ExecuteAsync_HandlerThrows_ThrowsActivityException`
Ensures that if a handler throws an exception during execution, the service propagates an `ActivityException`. No parameters; throws if the handler throws.

### `ExecuteAsync_ConditionalSkip_ReturnsSkipped`
Tests that conditional execution skips the activity when the condition evaluates to false. No parameters; returns a task with a skipped status. Does not throw.

### `ExecuteAsync_ConditionalPass_ExecutesActivity`
Validates that conditional execution proceeds to activity execution when the condition evaluates to true. No parameters; returns a task indicating execution occurred. Does not throw.

### `ExecuteAsync_WithRetryPolicy_RetriesOnFailure`
Confirms that a retry policy is applied when an activity fails, resulting in retries according to the policy. No parameters; returns a task with retry metadata. Does not throw.

### `ExecuteAsync_RetryWithEventualSuccess_Succeeds`
Ensures that retries continue until the activity succeeds, returning a successful result. No parameters; returns a completed task. Does not throw.

### `ExecuteAsync_AttemptsInResult_SetCorrectly`
Verifies that the number of retry attempts is correctly recorded in the execution result. No parameters; returns a task with attempt count. Does not throw.

### `ExecuteAsync_SetActivityIdInContext`
Asserts that the activity ID is propagated to the execution context during activity execution. No parameters; returns a task. Does not throw.

### `ExecuteAsync_HandlerWithExceptionType_RetriesIfRetryableException`
Tests that only retryable exceptions trigger retries, while non-retryable exceptions propagate immediately. No parameters; throws if the exception is non-retryable. Returns otherwise.

### `ExecuteAsync_ExponentialBackoffPolicy_UsesBackoff`
Validates that an exponential backoff policy is applied during retries, with delays increasing according to the policy. No parameters; returns a task with backoff metadata. Does not throw.

### `ExecuteAsync_MultipleHandlers_SelectsCorrectOne`
Ensures that when multiple handlers are registered, the correct one is selected based on activity type or other criteria. No parameters; returns a task indicating the correct handler was used. Does not throw.

### `ExecuteAsync_HandlerNullEmptyType_ThrowsActivityException`
Confirms that registering a handler with a null or empty type throws an `ActivityException`. No parameters; throws if the handler type is invalid.

### `ExecuteAsync_ActivityWithNoHandler_SucceedsIfNotRequired`
Tests that an activity without a handler succeeds if the handler is marked as optional. No parameters; returns a completed task. Does not throw.

### `ExecuteAsync_FailureRecordsErrorMessage`
Validates that execution failures are recorded with descriptive error messages in the result. No parameters; returns a task with error details. Does not throw.

### `ExecuteAsync_RetryExhaustionMessage_IsInformative`
Ensures that when retries are exhausted, the error message clearly indicates the number of attempts and final failure. No parameters; returns a task with an informative message. Does not throw.

### `ExecuteAsync_CorrelationIdPreserved`
Asserts that the correlation ID from the activity context is preserved and propagated through execution. No parameters; returns a task. Does not throw.

## Usage
