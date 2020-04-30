# ValidationFilter

`ValidationFilter` is an action filter for ASP.NET Core that validates incoming request models using `System.ComponentModel.DataAnnotations` attributes before the controller action executes. It intercepts the action pipeline, inspects the model state, and short-circuits the request with a structured error response when validation fails, eliminating the need for repetitive `ModelState.IsValid` checks in individual controllers.

## API

### ValidationFilter

```csharp
public ValidationFilter()
```

Default parameterless constructor. Initializes a new instance of the filter with no pre-configured message or error overrides. The `Message` and `Errors` properties will be populated during execution.

### OnActionExecutionAsync

```csharp
public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
```

**Purpose:** Intercepts the action execution pipeline before the controller action runs. Checks whether the model state is valid. If invalid, sets `context.Result` to a 400 Bad Request `ObjectResult` containing the validation details and prevents the action from executing. If valid, calls `next()` to proceed with the pipeline.

**Parameters:**
- `context` (`ActionExecutingContext`): The context for the executing action, containing model state, action arguments, and metadata.
- `next` (`ActionExecutionDelegate`): The delegate to invoke the next filter or the action itself.

**Return value:** A `Task` representing the asynchronous operation.

**Throws:** Does not throw exceptions directly. Exceptions from `next()` propagate normally.

### DataAnnotationValidationFilter

```csharp
public DataAnnotationValidationFilter()
```

Default parameterless constructor for the derived `DataAnnotationValidationFilter` class. This subclass extends `ValidationFilter` and provides the same validation behavior, typically used when a distinct filter type is needed for registration or metadata purposes.

### OnActionExecutionAsync (DataAnnotationValidationFilter)

```csharp
public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
```

**Purpose:** Inherited override that performs the same model validation logic as the base class. Checks model state validity and short-circuits with a structured error response when validation attributes fail.

**Parameters:**
- `context` (`ActionExecutingContext`): The action execution context.
- `next` (`ActionExecutionDelegate`): The pipeline continuation delegate.

**Return value:** A `Task` representing the asynchronous operation.

**Throws:** Does not throw exceptions directly.

### Message

```csharp
public string? Message { get; set; }
```

**Purpose:** A human-readable summary message describing the validation outcome. Set to `"Validation failed"` by default when the model state is invalid. Can be customized before or after execution to provide context-specific messaging. Null when no validation has occurred or validation passed.

### Errors

```csharp
public List<KeyValuePair<string, string[]>>? Errors { get; set; }
```

**Purpose:** A collection of field-level validation errors extracted from the model state. Each entry consists of a field name (key) and an array of error messages for that field (value). Populated automatically when validation fails. Null when no validation has occurred or validation passed.

### Timestamp

```csharp
public DateTime Timestamp { get; set; }
```

**Purpose:** The UTC timestamp at which the validation was performed. Set automatically during filter execution. Provides an audit trail for when the validation decision was made.

### AllowedValuesAttribute

```csharp
public AllowedValuesAttribute : ValidationAttribute
```

**Purpose:** A custom validation attribute that restricts a property's value to a predefined set of allowed values. Derives from `ValidationAttribute` and integrates with the `DataAnnotationValidationFilter` to produce model state errors when a value falls outside the permitted set.

## Usage

### Example 1: Applying ValidationFilter Globally

Register the filter globally so every controller action automatically benefits from model validation without explicit `ModelState.IsValid` checks.

```csharp
// Program.cs or Startup.cs
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});
```

A controller action that receives an invalid model will automatically return a 400 response:

```csharp
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    [HttpPost]
    public IActionResult Create([FromBody] CreateOrderRequest request)
    {
        // No ModelState.IsValid check needed — the filter handles it.
        var order = _orderService.Create(request);
        return Ok(order);
    }
}

// Example request with missing required fields returns:
// HTTP 400
// {
//   "message": "Validation failed",
//   "errors": [
//     { "key": "CustomerId", "values": ["The CustomerId field is required."] }
//   ],
//   "timestamp": "2025-03-21T14:30:00Z"
// }
```

### Example 2: Using DataAnnotationValidationFilter with AllowedValuesAttribute

Apply the filter at the controller or action level and leverage the custom `AllowedValuesAttribute` for domain-specific validation.

```csharp
[ServiceFilter(typeof(DataAnnotationValidationFilter))]
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    [HttpPut("{id}")]
    public IActionResult UpdateStatus(int id, [FromBody] ProductStatusUpdateRequest request)
    {
        _productService.UpdateStatus(id, request.NewStatus);
        return NoContent();
    }
}

public class ProductStatusUpdateRequest
{
    [AllowedValues("Draft", "Published", "Archived", ErrorMessage = "Status must be Draft, Published, or Archived.")]
    public string NewStatus { get; set; }
}
```

A request with `"NewStatus": "Deleted"` produces:

```json
{
  "message": "Validation failed",
  "errors": [
    { "key": "NewStatus", "values": ["Status must be Draft, Published, or Archived."] }
  ],
  "timestamp": "2025-03-21T14:35:12Z"
}
```

## Notes

- **Short-circuit behavior:** When validation fails, the filter sets `context.Result` and does not call `next()`. The controller action and any subsequent filters in the pipeline are skipped entirely. Ensure that any resource allocation or side effects expected before validation are placed in an earlier filter.
- **Nullability of Message and Errors:** Both `Message` and `Errors` remain `null` when validation passes and the pipeline proceeds normally. Consumers reading these properties after a successful request will encounter null values. Always null-check before accessing.
- **Timestamp precision:** `Timestamp` is set to `DateTime.UtcNow` at the moment of validation. For high-throughput systems, multiple requests processed within the same millisecond may share identical timestamps. This is intentional and sufficient for diagnostic purposes.
- **Thread safety:** The filter is instantiated per request by the ASP.NET Core DI container when registered as a service filter or type filter. Instance properties (`Message`, `Errors`, `Timestamp`) are scoped to a single request and are not shared across concurrent requests. No thread-safety concerns arise under normal scoped or transient registration. Avoid registering the filter as a singleton if property mutation per request is expected.
- **Model binding vs. validation:** The filter only inspects `ModelState` after model binding has completed. Binding errors (e.g., type mismatches, malformed JSON) appear as model state entries and are treated identically to validation attribute failures. The filter does not distinguish between the two categories.
- **AllowedValuesAttribute integration:** This attribute produces standard model state entries when validation fails. No special handling is required in the filter; it is treated like any other `ValidationAttribute` subclass. Ensure the attribute's constructor receives the permitted values and that the error message is set either via the attribute parameter or the `ErrorMessage` property.
