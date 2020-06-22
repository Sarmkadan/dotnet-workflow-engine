// ... (rest of the README content remains unchanged)
## ActivityException

The `ActivityException` class represents exceptions that occur during activity execution. It provides information about the activity that caused the exception, including its ID and the attempt number when the exception occurred. Use it to handle and log activity execution errors.

Example usage:
```csharp
try
{
    var activity = new Activity { Name = "", Duration = -1 };
    WorkflowValidator.ValidateActivity(activity);
}
catch (ActivityException ex)
{
    Console.WriteLine($"Activity execution failed for {ex.ActivityId} (attempt {ex.AttemptNumber}): {ex.Message}");
    // Output: Activity execution failed for activity-123 (attempt 1): Name cannot be empty
}
```

## ValidationException

The `ValidationException` class represents validation failures in workflows or activities. It contains a list of validation errors, the name of the entity that failed validation, and provides a detailed error message. Use it to handle and log validation issues during workflow execution.

Example usage:
```csharp
try
{
    var activity = new Activity { Name = "", Duration = -1 };
    WorkflowValidator.ValidateActivity(activity);
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed for {ex.EntityName}:");
    Console.WriteLine(ex.GetDetailedMessage());
    // Output: Validation failed for Activity. Errors: Name cannot be empty; Duration must be positive
}
```

## ConfigurationException

The `ConfigurationException` class represents configuration-related errors that occur during workflow engine initialization or execution. It provides detailed information about which configuration key and value caused the exception, making it useful for debugging configuration issues.

Example usage:
```csharp
try
{
    var config = new WorkflowConfig
    {
        MaxConcurrentWorkflows = -1,
        ConnectionString = "invalid-connection-string"
    };
    WorkflowEngine.Initialize(config);
}
catch (ConfigurationException ex)
{
    Console.WriteLine($"Configuration error: {ex.Message}");
    if (ex.ConfigurationKey != null)
    {
        Console.WriteLine($"Key: {ex.ConfigurationKey}");
    }
    if (ex.ConfigurationValue != null)
    {
        Console.WriteLine($"Value: {ex.ConfigurationValue}");
    }
    // Output: Configuration error: MaxConcurrentWorkflows must be positive
    // Key: MaxConcurrentWorkflows
    // Value: -1
}
```

## WorkflowException

The `WorkflowException` class is the base exception for all workflow engine related errors. It provides error codes for categorizing exceptions and correlation IDs for tracking related exceptions across distributed workflow executions. Use it as a base class for custom workflow-specific exceptions.

Example usage:
```csharp
try
{
    throw new WorkflowException("Workflow execution failed due to timeout", "WF_TIMEOUT", "corr-12345");
}
catch (WorkflowException ex)
{
    Console.WriteLine($"Workflow error: {ex.Message}");
    Console.WriteLine($"Error code: {ex.ErrorCode}");
    Console.WriteLine($"Correlation ID: {ex.CorrelationId}");

    // Output:
    // Workflow error: Workflow execution failed due to timeout
    // Error code: WF_TIMEOUT
    // Correlation ID: corr-12345
}
```

## IEventBus

The `IEventBus` interface provides a centralized event bus for publishing and subscribing to workflow events using the pub-sub pattern. It enables components to listen for workflow lifecycle events (started, completed, failed) and activity-level events (started, completed, failed) without tight coupling. The event bus supports asynchronous event handling with error isolation, ensuring that exceptions in one subscriber don't prevent other subscribers from receiving events.

