# WorkflowBuilderTests

Unit tests for the `WorkflowBuilder` class, verifying correct construction and registration of workflows, activities, transitions, and event handlers. Focuses on fluent API usage, validation, and integration with the workflow service.

## API

### `BuildBasicWorkflow_WithActivitiesAndTransitions_BuildsSuccessfully`
Ensures that a workflow containing activities and transitions is constructed without validation errors. No parameters or return value; throws only if validation fails.

### `Build_WithInvalidWorkflow_ThrowsValidationException`
Verifies that attempting to build a workflow with invalid configuration (e.g., missing transitions or invalid activity references) results in a `ValidationException`. No parameters or return value.

### `BuildAndRegister_RegistersWorkflowWithService`
Tests that a successfully built workflow is registered with the workflow service. No parameters or return value; throws if registration fails.

### `BuildAndRegister_WithTransitions_RegistersTransitionsWithService`
Ensures that all transitions defined in a workflow are registered with the service during `BuildAndRegister`. No parameters or return value; throws if registration fails.

### `AddEventActivity_CreatesEventActivity`
Validates that calling `AddEventActivity` on the builder creates and attaches an event-based activity. No parameters or return value.

### `AddConditionalTransition_CreatesConditionalTransition`
Confirms that `AddConditionalTransition` adds a transition with conditional logic to the workflow. No parameters or return value.

### `Fluent_AllMethodsReturnBuilder_ForChaining`
Asserts that every public method in the builder returns the builder instance, enabling fluent method chaining. No parameters or return value.

### `AddCustomActivity_AllowsArbitraryActivities`
Ensures that custom activity types can be added via the builder without restriction. No parameters or return value.

### `CreateSerial_WithActivityNames_CreatesSequentialWorkflow`
Tests construction of a sequential workflow using named activities. No parameters or return value.

### `CreateSerial_WithNoActivities_CreatesEmptyWorkflow`
Verifies that a workflow with no activities is still considered valid and constructible. No parameters or return value.

### `CreateSerial_WithOneActivity_CreatesValidWorkflow`
Ensures that a workflow containing exactly one activity is valid and buildable. No parameters or return value.

### `AddTaskActivity_WithHandler_SetsHandlerType`
Validates that adding a task activity with a specified handler assigns the correct handler type. No parameters or return value.

### `AddTaskActivity_WithoutHandler_HasNullHandler`
Confirms that a task activity added without a handler has a `null` handler reference. No parameters or return value.

### `BuildAndRegister_WithDuplicateWorkflowId_ThrowsWorkflowException`
Ensures that attempting to register a workflow with an ID already in use throws a `WorkflowException`. No parameters or return value.

### `MultipleTransitions_FromSameActivity_AllCreated`
Tests that multiple transitions originating from the same activity are all created and registered correctly. No parameters or return value.

### `ComplexWorkflow_WithManyActivities_BuildsSuccessfully`
Validates that a workflow with many activities and complex transition logic builds without error. No parameters or return value.

## Usage
