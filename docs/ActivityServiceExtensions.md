# ActivityServiceExtensions

Provides extension methods for registering, validating, and executing activities within the workflow engine. These methods centralize common activity-handling operations and expose metadata about registered handlers.

## API

### `IsHandlerRegistered`

Determines whether a handler with the specified name is registered in the activity service.

- **Parameters**
  - `handlerName` (`string`): The name of the handler to check.
- **Return value**
  - `bool`: `true` if a handler with the name is registered; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `handlerName` is `null`.

### `CreateActivity`

Creates an `Activity` instance from a handler with the specified name and input data.

- **Parameters**
  - `handlerName` (`string`): The name of the registered handler.
  - `input` (`object?`): Optional input data to pass to the handler.
- **Return value**
  - `Activity`: An `Activity` instance configured with the handler and input.
- **Exceptions**
  - Throws `ArgumentNullException` if `handlerName` is `null`.
  - Throws `InvalidOperationException` if no handler with the specified name is registered.

### `ValidateAllHandlers`

Validates all registered activity handlers and returns a dictionary indicating whether each handler is valid.

- **Parameters**
  - (None)
- **Return value**
  - `IReadOnlyDictionary<string, bool>`: A read-only dictionary mapping handler names to validation results (`true` if valid, `false` otherwise).
- **Exceptions**
  - Throws `InvalidOperationException` if no handlers are registered.

### `GetHandlerCount`

Returns the number of handlers currently registered in the activity service.

- **Parameters**
  - (None)
- **Return value**
  - `int`: The count of registered handlers.
- **Exceptions**
  - (None)

### `ExecuteActivityAsync`

Executes a single activity asynchronously and returns its result.

- **Parameters**
  - `activity` (`Activity`): The activity to execute.
  - `cancellationToken` (`CancellationToken`, optional): A token to monitor for cancellation requests.
- **Return value**
  - `Task<ActivityResult>`: A task representing the asynchronous operation, containing the activity result.
- **Exceptions**
  - Throws `ArgumentNullException` if `activity` is `null`.
  - Throws `InvalidOperationException` if the activity’s handler is not registered or is invalid.

### `ExecuteActivitiesAsync`

Executes multiple activities asynchronously and returns their results.

- **Parameters**
  - `activities` (`IEnumerable<Activity>`): The activities to execute.
  - `cancellationToken` (`CancellationToken`, optional): A token to monitor for cancellation requests.
- **Return value**
  - `Task<IReadOnlyList<ActivityResult>>`: A task representing the asynchronous operation, containing the results of all activities.
- **Exceptions**
  - Throws `ArgumentNullException` if `activities` is `null`.
  - Throws `InvalidOperationException` if any activity’s handler is not registered or is invalid.

## Usage

### Registering and validating handlers

```csharp
using var serviceProvider = new ServiceCollection()
    .AddWorkflowEngine()
    .AddActivity<CustomActivity>("customHandler")
    .BuildServiceProvider();

var extensions = serviceProvider.GetRequiredService<ActivityServiceExtensions>();

// Validate all registered handlers
var validationResults = extensions.ValidateAllHandlers();
Console.WriteLine($"Valid handlers: {validationResults.Count(kv => kv.Value)}");
```

### Executing activities

```csharp
using var serviceProvider = new ServiceCollection()
    .AddWorkflowEngine()
    .AddActivity<CustomActivity>("customHandler")
    .BuildServiceProvider();

var extensions = serviceProvider.GetRequiredService<ActivityServiceExtensions>();

var activity = extensions.CreateActivity("customHandler", new { Data = 42 });
var result = await extensions.ExecuteActivityAsync(activity);

if (result.IsSuccess)
{
    Console.WriteLine($"Activity completed: {result.Output}");
}
```

## Notes

- Thread safety is guaranteed for all methods; the underlying service uses internal synchronization to protect shared state.
- Validation results reflect the state of handlers at the time of invocation; concurrent registrations or removals may affect outcomes.
- Cancellation tokens are respected during execution; however, long-running handlers should implement cooperative cancellation checks.
- Handler names are case-sensitive; ensure consistent casing when registering and referencing handlers.
- `ExecuteActivitiesAsync` executes activities concurrently by default; order of results corresponds to the input sequence only if the underlying scheduler preserves it.
