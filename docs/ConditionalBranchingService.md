# ConditionalBranchingService

The `ConditionalBranchingService` is a core component within the `dotnet-workflow-engine` responsible for evaluating conditional logic to determine workflow progression. It analyzes transition expressions against the current workflow context to resolve valid execution paths, retrieve subsequent activities, and validate the syntactic and semantic correctness of transition definitions before runtime execution.

## API

### `ConditionalBranchingService`
The public constructor used to instantiate the service. It initializes the necessary internal dependencies required for expression evaluation and activity resolution.

### `ResolveBranchesAsync`
```csharp
public async Task<BranchingResult> ResolveBranchesAsync(...)
```
Evaluates all outgoing transitions from the current activity to determine which branches should be taken based on the current context.
*   **Purpose**: Computes the active execution paths by evaluating conditional expressions associated with transitions.
*   **Parameters**: Accepts the current workflow context and the source activity definition (specific parameter types depend on the internal implementation but generally include `WorkflowContext` and `Activity`).
*   **Return Value**: Returns a `BranchingResult` object containing the collection of valid transitions and metadata regarding the evaluation outcome.
*   **Exceptions**: Throws an exception if the expression evaluation fails due to runtime errors (e.g., type mismatch in expression) or if the context is null.

### `GetNextActivitiesAsync`
```csharp
public async Task<List<Activity>> GetNextActivitiesAsync(...)
```
Retrieves the concrete list of activity instances that should be executed immediately following the current step.
*   **Purpose**: Resolves the target activities for all valid branches determined by the current context.
*   **Parameters**: Requires the current workflow context and the current activity identifier.
*   **Return Value**: Returns a `List<Activity>` representing the next steps in the workflow. The list may contain multiple activities if parallel branching occurs, or be empty if the workflow terminates.
*   **Exceptions**: Throws if the target activities cannot be resolved from the registry or if the underlying branch resolution fails.

### `ValidateTransitionExpressions`
```csharp
public List<TransitionEvaluationError> ValidateTransitionExpressions(...)
```
Performs a static validation of transition expressions defined on an activity without executing them against a live context.
*   **Purpose**: Identifies syntax errors, missing variables, or invalid operators in transition conditions prior to workflow deployment or execution.
*   **Parameters**: Takes the activity definition containing the transitions to be validated.
*   **Return Value**: Returns a `List<TransitionEvaluationError>`. If the list is empty, all expressions are valid. If populated, each item describes a specific validation failure.
*   **Exceptions**: Generally does not throw exceptions for invalid expressions; instead, it captures them in the return list. It may throw if the input activity definition is malformed or null.

## Usage

### Example 1: Resolving Branches and Fetching Next Activities
This example demonstrates how to use the service within a workflow executor to determine the next steps dynamically.

```csharp
public async Task ExecuteCurrentStepAsync(WorkflowContext context, Activity currentActivity)
{
    var branchingService = new ConditionalBranchingService();

    // Evaluate conditions to find valid paths
    var branchResult = await branchingService.ResolveBranchesAsync(context, currentActivity);

    if (!branchResult.HasValidBranches)
    {
        // Handle case where no conditions were met (e.g., log warning or terminate)
        return;
    }

    // Retrieve the actual activity objects to queue for execution
    var nextActivities = await branchingService.GetNextActivitiesAsync(context, currentActivity.Id);

    foreach (var activity in nextActivities)
    {
        await QueueActivityForExecutionAsync(activity, context);
    }
}
```

### Example 2: Validating Workflow Definitions
This example shows how to validate a workflow definition before starting an instance to ensure all transition expressions are well-formed.

```csharp
public bool IsWorkflowDefinitionValid(Activity workflowRoot)
{
    var branchingService = new ConditionalBranchingService();
    
    // Validate all transitions recursively or at the root level depending on implementation
    var errors = branchingService.ValidateTransitionExpressions(workflowRoot);

    if (errors.Any())
    {
        foreach (var error in errors)
        {
            Console.WriteLine($"Validation Failed at {error.TransitionId}: {error.Message}");
        }
        return false;
    }

    return true;
}
```

## Notes

*   **Thread Safety**: The `ConditionalBranchingService` instance itself should be treated as stateless regarding workflow data, but the asynchronous methods (`ResolveBranchesAsync`, `GetNextActivitiesAsync`) rely on the passed `WorkflowContext`. While the service can be shared across threads, the context object passed to these methods must not be mutated concurrently by other threads during the evaluation window to prevent race conditions in expression evaluation.
*   **Empty Results**: `GetNextActivitiesAsync` may return an empty list if the current activity is an end state or if no transition conditions evaluate to true. Callers must handle this scenario gracefully to avoid assuming further execution is always possible.
*   **Validation vs. Execution**: `ValidateTransitionExpressions` performs static analysis. It does not guarantee that an expression will succeed at runtime (e.g., it cannot predict null reference exceptions caused by missing runtime data in the context), only that the expression syntax and referenced schema are correct.
*   **Error Handling**: Distinct handling is required for `ValidateTransitionExpressions` (which returns errors) versus the async execution methods (which typically throw on fatal evaluation errors). Do not rely on the validation method to catch runtime logic errors that depend on specific data values.
