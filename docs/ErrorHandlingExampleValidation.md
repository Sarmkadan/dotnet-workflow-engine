# ErrorHandlingExampleValidation

`ErrorHandlingExampleValidation` is a static helper class that provides validation logic for the *Error Handling Example* workflow.  
It validates a `ProcessingRequest` object and its constituent rules, ensuring that all required fields are present, that rule types are supported, and that numeric rule values are positive.  The class exposes a set of strongly‑typed validation methods that return detailed error messages, a convenience `IsValid` predicate, and an `EnsureValid` method that throws a `ValidationException` when the request is not valid.

## API

### `public static IReadOnlyList<string> Validate(ProcessingRequest request)`

Validates the entire `ProcessingRequest`.  
- **Parameters**:  
  - `request`: The request to validate.  
- **Returns**: A read‑only list of error messages. An empty list indicates a valid request.  
- **Throws**:  
  - `ArgumentNullException` if `request` is `null`.  

### `public static bool IsValid(ProcessingRequest request)`

Convenience predicate that returns `true` when `Validate(request)` yields no errors.  
- **Parameters**:  
  - `request`: The request to validate.  
- **Returns**: `true` if the request is valid; otherwise `false`.  
- **Throws**:  
  - `ArgumentNullException` if `request` is `null`.  

### `public static void EnsureValid(ProcessingRequest request)`

Validates the request and throws a `ValidationException` if any errors are found.  
- **Parameters**:  
  - `request`: The request to validate.  
- **Throws**:  
  - `ValidationException` containing the list of validation errors.  
  - `ArgumentNullException` if `request` is `null`.  

### `public static IReadOnlyList<string> ValidateProcessingRules(IEnumerable<ProcessingRule> rules)`

Validates a collection of `ProcessingRule` objects.  
- **Parameters**:  
  - `rules`: The rules to validate.  
- **Returns**: A read‑only list of error messages.  
- **Throws**:  
  - `ArgumentNullException` if `rules` is `null`.  

### `public static IReadOnlyList<string> ValidateRuleType(string ruleType)`

Validates that the supplied rule type is supported by the workflow.  
- **Parameters**:  
  - `ruleType`: The rule type string to validate.  
- **Returns**: A read‑only list of error messages.  
- **Throws**:  
  - `ArgumentNullException` if `ruleType` is `null`.  

### `public static IReadOnlyList<string> ValidatePositiveIntegerRule(int value)`

Validates that a numeric rule value is a positive integer.  
- **Parameters**:  
  - `value`: The integer value to validate.  
- **Returns**: A read‑only list of error messages.  
- **Throws**:  
  - None.  

## Usage

