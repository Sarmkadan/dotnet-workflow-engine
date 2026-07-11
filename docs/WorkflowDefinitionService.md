# WorkflowDefinitionService

The `WorkflowDefinitionService` is a core component of the `dotnet-workflow-engine` responsible for managing the lifecycle of workflow definitions. It provides methods to create, retrieve, modify, validate, and publish workflows, as well as manage their constituent activities and transitions. This service acts as a central repository for workflow definitions, enabling their execution and persistence.

## API

### `Workflow CreateWorkflow()`
Creates a new, empty `Workflow` instance with default values. This method initializes a workflow but does not persist it until explicitly added via `AddWorkflow`.

**Returns:**
- A new `Workflow` object.

---

### `void AddWorkflow(Workflow workflow)`
Adds a workflow to the service's internal collection. If the workflow already exists (e.g., by identifier), this method may overwrite or throw an exception, depending on implementation.

**Parameters:**
- `workflow`: The `Workflow` instance to add.

**Throws:**
- `ArgumentNullException`: If `workflow` is `null`.
- `InvalidOperationException`: If the workflow conflicts with an existing one (e.g., duplicate identifier).

---

### `virtual Workflow? GetWorkflow()`
Retrieves a workflow from the service's collection. The criteria for retrieval (e.g., by identifier) are implementation-specific.

**Returns:**
- The `Workflow` instance if found; otherwise, `null`.

---

### `List<Workflow> GetAllWorkflows()`
Retrieves all workflows currently managed by the service.

**Returns:**
- A `List<Workflow>` containing all workflows. Returns an empty list if no workflows exist.

---

### `void AddActivity(Activity activity)`
Adds an activity to the currently active workflow. The workflow must be created or retrieved prior to calling this method.

**Parameters:**
- `activity`: The `Activity` instance to add.

**Throws:**
- `InvalidOperationException`: If no workflow is active or if the activity conflicts with an existing one (e.g., duplicate identifier).

---

### `void AddTransition(Transition transition)`
Adds a transition between activities in the currently active workflow. The workflow must be created or retrieved prior to calling this method.

**Parameters:**
- `transition`: The `Transition` instance to add.

**Throws:**
- `InvalidOperationException`: If no workflow is active or if the transition is invalid (e.g., references non-existent activities).

---

### `void SetStartActivity(Activity activity)`
Designates an activity as the starting point of the currently active workflow.

**Parameters:**
- `activity`: The `Activity` to set as the start activity.

**Throws:**
- `InvalidOperationException`: If no workflow is active or if the activity is not part of the workflow.

---

### `void SetEndActivity(Activity activity)`
Designates an activity as the endpoint of the currently active workflow.

**Parameters:**
- `activity`: The `Activity` to set as the end activity.

**Throws:**
- `InvalidOperationException`: If no workflow is active or if the activity is not part of the workflow.

---

### `void PublishWorkflow()`
Marks the currently active workflow as published, enabling its execution. Unpublished workflows may be treated as drafts and excluded from runtime operations.

**Throws:**
- `InvalidOperationException`: If no workflow is active or if the workflow fails validation.

---

### `bool ValidateWorkflow()`
Validates the structure and integrity of the currently active workflow. Checks may include verifying the presence of a start/end activity, valid transitions, and absence of orphaned activities.

**Returns:**
- `true` if the workflow is valid; otherwise, `false`.

---

### `List<Activity> GetActivities()`
Retrieves all activities associated with the currently active workflow.

**Returns:**
- A `List<Activity>` containing all activities. Returns an empty list if no workflow is active or has no activities.

---

### `Activity? GetActivity(string activityId)`
Retrieves a specific activity from the currently active workflow by its identifier.

**Parameters:**
- `activityId`: The unique identifier of the activity.

**Returns:**
- The `Activity` instance if found; otherwise, `null`.

---

### `bool DeleteWorkflow(string workflowId)`
Removes a workflow from the service's collection by its identifier.

**Parameters:**
- `workflowId`: The unique identifier of the workflow to delete.

**Returns:**
- `true` if the workflow was deleted; `false` if the workflow was not found.

---

### `Workflow CloneWorkflow(Workflow workflow)`
Creates a deep copy of the specified workflow, including all activities and transitions.

**Parameters:**
- `workflow`: The `Workflow` instance to clone.

**Returns:**
- A new `Workflow` instance with identical structure and data.

**Throws:**
- `ArgumentNullException`: If `workflow` is `null`.

## Usage

### Example 1: Defining and Publishing a Workflow
