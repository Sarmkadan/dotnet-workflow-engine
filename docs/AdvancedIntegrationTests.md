# AdvancedIntegrationTests

The `AdvancedIntegrationTests` class contains integration tests for the dotnet-workflow-engine library. These tests validate complex workflow scenarios including parallel execution paths, error recovery, state preservation across long-running activities, instance isolation, conditional routing, lifecycle state transitions, activity timeouts, workflow construction, and serialization round-trips. Each test exercises the engine under realistic conditions to ensure correctness and robustness.

## API

### `ComplexWorkflow_WithParallelPaths_ExecutesSuccessfully`

- **Description**: Verifies that a workflow containing multiple parallel branches completes successfully and that all branches execute as expected.
- **Parameters**: None.
- **Return value**: `Task` – completes when the test finishes.
- **Throws**: An exception if any parallel path fails to execute or if the final workflow state does not match the expected outcome.

### `WorkflowWithErrorHandling_RecoverableError_CompletesSuccessfully`

- **Description**: Tests that a workflow with a recoverable error (e.g., a transient fault) can be handled by the error-handling mechanism and eventually completes successfully.
- **Parameters**: None.
- **Return value**: `Task` – completes when the test finishes.
- **Throws**: An exception if the error is not recovered or if the workflow terminates in an unexpected state.

### `LongRunningWorkflow_PreservesStateAcrossActivities`

- **Description**: Ensures that state data is correctly persisted and restored across multiple activities in a long-running workflow, even when the workflow is suspended and resumed.
- **Parameters**: None.
- **Return value**: `Task` – completes when the test finishes.
- **Throws**: An exception if state is lost, corrupted, or not correctly propagated between activities.

### `WorkflowWithMultipleInstances_EachMaintainsOwnState`

- **Description**: Validates that separate instances of the same workflow definition maintain independent state and do not interfere with each other.
- **Parameters**: None.
- **Return value**: `Task` – completes when the test finishes.
- **Throws**: An exception if state leakage or cross-instance contamination is detected.

### `WorkflowWithConditionalRouting_SelectsCorrectPathBasedOnContext`

- **Description**: Confirms that conditional branches in a workflow are selected according to the context data provided at runtime, and that the correct sequence of activities executes.
- **Parameters**: None.
- **Return value**: `Task` – completes when the test finishes.
- **Throws**: An exception if the wrong path is taken or if the workflow outcome does not match the expected result for the given context.

### `WorkflowLifecycle_FullCycle_StateTransitionsCorrectly`

- **Description**: Tests the complete lifecycle of a workflow instance from creation through execution, suspension, resumption, and completion, verifying that all state transitions occur in the correct order.
- **Parameters**: None.
- **Return value**: `void` – the test runs synchronously.
- **Throws**: An exception if any state transition is invalid or if the final state is not the expected terminal state.

### `ActivityWithTimeout_CompletesWithinTimeLimit`

- **Description**: Verifies that an activity configured with a timeout either completes before the timeout expires or is cancelled gracefully, and that the workflow handles the timeout correctly.
- **Parameters**: None.
- **Return value**: `Task` – completes when the test finishes.
- **Throws**: An exception if the activity does not respect the timeout or if the workflow fails to handle the timeout event.

### `WorkflowBuilder_CreateSerialWorkflow_BuildsValidWorkflow`

- **Description**: Validates that the workflow builder correctly constructs a serial (sequential) workflow definition with the expected activity order and structure.
- **Parameters**: None.
- **Return value**: `void` – the test runs synchronously.
- **Throws**: An exception if the built workflow is malformed, missing activities, or has an incorrect topology.

### `WorkflowSerialization_RoundTrip_PreservesStructure`

- **Description**: Ensures that a workflow definition can be serialized to a persistent format (e.g., JSON or XML) and deserialized back without losing any structural or configuration information.
- **Parameters**: None.
- **Return value**: `void` – the test runs synchronously.
- **Throws**: An exception if the round-trip results in a different workflow definition or if serialization/deserialization fails.

## Usage

The following examples demonstrate how to execute these integration tests within a test suite. The tests are designed to be run with a test framework such as xUnit or NUnit.

**Example 1: Running a parallel workflow test**

```csharp
using System.Threading.Tasks;
using Xunit;

public class WorkflowIntegrationTests
{
    [Fact]
    public async Task ParallelWorkflow_ShouldCompleteSuccessfully()
    {
        var test = new AdvancedIntegrationTests();
        await test.ComplexWorkflow_WithParallelPaths_ExecutesSuccessfully();
    }
}
```

**Example 2: Verifying workflow serialization round-trip**

```csharp
using Xunit;

public class WorkflowSerializationTests
{
    [Fact]
    public void Serialization_ShouldPreserveWorkflowStructure()
    {
        var test = new AdvancedIntegrationTests();
        test.WorkflowSerialization_RoundTrip_PreservesStructure();
    }
}
```

## Notes

- **Edge cases**:  
  - Parallel path tests may expose race conditions in the workflow engine’s concurrency handling.  
  - Error-handling tests rely on the engine correctly distinguishing recoverable from non-recoverable faults.  
  - Long-running workflow tests assume the engine supports persistence and resumption; failures may indicate state serialization issues.  
  - Multiple-instance tests require strict isolation; any shared static state in activities will cause false positives.  
  - Conditional routing tests depend on the context object being correctly passed and evaluated at each branch point.  
  - Lifecycle tests verify that state transitions (e.g., `Created` → `Running` → `Suspended` → `Running` → `Completed`) are enforced.  
  - Timeout tests assume the engine’s timer mechanism is accurate and that cancellation propagates correctly.  
  - Builder and serialization tests validate the workflow definition model, not runtime execution.

- **Thread safety**:  
  The `AdvancedIntegrationTests` class is not thread-safe. Each test method should be invoked sequentially on a single instance, or separate instances should be used for concurrent execution. The tests themselves may create and manipulate workflow engine resources that are not designed for parallel access; running multiple tests concurrently on the same engine instance can lead to unpredictable results. It is recommended to use a test runner that isolates each test method (e.g., xUnit’s default collection-per-class behavior).
