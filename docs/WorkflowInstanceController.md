# WorkflowInstanceController

The `WorkflowInstanceController` provides HTTP endpoints for managing workflow instances in the dotnet-workflow-engine. It enables execution, retrieval, listing, retry, termination, and history inspection of workflow instances via RESTful API calls.

## API

### `public WorkflowInstanceController`

Constructor for the controller. Initializes required services for workflow instance management.

### `public async Task<IActionResult> ExecuteWorkflow(string workflowName, string? correlationId = null)`

Executes a new instance of the workflow specified by `workflowName`.

- **Parameters**
  - `workflowName`: The name of the workflow to execute.
  - `correlationId` (optional): An optional correlation identifier for the workflow instance.
- **Return value**: An `IActionResult` indicating the outcome of the operation, typically including the created workflow instance identifier.
- **Exceptions**: Throws if the workflow name is invalid or the workflow cannot be found.

### `public async Task<IActionResult> GetInstance(string instanceId)`

Retrieves the current state of a workflow instance by its identifier.

- **Parameters**
  - `instanceId`: The unique identifier of the workflow instance to retrieve.
- **Return value**: An `IActionResult` containing the workflow instance details or a not-found response.
- **Exceptions**: Throws if the instance identifier is malformed or the instance does not exist.

### `public async Task<IActionResult> ListInstances(int? page = null, int? pageSize = null)`

Lists workflow instances with optional pagination.

- **Parameters**
  - `page` (optional): The page number for pagination (1-based).
  - `pageSize` (optional): The number of items per page.
- **Return value**: An `IActionResult` containing a paginated list of workflow instances.
- **Exceptions**: Throws if pagination parameters are invalid.

### `public async Task<IActionResult> RetryInstance(string instanceId)`

Retries a failed workflow instance from its last persisted state.

- **Parameters**
  - `instanceId`: The unique identifier of the workflow instance to retry.
- **Return value**: An `IActionResult` indicating success or failure of the retry operation.
- **Exceptions**: Throws if the instance identifier is invalid or the instance is not in a retryable state.

### `public async Task<IActionResult> TerminateInstance(string instanceId)`

Terminates a running workflow instance immediately.

- **Parameters**
  - `instanceId`: The unique identifier of the workflow instance to terminate.
- **Return value**: An `IActionResult` indicating the outcome of the termination.
- **Exceptions**: Throws if the instance identifier is invalid or the instance cannot be terminated.

### `public async Task<IActionResult> GetInstanceHistory(string instanceId)`

Retrieves the execution history of a workflow instance.

- **Parameters**
  - `instanceId`: The unique identifier of the workflow instance.
- **Return value**: An `IActionResult` containing the workflow instance history or a not-found response.
- **Exceptions**: Throws if the instance identifier is malformed or the instance does not exist.

## Usage
