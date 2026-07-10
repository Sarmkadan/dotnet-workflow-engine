# ActivityException

`ActivityException` is a custom exception type used within the `dotnet-workflow-engine` project to signal failures that occur during the execution of workflow activities. It carries contextual information about the activity where the exception originated, including its identifier and the current retry attempt number, which is useful for implementing retry logic or diagnostics in workflow processing systems.

## API

### `ActivityId`

A read-only property of type `string` that identifies the workflow activity in which the exception occurred.

- **Purpose**: Provides traceability to the specific activity that failed, enabling targeted error handling or logging.
- **Return value**: The unique identifier of the activity as a string.
- **Exceptions**: Never throws.

### `AttemptNumber`

A read-only property of type `int` that indicates the current retry attempt number for the failing activity.

- **Purpose**: Helps in implementing retry policies by tracking how many times the activity has been retried.
- **Return value**: The 1-based attempt number (e.g., 1 for the first attempt, 2 for the first retry).
- **Exceptions**: Never throws.

### Constructors

#### `ActivityException()`

Initializes a new instance of the `ActivityException` class with default values.

- **Purpose**: Provides a parameterless constructor for general exception usage.
- **Exceptions**: Never throws.

#### `ActivityException(string message)`

Initializes a new instance of the `ActivityException` class with a specified error message.

- **Parameters**:
  - `message` (string): The message that describes the error.
- **Purpose**: Allows creation of an exception with a custom error description.
- **Exceptions**: Never throws.

#### `ActivityException(string message, Exception innerException)`

Initializes a new instance of the `ActivityException` class with a specified error message and a reference to the inner exception that is the cause of this exception.

- **Parameters**:
  - `message` (string): The message that describes the error.
  - `innerException` (Exception): The exception that is the cause of the current exception.
- **Purpose**: Supports exception chaining for richer error context.
- **Exceptions**: Never throws.

## Usage

### Basic Usage
