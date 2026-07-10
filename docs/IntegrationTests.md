# IntegrationTests

The `IntegrationTests` class contains end-to-end and behavioral tests for the workflow engine, validating workflow execution, state management, activity handling, and error conditions across realistic scenarios. These tests exercise the full stack from instance creation to activity execution, including concurrency, auditing, and validation.

## API

### `public async Task EndToEnd_SimpleWorkflow_ExecutesSuccessfully()`
Validates that a workflow with a single linear path executes from start to completion without errors. Ensures all activities are invoked in order and the final state reflects successful completion.

### `public async Task EndToEnd_ConditionalRouting_SelectsCorrectPath()`
Tests workflows with branching logic where conditions determine the execution path. Verifies that the engine selects the correct branch based on runtime variable values and that unselected branches are skipped.

### `public void CreateInstance_NewWorkflowInstance_InitializesCorrectly()`
Confirms that creating a new workflow instance populates the initial state, sets the correct status, and prepares the execution context with default values. No parameters are required.

### `public void CreateInstance_NonExistentWorkflow_ThrowsWorkflowException()`
Ensures that attempting to create an instance of a non-existent workflow definition throws a `WorkflowException`. Useful to validate input validation in public APIs.

### `public async Task ExecuteWorkflow_WithAuditTrail_LogsAllEvents()`
Checks that every significant event during workflow execution—start, activity execution, transitions, and completion—is recorded in the audit trail with accurate timestamps and payloads.

### `public async Task WorkflowExecution_MultipleInstances_MaintainsIndependentState()`
Validates that multiple concurrent or sequential workflow instances do not interfere with one another. Each instance must maintain its own state, variables, and execution history.

### `public async Task WorkflowExecution_RetryOnFailure_EventuallySucceeds()`
Tests the engine’s retry policy by simulating transient failures. Confirms that activities marked for retry are retried the configured number of times before ultimately succeeding or exhausting attempts.

### `public void Instance_StateTransitions_AreCorrect()`
Exercises all defined state transitions for a workflow instance (e.g., `Running` → `Completed`, `Faulted` → `Compensating`). Verifies that transitions occur only when valid and that side effects are applied.

### `public void ActivityExecution_RecordsExecutedActivities()`
Ensures that every executed activity—including skipped or compensated ones—is recorded with its inputs, outputs, status, and execution time in the activity log.

### `public void ActivityResult_StatusTransitions_WorkCorrectly()`
Validates that activity results correctly transition through statuses (`Executing`, `Completed`, `Faulted`, `Compensated`) and that downstream logic reacts appropriately to each state.

### `public void ActivityResult_SkippedStatus_WorksCorrectly()`
Confirms that activities skipped due to conditional routing or prior failures are marked with `Skipped` status and that downstream logic handles this state predictably.

### `public async Task ExecutionContext_VariableManagement_WorksCorrectly()`
Tests the manipulation and persistence of workflow-scoped variables across activities. Ensures variables are updated, read, and scoped correctly throughout the workflow lifecycle.

### `public void ExecutionContext_Reset_ClearsAllData()`
Verifies that resetting the execution context removes all variables, state, and history, returning the instance to a clean state suitable for reuse or disposal.

### `public async Task ConcurrentWorkflowExecution_HandlesConcurrency()`
Stress-tests the engine by launching multiple workflows simultaneously. Validates thread-safe access to shared resources and correct isolation of execution contexts.

### `public void WorkflowValidation_ValidatesCompleteWorkflow()`
Ensures that the engine rejects workflow definitions with missing start nodes, unreachable activities, or invalid transitions during validation phase.

### `public void WorkflowValidation_DetectsInvalidActivities()`
Confirms that workflows containing activities with invalid configurations (e.g., missing handlers, invalid expressions) are rejected during validation with descriptive errors.

### `public void ExpressionEvaluation_ComplexConditions_EvaluateCorrectly()`
Tests the evaluation of complex boolean expressions involving variables, literals, and logical operators. Ensures correct routing and activity selection based on runtime values.

## Usage

### Example 1: Validating a Simple Workflow
