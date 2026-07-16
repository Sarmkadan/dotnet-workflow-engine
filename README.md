// ... (rest of the README content remains unchanged)
## MessageEventServiceTests

The `MessageEventServiceTests` class contains comprehensive unit tests for the `MessageEventService` class, which handles the publishing of messages to the workflow engine. It verifies the correct behavior of the service when publishing messages with valid or null data, when there are no waiting instances, when there are multiple waiting instances, and when resuming a workflow instance. 

Example usage:
```csharp
var service = new MessageEventService(
    new EventBus(new LoggerFactory()),
    new WorkflowExecutionService(
        new WorkflowDefinitionService(),
        new AuditService(new Mock<IAuditRepository>().Object),
        new ActivityService(new RetryPolicyService())
    ),
    new AuditService(new Mock<IAuditRepository>().Object)
);

var message = new WorkflowMessage
{
    CorrelationKey = "corr-1",
    MessageName = "OrderApproved",
    Payload = new Dictionary<string, object?> { { "orderId", "123" }, { "amount", 500 } }
};

var result = await service.PublishMessageAsync(message);
```


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

// ... (rest of the README content remains unchanged)
```