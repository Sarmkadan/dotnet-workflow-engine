# WorkflowDefinitionServiceTests

Unit tests for the `WorkflowDefinitionService` class, verifying workflow creation, activity management, transitions, and start/end activity configuration. The test suite ensures correct behavior under valid and invalid inputs, including validation, duplicate handling, and state consistency.

## API

### `CreateWorkflow_WithValidData_CreatesWorkflow`
Tests successful workflow creation with valid data. Ensures the workflow is persisted and returned with the expected identifier. No exceptions are thrown under valid conditions.

### `CreateWorkflow_WithNullOrEmptyId_ThrowsValidationException`
Verifies that attempting to create a workflow with a null or empty identifier results in a `ValidationException`. Validates input sanitization and error handling.

### `CreateWorkflow_WithDuplicateId_ThrowsWorkflowException`
Ensures that creating a workflow with an identifier matching an existing workflow throws a `WorkflowException`. Tests uniqueness enforcement and conflict resolution.

### `GetWorkflow_WithExistingId_ReturnsWorkflow`
Confirms that retrieving a workflow by its existing identifier returns the correct workflow instance. Validates retrieval logic and data integrity.

### `GetWorkflow_WithNonExistentId_ReturnsNull`
Checks that querying for a non-existent workflow identifier returns `null` instead of throwing an exception. Ensures graceful handling of missing resources.

### `GetAllWorkflows_ReturnsAllCreatedWorkflows`
Validates that retrieving all workflows returns a complete list of created workflows. Ensures no workflows are omitted and the collection is accurate.

### `GetAllWorkflows_WhenEmpty_ReturnsEmptyList`
Confirms that when no workflows exist, the method returns an empty list rather than `null`. Tests handling of empty state.

### `AddActivity_ToExistingWorkflow_AddsActivitySuccessfully`
Ensures that adding an activity to an existing workflow succeeds and persists the activity. Validates activity integration and state updates.

### `AddActivity_ToNonExistentWorkflow_ThrowsWorkflowException`
Verifies that adding an activity to a non-existent workflow throws a `WorkflowException`. Tests error handling for invalid workflow references.

### `AddActivity_WithDuplicateId_ThrowsWorkflowException`
Checks that attempting to add an activity with an identifier matching an existing activity throws a `WorkflowException`. Ensures uniqueness within a workflow.

### `AddActivity_WithInvalidActivity_ThrowsValidationException`
Validates that adding an activity with invalid data (e.g., null or malformed properties) throws a `ValidationException`. Ensures input validation is enforced.

### `AddActivity_UpdatesWorkflowModifiedAt`
Confirms that adding an activity updates the workflow's `ModifiedAt` timestamp. Tests temporal state consistency and audit trail requirements.

### `AddTransition_BetweenExistingActivities_AddsTransitionSuccessfully`
Ensures that adding a transition between two existing activities succeeds and persists the transition. Validates transition integration and state updates.

### `AddTransition_WithNonExistentFromActivity_ThrowsWorkflowException`
Verifies that adding a transition with a non-existent source activity throws a `WorkflowException`. Tests referential integrity enforcement.

### `AddTransition_WithNonExistentToActivity_ThrowsWorkflowException`
Checks that adding a transition with a non-existent target activity throws a `WorkflowException`. Ensures referential integrity is maintained.

### `AddTransition_WithDuplicateId_ThrowsWorkflowException`
Validates that attempting to add a transition with an identifier matching an existing transition throws a `WorkflowException`. Ensures uniqueness within a workflow.

### `SetStartActivity_WithExistingActivity_SetsStartActivity`
Confirms that setting the start activity to an existing activity within a workflow succeeds. Validates configuration of workflow entry points.

### `SetStartActivity_WithNonExistentWorkflow_ThrowsWorkflowException`
Ensures that setting the start activity on a non-existent workflow throws a `WorkflowException`. Tests error handling for invalid workflow references.

### `SetStartActivity_WithNonExistentActivity_ThrowsWorkflowException`
Verifies that setting the start activity to a non-existent activity throws a `WorkflowException`. Ensures referential integrity in workflow configuration.

### `SetEndActivity_WithExistingActivity_SetsEndActivity`
Confirms that setting the end activity to an existing activity within a workflow succeeds. Validates configuration of workflow exit points.

## Usage
