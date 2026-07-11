# WorkflowExecutionService

The `WorkflowExecutionService` is the primary runtime component responsible for managing the lifecycle of workflow instances within the `dotnet-workflow-engine`. It provides the necessary APIs to instantiate, start, resume, and terminate workflows, as well as to execute individual activities and query the current state of running or completed instances. This service acts as the central coordinator for workflow progression, handling state persistence and correlation logic.

## API

### Constructors

#### `public WorkflowExecutionService()`
Initializes a new instance of the `WorkflowExecutionService` class. This constructor sets up the internal state managers and activity registries required for workflow execution.

### Instance Management

#### `public WorkflowInstance CreateInstance(string workflowType, string? correlationId = null)`
Creates a new `WorkflowInstance` in an initialized but not yet started state.
*   **Parameters**:
    *   `workflowType`: The unique identifier or class name of the workflow definition to instantiate.
    *   `correlationId`: An optional string used to group related workflow instances for querying.
*   **Returns**: A `WorkflowInstance` object representing the new entity.
*   **Throws**: Throws an exception if the specified `workflowType` is not registered or recognized by the engine.

#### `public async Task<WorkflowInstance> StartAsync(WorkflowInstance instance)`
Begins the execution of a created workflow instance. This triggers the evaluation of the initial state and schedules the first set of activities.
*   **Parameters**:
    *   `instance`: The `WorkflowInstance` to start.
*   **Returns**: A `Task` yielding the updated `WorkflowInstance` with its status set to running.
*   **Throws**: Throws an exception if the instance is already running, completed, or failed.

#### `public void CompleteInstance(string instanceId)`
Marks a specific workflow instance as successfully completed. This method should be called when the workflow reaches its natural end state or when external logic determines success.
*   **Parameters**:
    *   `instanceId`: The unique identifier of the instance to complete.
*   **Throws**: Throws an exception if the instance does not exist or is not in a state允许 transition to completed.

#### `public void FailInstance(string instanceId, Exception exception)`
Marks a specific workflow instance as failed due to an error.
*   **Parameters**:
    *   `instanceId`: The unique identifier of the instance to fail.
    *   `exception`: The `Exception` object describing the cause of the failure.
*   **Throws**: Throws an exception if the instance does not exist.

#### `public WorkflowInstance? GetInstance(string instanceId)`
Retrieves a specific workflow instance by its unique identifier.
*   **Parameters**:
    *   `instanceId`: The unique identifier of the instance.
*   **Returns**: The `WorkflowInstance` if found; otherwise, `null`.

#### `public List<WorkflowInstance> GetInstancesByWorkflow(string workflowType)`
Retrieves all instances associated with a specific workflow definition.
*   **Parameters**:
    *   `workflowType`: The identifier of the workflow definition.
*   **Returns**: A `List<WorkflowInstance>` containing all matching instances regardless of their status.

#### `public List<WorkflowInstance> GetInstancesByCorrelation(string correlationId)`
Retrieves all instances sharing a specific correlation ID.
*   **Parameters**:
    *   `correlationId`: The correlation identifier to filter by.
*   **Returns**: A `List<WorkflowInstance>` containing all matching instances. Returns an empty list if no matches are found.

#### `public List<WorkflowInstance> GetActiveInstances()`
Retrieves all instances currently in a running or suspended state.
*   **Returns**: A `List<WorkflowInstance>` containing only active instances.

#### `public async Task ResumeInstanceAsync(string instanceId)`
Resumes execution of a suspended or idle workflow instance. This is typically used after a long-running activity has completed and the engine needs to proceed to the next step.
*   **Parameters**:
    *   `instanceId`: The unique identifier of the instance to resume.
*   **Returns**: A `Task` that completes when the resume operation and subsequent immediate activities are processed.
*   **Throws**: Throws an exception if the instance is not in a resumable state (e.g., already completed or failed).

#### `public async Task ResumeFromMessageAsync(string correlationId, string messageName, object? payload = null)`
Resumes one or more workflow instances waiting for a specific message, identified by correlation ID and message name.
*   **Parameters**:
    *   `correlationId`: The correlation ID used to locate the waiting instance(s).
    *   `messageName`: The name of the message event the workflow is waiting for.
    *   `payload`: Optional data passed into the workflow upon resumption.
*   **Returns**: A `Task` that completes when the message has been delivered and the workflow(s) have progressed.
*   **Throws**: Throws an exception if no instance is found waiting for the specified message and correlation.

### Activity Execution