Example usage:
```csharp
// Subscribe to workflow started events
var eventBus = new EventBus(logger);

eventBus.Subscribe<WorkflowStartedEvent>(async (workflowEvent) => {
    Console.WriteLine($"Workflow started: {workflowEvent.WorkflowId} at {workflowEvent.Timestamp}");
    Console.WriteLine($"Instance: {workflowEvent.InstanceId}");
    if (workflowEvent.InputData != null)
    {
        foreach (var kvp in workflowEvent.InputData)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }
    }
});

// Subscribe to workflow completed events
eventBus.Subscribe<WorkflowCompletedEvent>(async (workflowEvent) => {
    Console.WriteLine($"Workflow completed: {workflowEvent.WorkflowId} in {workflowEvent.DurationMs}ms");
    Console.WriteLine($"Instance: {workflowEvent.InstanceId}");
    if (workflowEvent.OutputData != null)
    {
        foreach (var kvp in workflowEvent.OutputData)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }
    }
});

// Subscribe to activity failed events
eventBus.Subscribe<ActivityFailedEvent>(async (activityEvent) => {
    Console.WriteLine($"Activity failed: {activityEvent.ActivityId} in workflow {activityEvent.InstanceId}");
    Console.WriteLine($"Error: {activityEvent.ErrorMessage}");
    Console.WriteLine($"Retry attempt: {activityEvent.RetryAttempt}");
});

// Publish events
var workflowStarted = new WorkflowStartedEvent {
    WorkflowId = "wf-123",
    InstanceId = "inst-456",
    InputData = new Dictionary<string, object> { { "userId", 42 }, { "action", "process_order" } }
};

await eventBus.PublishAsync(workflowStarted);
```

## ExpressionEvaluatorTests

The `ExpressionEvaluatorTests` class contains comprehensive unit tests for the `ExpressionEvaluator` class, verifying expression evaluation logic used throughout the workflow engine. These tests cover null/empty expressions, literal values, variable references, comparison operations, logical operators, and complex expression combinations. The test suite validates both the `Evaluate` method (which returns boolean results) and the `ValidateExpression` method (which validates expression syntax).


Example usage:
```csharp
// Create execution context with variables
var context = new WorkflowExecutionContext
{
    WorkflowInstanceId = "test-instance",
    Variables = new Dictionary<string, object?> 
    {
        { "isApproved", true },
        { "amount", 150 },
        { "status", "active" },
        { "description", "This is an important task" }
    }
};

// Test null/empty expressions
bool result1 = ExpressionEvaluator.Evaluate(null, context); // Returns true
bool result2 = ExpressionEvaluator.Evaluate("", context); // Returns true

// Test literal values
bool result3 = ExpressionEvaluator.Evaluate("true", context); // Returns true
bool result4 = ExpressionEvaluator.Evaluate("false", context); // Returns false

// Test variable references
bool result5 = ExpressionEvaluator.Evaluate("${isApproved}", context); // Returns true
bool result6 = ExpressionEvaluator.Evaluate("${status}", context); // Returns true

// Test comparison operations
bool result7 = ExpressionEvaluator.Evaluate("${amount} > \"100\"", context); // Returns true
bool result8 = ExpressionEvaluator.Evaluate("${status} == \"active\"", context); // Returns true
bool result9 = ExpressionEvaluator.Evaluate("${description} contains \"important\"", context); // Returns true

// Test logical operators
bool result10 = ExpressionEvaluator.Evaluate("${isApproved} && ${amount} > \"100\"", context); // Returns true
bool result11 = ExpressionEvaluator.Evaluate("${isApproved} || ${status} == \"inactive\"", context); // Returns true
bool result12 = ExpressionEvaluator.Evaluate("!${isApproved}", context); // Returns false

// Validate expressions
bool isValid1 = ExpressionEvaluator.ValidateExpression("${status} == \"active\"", out var errors1);
bool isValid2 = ExpressionEvaluator.ValidateExpression("${amount} > \"100\" && ${isApproved}", out var errors2);
```

## ActivityServiceTests

The `ActivityServiceTests` class contains comprehensive unit tests for the `ActivityService` class, which is responsible for executing workflow activities with support for conditional execution, retry policies, error handling, and context management. The test suite covers handler registration, various execution scenarios (gateway activities, invalid activities, missing handlers), conditional branching, retry policies (fixed delay, exponential backoff), error handling, and context preservation.



Example usage:

