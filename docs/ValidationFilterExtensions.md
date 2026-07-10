# ValidationFilterExtensions

The `ValidationFilterExtensions` class provides a set of static helper methods for collecting, combining, and inspecting validation errors within the workflow engine’s validation pipeline. These methods are designed to work with lists of error strings and property‑error dictionaries, enabling consistent error aggregation and conditional logic in custom validation filters.

## API

### `AddValidationError`

```csharp
public static void AddValidationError(List<string> errors, string error)
```

Adds a single error message to a list of errors.  
**Parameters:**  
- `errors` – The list to which the error is appended. Must not be `null`.  
- `error` – The error message to add. Must not be `null` or empty.  

**Throws:**  
- `ArgumentNullException` if `errors` is `null`.  
- `ArgumentNullException` if `error` is `null`.  
- `ArgumentException` if `error` is empty or consists only of white space.

### `ValidateValue`

```csharp
public static List<string> ValidateValue<T>(T value, Func<T, bool> predicate, string errorMessage)
```

Evaluates a predicate against a value and returns a list containing the provided error message if the predicate fails; otherwise returns an empty list.  
**Parameters:**  
- `value` – The value to validate.  
- `predicate` – A function that returns `true` when the value is considered valid. Must not be `null`.  
- `errorMessage` – The error message to include when validation fails. Must not be `null` or empty.  

**Returns:**  
A `List<string>` containing `errorMessage` if `predicate(value)` is `false`; otherwise an empty list.  

**Throws:**  
- `ArgumentNullException` if `predicate` is `null`.  
- `ArgumentNullException` if `errorMessage` is `null`.  
- `ArgumentException` if `errorMessage` is empty or white space.

### `CombineErrors`

```csharp
public static string CombineErrors(IEnumerable<string> errors, string separator = "; ")
```

Concatenates all error messages into a single string using the specified separator.  
**Parameters:**  
- `errors` – A collection of error strings. May be `null` or empty.  
- `separator` – The string used to separate individual errors. Defaults to `"; "`. Must not be `null`.  

**Returns:**  
A single string containing all errors joined by the separator. Returns an empty string if `errors` is `null` or contains no elements.  

**Throws:**  
- `ArgumentNullException` if `separator` is `null`.

### `HasPropertyErrors`

```csharp
public static bool HasPropertyErrors(Dictionary<string, List<string>> propertyErrors, string propertyName)
```

Checks whether a specific property has any associated validation errors in a property‑error dictionary.  
**Parameters:**  
- `propertyErrors` – A dictionary mapping property names to lists of error messages. Must not be `null`.  
- `propertyName` – The name of the property to check. Must not be `null` or empty.  

**Returns:**  
`true` if the dictionary contains an entry for `propertyName` and that entry’s list is not empty; otherwise `false`.  

**Throws:**  
- `ArgumentNullException` if `propertyErrors` is `null`.  
- `ArgumentNullException` if `propertyName` is `null`.  
- `ArgumentException` if `propertyName` is empty or white space.

## Usage

### Example 1: Validating a workflow step input

```csharp
public List<string> ValidateStepInput(StepInput input)
{
    var errors = new List<string>();

    // Validate that the amount is positive
    ValidationFilterExtensions.AddValidationError(
        errors,
        "Amount must be greater than zero.");

    var amountErrors = ValidationFilterExtensions.ValidateValue(
        input.Amount,
        amount => amount > 0,
        "Amount must be greater than zero.");

    errors.AddRange(amountErrors);

    // Validate that the description is not empty
    var descErrors = ValidationFilterExtensions.ValidateValue(
        input.Description,
        desc => !string.IsNullOrWhiteSpace(desc),
        "Description is required.");

    errors.AddRange(descErrors);

    return errors;
}
```

### Example 2: Combining property errors and checking for specific field errors

```csharp
var propertyErrors = new Dictionary<string, List<string>>
{
    ["Email"] = new List<string> { "Email is invalid." },
    ["Name"]  = new List<string>()
};

// Check if the "Email" property has errors
if (ValidationFilterExtensions.HasPropertyErrors(propertyErrors, "Email"))
{
    // Combine all errors into a single message for logging
    string combined = ValidationFilterExtensions.CombineErrors(
        propertyErrors["Email"],
        " | ");

    Console.WriteLine($"Email validation failed: {combined}");
}

// "Name" has no errors – HasPropertyErrors returns false
bool nameHasErrors = ValidationFilterExtensions.HasPropertyErrors(propertyErrors, "Name");
Console.WriteLine($"Name has errors: {nameHasErrors}"); // False
```

## Notes

- All methods throw `ArgumentNullException` when required parameters are `null`. Always pass non‑null collections and strings to avoid runtime exceptions.  
- `CombineErrors` gracefully handles `null` or empty input by returning an empty string. This allows safe usage even when no errors have been collected.  
- `HasPropertyErrors` does not throw if the property name is missing from the dictionary; it simply returns `false`. Only a `null` or empty property name causes an exception.  
- The static methods are thread‑safe in themselves because they do not maintain any shared internal state. However, when multiple threads concurrently modify the same `List<string>` or `Dictionary` passed as an argument, external synchronization (e.g., a lock) is required to prevent data corruption.  
- `ValidateValue` is a pure function: it does not modify the input value or any external state. The returned list is a new instance and can be safely stored or discarded.
