# WorkflowCoreTests

`WorkflowCoreTests` is the primary test fixture for the `dotnet-workflow-engine` project. It contains unit and integration tests that verify the correctness of core workflow infrastructure, including workflow instance lifecycle transitions, activity execution recording, activity result handling, workflow definition validation, event bus dispatch, and cleanup behaviour when activity execution faults. The suite targets the engine’s internal services and models directly, ensuring that state mutations, error paths, and cross-component interactions behave as specified.

## API

### `public void WorkflowInstance_Start_TransitionsToActiveStatus`
Validates that invoking the start mechanism on a `WorkflowInstance` correctly transitions its status from an initial non-active state (e.g., `Idle` or `Suspended`) to `Active`. The test arranges a workflow instance in a known pre-start state, calls the start method, and asserts that the status property equals the `Active` enumeration value. No parameters or return value; throws an assertion exception if the status does not match the expected active state.

### `public void WorkflowInstance_RecordActivityExecution_DoesNotDuplicateEntries`
Ensures that calling `RecordActivityExecution` multiple times with the same logical activity identifier does not produce duplicate execution records in the workflow instance’s execution history. The test primes an instance, records the same activity execution twice, and asserts that the collection of recorded activities contains exactly one entry for that identifier. Throws an assertion failure if duplicates are present or if the count is otherwise incorrect.

### `public void ActivityResult_SetSuccess_MarksCompletedAndExposesOutput`
Verifies that setting an `ActivityResult` to a success state marks the result as completed and makes the output value accessible. The test constructs an `ActivityResult`, invokes the success-setter (or factory method) with a known output payload, then asserts both that a completion flag is `true` and that the output property equals the supplied payload. Throws an assertion exception if the completion marker is not set or the output does not round-trip correctly.

### `public void WorkflowValidator_ValidateWorkflow_MissingId_ReportsError`
Confirms that the `WorkflowValidator`’s `ValidateWorkflow` method returns a validation result containing at least one error when the workflow definition under validation lacks a required identifier. The test supplies a workflow definition with a null or empty ID, calls `ValidateWorkflow`, and asserts that the error collection is non-empty and includes a message referencing the missing ID. Throws an assertion failure if validation passes or the error message is absent.

### `public async Task EventBus_Publish_InvokesSubscribedHandler`
An asynchronous integration test proving that publishing an event through the `EventBus` causes the registered subscriber handler to be invoked with the correct event data. The test subscribes a handler that captures the received event, publishes a known event object, and awaits a short delay or completion signal. It then asserts that the captured event is non-null and matches the published payload. Throws assertion failures if the handler is never called or receives an incorrect event.

### `public async Task WorkflowExecutionService_ActivityThrowsException_ActiveActivitiesCleanedUp`
Verifies that when an activity executed by `WorkflowExecutionService` throws an unhandled exception, the service cleans up the active-activity tracking set so that the faulted activity is no longer listed as active. The test arranges a workflow step that throws, executes it through the service, catches the expected exception, and asserts that the collection of active activities for the workflow instance is empty or does not contain the faulted activity’s identifier. Throws an assertion failure if the active set still holds the activity after the exception.

## Usage

### Example 1: Validating a workflow definition before registration
```csharp
[Fact]
public void RegisterWorkflow_RejectsDefinitionWithoutId()
{
    var validator = new WorkflowValidator();
    var definition = new WorkflowDefinition
    {
        Id = null,
        Steps = new List<WorkflowStep>
        {
            new WorkflowStep { ActivityType = typeof(MyActivity) }
        }
    };

    var result = validator.ValidateWorkflow(definition);

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.Message.Contains("Id"));
}
```

### Example 2: Starting an instance and asserting status and execution recording
```csharp
[Fact]
public void ExecuteSimpleWorkflow_RecordsActivityOnce()
{
    var instance = new WorkflowInstance
    {
        DefinitionId = "simple-workflow",
        Status = WorkflowStatus.Idle
    };

    instance.Start();
    Assert.Equal(WorkflowStatus.Active, instance.Status);

    var activityId = "step-1";
    instance.RecordActivityExecution(activityId);
    instance.RecordActivityExecution(activityId);

    var executions = instance.GetActivityExecutions(activityId);
    Assert.Single(executions);
}
```

## Notes

- **Edge cases in status transitions:** `WorkflowInstance_Start_TransitionsToActiveStatus` assumes the instance is in a startable state. Starting an instance that is already `Active`, `Completed`, or `Faulted` is not covered by this test; production code should guard against invalid transitions.
- **Duplicate detection granularity:** `WorkflowInstance_RecordActivityExecution_DoesNotDuplicateEntries` relies on activity identifier equality. If identifiers are case-sensitive or include composite keys, the deduplication logic must match the same equality semantics used by the production `RecordActivityExecution` implementation.
- **ActivityResult immutability:** `ActivityResult_SetSuccess_MarksCompletedAndExposesOutput` tests a success path. Consumers should not assume the output value is deeply cloned; if the payload is a mutable reference type, modifications after setting success may affect the stored output.
- **Validation error accumulation:** `WorkflowValidator_ValidateWorkflow_MissingId_ReportsError` checks for at least one error. A validator that accumulates multiple errors may report additional problems alongside the missing ID; tests downstream should not depend on the exact error count being one.
- **Event bus asynchrony:** `EventBus_Publish_InvokesSubscribedHandler` is an `async Task` and likely involves in-memory dispatch with no durable persistence. Tests that rely on real timers or thread-pool scheduling may be flaky under high load; prefer deterministic completion signals where possible.
- **Thread safety of active-activity cleanup:** `WorkflowExecutionService_ActivityThrowsException_ActiveActivitiesCleanedUp` verifies cleanup after a synchronous exception. If the production service executes activities on multiple threads, the active-activities collection must be thread-safe (e.g., using `ConcurrentDictionary` or locking). This test alone does not prove safety under concurrent fault conditions.
