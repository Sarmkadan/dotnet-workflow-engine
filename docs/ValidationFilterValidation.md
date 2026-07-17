# ValidationFilterValidation

A static utility class providing validation functionality for workflow components. It offers methods to validate objects, check validity status, and enforce validation constraints by throwing exceptions when invalid.

## API

### Validate

Validates the current context and returns a list of validation error messages.

**Parameters:** None (static method)

**Return Value:** `IReadOnlyList<string>` - A read-only list of error messages. Returns an empty list if valid.

**Exceptions:** None

---

### IsValid

Indicates whether the current context passes all validation checks.

**Parameters:** None (static method)

**Return Value:** `bool` - `true` if validation passes; otherwise, `false`.

**Exceptions:** None

---

### EnsureValid

Validates the current context and throws an exception if validation fails.

**Parameters:** None (static method)

**Return Value:** `void`

**Exceptions:** Throws `ValidationException` if validation fails.

---

### Validate (Overload)

Validates a specific object instance and returns a list of validation error messages.

**Parameters:** `object instance` - The object to validate.

**Return Value:** `IReadOnlyList<string>` - A read-only list of error messages. Returns an empty list if valid.

**Exceptions:** None

---

### IsValid (Overload)

Indicates whether a specific object instance passes all validation checks.

**Parameters:** `object instance` - The object to validate.

**Return Value:** `bool` - `true` if validation passes; otherwise, `false`.

**Exceptions:** None

---

### EnsureValid (Overload)

Validates a specific object instance and throws an exception if validation fails.

**Parameters:** `object instance` - The object to validate.

**Return Value:** `void`

**Exceptions:** Throws `ValidationException` if validation fails.

---

### Validate (Overload)

Validates a workflow step and returns a list of validation error messages.

**Parameters:** `WorkflowStep step` - The workflow step to validate.

**Return Value:** `IReadOnlyList<string>` - A read-only list of error messages. Returns an empty list if valid.

**Exceptions:** None

---

### IsValid (Overload)

Indicates whether a workflow step passes all validation checks.

**Parameters:** `WorkflowStep step` - The workflow step to validate.

**Return Value:** `bool` - `true` if validation passes; otherwise, `false`.

**Exceptions:** None

---

### EnsureValid (Overload)

Validates a workflow step and throws an exception if validation fails.

**Parameters:** `WorkflowStep step` - The workflow step to validate.

**Return Value:** `void`

**Exceptions:** Throws `ValidationException` if validation fails.

---

### Validate (Overload)

Validates a workflow definition and returns a list of validation error messages.

**Parameters:** `WorkflowDefinition definition` - The workflow definition to validate.

**Return Value:** `IReadOnlyList<string>` - A read-only list of error messages. Returns an empty list if valid.

**Exceptions:** None

---

## Usage

### Example 1: Validating a Workflow Step

```csharp
var step = new WorkflowStep { Name = "ProcessOrder", Action = "ValidateInventory" };
var errors = ValidationFilterValidation.Validate(step);

if (errors.Any())
{
    Console.WriteLine($"Validation failed: {string.Join(", ", errors)}");
}
else
{
    Console.WriteLine("Step is valid.");
}
```

### Example 2: Enforcing Validation in a Workflow Controller

```csharp
public class WorkflowController
{
    public void ExecuteStep(WorkflowStep step)
    {
        ValidationFilterValidation.EnsureValid(step); // Throws if invalid
        // Proceed with execution
    }
}
```

## Notes

- All methods are static and do not maintain instance state, making them inherently thread-safe for concurrent use.
- `Validate` methods return an empty list when no errors are found, allowing callers to distinguish between valid and invalid states without exceptions.
- `EnsureValid` methods throw `ValidationException` immediately upon detecting invalid state, suitable for fail-fast scenarios.
- Overloads target different validation contexts (general objects, workflow steps, definitions) to provide type-specific validation logic.
- Validation rules may include null checks, required field constraints, or domain-specific business rules depending on the target type.
