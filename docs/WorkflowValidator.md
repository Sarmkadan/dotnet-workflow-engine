# WorkflowValidator

A static utility class that provides validation capabilities for workflow definitions, activities, and transitions in the `dotnet-workflow-engine` project. It aggregates validation results with warnings and errors, and generates a human-readable report of all issues encountered during validation.

## API

### `public static ValidationResult ValidateWorkflow(WorkflowDefinition workflow)`

Validates the structural integrity and semantic correctness of a complete workflow definition. Checks include activity references, transition validity, and workflow-level constraints.

- **Parameters**
  - `workflow` – The workflow definition to validate.
- **Returns**
  - A `ValidationResult` containing all accumulated errors and warnings.
- **Throws**
  - `ArgumentNullException` if `workflow` is `null`.

---

### `public static ValidationResult ValidateActivity(ActivityDefinition activity)`

Validates an individual activity definition for correctness, including its type, configuration, and referenced dependencies.

- **Parameters**
  - `activity` – The activity definition to validate.
- **Returns**
  - A `ValidationResult` containing all accumulated errors and warnings.
- **Throws**
  - `ArgumentNullException` if `activity` is `null`.

---

### `public static ValidationResult ValidateTransition(TransitionDefinition transition)`

Validates a transition definition for correctness, including source/target activity existence, condition syntax, and transition constraints.

- **Parameters**
  - `transition` – The transition definition to validate.
- **Returns**
  - A `ValidationResult` containing all accumulated errors and warnings.
- **Throws**
  - `ArgumentNullException` if `transition` is `null`.

---

### `public void AddError(string message)`

Adds a validation error to the current report. Errors are considered blocking issues that prevent workflow execution.

- **Parameters**
  - `message` – The error message to add.
- **Throws**
  - `ArgumentNullException` if `message` is `null`.

---

### `public void AddWarning(string message)`

Adds a validation warning to the current report. Warnings indicate potential issues that do not prevent execution but may lead to unexpected behavior.

- **Parameters**
  - `message` – The warning message to add.
- **Throws**
  - `ArgumentNullException` if `message` is `null`.

---
### `public string GetReport()`

Generates a formatted report string summarizing all accumulated errors and warnings. The report includes a header, error count, warning count, and a detailed list of messages.

- **Returns**
  - A string containing the full validation report.
- **Throws**
  - `InvalidOperationException` if called before any validation method or after report generation in a non-resettable context.

## Usage
