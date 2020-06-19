// ... (rest of the README content remains unchanged)

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
