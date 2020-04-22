# ValidationException

The `ValidationException` class represents an exception that is thrown when one or more validation rules fail during workflow execution. It encapsulates a collection of error messages and an optional entity name, enabling callers to inspect the specific validation failures that caused the exception. This exception is typically used in validation scenarios where multiple errors need to be reported together rather than throwing separate exceptions for each failure.

## API

### Properties

#### `public IReadOnlyList<string> ValidationErrors`

Gets the list of validation error messages associated with this exception. The list is read-only and will never be `null`, though it may be empty if no errors were provided at construction time.

#### `public string? EntityName`

Gets the optional name of the entity that failed validation. This value may be `null` if no entity name was specified when the exception was created.

### Constructors

The class provides three constructor overloads:

- **`ValidationException()`**  
  Initializes a new instance of the `ValidationException` class with an empty list of validation errors and no entity name.

- **`ValidationException(IReadOnlyList<string> validationErrors)`**  
  Initializes a new instance with the specified collection of validation errors. The `ValidationErrors` property is set to the provided list. The `EntityName` property is `null`.  
  *Throws:* `ArgumentNullException` if `validationErrors` is `null`.

- **`ValidationException(string? entityName, IReadOnlyList<string> validationErrors)`**  
  Initializes a new instance with the specified entity name and validation errors. The `EntityName` property is set to the provided value (which may be `null`), and `ValidationErrors` is set to the provided list.  
  *Throws:* `ArgumentNullException` if `validationErrors` is `null`.

### Methods

#### `public string GetDetailedMessage()`

Returns a formatted string that includes the entity name (if present) and all validation error messages. The exact format is implementation‑defined, but it is intended to provide a human‑readable summary of the validation failures.

**Returns:** A `string` containing the detailed message.  
**Throws:** This method does not throw.

## Usage

### Example 1: Throwing and catching a ValidationException with multiple errors

```csharp
using System;
using System.Collections.Generic;
using WorkflowEngine.Validation;

public class OrderValidator
{
    public void Validate(Order order)
    {
        var errors = new List<string>();
        if (order.Total <= 0)
            errors.Add("Order total must be greater than zero.");
        if (string.IsNullOrWhiteSpace(order.CustomerName))
            errors.Add("Customer name is required.");

        if (errors.Count > 0)
            throw new ValidationException("Order", errors);
    }
}

// Usage
try
{
    var validator = new OrderValidator();
    validator.Validate(new Order { Total = -5, CustomerName = "" });
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed for entity: {ex.EntityName}");
    foreach (var error in ex.ValidationErrors)
        Console.WriteLine($"  - {error}");
    Console.WriteLine(ex.GetDetailedMessage());
}
```

### Example 2: Using the parameterless constructor and adding errors later (not recommended, but possible)

```csharp
using System;
using System.Collections.Generic;
using WorkflowEngine.Validation;

// Note: ValidationErrors is read-only, so errors must be provided at construction time.
// The parameterless constructor creates an empty list.
var ex = new ValidationException();
Console.WriteLine($"Error count: {ex.ValidationErrors.Count}"); // Output: 0
Console.WriteLine($"EntityName: {ex.EntityName}");            // Output: (null)
Console.WriteLine(ex.GetDetailedMessage());                   // Output: (empty or default message)
```

## Notes

- **Immutability:** After construction, the `ValidationErrors` collection and `EntityName` property are immutable. The `IReadOnlyList<string>` interface prevents modification of the list, and the property itself is read-only. This makes instances of `ValidationException` safe for concurrent read access once fully constructed.
- **Empty errors:** The `ValidationErrors` list may be empty. In such cases, `GetDetailedMessage()` returns a message that indicates no validation errors were provided, or simply the entity name if present.
- **Null entity name:** `EntityName` can be `null`. `GetDetailedMessage()` handles this gracefully and omits the entity name from the output when it is `null`.
- **Thread safety:** Because the exception is immutable after construction, reading its properties and calling `GetDetailedMessage()` from multiple threads simultaneously is safe. No synchronization is required.
- **Inheritance:** `ValidationException` inherits from `Exception` and can be caught using a standard `catch (Exception)` block, though it is recommended to catch the specific type for precise error handling.