#### `public async Task ExecuteActivityAsync(string instanceId, string activityName, object? input = null)`
Explicitly executes a specific activity within the context of a running workflow instance. This is often used for manual service tasks or testing specific nodes.
*   **Parameters**:
    *   `instanceId`: The unique identifier of the workflow instance.
    *   `activityName`: The name of the activity to execute.
    *   `input`: Optional input data for the activity.
*   **Returns**: A `Task` that completes when the activity execution finishes.
*   **Throws**: Throws an exception if the instance is not active or the activity name is invalid within the current workflow context.

### Statistics

#### `public (int Total, int Active, int Completed, int Failed) GetStatistics()`
Retrieves aggregate counts of workflow instances categorized by their current status.
*   **Returns**: A tuple containing:
    *   `Total`: Total number of instances.
    *   `Active`: Number of running/suspended instances.
    *   `Completed`: Number of successfully completed instances.
    *   `Failed`: Number of failed instances.

## Usage

### Example 1: Creating and Starting a Workflow
The following example demonstrates how to instantiate a new order processing workflow, start it, and handle potential initialization errors.

```csharp
public async Task ProcessNewOrderAsync(string orderId, OrderDetails details)
{
    var service = new WorkflowExecutionService();
    
    // Create the instance with the order ID as the correlation key
    var instance = service.CreateInstance("OrderProcessingWorkflow", orderId);
    
    // Attach initial data if the engine supports input binding via instance context
    // (Assuming hypothetical context setter for demonstration)
    instance.SetVariable("OrderDetails", details);

    try 
    {
        // Start the workflow execution
        var runningInstance = await service.StartAsync(instance);
        Console.WriteLine($"Workflow started with ID: {runningInstance.Id}");
    }
    catch (Exception ex)
    {
        // Handle cases where the workflow definition is missing or invalid
        Console.WriteLine($"Failed to start workflow: {ex.Message}");
        throw;
    }
}
```

### Example 2: Resuming a Workflow from an External Event
This example shows how to resume a workflow that is waiting for a payment confirmation message using the correlation ID.

```csharp
public async Task HandlePaymentConfirmationAsync(string orderId, PaymentResult result)
{
    var service = new WorkflowExecutionService();

    try 
    {
        // Resume any instance waiting for "PaymentReceived" with the matching order ID
        await service.ResumeFromMessageAsync(
            correlationId: orderId, 
            messageName: "PaymentReceived", 
            payload: result
        );
        
        Console.WriteLine($"Payment processed for order {orderId}, workflow resumed.");
    }
    catch (Exception ex)
    {
        // This might occur if the workflow already timed out or completed
        Console.WriteLine($"Could not resume workflow: {ex.Message}");
        
        // Optionally fail the instance if the payment arrives too late
        var instance = service.GetInstance(orderId); // Assuming ID matches correlation for simplicity
        if (instance != null && instance.Status == WorkflowStatus.Active)
        {
             service.FailInstance(instance.Id, new TimeoutException("Payment received after workflow timeout."));
        }
    }
}
```

## Notes

*   **Thread Safety**: The `WorkflowExecutionService` methods are not guaranteed to be thread-safe for concurrent modifications of the *same* `WorkflowInstance`. While multiple threads may call `GetInstancesByWorkflow` or `GetStatistics` simultaneously, concurrent calls to `ResumeInstanceAsync`, `ExecuteActivityAsync`, or `FailInstance` targeting the same `instanceId` may result in race conditions or state inconsistencies. External synchronization or a singleton service pattern per workflow aggregate is recommended.
*   **State Transitions**: Methods like `CompleteInstance` and `FailInstance` are terminal operations. Once called, the instance cannot be resumed via `ResumeInstanceAsync`. Attempting to do so will result in an exception.
*   **Correlation Uniqueness**: `ResumeFromMessageAsync` may resume multiple instances if the combination of `correlationId` and `messageName` is not unique within the active instance set. Ensure correlation IDs are sufficiently granular (e.g., including a transaction ID) if one-to-one mapping is required.
*   **Null Handling**: `GetInstance` returns `null` rather than throwing if an ID is not found. Callers must check for null before accessing properties of the returned instance. Conversely, mutation methods (`FailInstance`, `CompleteInstance`) typically throw if the target ID is invalid.
*   **Async Consistency**: Methods returning `Task` (e.g., `StartAsync`, `ResumeInstanceAsync`) perform asynchronous I/O or scheduling. The state of the `WorkflowInstance` object returned or retrieved immediately after calling these methods may reflect the state *before* the asynchronous operation fully commits to persistent storage, depending on the underlying implementation of the engine's storage provider.
