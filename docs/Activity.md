# Activity

Represents a unit of work within a workflow, defining its identity, behavior, and execution constraints. Activities are the building blocks of workflows in the engine, encapsulating input/output handling, retry policies, timeouts, and conditional execution logic.

## API

### `public string Id`
A unique identifier for the activity within the workflow. Must be non-null and unique across all activities in the same workflow definition.

### `public string Name`
A human-readable name for the activity. Used for logging, debugging, and display purposes. Must be non-null.

### `public string? Description`
An optional descriptive text providing additional context about the activity's purpose or behavior.

### `public string Type`
The type or category of the activity, used by the workflow engine to determine how the activity should be processed. Must be non-null.

### `public ExecutionMode ExecutionMode`
Defines how the activity should be executed. Possible values include `Synchronous`, `Asynchronous`, or `FireAndForget`. Defaults to `Synchronous`.

### `public string? HandlerType`
The fully qualified type name of the handler responsible for executing the activity's logic. If `null`, the activity is treated as a placeholder or composite activity.

### `public Dictionary<string, object?> InputParameters`
A collection of named input parameters passed to the activity's handler during execution. Keys are parameter names; values may be `null`.

### `public Dictionary<string, string> OutputMapping`
Maps output parameter names from the activity's handler to workflow-level output variables. Keys are output names; values are the target workflow variable names.

### `public RetryPolicy RetryPolicy`
Defines the retry strategy for the activity, including maximum retries, backoff intervals, and exception filtering. If `null`, no retries are performed.

### `public int MaxRetries`
The maximum number of retry attempts for the activity. Ignored if `RetryPolicy` is `null`. Defaults to `0`.

### `public int TimeoutSeconds`
The maximum duration, in seconds, allowed for the activity's execution. If exceeded, the activity is terminated and marked as failed. Defaults to `300` (5 minutes).

### `public string? MessageName`
An optional message name used for correlation in message-driven workflows. If specified, the workflow engine will correlate messages with this name to this activity.

### `public string? CorrelationProperty`
The name of a property in the workflow's context used for correlation. Messages with matching values for this property will be routed to this activity.

### `public bool IsOptional`
Indicates whether the activity's failure should halt the entire workflow. If `true`, workflow execution continues even if this activity fails. Defaults to `false`.

### `public string? ConditionExpression`
A boolean expression evaluated at runtime to determine whether the activity should be executed. If the expression evaluates to `false`, the activity is skipped.

### `public Dictionary<string, object?> Metadata`
A collection of additional, workflow-specific data associated with the activity. Keys and values are arbitrary and may be used by custom handlers or workflow logic.

### `public DateTime CreatedAt`
The timestamp when the activity was created or added to the workflow definition. Set automatically by the workflow engine.

### `public bool Validate`
Determines whether the activity should be validated during workflow compilation. If `true`, the engine performs structural and logical validation. Defaults to `true`.

### `public void SetInputParameter(string name, object? value)`
Sets an input parameter for the activity.

- **Parameters**:
  - `name`: The name of the parameter. Must not be `null` or empty.
  - `value`: The value to assign. May be `null`.
- **Throws**: `ArgumentNullException` if `name` is `null`.

### `public object? GetInputParameter(string name)`
Retrieves the value of an input parameter.

- **Parameters**:
  - `name`: The name of the parameter. Must not be `null` or empty.
- **Returns**: The parameter value, or `null` if the parameter does not exist or has no value.
- **Throws**: `ArgumentNullException` if `name` is `null`.

## Usage

### Example 1: Basic Activity Definition
