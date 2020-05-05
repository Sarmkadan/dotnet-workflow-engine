# Transition

Represents a directed edge between two activities in a workflow, defining the conditions and metadata under which the workflow progresses from one activity to another. Transitions are used to model the control flow of a workflow, including conditional branching, default paths, and priority-based routing.

## API

### `public string Id`
A unique identifier for the transition. This value is automatically generated if not explicitly set during creation.

### `public string FromActivityId`
The identifier of the activity from which this transition originates. Must correspond to an existing activity in the workflow.

### `public string ToActivityId`
The identifier of the target activity for this transition. Must correspond to an existing activity in the workflow.

### `public string? ConditionExpression`
An optional expression that determines whether this transition is valid for execution. If `null`, the transition is considered unconditional. The expression is evaluated in the context of the workflow instance's state.

### `public string? Label`
An optional human-readable description or name for the transition. Used for display purposes and debugging.

### `public bool IsDefault`
Indicates whether this transition is the default path when no other transitions from the same activity are valid. Only one default transition per `FromActivityId` is permitted.

### `public int Priority`
The priority of this transition relative to others originating from the same activity. Higher values indicate higher priority. Used to resolve conflicts when multiple transitions are valid.

### `public DateTime CreatedAt`
The timestamp when this transition was created. Automatically set to the current UTC time during instantiation.

### `public bool Validate`
Validates the transition's properties, ensuring required fields are populated and constraints (e.g., uniqueness of `IsDefault`) are satisfied. Returns `true` if validation passes; otherwise, `false`.

**Throws:**
- `InvalidOperationException`: If validation fails (e.g., duplicate default transition, invalid activity IDs).

### `public static Transition CreateDefault(string fromActivityId, string toActivityId)`
Creates a default transition between two activities. The transition will have no condition and will be marked as the default path.

**Parameters:**
- `fromActivityId`: The ID of the source activity.
- `toActivityId`: The ID of the target activity.

**Returns:**
A new `Transition` instance configured as a default transition.

**Throws:**
- `ArgumentException`: If either `fromActivityId` or `toActivityId` is `null` or whitespace.

### `public static Transition CreateConditional(string fromActivityId, string toActivityId, string conditionExpression)`
Creates a conditional transition between two activities. The transition will only be valid if the `conditionExpression` evaluates to `true`.

**Parameters:**
- `fromActivityId`: The ID of the source activity.
- `toActivityId`: The ID of the target activity.
- `conditionExpression`: The expression determining the transition's validity.

**Returns:**
A new `Transition` instance configured as a conditional transition.

**Throws:**
- `ArgumentException`: If any parameter is `null` or whitespace.

## Usage

### Example 1: Creating a Default Transition
