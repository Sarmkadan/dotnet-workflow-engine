# ConfigurationException

`ConfigurationException` is a specialized exception type used to signal problems related to workflow configuration settings. It carries optional information about the offending configuration key and its associated value, enabling callers to diagnose configuration issues with greater precision.

## API

### Fields

- **ConfigurationKey** (`string?`)  
  Gets or sets the configuration key that triggered the exception. May be `null` if the key is unknown or not applicable.

- **ConfigurationValue** (`string?`)  
  Gets or sets the value associated with `ConfigurationKey` that caused the validation failure. May be `null` if the value is irrelevant or unavailable.

### Constructors

- **ConfigurationException(string message)**  
  Initializes a new instance with a specified error message.  
  - *Parameters*  
    - `message`: A description of the error. May be `null`.  
  - *Behavior*  
    Calls the base `Exception(string)` constructor. Does not throw unless the runtime encounters an internal allocation failure.

- **ConfigurationException(string message, string configurationKey)**  
  Initializes a new instance with a message and the configuration key involved.  
  - *Parameters*  
    - `message`: Error description. May be `null`.  
    - `configurationKey`: The key associated with the faulty setting. May be `null`.  
  - *Behavior*  
    Calls the base `Exception(string)` constructor and assigns `configurationKey` to the `ConfigurationKey` field. No exceptions are thrown for null arguments.

- **ConfigurationException(string message, string configurationKey, string configurationValue)**  
  Initializes a new instance with a message, configuration key, and configuration value.  
  - *Parameters*  
    - `message`: Error description. May be `null`.  
    - `configurationKey`: The key associated with the faulty setting. May be `null`.  
    - `configurationValue`: The value that caused the validation failure. May be `null`.  
  - *Behavior*  
    Calls the base `Exception(string)` constructor and assigns the key and value to the respective fields. No exceptions are thrown for null arguments.

- **ConfigurationException(string message, Exception innerException)**  
  Initializes a new instance with a specified error message and a reference to the inner exception that caused this exception.  
  - *Parameters*  
    - `message`: Error description. May be `null`.  
    - `innerException`: The exception that is the cause of the current exception. May be `null`.  
  - *Behavior*  
    Calls the base `Exception(string, Exception)` constructor. Does not throw for a null `innerException`.

- **ConfigurationException()**  
  Initializes a new instance of the `ConfigurationException` class with default properties.  
  - *Behavior*  
    Calls the parameterless base `Exception()` constructor. The `ConfigurationKey` and `ConfigurationValue` fields remain `null`.

## Usage

```csharp
using DotnetWorkflowEngine.Configuration;

// Example 1: Throwing an exception when a required setting is missing.
string? apiEndpoint = GetSetting("ApiEndpoint");
if (string.IsNullOrEmpty(apiEndpoint))
{
    throw new ConfigurationException(
        "The ApiEndpoint setting must be provided.",
        configurationKey: "ApiEndpoint");
}

// Example 2: Preserving the original exception while adding configuration context.
try
{
    var timeout = int.Parse(GetSetting("TaskTimeout"));
}
catch (FormatException fmtEx)
{
    throw new ConfigurationException(
        "TaskTimeout must be a valid integer.",
        configurationKey: "TaskTimeout",
        configurationValue: GetSetting("TaskTimeout"),
        innerException: fmtEx);
}
```

## Notes

- The `ConfigurationKey` and `ConfigurationValue` fields are mutable after construction; concurrent modification from multiple threads without external synchronization can lead to race conditions. For thread‑safe consumption, treat the instance as immutable after it is thrown, or synchronize access manually.
- All members accept `null` values for string parameters; the exception does not validate or normalize these inputs. Callers should decide whether `null` conveys meaningful information (e.g., unknown key) or should be avoided.
- Because the class inherits from `Exception`, it behaves like any other .NET exception with respect to serialization, stack trace capture, and handling by standard exception‑filtering mechanisms. No additional serialization logic is implemented.