```csharp
// Create ActivityService with retry policy service
var retryPolicyService = new RetryPolicyService();
var activityService = new ActivityService(retryPolicyService);

// Register a custom activity handler
var mockHandler = new Mock<ActivityService.IActivityHandler>();
mockHandler.Setup(h => h.ExecuteAsync(
    It.IsAny<Activity>(),
    It.IsAny<WorkflowExecutionContext>()))
    .ReturnsAsync(new Dictionary<string, object?> { { "result", "success" } });

activityService.RegisterHandler("custom-handler", mockHandler.Object);

// Create an activity
var activity = new Activity
{
    Id = "process-order",
    Name = "Process Order Activity",
    HandlerType = "custom-handler",
    ConditionExpression = "${orderTotal} > 100",
    RetryPolicy = RetryPolicy.FixedDelay,
    MaxRetries = 3,
    TimeoutSeconds = 30
};

// Create execution context
var context = new WorkflowExecutionContext
{
    WorkflowInstanceId = "order-processing-workflow",
    CorrelationId = "order-12345",
    Variables = new Dictionary<string, object?>
    {
        { "orderTotal", 150 },
        { "customerId", 42 }
    }
};

// Execute the activity
var result = await activityService.ExecuteAsync(activity, context);

if (result.IsSuccess())
{
    Console.WriteLine($"Activity completed successfully: {result.Status}");
    Console.WriteLine($"Output: {string.Join(", ", result.Output.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
}
else if (result.Status == ActivityStatus.Skipped)
{
    Console.WriteLine("Activity was skipped due to condition evaluation");
}
else
{
    Console.WriteLine($"Activity failed: {result.ErrorMessage}");
}
```

## ActivityServiceTests

The `ActivityServiceTests` class contains comprehensive unit tests for the `ActivityService` class, which is responsible for executing workflow activities with support for conditional execution, retry policies, error handling, and context management. The test suite covers handler registration, various execution scenarios (gateway activities, invalid activities, missing handlers), conditional branching, retry policies (fixed delay, exponential backoff), error handling, and context preservation.





Example usage:


```csharp
// Create ActivityService with retry policy service
var retryPolicyService = new RetryPolicyService();
var activityService = new ActivityService(retryPolicyService);

// Register a custom activity handler
var mockHandler = new Mock<ActivityService.IActivityHandler>();
mockHandler.Setup(h => h.ExecuteAsync(
  It.IsAny<Activity>(),
  It.IsAny<WorkflowExecutionContext>()))
  .ReturnsAsync(new Dictionary<string, object?> { { "result", "success" } });

activityService.RegisterHandler("custom-handler", mockHandler.Object);

// Create an activity
var activity = new Activity
{
  Id = "process-order",
  Name = "Process Order Activity",
  HandlerType = "custom-handler",
  ConditionExpression = "${orderTotal} > 100",
  RetryPolicy = RetryPolicy.FixedDelay,
  MaxRetries = 3,
  TimeoutSeconds = 30
};

// Create execution context
var context = new WorkflowExecutionContext
{
  WorkflowInstanceId = "order-processing-workflow",
  CorrelationId = "order-12345",
  Variables = new Dictionary<string, object?>
  {
    { "orderTotal", 150 },
    { "customerId", 42 }
  }
};

// Execute the activity
var result = await activityService.ExecuteAsync(activity, context);

if (result.IsSuccess())
{
  Console.WriteLine($"Activity completed successfully: {result.Status}");
  Console.WriteLine($"Output: {string.Join(", ", result.Output.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
}
else if (result.Status == ActivityStatus.Skipped)
{
  Console.WriteLine("Activity was skipped due to condition evaluation");
}
else
{
  Console.WriteLine($"Activity failed: {result.ErrorMessage}");
}
```

## ConditionalBranchingServiceTests

The `ConditionalBranchingServiceTests` class contains comprehensive unit tests for the `ConditionalBranchingService` class, which handles conditional branching logic in workflows. These tests verify branch resolution, activity navigation, transition expression evaluation, default transition behavior, priority ordering, error handling, and validation of conditional expressions. The test suite covers scenarios like unconditional transitions, conditional transitions with matching/not-matching conditions, multiple conditional transitions, mixed conditional and unconditional transitions, default transitions, priority-based selection, variable evaluation in expressions, and error conditions.





