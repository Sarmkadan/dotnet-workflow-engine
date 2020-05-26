# ActivityService

The `ActivityService` class is the central orchestrator for registering and executing activity handlers within the workflow engine. It maintains a registry of handler types, validates activity names against the registry, and provides an asynchronous execution pipeline that dispatches work to the appropriate handler. The service is designed to decouple activity definitions from their implementations, enabling dynamic handler registration and runtime execution.

## API

### `ActivityService()`

Initializes a new instance of the `ActivityService` with an empty handler registry.

- **Parameters**: None.
- **Return value**: None.
- **Throws**: None.

### `void RegisterHandler(Type handlerType)`

Registers a handler type that can later be used to execute an activity. The handler type must implement a predefined interface or base class expected by the engine (e.g., `IActivityHandler`). Duplicate registrations for the same type are silently ignored.

- **Parameters**:
  - `handlerType` (`Type`): The type of the handler to register. Must not be `null` and must be a concrete class that satisfies the engine’s handler contract.
- **Return value**: None.
- **Throws**:
  - `ArgumentNullException` if `handlerType` is `null`.
  - `InvalidOperationException` if `handlerType` does not meet the required handler contract (e.g., missing a parameterless constructor or missing the expected interface).

### `virtual async Task<ActivityResult> ExecuteAsync(string activityName, object? input = null)`

Executes the activity identified by `activityName` using the registered handler. The method resolves the handler, invokes its execution logic with the provided `input`, and returns the result.

- **Parameters**:
  - `activityName` (`string`): The name of the activity to execute. This name must match a previously registered handler type’s identifier (typically the class name or a custom attribute).
  - `input` (`object?`, optional): Optional input data to pass to the handler. Default is `null`.
- **Return value**: `Task<ActivityResult>` – a task that represents the asynchronous execution. The result contains status, output data, and any error information produced by the handler.
- **Throws**:
  - `ArgumentNullException` if `activityName` is `null`.
  - `ArgumentException` if `activityName` is empty or consists only of white-space.
  - `KeyNotFoundException` if no handler is registered for the given `activityName`.
  - `InvalidOperationException` if the resolved handler fails to instantiate or if the handler’s execution throws an unhandled exception (the exception is wrapped in the `ActivityResult` when possible, but may propagate if the handler itself throws during construction).

### `List<string> GetRegisteredHandlerTypes()`

Returns a list of all registered handler type names. The names correspond to the identifiers used to register each handler (typically the full type name or a custom key).

- **Parameters**: None.
- **Return value**: `List<string>` – a new list containing the names of all currently registered handlers. The list is a snapshot; subsequent registrations are not reflected.
- **Throws**: None.

### `bool ValidateActivity(string activityName)`

Checks whether the specified activity name corresponds to a registered handler.

- **Parameters**:
  - `activityName` (`string`): The activity name to validate.
- **Return value**: `bool` – `true` if a handler is registered for the given name; otherwise `false`.
- **Throws**:
  - `ArgumentNullException` if `activityName` is `null`.

## Usage

### Example 1: Basic registration and execution

```csharp
using WorkflowEngine;

public class SendEmailHandler : IActivityHandler
{
    public Task<ActivityResult> ExecuteAsync(object? input)
    {
        // Simulate sending an email
        Console.WriteLine($"Sending email with input: {input}");
        return Task.FromResult(new ActivityResult { Status = ActivityStatus.Completed });
    }
}

var service = new ActivityService();
service.RegisterHandler(typeof(SendEmailHandler));

// Validate before execution
if (service.ValidateActivity("SendEmailHandler"))
{
    ActivityResult result = await service.ExecuteAsync("SendEmailHandler", "Hello, user!");
    Console.WriteLine($"Execution status: {result.Status}");
}
```

### Example 2: Inspecting registered handlers and handling missing activity

```csharp
var service = new ActivityService();
service.RegisterHandler(typeof(LogActivityHandler));
service.RegisterHandler(typeof(NotifyHandler));

// List all registered handlers
List<string> handlers = service.GetRegisteredHandlerTypes();
Console.WriteLine("Registered handlers: " + string.Join(", ", handlers));

// Attempt to execute an unregistered activity
string unknownActivity = "UnknownHandler";
if (!service.ValidateActivity(unknownActivity))
{
    Console.WriteLine($"Activity '{unknownActivity}' is not registered.");
}
else
{
    await service.ExecuteAsync(unknownActivity);
}
```

## Notes

- **Thread safety**: The `ActivityService` is not inherently thread-safe. Concurrent calls to `RegisterHandler` and `ExecuteAsync` may produce race conditions on the internal registry. For multi-threaded scenarios, external synchronization (e.g., a lock or a concurrent dictionary wrapper) is recommended.
- **Handler instantiation**: Each call to `ExecuteAsync` creates a new instance of the registered handler type via its parameterless constructor. Handlers should be stateless or designed for single-use to avoid unintended side effects.
- **Registration of abstract or interface types**: `RegisterHandler` will throw `InvalidOperationException` if the provided type is an interface or an abstract class. Only concrete, instantiable types are accepted.
- **Duplicate registrations**: Registering the same handler type multiple times does not cause an error; the registration is idempotent. The first registration is retained.
- **Empty or null activity names**: Both `ExecuteAsync` and `ValidateActivity` throw `ArgumentNullException` for `null` names and `ArgumentException` for empty or white-space-only names. Always validate input before calling these methods.
- **Handler type discovery**: The `GetRegisteredHandlerTypes` method returns a snapshot of the current registry. If handlers are added or removed after the call, the returned list is not updated.
