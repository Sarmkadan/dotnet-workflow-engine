# SerializationHelper

`SerializationHelper` is a static utility class that provides a consistent abstraction over JSON serialization and deserialization for the `dotnet-workflow-engine` project. It wraps the underlying `System.Text.Json` infrastructure to offer both compact and human-readable serialization, safe deserialization with fallback defaults, deep cloning via serialization round-tripping, merging of JSON structures, and validation/formatting helpers. A dedicated `SerializationException` is provided for structured error reporting when serialization or deserialization fails.

## API

### Serialization Methods

#### `public static string ToJson<T>(T value)`
Serializes an object to its compact JSON string representation.
- **Parameters**: `value` – the object to serialize.
- **Returns**: A minified JSON string.
- **Throws**: `SerializationException` if the object cannot be serialized.

#### `public static string ToJsonPretty<T>(T value)`
Serializes an object to an indented, human-readable JSON string.
- **Parameters**: `value` – the object to serialize.
- **Returns**: A pretty-printed JSON string with line breaks and indentation.
- **Throws**: `SerializationException` if the object cannot be serialized.

#### `public static T? FromJson<T>(string json)`
Deserializes a JSON string to an instance of the specified type.
- **Parameters**: `json` – the JSON string to deserialize.
- **Returns**: An instance of `T`, or `null` if the input is null or empty.
- **Throws**: `SerializationException` if the JSON is malformed or cannot be mapped to `T`.

#### `public static Dictionary<string, object>? FromJsonToDict(string json)`
Deserializes a JSON string into a loosely-typed dictionary.
- **Parameters**: `json` – the JSON string to deserialize.
- **Returns**: A `Dictionary<string, object>` representing the JSON structure, or `null` if the input is null or empty. Nested objects are preserved as `JsonElement` values.
- **Throws**: `SerializationException` if the JSON is malformed.

#### `public static T? TryFromJson<T>(string json)`
Attempts to deserialize a JSON string without throwing on failure.
- **Parameters**: `json` – the JSON string to deserialize.
- **Returns**: An instance of `T` on success, or `default(T)` (which is `null` for reference types) if deserialization fails for any reason.
- **Throws**: Never throws; all exceptions are caught internally.

#### `public static T? DeepClone<T>(T source)`
Creates a deep copy of an object by serializing it to JSON and deserializing the result.
- **Parameters**: `source` – the object to clone.
- **Returns**: A new instance of `T` that is a value-level copy of the source, or `null` if the source is `null`.
- **Throws**: `SerializationException` if the object cannot be serialized or the resulting JSON cannot be deserialized back to `T`.

#### `public static T? Merge<T>(T target, string jsonPatch)`
Merges a JSON string into an existing object instance, producing a new combined object.
- **Parameters**: `target` – the base object to merge into; `jsonPatch` – a JSON string whose properties are applied on top of the target.
- **Returns**: A new instance of `T` with properties from `jsonPatch` overriding those in `target`, or `null` if both inputs are effectively null.
- **Throws**: `SerializationException` if serialization of the target or deserialization of the merged result fails.

#### `public static T? FromJsonElement<T>(JsonElement element)`
Deserializes a `JsonElement` directly to an instance of `T`.
- **Parameters**: `element` – the `JsonElement` to deserialize.
- **Returns**: An instance of `T`, or `null` if the element’s value kind is `Null` or `Undefined`.
- **Throws**: `SerializationException` if the element cannot be mapped to `T`.

#### `public static JsonElement ToJsonElement<T>(T value)`
Serializes an object directly to a `JsonElement` without producing an intermediate string.
- **Parameters**: `value` – the object to serialize.
- **Returns**: A `JsonElement` representing the serialized object.
- **Throws**: `SerializationException` if the object cannot be serialized.

### Validation and Formatting

#### `public static bool IsValidJson(string json)`
Checks whether a string is syntactically valid JSON.
- **Parameters**: `json` – the string to validate.
- **Returns**: `true` if the string is valid JSON; `false` otherwise, including for null or empty input.
- **Throws**: Never throws.

