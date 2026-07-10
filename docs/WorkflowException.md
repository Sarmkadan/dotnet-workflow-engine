# WorkflowException

`WorkflowException` is a specialized exception type used within the `dotnet-workflow-engine` project to represent errors that occur during workflow execution. It extends the base `Exception` class to include workflow-specific context such as error codes and correlation identifiers, enabling more granular error handling and debugging in distributed workflow scenarios.

## API

### `public string? ErrorCode`
A read-only property that holds an optional error code associated with the exception. This code can be used to categorize or programmatically handle different types of workflow failures. Returns `null` if no error code was provided during construction.

### `public string? CorrelationId`
A read-only property that holds an optional correlation identifier for the exception. This ID can be used to trace the exception back to a specific workflow instance or execution context. Returns `null` if no correlation ID was provided during construction.

### `public WorkflowException(string message) : base(message)`
Constructs a new `WorkflowException` with the specified error message. The `ErrorCode` and `CorrelationId` properties will be `null`.

- **Parameters**:
  - `message` (string): A human-readable description of the error.
- **Throws**: No exceptions are thrown by this constructor.

### `public WorkflowException(string message, string errorCode) : base(message)`
Constructs a new `WorkflowException` with the specified error message and error code. The `CorrelationId` property will be `null`.

- **Parameters**:
  - `message` (string): A human-readable description of the error.
  - `errorCode` (string): A machine-readable code representing the type of error.
- **Throws**: No exceptions are thrown by this constructor.

### `public WorkflowException(string message, Exception innerException) : base(message, innerException)`
Constructs a new `WorkflowException` with the specified error message and inner exception. The `ErrorCode` and `CorrelationId` properties will be `null`.

- **Parameters**:
  - `message` (string): A human-readable description of the error.
  - `innerException` (Exception): The exception that is the cause of the current exception.
- **Throws**: No exceptions are thrown by this constructor.

### `public WorkflowException`
Default constructor. Constructs a new `WorkflowException` with no message, error code, or correlation ID. All properties (`ErrorCode`, `CorrelationId`, and the base `Message`) will be `null`.

- **Throws**: No exceptions are thrown by this constructor.

## Usage
