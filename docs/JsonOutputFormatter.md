# JsonOutputFormatter

Provides functionality to serialize objects into JSON strings asynchronously. This formatter is designed for use within the workflow engine to produce JSON representations of workflow outputs, step results, or any serializable data. It leverages `System.Text.Json` under the hood and exposes three overloads of the `FormatAsync` method to accommodate both generic and non-generic formatting scenarios.

## API

### `public JsonOutputFormatter()`

Initializes a new instance of the `JsonOutputFormatter` class with default JSON serializer settings.

---

### `public Task<string> FormatAsync<T>(T value)`

Serializes the specified value to a JSON string.

- **Type parameters**: `T` – The type of the value to serialize.
- **Parameters**:
  - `value` – The object to format. Can be `null`.
- **Returns**: A `Task<string>` that resolves to the JSON representation of `value`.
- **Throws**:
  - `ArgumentNullException` – if `value` is `null` and the type `T` is a value type (struct) that cannot be null. For reference types, `null` is serialized as `"null"`.
  - `JsonException` – if serialization fails (e.g., circular references, unsupported types).

---

### `public Task<string> FormatAsync<T>(T value, JsonSerializerOptions? options)`

Serializes the specified value to a JSON string using custom serializer options.

- **Type parameters**: `T` – The type of the value to serialize.
- **Parameters**:
  - `value` – The object to format. Can be `null`.
  - `options` – A `JsonSerializerOptions` instance that controls serialization behavior (e.g., naming policy, indentation). Pass `null` to use the default options.
- **Returns**: A `Task<string>` that resolves to the JSON representation of `value`.
- **Throws**:
  - `ArgumentNullException` – if `value` is `null` and `T` is a non-nullable value type.
  - `JsonException` – if serialization fails.

---

### `public Task<string> FormatAsync(object value)`

Serializes the specified value to a JSON string using the runtime type of the object.

- **Parameters**:
  - `value` – The object to format. Can be `null`.
- **Returns**: A `Task<string>` that resolves to the JSON representation of `value`.
- **Throws**:
  - `JsonException` – if serialization fails (e.g., the runtime type is not supported, circular references).

## Usage

### Example 1: Formatting a simple object with default settings

```csharp
using System;
using System.Threading.Tasks;
using WorkflowEngine.Formatters;

public class WorkflowResult
{
    public string Id { get; set; }
    public int StatusCode { get; set; }
    public DateTime CompletedAt { get; set; }
}

public async Task FormatResultAsync()
{
    var formatter = new JsonOutputFormatter();
    var result = new WorkflowResult
    {
        Id = "wf-001",
        StatusCode = 200,
        CompletedAt = DateTime.UtcNow
    };

    string json = await formatter.FormatAsync(result);
    Console.WriteLine(json);
    // Output: {"Id":"wf-001","StatusCode":200,"CompletedAt":"2025-03-20T12:34:56Z"}
}
```

### Example 2: Formatting with custom options (camelCase, indented)

```csharp
using System;
using System.Text.Json;
using System.Threading.Tasks;
using WorkflowEngine.Formatters;

public async Task FormatWithOptionsAsync()
{
    var formatter = new JsonOutputFormatter();
    var options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    var data = new { UserName = "Alice", Score = 95.5 };
    string json = await formatter.FormatAsync(data, options);
    Console.WriteLine(json);
    // Output:
    // {
    //   "userName": "Alice",
    //   "score": 95.5
    // }
}
```

## Notes

- **Thread safety**: Instances of `JsonOutputFormatter` are not inherently thread-safe. If the same instance is used concurrently from multiple threads, external synchronization is required. The `JsonSerializerOptions` object passed to the generic overload is also not thread-safe; do not mutate options while a formatting operation is in progress.
- **Null handling**: When `value` is `null` and the generic type `T` is a reference type, the output will be the JSON literal `null`. For non-nullable value types, passing `null` will cause an `ArgumentNullException`.
- **Circular references**: By default, `System.Text.Json` throws a `JsonException` when it detects circular references. To handle such cases, configure the `ReferenceHandler` property in `JsonSerializerOptions` (e.g., `ReferenceHandler.Preserve`).
- **Large objects**: The `FormatAsync` methods serialize the entire object graph into memory before returning the string. For very large outputs, consider streaming alternatives or increasing the `MaxDepth` option if deep nesting is expected.
- **Performance**: The formatter uses `System.Text.Json` which is optimized for high throughput. However, repeated creation of `JsonSerializerOptions` instances can incur overhead; cache options when reusing the same configuration.
