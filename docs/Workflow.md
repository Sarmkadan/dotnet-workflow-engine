# Workflow

Represents a configurable workflow definition containing activities and transitions that can be executed by a workflow engine. The type tracks workflow identity, lifecycle state, versioning, and audit metadata while providing methods to inspect and navigate the workflow graph.

## API

### `public string Id`
Unique identifier for the workflow instance. Must be non-null and immutable once set.

### `public string Name`
Human-readable name of the workflow. Used for display and identification.

### `public string? Description`
Optional descriptive text providing additional context about the workflow’s purpose.

### `public int Version`
Incremental version number indicating the workflow definition’s revision. Starts at 1 and increases with each modification.

### `public WorkflowStatus Status`
Current execution status of the workflow (e.g., `Draft`, `Published`, `Completed`). Determines which transitions are valid.

### `public List<Activity> Activities`
Collection of all activities defined in the workflow. Never null; may be empty.

### `public List<Transition> Transitions`
Collection of all transitions between activities. Never null; may be empty.

### `public string? StartActivityId`
Identifier of the initial activity where execution begins. Null if the workflow has no start activity defined.

### `public string? EndActivityId`
Identifier of the terminal activity where execution concludes. Null if the workflow has no defined end.

### `public DateTime CreatedAt`
Timestamp indicating when the workflow definition was first created.

### `public DateTime ModifiedAt`
Timestamp indicating the most recent modification to the workflow definition.

### `public string? CreatedBy`
Identifier of the user or system that created the workflow definition.

### `public string? ModifiedBy`
Identifier of the user or system that last modified the workflow definition.

### `public bool Validate`
Indicates whether the workflow definition should be validated before execution. When true, structural and semantic checks are enforced.

### `public List<Activity> GetNextActivities(string activityId)`
Returns the list of activities directly reachable from the activity identified by `activityId` via outgoing transitions. Returns an empty list if no transitions exist or the activity is not found. Throws `ArgumentNullException` if `activityId` is null.

### `public List<Activity> GetPreviousActivities(string activityId)`
Returns the list of activities with transitions leading into the activity identified by `activityId`. Returns an empty list if no incoming transitions exist or the activity is not found. Throws `ArgumentNullException` if `activityId` is null.

### `public void Publish()`
Promotes the workflow definition from a draft or modified state to a published state, making it eligible for execution. Throws `InvalidOperationException` if the workflow definition is structurally invalid or lacks a valid start activity.

## Usage
