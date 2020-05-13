# ErrorHandlingExample

The `ErrorHandlingExample` class provides a structured approach to managing workflow initialization, data processing, and error retrieval within the `dotnet-workflow-engine` project. It encapsulates configuration via `DataSourceUrl` and `ProcessingRules`, and exposes asynchronous methods that return `ActionResult` to indicate success or failure. This class is intended to be used as a controller-level component that coordinates workflow steps and surfaces error information in a consistent HTTP-friendly format.

## API

### `public ErrorHandlingExample()`

Initializes a new instance of the `ErrorHandlingExample` class.  
No parameters.  
No return value.  
Does not throw.

### `public async Task<ActionResult> InitializeWorkflow()`

Prepares the workflow engine for execution by validating the current configuration and establishing any required connections or resources.  
**Parameters:** None.  
**Returns:** A `Task<ActionResult>` that resolves to an `OkResult` on success, or an `ObjectResult` with error details on failure.  
**Throws:**  
- `InvalidOperationException` if `DataSourceUrl` is `null` or empty.  
- `InvalidOperationException` if `ProcessingRules` is `null`.  
- Any exception thrown by underlying infrastructure (e.g., network failures) is caught and wrapped into an error `ActionResult`.

### `public async Task<ActionResult> ProcessData()`

Executes the data processing pipeline using the configured `DataSourceUrl` and `ProcessingRules`.  
**Parameters:** None.  
**Returns:** A `Task<ActionResult>` that resolves to an `OkResult` containing processing results on success, or an error `ActionResult` on failure.  
**Throws:**  
- `InvalidOperationException` if `InitializeWorkflow` has not been called successfully prior to this method.  
- `InvalidOperationException` if `DataSourceUrl` is `null` or empty.  
- Any exception from data access or rule evaluation is caught and returned as an error `ActionResult`.

### `public async Task<ActionResult> GetErrorInfo()`

Retrieves the most recent error information recorded during workflow initialization or data processing.  
**Parameters:** None.  
**Returns:** A `Task<ActionResult>` that resolves to an `OkResult` with a serialized error object (or `null` if no error has occurred), or an error `ActionResult` if retrieval itself fails.  
**Throws:**  
- `InvalidOperationException` if the internal error store is unavailable.  
- This method does not clear the error state; repeated calls return the same error until a new operation overwrites it.

### `public string DataSourceUrl`

Gets or sets the URL of the data source to be used by the workflow.  
**Value:** A string representing the endpoint or file path.  
**Default:** `null`.  
**Remarks:** Setting this property after `InitializeWorkflow` has been called does not affect the already-initialized workflow; a new call to `InitializeWorkflow` is required.

### `public Dictionary<string, object> ProcessingRules`

Gets or sets a dictionary of processing rules, where each key is a rule name and the value is a rule configuration object.  
**Value:** A `Dictionary<string, object>` instance.  
**Default:** `null`.  
**Remarks:** The dictionary is not copied; external modifications after assignment affect the instance. Setting this property to `null` will cause subsequent calls to `InitializeWorkflow` to throw.

## Usage

### Example 1: Basic workflow execution

```csharp
var handler = new ErrorHandlingExample
{
    DataSourceUrl = "https://api.example.com/data",
    ProcessingRules = new Dictionary<string, object>
    {
        ["validation"] = new { strict = true },
        ["transformation"] = new { format = "json" }
    }
};

ActionResult initResult = await handler.InitializeWorkflow();
if (initResult is OkResult)
{
    ActionResult processResult = await handler.ProcessData();
    if (processResult is OkResult ok)
    {
        Console.WriteLine("Processing succeeded.");
    }
    else
    {
        ActionResult errorInfo = await handler.GetErrorInfo();
        Console.WriteLine($"Processing failed. Error details: {errorInfo}");
    }
}
else
{
    Console.WriteLine("Initialization failed.");
}
```

### Example 2: Error handling with explicit checks

```csharp
var handler = new ErrorHandlingExample();
handler.DataSourceUrl = "ftp://invalid-host/data.csv";
handler.ProcessingRules = new Dictionary<string, object>();

try
{
    await handler.InitializeWorkflow();
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Configuration error: {ex.Message}");
    return;
}

ActionResult result = await handler.ProcessData();
if (result is ObjectResult errorResult && errorResult.Value is ProblemDetails problem)
{
    Console.WriteLine($"ProcessData returned status {problem.Status}: {problem.Detail}");
    ActionResult errorDetail = await handler.GetErrorInfo();
    // Log or inspect errorDetail
}
```

## Notes

- **Edge Cases:**  
  - If `DataSourceUrl` is set to an empty string, `InitializeWorkflow` throws an `InvalidOperationException`.  
  - If `ProcessingRules` is set to an empty dictionary, processing may succeed but produce no transformations.  
  - `GetErrorInfo` returns `null` (as part of an `OkResult`) if no error has been recorded since the last successful `InitializeWorkflow` or `ProcessData` call.  
  - Calling `ProcessData` without first calling `InitializeWorkflow` throws `InvalidOperationException`.

- **Thread Safety:**  
  Instances of `ErrorHandlingExample` are **not thread-safe**. The mutable properties `DataSourceUrl` and `ProcessingRules` can be read or written concurrently, leading to inconsistent state. The asynchronous methods should not be invoked concurrently on the same instance. If concurrent access is required, external synchronization (e.g., a lock or dedicated scope per thread) must be used.