Example usage:


```csharp
// Create ConditionalBranchingService
var loggerMock = new Mock<ILogger<ConditionalBranchingService>>();
var branchingService = new ConditionalBranchingService(loggerMock.Object);

// Create a workflow with activities
var workflow = new Workflow
{
  Id = "order-processing-workflow",
  Name = "Order Processing Workflow",
  Activities = new List<Activity>
  {
    new Activity { Id = "validate-order", Name = "Validate Order", TimeoutSeconds = 30 },
    new Activity { Id = "check-inventory", Name = "Check Inventory", TimeoutSeconds = 30 },
    new Activity { Id = "approve-order", Name = "Approve Order", TimeoutSeconds = 30 },
    new Activity { Id = "reject-order", Name = "Reject Order", TimeoutSeconds = 30 },
    new Activity { Id = "process-payment", Name = "Process Payment", TimeoutSeconds = 30 }
  },
  Transitions = new List<Transition>
  {
    // Unconditional transition
    new Transition { Id = "t1", FromActivityId = "validate-order", ToActivityId = "check-inventory" },
    
    // Conditional transitions
    new Transition
    {
      Id = "t2",
      FromActivityId = "check-inventory",
      ToActivityId = "approve-order",
      ConditionExpression = "${inventoryAvailable} == true"
    },
    new Transition
    {
      Id = "t3",
      FromActivityId = "check-inventory",
      ToActivityId = "reject-order",
      ConditionExpression = "${inventoryAvailable} == false"
    },
    
    // Default transition (used when no conditional matches)
    new Transition
    {
      Id = "t4",
      FromActivityId = "check-inventory",
      ToActivityId = "process-payment",
      IsDefault = true
    },
    
    // Priority-based conditional transitions
    new Transition
    {
      Id = "t5",
      FromActivityId = "approve-order",
      ToActivityId = "process-payment",
      ConditionExpression = "${amount} > 1000",
      Priority = 10
    },
    new Transition
    {
      Id = "t6",
      FromActivityId = "approve-order",
      ToActivityId = "manual-review",
      ConditionExpression = "${amount} <= 1000",
      Priority = 5
    }
  }
};

// Create execution context with workflow variables
var context = new WorkflowExecutionContext
{
  WorkflowInstanceId = "inst-12345",
  Variables = new Dictionary<string, object?>
  {
    { "inventoryAvailable", true },
    { "amount", 1500 },
    { "customerId", 42 }
  }
};

// Resolve branches from an activity
var result = await branchingService.ResolveBranchesAsync(
  workflow, 
  "check-inventory", 
  context
);

// Analyze results
Console.WriteLine($"Activity: {result.ActivityId}");
Console.WriteLine($"Any condition matched: {result.AnyConditionMatched}");
Console.WriteLine($"Used default transition: {result.UsedDefaultTransition}");

if (result.EvaluationErrors.Any())
{
  Console.WriteLine("Evaluation errors:");
  foreach (var error in result.EvaluationErrors)
  {
    Console.WriteLine($"  - Transition {error.TransitionId}: {error.ErrorMessage}");
  }
}

Console.WriteLine($"Selected transitions ({result.SelectedTransitions.Count}):");
foreach (var transition in result.SelectedTransitions)
{
  Console.WriteLine($"  - {transition.Id}: {transition.FromActivityId} → {transition.ToActivityId}");
}

Console.WriteLine($"Skipped transitions ({result.SkippedTransitions.Count}):");
foreach (var transition in result.SkippedTransitions)
{
  Console.WriteLine($"  - {transition.Id}: {transition.FromActivityId} → {transition.ToActivityId}");
}

// Get next activities (alternative method)
var nextActivities = await branchingService.GetNextActivitiesAsync(
  workflow, 
  "check-inventory", 
  context
);

Console.WriteLine($"Next activities: {string.Join(", ", nextActivities.Select(a => a.Id))}");

// Validate transition expressions in workflow
var validationErrors = branchingService.ValidateTransitionExpressions(workflow);
if (validationErrors.Any())
{
  Console.WriteLine("Validation errors found:");
  foreach (var error in validationErrors)
  {
    Console.WriteLine($"  - Transition {error.TransitionId}: {error.ErrorMessage}");
  }
}
```

