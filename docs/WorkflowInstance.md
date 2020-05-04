# WorkflowInstance
The `WorkflowInstance` class represents a single instance of a workflow, encapsulating its execution state, context, and metadata. It provides properties to access the instance's identifier, workflow identifier, status, and other relevant information. Additionally, it offers methods to control the workflow's execution, such as starting, completing, or failing the instance.

## API
### Properties
* `Id`: A unique identifier for the workflow instance.
* `WorkflowId`: The identifier of the workflow definition associated with this instance.
* `Status`: The current status of the workflow instance, represented by the `WorkflowStatus` enum.
* `CurrentActivityId`: The identifier of the current activity being executed, or `null` if no activity is currently running.
* `ExecutedActivities`: A list of activity identifiers that have been executed in this workflow instance.
* `ActiveActivities`: A list of activity identifiers that are currently active in this workflow instance.
* `Context`: A dictionary containing the workflow instance's context data, where keys are strings and values are objects.
* `CreatedAt`: The date and time when the workflow instance was created.
* `StartedAt`: The date and time when the workflow instance was started, or `null` if it has not been started yet.
* `CompletedAt`: The date and time when the workflow instance was completed, or `null` if it has not been completed yet.
* `ExecutionTimeMs`: The total execution time of the workflow instance in milliseconds.
* `ErrorMessage`: An error message associated with the workflow instance, or `null` if no error has occurred.
* `CorrelationId`: A correlation identifier for the workflow instance, or `null` if not specified.
* `Metadata`: A dictionary containing metadata associated with the workflow instance, where keys are strings and values are objects.
* `InitiatedBy`: The identifier of the entity that initiated the workflow instance, or `null` if not specified.

### Methods
* `Start()`: Starts the execution of the workflow instance. This method does not take any parameters and does not return a value. It may throw an exception if the instance is already started or if an error occurs during startup.
* `Complete()`: Completes the execution of the workflow instance. This method does not take any parameters and does not return a value. It may throw an exception if the instance is not started or if an error occurs during completion.
* `Fail()`: Fails the execution of the workflow instance. This method does not take any parameters and does not return a value. It may throw an exception if the instance is not started or if an error occurs during failure.

## Usage
The following examples demonstrate how to use the `WorkflowInstance` class:
```csharp
// Create a new workflow instance
var instance = new WorkflowInstance();
instance.Id = "my-instance";
instance.WorkflowId = "my-workflow";

// Start the workflow instance
instance.Start();

// Complete the workflow instance
instance.Complete();
```

```csharp
// Create a new workflow instance with context data
var instance = new WorkflowInstance();
instance.Id = "my-instance";
instance.WorkflowId = "my-workflow";
instance.Context = new Dictionary<string, object?>
{
    { "key1", "value1" },
    { "key2", 42 }
};

// Start the workflow instance
instance.Start();

// Fail the workflow instance with an error message
instance.Fail();
instance.ErrorMessage = "Something went wrong";
```

## Notes
When working with `WorkflowInstance` objects, consider the following edge cases and thread-safety remarks:
* The `Start()`, `Complete()`, and `Fail()` methods are not thread-safe. If multiple threads need to access the same workflow instance, synchronization mechanisms should be employed to prevent concurrent modifications.
* The `Context` and `Metadata` dictionaries are not thread-safe either. If multiple threads need to access or modify these dictionaries, synchronization mechanisms should be used to prevent concurrent modifications.
* The `ExecutionTimeMs` property may not be accurate if the workflow instance is executed across multiple threads or processes.
* The `ErrorMessage` property may be overwritten if multiple errors occur during the execution of the workflow instance. It is recommended to handle errors as soon as they occur to prevent loss of error information.
