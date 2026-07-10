# MessageEventServiceTests

`MessageEventServiceTests` is the test suite for the `MessageEventService` component in the `dotnet-workflow-engine` project. It validates the behavior of publishing messages to waiting workflow instances, covering success paths, error handling, edge cases around instance state, and payload preservation. The tests ensure that the service correctly resumes suspended workflows when a matching message arrives and fails gracefully when preconditions are not met.

## API

### public async Task PublishMessageAsync_WithValidMessage_PublishesEvent
Verifies that calling `PublishMessageAsync` with a properly constructed message object successfully publishes an event without throwing. The test confirms the happy path where all inputs are valid and the service completes its work normally.

### public async Task PublishMessageAsync_WithNullMessage_ThrowsArgumentNullException
Ensures that passing a `null` message argument to `PublishMessageAsync` immediately throws an `ArgumentNullException`. This guards against null dereference deeper in the call chain and enforces a fail-fast contract.

### public async Task PublishMessageAsync_WithNoWaitingInstance_LogsWarningAndReturnsFalse
When no workflow instance is waiting for the published message name, the method logs a warning diagnostic and returns `false`. The test asserts that the return value correctly indicates no instance was resumed and that a warning-level log entry is produced.

### public async Task PublishMessageAsync_WithWaitingInstance_ResumesWorkflow
Confirms that when exactly one workflow instance is suspended and waiting for a message with a matching name, `PublishMessageAsync` resumes that instance. The test validates that the instance transitions out of the waiting state and continues execution.

### public async Task PublishMessageAsync_WithMultipleWaitingInstances_ResumesFirstMatch
When multiple instances are waiting for the same message name, the service resumes only the first matching instance (typically the one that entered the waiting state earliest). The test ensures deterministic selection and that exactly one instance is resumed.

### public async Task PublishMessageAsync_WhenResumeThrows_FailsInstanceAndRethrows
If the resume operation itself throws an exception, the service marks the affected instance as failed and rethrows the original exception. This test verifies both the failure transition on the instance and the exception propagation behavior.

### public async Task PublishMessageAsync_PreservesPayloadInEvent
Validates that the payload attached to the published message is preserved intact and delivered to the resumed workflow instance. The test compares the payload before and after the event reaches the instance to confirm no corruption or truncation occurs.

### public async Task PublishMessageAsync_WithMatchingInstanceButWrongMessageName_DoesNotResume
An instance waiting for message name "A" must not be resumed when a message with name "B" is published, even if other criteria match. The test ensures that message name matching is strict and no false-positive resumptions occur.

### public async Task PublishMessageAsync_WithInstanceNotInWaitingState_IgnoresInstance
Workflow instances that exist but are not in a waiting/suspended state are ignored by `PublishMessageAsync`. The test confirms that only instances explicitly awaiting a message are candidates for resumption.

### public async Task PublishMessageAsync_MultipleMessages_CanBePublishedSequentially
Demonstrates that the service supports publishing multiple distinct messages in sequence without state corruption. Each message independently finds its matching waiting instance, and sequential calls do not interfere with one another.

## Usage

### Example 1: Publishing a message to resume a waiting workflow
```csharp
var messageEventService = new MessageEventService(instanceStore, logger);
var message = new WorkflowMessage
{
    Name = "PaymentReceived",
    Payload = new { Amount = 150.00m, Currency = "USD" }
};

bool resumed = await messageEventService.PublishMessageAsync(message);

if (resumed)
{
    Console.WriteLine("Workflow instance resumed successfully.");
}
else
{
    Console.WriteLine("No instance was waiting for this message.");
}
```

### Example 2: Handling a null message with argument validation
```csharp
var messageEventService = new MessageEventService(instanceStore, logger);

try
{
    await messageEventService.PublishMessageAsync(null);
}
catch (ArgumentNullException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
    // Handle the null case, e.g., log and return an error response
}
```

## Notes

- **Message name matching is case-sensitive and exact.** Even a minor mismatch causes the service to skip an otherwise eligible instance.
- **Only instances in a waiting state are considered.** Instances that are actively executing, completed, or failed are never resumed by `PublishMessageAsync`, regardless of message name.
- **First-match semantics apply when multiple instances wait for the same message name.** The ordering is typically based on the time each instance entered the waiting state. Callers should not rely on any other prioritization.
- **Payload immutability is assumed but not enforced by the service.** The tests verify that the payload object reference and its contents survive the publish-resume round trip intact. If the caller mutates the payload after publishing, behavior is undefined.
- **Exception handling during resume is dual-purpose:** the instance is transitioned to a failed state for auditability, and the exception is rethrown so the caller can react (e.g., retry or escalate).
- **Thread safety is not explicitly tested in this suite.** The sequential multiple-message test suggests the service is reentrant, but concurrent publishes from multiple threads are not covered here and should be validated separately if required by the deployment scenario.