## IWorkflowMessage

The `IWorkflowMessage` interface represents messages received from external systems that can be correlated to waiting workflow instances. It provides the essential correlation information (`CorrelationKey` and `MessageName`) along with a flexible payload container for message-specific data. Use it to construct and dispatch messages that trigger or resume workflow instances based on external events.

Example usage:

## IntegrationTests

The `IntegrationTests` class contains comprehensive integration tests that verify the end-to-end functionality of the workflow engine. These tests validate workflow execution, activity handling, state transitions, error recovery, conditional routing, audit trail logging, and concurrent execution scenarios. The test suite exercises the complete workflow lifecycle from instance creation through execution to completion.

Example usage:

```csharp
// Create services for testing workflow execution
var auditRepoMock = new Mock<IAuditRepository>();
auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);

var auditService = new AuditService(auditRepoMock.Object);
var definitionService = new WorkflowDefinitionService();
var retryPolicyService = new RetryPolicyService();
var activityService = new ActivityService(retryPolicyService);
var executionService = new WorkflowExecutionService(definitionService, auditService, activityService);

// Create a simple workflow with three activities
var workflow = new Workflow
{
    Id = "order-processing-workflow",
    Name = "Order Processing Workflow",
    StartActivityId = "validate-order",
    EndActivityId = "confirm-order",
    Activities = new List<Activity>
    {
        new Activity { Id = "validate-order", Name = "Validate Order", TimeoutSeconds = 30, MaxRetries = 3, HandlerType = "default" },
        new Activity { Id = "check-inventory", Name = "Check Inventory", TimeoutSeconds = 30, MaxRetries = 2, HandlerType = "default" },
        new Activity { Id = "confirm-order", Name = "Confirm Order", TimeoutSeconds = 30, MaxRetries = 1, HandlerType = "default" }
    },
    Transitions = new List<Transition>
    {
        new Transition { Id = "t1", FromActivityId = "validate-order", ToActivityId = "check-inventory" },
        new Transition { Id = "t2", FromActivityId = "check-inventory", ToActivityId = "confirm-order" }
    }
};
workflow.Publish();

definitionService.AddWorkflow(workflow);

// Create and start a workflow instance
var instance = executionService.CreateInstance(workflow.Id, "order-12345", "system");
instance.Start();

// Execute the workflow
var result = await executionService.StartAsync(instance.Id);

if (result.IsSuccess())
{
    Console.WriteLine($"Workflow completed successfully: {result.Status}");
    Console.WriteLine($"Total duration: {result.DurationMs}ms");
}
else
{
    Console.WriteLine($"Workflow failed: {result.ErrorMessage}");
}
```

## IWorkflowMessage
```csharp
// Create a payment confirmation message
var paymentMessage = new WorkflowMessage
{
    CorrelationKey = "order-12345",
    MessageName = "PaymentConfirmed",
    Payload = new Dictionary<string, object?>
    {
        { "orderId", "order-12345" },
        { "amount", 99.99m },
        { "paymentMethod", "credit_card" },
        { "transactionId", "txn-abc-789" }
    }
};

// Dispatch the message to the workflow engine
var messageDispatcher = new MessageDispatcher(eventBus);
await messageDispatcher.DispatchAsync(paymentMessage);

// Alternatively, use the concrete WorkflowMessage class directly
var approvalMessage = new WorkflowMessage
{
    CorrelationKey = "approval-9876",
    MessageName = "ApprovalGranted",
    Payload = new Dictionary<string, object?>
    {
        { "approverId", "user-42" },
        { "approvalLevel", "manager" },
        { "comments", "Looks good to proceed" }
    }
};
```