#### `public static string PrettyPrintJson(string json)`
Reformats a JSON string with indentation and line breaks for readability.
- **Parameters**: `json` – a valid JSON string (compact or already pretty).
- **Returns**: A pretty-printed JSON string.
- **Throws**: `SerializationException` if the input is not valid JSON.

#### `public static string MinifyJson(string json)`
Removes all insignificant whitespace from a JSON string.
- **Parameters**: `json` – a valid JSON string (pretty or compact).
- **Returns**: A minified JSON string.
- **Throws**: `SerializationException` if the input is not valid JSON.

### SerializationException

#### `public SerializationException(string message) : base(message)`
Initializes a new instance of `SerializationException` with a descriptive message.
- **Parameters**: `message` – the error message that describes the serialization failure.

#### `public SerializationException(string message, Exception innerException) : base(message, innerException)`
Initializes a new instance of `SerializationException` with a message and a reference to the inner exception that caused it.
- **Parameters**: `message` – the error message; `innerException` – the underlying exception.

## Usage

### Example 1: Safe Deserialization with Fallback

```csharp
using dotnet_workflow_engine;

public class WorkflowStep
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Order { get; set; }
}

// Attempt to deserialize potentially malformed input without crashing
string input = GetUntrustedJsonFromExternalSource();
WorkflowStep step = SerializationHelper.TryFromJson<WorkflowStep>(input);

if (step == null)
{
    // Fall back to a default instance
    step = new WorkflowStep { Id = "fallback", Name = "Default Step", Order = 0 };
}

// Proceed with the guaranteed non-null instance
ExecuteStep(step);
```

### Example 2: Deep Cloning and Merging Configuration

```csharp
using dotnet_workflow_engine;

public class WorkflowConfig
{
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;
    public Dictionary<string, string> Parameters { get; set; } = new();
}

// Base configuration loaded from a file
WorkflowConfig baseConfig = SerializationHelper.FromJson<WorkflowConfig>(
    File.ReadAllText("base_config.json"));

// Create a deep clone to avoid mutating the original
WorkflowConfig clonedConfig = SerializationHelper.DeepClone(baseConfig);

// Apply environment-specific overrides from a JSON patch
string envOverrides = @"{ ""MaxRetries"": 5, ""TimeoutSeconds"": 60 }";
WorkflowConfig mergedConfig = SerializationHelper.Merge(clonedConfig, envOverrides);

// Serialize the final configuration for logging
string logEntry = SerializationHelper.ToJsonPretty(mergedConfig);
Console.WriteLine($"Final configuration: {logEntry}");
```

## Notes

- **Null handling**: Methods returning nullable types (`T?`, `Dictionary<string, object>?`) return `null` when the input is `null` or, in the case of `TryFromJson<T>`, when deserialization fails. `FromJson<T>` and `FromJsonToDict` also return `null` for null or empty string inputs without throwing.
- **Deep cloning limitations**: `DeepClone<T>` relies on JSON round-tripping. Properties not supported by `System.Text.Json` (e.g., circular references, non-public setters without configuration, certain collection types) will cause a `SerializationException`. The clone is a value-level copy; reference identity is not preserved.
- **Merge behavior**: `Merge<T>` serializes the target to JSON, then deserializes the patch on top of it. Properties present in the patch overwrite those in the target; properties absent in the patch retain their target values. The result is always a new instance.
- **`FromJsonToDict` nested values**: Nested objects within the returned dictionary are stored as `JsonElement` instances, not recursively expanded into nested dictionaries. Callers must use `FromJsonElement<T>` or `JsonElement` APIs to traverse them further.
- **Thread safety**: All methods are static and operate on immutable inputs or produce new output instances. The class itself maintains no shared state and is safe to call concurrently from multiple threads.
- **Exception type**: All serialization/deserialization failures throw `SerializationException` (not the underlying `System.Text.Json` exceptions). The inner exception is preserved when constructing `SerializationException` with the two-parameter constructor, enabling callers to inspect the root cause.
