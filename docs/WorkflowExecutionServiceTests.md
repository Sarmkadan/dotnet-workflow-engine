# WorkflowExecutionServiceTests

Unit test class for `WorkflowExecutionService` that validates the core workflow lifecycle operations: instance creation, retrieval, and start‑up under various preconditions.

## API

### `public WorkflowExecutionServiceTests()`
Default constructor. No parameters. Creates a new test instance; any required mocks or test doubles are initialized internally.

### `public void CreateInstance_ShouldCreateInstance_WhenWorkflowIsActive`
**Purpose:** Confirms that calling `WorkflowExecutionService.CreateInstance` with an active workflow definition returns a newly created workflow instance.  
**Parameters:** None.  
**Return:** `void`.  
**Throws:** The test fails (throws an assertion exception) if the method does not return an instance or if an unexpected exception is thrown.

### `public void CreateInstance_ShouldThrowWorkflowException_WhenWorkflowNotFound`
**Purpose:** Verifies that `CreateInstance` throws a `WorkflowException` when the requested workflow definition cannot be found in the service’s catalog.  
**Parameters:** None.  
**Return:** `void`.  
**Throws:** The test expects a `WorkflowException`; any other outcome results in a test failure.

### `public void CreateInstance_ShouldThrowStateException_WhenWorkflowIsNotActive`
**Purpose:** Ensures that `CreateInstance` throws a `StateException` when the workflow definition exists but is marked as inactive.  
**Parameters:** None.  
**Return:** `void`.  
**Throws:** The test expects a `StateException`; deviation causes a test failure.

### `public async Task StartAsync_ShouldThrowWorkflowException_WhenInstanceNotFound`
**Purpose:** Checks that invoking `WorkflowExecutionService.StartAsync` with a non‑existent instance identifier throws a `WorkflowException`.  
**Parameters:** None.  
**Return:** `Task` representing the asynchronous operation.  
**Throws:** The test anticipates a `WorkflowException`; failure to throw or throwing a different exception marks the test as failed.

### `public void GetInstance_ShouldReturnInstance_WhenExists`
**Purpose:** Asserts that `GetInstance` returns the correct workflow instance when the instance identifier corresponds to an existing instance.  
**Parameters:** None.  
**Return:** `void`.  
**Throws:** The test fails if the returned instance is null, does not match the expected instance, or if an unexpected exception is thrown.

## Usage

```csharp
// Example 1: Executing a single test method directly (useful for debugging)
var tests = new WorkflowExecutionServiceTests();
tests.CreateInstance_ShouldCreateInstance_WhenWorkflowIsActive(); // should complete without exception
```

```csharp
// Example 2: Running the whole test class via a test runner (e.g., xUnit, NUnit, MSTest)
// In a test project, simply reference the assembly containing WorkflowExecutionServiceTests
// and execute the test suite:
//
// dotnet test
//
// The test runner will discover each public method decorated with the appropriate test
// attribute and execute them in isolation.
```

## Notes

- Each test method assumes a clean state of the `WorkflowExecutionService`; implementations should reset any internal caches or repositories in the test setup to avoid cross‑test contamination.  
- The test class itself is **not thread‑safe**; concurrent invocation of its methods from multiple threads may lead to unpredictable results because they share the same service instance.  
- Edge cases such as passing `null` identifiers, providing malformed workflow definitions, or attempting to start an instance that is already running are covered by other test classes; this class focuses solely on the scenarios listed above.  
- Asynchronous tests (`StartAsync_ShouldThrowWorkflowException_WhenInstanceNotFound`) must be awaited; the test runner handles this automatically when the method is marked as async.  
- No static state is introduced by the test class; however, the system under test may contain static members, so proper teardown is required to prevent leakage between test runs.
