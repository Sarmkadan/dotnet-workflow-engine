# ConditionalBranchingServiceTestsExtensions

Utility class providing extension methods and factory methods for testing conditional branching behavior in workflow engines. It simplifies the creation of test contexts and assertions around transition selection, skipping, error handling, and condition evaluation within workflow executions.

## API

### `ShouldHaveSingleSelectedTransitionAsync`
Verifies that exactly one transition was selected during branching.
- **Parameters**:
  - `BranchingResult result`: The branching result to validate.
- **Return value**: `Task<BranchingResult>` (the same result for fluent chaining).
- **Throws**: `XunitException` if zero or more than one transition was selected.

### `ShouldHaveSingleSkippedTransitionAsync`
Verifies that exactly one transition was skipped during branching.
- **Parameters**:
  - `BranchingResult result`: The branching result to validate.
- **Return value**: `Task<BranchingResult>` (the same result for fluent chaining).
- **Throws**: `XunitException` if zero or more than one transition was skipped.

### `ShouldHaveSelectedTransitionsCountAsync`
Validates the exact number of selected transitions.
- **Parameters**:
  - `BranchingResult result`: The branching result to validate.
  - `int expectedCount`: The expected number of selected transitions.
- **Return value**: `Task<BranchingResult>` (the same result for fluent chaining).
- **Throws**: `XunitException` if the actual count does not match `expectedCount`.

### `ShouldHaveSkippedTransitionsCountAsync`
Validates the exact number of skipped transitions.
- **Parameters**:
  - `BranchingResult result`: The branching result to validate.
  - `int expectedCount`: The expected number of skipped transitions.
- **Return value**: `Task<BranchingResult>` (the same result for fluent chaining).
- **Throws**: `XunitException` if the actual count does not match `expectedCount`.

### `ShouldHaveNoErrorsAsync`
Ensures that no errors occurred during branching.
- **Parameters**:
  - `BranchingResult result`: The branching result to validate.
- **Return value**: `Task<BranchingResult>` (the same result for fluent chaining).
- **Throws**: `XunitException` if any errors are present.

### `ShouldHaveErrorsCountAsync`
Validates the exact number of errors that occurred.
- **Parameters**:
  - `BranchingResult result`: The branching result to validate.
  - `int expectedCount`: The expected number of errors.
- **Return value**: `Task<BranchingResult>` (the same result for fluent chaining).
- **Throws**: `XunitException` if the actual error count does not match `expectedCount`.

### `ShouldHaveUsedDefaultTransitionAsync`
Checks whether the default transition was selected.
- **Parameters**:
  - `BranchingResult result`: The branching result to validate.
- **Return value**: `Task<BranchingResult>` (the same result for fluent chaining).
- **Throws**: `XunitException` if the default transition was not selected.

### `ShouldNotHaveUsedDefaultTransitionAsync`
Checks whether the default transition was not selected.
- **Parameters**:
  - `BranchingResult result`: The branching result to validate.
- **Return value**: `Task<BranchingResult>` (the same result for fluent chaining).
- **Throws**: `XunitException` if the default transition was selected.

### `ShouldHaveAnyConditionMatchedAsync`
Verifies that at least one transition condition evaluated to true.
- **Parameters**:
  - `BranchingResult result`: The branching result to validate.
- **Return value**: `Task<BranchingResult>` (the same result for fluent chaining).
- **Throws**: `XunitException` if no conditions matched.

### `ShouldNotHaveAnyConditionMatchedAsync`
Verifies that no transition conditions evaluated to true.
- **Parameters**:
  - `BranchingResult result`: The branching result to validate.
- **Return value**: `Task<BranchingResult>` (the same result for fluent chaining).
- **Throws**: `XunitException` if any condition matched.

### `ShouldHaveSelectedTransitionsInOrderAsync`
Ensures that selected transitions appear in the expected execution order.
- **Parameters**:
  - `BranchingResult result`: The branching result to validate.
  - `IEnumerable<string> expectedTransitionIds`: The expected order of transition IDs.
