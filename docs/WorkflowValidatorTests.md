# WorkflowValidatorTests

The `WorkflowValidatorTests` class contains unit tests for the `WorkflowValidator` component of the `dotnet-workflow-engine` project. Each test method validates a specific aspect of workflow, activity, or transition validation, ensuring that the validator correctly returns valid results, errors, or warnings under the expected conditions. The tests are designed to be run with a standard .NET test framework (e.g., xUnit, NUnit) and assert the behavior of the validator without external dependencies.

## API

All test methods are `public void` and accept no parameters. They do not return a value; instead, they use assertions to verify the outcome. If an assertion fails, the test runner throws an appropriate exception (e.g., `AssertFailedException`). The following list describes each test’s purpose and what it asserts.

### Workflow Validation Tests

- **`ValidateWorkflow_ValidWorkflow_ReturnsValid`**  
  Asserts that a fully valid workflow (with required `Id`, `Name`, at least one activity, a start activity, an end activity, and valid transitions) passes validation without errors or warnings.

- **`ValidateWorkflow_MissingId_ReturnsError`**  
  Asserts that a workflow without an `Id` produces at least one validation error.

- **`ValidateWorkflow_MissingName_ReturnsError`**  
  Asserts that a workflow without a `Name` produces at least one validation error.

- **`ValidateWorkflow_NoActivities_ReturnsError`**  
  Asserts that a workflow with an empty or null activity list produces at least one validation error.

- **`ValidateWorkflow_InvalidActivity_ReturnsError`**  
  Asserts that a workflow containing an activity that fails its own validation (e.g., missing `Id` or `Name`) produces at least one validation error.

- **`ValidateWorkflow_StartActivityNotFound_ReturnsError`**  
  Asserts that a workflow whose start activity reference does not match any activity in the list produces at least one validation error.

- **`ValidateWorkflow_EndActivityNotFound_ReturnsError`**  
  Asserts that a workflow whose end activity reference does not match any activity in the list produces at least one validation error.

- **`ValidateWorkflow_NoStartActivity_ReturnsWarning`**  
  Asserts that a workflow without a designated start activity produces at least one validation warning (but not necessarily an error).

- **`ValidateWorkflow_InvalidTransition_ReturnsError`**  
  Asserts that a workflow containing a transition that fails its own validation (e.g., missing `FromActivity` or `ToActivity`) produces at least one validation error.

### Activity Validation Tests

- **`ValidateActivity_ValidActivity_ReturnsValid`**  
  Asserts that a fully valid activity (with required `Id`, `Name`, valid `Timeout`, and non-negative `Retries`) passes validation without errors or warnings.

- **`ValidateActivity_MissingId_ReturnsError`**  
  Asserts that an activity without an `Id` produces at least one validation error.

- **`ValidateActivity_MissingName_ReturnsError`**  
  Asserts that an activity without a `Name` produces at least one validation error.

- **`ValidateActivity_InvalidTimeout_ReturnsError`**  
  Asserts that an activity with an invalid or out-of-range `Timeout` value produces at least one validation error.

- **`ValidateActivity_NegativeRetries_ReturnsError`**  
  Asserts that an activity with a negative `Retries` value produces at least one validation error.

- **`ValidateActivity_RetriesWithoutPolicy_ReturnsWarning`**  
  Asserts that an activity with a positive `Retries` value but no retry policy defined produces at least one validation warning.

### Transition Validation Tests

- **`ValidateTransition_ValidTransition_ReturnsValid`**  
  Asserts that a fully valid transition (with non-empty `FromActivity` and `ToActivity` references that exist in the workflow) passes validation without errors or warnings.

- **`ValidateTransition_MissingFromActivity_ReturnsError`**  
  Asserts that a transition without a `FromActivity` reference produces at least one validation error.

- **`ValidateTransition_MissingToActivity_ReturnsError`**  
  Asserts that a transition without a `ToActivity` reference produces at least one validation error.

- **`ValidateTransition_FromActivityNotFound_ReturnsError`**  
  Asserts that a transition whose `FromActivity` reference does not match any activity in the workflow produces at least one validation error.

- **`ValidateTransition_ToActivityNotFound_ReturnsError`**  
  Asserts that a transition whose `ToActivity` reference does not match any activity in the workflow produces at least one validation error.

## Usage

The following examples illustrate how the tests in `WorkflowValidatorTests` are structured and how they can be executed in a typical .NET test project.

### Example 1: Running a specific test via the command line

```bash
dotnet test --filter "FullyQualifiedName~ValidateWorkflow_ValidWorkflow_ReturnsValid"
```

This command runs only the test that validates a correct workflow, using the test runner’s filtering capability.

### Example 2: Writing a new test that follows the same pattern

```csharp
using Xunit;
using WorkflowEngine.Validation;

public class CustomWorkflowTests
{
    [Fact]
    public void ValidateWorkflow_WithDuplicateActivityIds_ReturnsError()
    {
        // Arrange
        var validator = new WorkflowValidator();
        var workflow = new Workflow
        {
            Id = "wf1",
            Name = "Test",
            Activities = new List<Activity>
            {
                new Activity { Id = "a1", Name = "Step1" },
                new Activity { Id = "a1", Name = "Step2" } // duplicate Id
            },
            StartActivityId = "a1",
            EndActivityId = "a1"
        };

        // Act
        var result = validator.Validate(workflow);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("duplicate"));
    }
}
```

This example demonstrates how to reuse the `WorkflowValidator` class in a custom test, following the same assertion style used by `WorkflowValidatorTests`.

## Notes

- **Edge Cases Covered**  
  The tests address common boundary conditions: missing required fields (`Id`, `Name`), empty collections, invalid numeric values (negative retries, invalid timeout), missing or non-existent references (start/end activities, transition endpoints), and warnings for missing retry policies. These scenarios represent the most frequent validation failures in workflow definitions.

- **Thread Safety**  
  The `WorkflowValidatorTests` class itself is stateless and does not maintain any shared mutable state. Each test method creates its own instances of the validator and workflow objects. Therefore, tests can be run in parallel by the test framework without interference, provided the underlying `WorkflowValidator` implementation is also thread‑safe (the validator is expected to be stateless and reentrant).

- **Dependencies**  
  The tests assume the existence of a `WorkflowValidator` class with methods `Validate(Workflow)`, `Validate(Activity)`, and `Validate(Transition)`, each returning a `ValidationResult` object containing `IsValid`, `Errors`, and `Warnings` collections. The exact types and namespaces should match the project’s implementation.
