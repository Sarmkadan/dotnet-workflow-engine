# ActivityResult

A lightweight value object used by the workflow engine to capture the outcome and telemetry of a single activity execution. It records success, failure, or waiting states, along with timing, retry counts, output values, and any error context.

## API

### `public string ActivityId`
Identifier of the activity whose execution this result represents. Immutable once set.

### `public ActivityStatus Status`
Current execution status of the activity. One of `Success`, `Failed`, `Waiting`, or `Skipped`. Defaults to `Waiting`.

### `public Dictionary<string, object?> Output`
Collection of key/value pairs produced by the activity when it completes successfully. Empty if the activity has not yet completed or did not produce output.

### `public string? ErrorMessage`
Human-readable error message populated when `Status` is `Failed`. `null` otherwise.

### `public string? StackTrace`
Stack trace captured at the point of failure when `Status` is `Failed`. `null` otherwise.

### `public DateTime StartTime`
Timestamp when the activity execution began. Set automatically when the result is created.

### `public DateTime? EndTime`
Timestamp when the activity execution ended. `null` if the activity is still running or waiting.

### `public long ExecutionDurationMs`
Elapsed time in milliseconds between `StartTime` and `EndTime`. Zero if the activity has not yet completed.

### `public int AttemptNumber`
Ordinal number of the current execution attempt (1-based). Incremented on each retry.

### `public int TotalAttempts`
Total number of attempts allowed for this activity. Used to determine whether retries should continue.

### `public Dictionary<string, object?> Metadata`
Immutable collection of user-defined key/value pairs attached to the activity. Populated once when the result is created.

### `public ActivityResult()`
Constructs a new instance with default values:
- `Status` set to `Waiting`
- `StartTime` set to `DateTime.UtcNow`
- `AttemptNumber` set to 1
- `TotalAttempts` set to 1
- `Output`, `ErrorMessage`, `StackTrace`, `EndTime`, and `Metadata` initialized to empty collections or `null`

### `public ActivityResult(string activityId, int totalAttempts, Dictionary<string, object?>? metadata = null)`
Constructs a new instance with the specified `activityId`, `totalAttempts`, and optional `metadata`. Sets:
- `ActivityId` to the provided value
- `Status` to `Waiting`
- `StartTime` to `DateTime.UtcNow`
- `AttemptNumber` to 1
- `Output`, `ErrorMessage`, `StackTrace`, and `EndTime` initialized to empty collections or `null`
- `Metadata` to the provided dictionary or an empty dictionary if `null`

### `public void SetSuccess(Dictionary<string, object?> output)`
Marks the activity as successfully completed and records the provided `output`.
- Sets `Status` to `Success`
- Sets `Output` to the provided dictionary
- Sets `EndTime` to `DateTime.UtcNow`
- Computes and stores `ExecutionDurationMs`

### `public void SetFailure(string errorMessage, string? stackTrace = null)`
Marks the activity as failed and records the error context.
- Sets `Status` to `Failed`
- Sets `ErrorMessage` to the provided `errorMessage`
- Sets `StackTrace` to the provided `stackTrace` or `null`
- Sets `EndTime` to `DateTime.UtcNow`
- Computes and stores `ExecutionDurationMs`

### `public void SetSkipped()`
Marks the activity as skipped. Skipped activities are considered completed without execution.
- Sets `Status` to `Skipped`
- Sets `EndTime` to `DateTime.UtcNow`
- Computes and stores `ExecutionDurationMs`

### `public void SetWaiting()`
Resets the activity to a waiting state. Clears any previous outcome and timing data.
- Sets `Status` to `Waiting`
- Sets `Output`, `ErrorMessage`, and `StackTrace` to `null`
- Sets `EndTime` to `null`
- Sets `ExecutionDurationMs` to 0

### `public bool IsSuccess`
Returns `true` if `Status` is `Success`; otherwise `false`.

### `public bool IsFailed`
Returns `true` if `Status` is `Failed`; otherwise `false`.

### `public bool IsWaiting`
Returns `true` if `Status` is `Waiting`; otherwise `false`.

## Usage