- **Return value**: `Task<BranchingResult>` (the same result for fluent chaining).
- **Throws**: `XunitException` if the order does not match or if transitions are missing.

### `ShouldHaveSkippedTransitionsAsync`
Validates the set of skipped transitions.
- **Parameters**:
  - `BranchingResult result`: The branching result to validate.
  - `IEnumerable<string> expectedTransitionIds`: The expected IDs of skipped transitions.
- **Return value**: `Task<BranchingResult>` (the same result for fluent chaining).
- **Throws**: `XunitException` if the skipped transitions do not match the expected set.

### `CreateWorkflow`
Factory method to create a new `Workflow` instance.
- **Parameters**: None.
- **Return value**: `Workflow` (a new workflow instance).
- **Throws**: None.

### `CreateContext`
Factory method to create a new `WorkflowExecutionContext`.
- **Parameters**: None.
- **Return value**: `WorkflowExecutionContext` (a new execution context).
- **Throws**: None.

### `CreateService`
Factory method to create a new `ConditionalBranchingService`.
- **Parameters**:
  - `Workflow workflow`: The workflow to associate with the service.
  - `WorkflowExecutionContext context`: The execution context for the service.
- **Return value**: `ConditionalBranchingService` (a new service instance).
- **Throws**: `ArgumentNullException` if `workflow` or `context` is null.

### `CreateWorkflowWithTransitions`
Factory method to create a workflow with predefined transitions.
- **Parameters**:
  - `IEnumerable<Transition> transitions`: The transitions to add to the workflow.
- **Return value**: `Workflow` (a new workflow with the specified transitions).
- **Throws**: `ArgumentNullException` if `transitions` is null.

### `ShouldHaveValidTransitionExpressions`
Validates that all transition expressions in a workflow are syntactically valid.
- **Parameters**:
  - `Workflow workflow`: The workflow to validate.
- **Return value**: None.
- **Throws**: `XunitException` if any transition expression is invalid.

### `ShouldHaveInvalidTransitionExpressions`
Validates that at least one transition expression in a workflow is syntactically invalid.
- **Parameters**:
  - `Workflow workflow`: The workflow to validate.
- **Return value**: None.
- **Throws**: `XunitException` if all transition expressions are valid.

## Usage

```csharp
// Example 1: Validating branching behavior with a single selected transition
var workflow = ConditionalBranchingServiceTestsExtensions.CreateWorkflow();
var context = ConditionalBranchingServiceTestsExtensions.CreateContext();
var service = ConditionalBranchingServiceTestsExtensions.CreateService(workflow, context);

var result = await service.ExecuteBranchingAsync();

await result.ShouldHaveSingleSelectedTransitionAsync();
await result.ShouldHaveNoErrorsAsync();
```

```csharp
// Example 2: Testing transition expression validation
var transitions = new[]
{
    new Transition { Id = "t1", Condition = "input.Value > 10" },
    new Transition { Id = "t2", Condition = "input.Value <= 10" }
};
var workflow = ConditionalBranchingServiceTestsExtensions.CreateWorkflowWithTransitions(transitions);

ConditionalBranchingServiceTestsExtensions.ShouldHaveValidTransitionExpressions(workflow);
```

## Notes

- All assertion methods are designed for use with xUnit and throw `XunitException` on failure, making them compatible with test frameworks that support exception-based assertions.
- Factory methods are stateless and thread-safe; they do not maintain shared state across invocations.
- The `CreateService` method requires non-null `Workflow` and `WorkflowExecutionContext` parameters; passing null will result in an `ArgumentNullException`.
- Assertion methods operate on `BranchingResult` and do not modify the result object; they are safe for use in fluent assertion chains.
- Edge cases such as empty transition sets or null conditions are handled by the underlying workflow engine and reflected in the `BranchingResult` properties; these methods validate only the logical outcomes.
