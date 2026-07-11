# WorkflowBuilder

A builder class used to construct and configure workflow definitions in the dotnet-workflow-engine. It provides a fluent interface for assembling activities, transitions, message catch events, and other workflow elements, and finally producing a `Workflow` instance that can be executed by the engine.

## API

### `public WorkflowBuilder()`

Initializes a new instance of the `WorkflowBuilder` class with an empty workflow configuration.

### `public WorkflowBuilder WithDescription(string description)`

Sets the description of the workflow being built.

- **Parameters**
  - `description` (string): The description text to assign to the workflow.
- **Return Value**
  - Returns the current `WorkflowBuilder` instance to support method chaining.
- **Exceptions**
  - Throws `ArgumentNullException` if `description` is `null`.

### `public WorkflowBuilder AddActivity(string name, Action<ActivityBuilder> configure)`

Adds an activity with the specified name to the workflow and applies additional configuration.

- **Parameters**
  - `name` (string): The unique name of the activity.
  - `configure` (Action<ActivityBuilder>): An action that configures the activity using an `ActivityBuilder`.
- **Return Value**
  - Returns the current `WorkflowBuilder` instance to support method chaining.
- **Exceptions**
  - Throws `ArgumentNullException` if `name` or `configure` is `null`.
  - Throws `InvalidOperationException` if an activity with the same `name` already exists.

### `public WorkflowBuilder AddMessageCatchEvent(string name, string messageType)`

Adds a message catch event with the specified name and message type to the workflow.

- **Parameters**
  - `name` (string): The unique name of the message catch event.
  - `messageType` (string): The type of message to catch.
- **Return Value**
  - Returns the current `WorkflowBuilder` instance to support method chaining.
- **Exceptions**
  - Throws `ArgumentNullException` if `name` or `messageType` is `null`.
  - Throws `InvalidOperationException` if an event with the same `name` already exists.

### `public WorkflowBuilder AddTaskActivity(string name, string taskType)`

Adds a task activity with the specified name and task type to the workflow.

- **Parameters**
  - `name` (string): The unique name of the task activity.
  - `taskType` (string): The type of task to execute.
- **Return Value**
  - Returns the current `WorkflowBuilder` instance to support method chaining.
- **Exceptions**
  - Throws `ArgumentNullException` if `name` or `taskType` is `null`.
  - Throws `InvalidOperationException` if an activity with the same `name` already exists.

### `public WorkflowBuilder AddTransition(string from, string to, string? trigger = null)`

Adds a transition from one activity to another, optionally triggered by a specific event.

- **Parameters**
  - `from` (string): The name of the source activity.
  - `to` (string): The name of the target activity.
  - `trigger` (string?, optional): The name of the trigger that causes the transition. Defaults to `null`.
- **Return Value**
  - Returns the current `WorkflowBuilder` instance to support method chaining.
- **Exceptions**
  - Throws `ArgumentNullException` if `from` or `to` is `null`.
  - Throws `InvalidOperationException` if either `from` or `to` does not exist in the workflow.

### `public WorkflowBuilder WithStartActivity(string startActivityName)`

Sets the start activity of the workflow.

- **Parameters**
  - `startActivityName` (string): The name of the activity to use as the workflow start point.
- **Return Value**
  - Returns the current `WorkflowBuilder` instance to support method chaining.
- **Exceptions**
  - Throws `ArgumentNullException` if `startActivityName` is `null`.
  - Throws `InvalidOperationException` if an activity with the specified `startActivityName` does not exist.

### `public WorkflowBuilder WithEndActivity(string endActivityName)`

Sets the end activity of the workflow.

- **Parameters**
  - `endActivityName` (string): The name of the activity to use as the workflow end point.
- **Return Value**
  - Returns the current `WorkflowBuilder` instance to support method chaining.
- **Exceptions**
  - Throws `ArgumentNullException` if `endActivityName` is `null`.
  - Throws `InvalidOperationException` if an activity with the specified `endActivityName` does not exist.

### `public Workflow Build()`

Constructs and returns a `Workflow` instance based on the current configuration.

- **Return Value**
  - Returns a new `Workflow` instance representing the configured workflow.
- **Exceptions**
  - Throws `InvalidOperationException` if required properties (e.g., start activity) are not set or if the workflow is invalid.

### `public Workflow BuildAndRegister(IWorkflowRegistry registry)`

Constructs a `Workflow` instance and registers it with the provided registry.

- **Parameters**
  - `registry` (IWorkflowRegistry): The registry to register the workflow with.
- **Return Value**
  - Returns the newly created and registered `Workflow` instance.
- **Exceptions**
  - Throws `ArgumentNullException` if `registry` is `null`.
  - Throws `InvalidOperationException` if required properties are not set or if the workflow is invalid.

### `public static WorkflowBuilder CreateSerial()`

Creates and returns a new instance of `WorkflowBuilder` configured for serial execution.

- **Return Value**
  - Returns a new `WorkflowBuilder` instance suitable for building a serial workflow.
- **Exceptions**
  - None.
