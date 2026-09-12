<full file content here>
## RateLimitingMiddlewareTests
The `RateLimitingMiddlewareTests` class provides a set of tests for the rate limiting middleware. It tests various scenarios such as requests under the limit, requests over the limit, and exempt paths. Here is an example of how to use it:
```csharp
var tests = new RateLimitingMiddlewareTests();
await tests.InvokeAsync_RequestUnderLimit_PassesThrough();
await tests.InvokeAsync_RequestOverLimit_Returns429();
```

## ActivityService

`ActivityService` (in `Services/ActivityService.cs`) executes workflow activities and manages the handlers that run them. It lives in the `DotNetWorkflowEngine.Services` namespace.

### Purpose

The service is the execution core for a single activity within a workflow. Given an `Activity` and an `ExecutionContext`, it:

- Validates the activity configuration before running it.
- Resolves the registered handler for the activity's `HandlerType` and invokes it.
- Enforces a per-attempt timeout via `Activity.TimeoutSeconds` (a value of `0` disables the timeout).
- Applies the activity's retry policy (`FixedDelay`, `ExponentialBackoff`, `LinearBackoff`, or `NoRetry`) on failure.
- Short-circuits gateway activities and activities that don't require a handler.
- Evaluates a conditional skip expression (`Activity.ConditionExpression`) before execution.

### Public API

| Member | Description |
| --- | --- |
| `ActivityService(RetryPolicyService retryPolicyService)` | Constructor. Throws `ArgumentNullException` if the retry policy service is `null`. |
| `void RegisterHandler(string handlerType, IActivityHandler handler)` | Registers a handler for a given activity type. Throws `ArgumentException` for a null/empty type and `ArgumentNullException` for a null handler. |
| `Task<ActivityResult> ExecuteAsync(Activity activity, ExecutionContext context)` | Executes the activity with its configured retry policy. Throws `ValidationException` on invalid configuration and `ActivityException` if execution fails after all retries. |
| `List<string> GetRegisteredHandlerTypes()` | Returns the handler types currently registered. |
| `bool ValidateActivity(Activity activity, out List<string> errors)` | Validates an activity without executing it. |

The nested `IActivityHandler` interface defines the contract handlers must implement:

```csharp
public interface IActivityHandler
{
    Task<Dictionary<string, object?>> ExecuteAsync(Activity activity, ExecutionContext context);
}
```

### Usage example

```csharp
using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;

// 1. Create the retry policy service and the activity service.
var retryPolicyService = new RetryPolicyService();
var activityService = new ActivityService(retryPolicyService);

// 2. Register a handler for the activity type.
activityService.RegisterHandler("send-email", new EmailHandler());

// 3. Build the activity to execute.
var activity = new Activity
{
    Id = "send-welcome-email",
    Name = "Send welcome email",
    HandlerType = "send-email",
    RetryPolicy = RetryPolicy.ExponentialBackoff,
    MaxRetries = 3,
    TimeoutSeconds = 30
};

// 4. Execute it within a workflow context.
var context = new ExecutionContext();
var result = await activityService.ExecuteAsync(activity, context);

if (result.Succeeded)
{
    Console.WriteLine("Activity completed.");
}
else
{
    Console.WriteLine($"Activity failed: {result.ErrorMessage}");
}
```

A minimal handler implementation looks like this:

```csharp
public class EmailHandler : ActivityService.IActivityHandler
{
    public Task<Dictionary<string, object?>> ExecuteAsync(Activity activity, ExecutionContext context)
    {
        // Perform the work here.
        return Task.FromResult(new Dictionary<string, object?>
        {
            ["sent"] = true
        });
    }
}
```
