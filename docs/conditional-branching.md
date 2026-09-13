# Conditional branching

`ConditionalBranchingService` evaluates the transitions leaving a completed activity. It can return the selected transitions as a `BranchingResult`, map those transitions to the next workflow activities, or validate transition expression syntax before execution.

The service is implemented in `Services/ConditionalBranchingService.cs`. Its result and error models are defined in `Models/BranchingResult.cs`. All of these types are public.

## Branch selection rules

`ResolveBranchesAsync` considers only transitions whose `FromActivityId` exactly equals the supplied `activityId`. Outgoing transitions are ordered by descending `Priority`, then divided into three groups:

1. Non-default transitions with a non-null `ConditionExpression` are evaluated independently in priority order. Every expression that evaluates to `true` is selected; this is not a first-match operation.
2. Non-default transitions with a null `ConditionExpression` are unconditional and are always selected. They appear after selected conditional transitions in `SelectedTransitions`.
3. A default transition is selected only when neither a conditional nor an unconditional transition was selected. If several defaults exist, only the highest-priority default is selected.

An empty string is a non-null condition, so it is treated as a conditional expression. Expression evaluation determines whether it matches.

Expression syntax is provided by `ExpressionEvaluator`. Expressions read values from `ExecutionContext.Variables`; for example, `${amount} >= 100` compares the `amount` variable with `100`.

## Public API

### Constructor

```csharp
public ConditionalBranchingService(
    ILogger<ConditionalBranchingService> logger)
```

Creates the service. A null logger throws `ArgumentNullException`. The service keeps no per-workflow resolution state; it only retains the logger supplied to the constructor.

### ResolveBranchesAsync

```csharp
public Task<BranchingResult> ResolveBranchesAsync(
    Workflow workflow,
    string activityId,
    ExecutionContext context,
    CancellationToken cancellationToken = default)
```

Resolves all outgoing branches for `activityId` according to the rules above.

- `workflow` supplies the transitions to inspect and is required.
- `activityId` identifies the completed source activity and must not be null, empty, or whitespace.
- `context` supplies variables used by conditional expressions and is required.
- `cancellationToken` is checked before resolution and before each conditional expression.

The method returns `BranchingResult.Empty(activityId)` when the activity has no outgoing transitions. An invalid expression or other evaluation failure is recorded in `EvaluationErrors`, and that transition is treated as non-matching and added to `SkippedTransitions`; the failure is not rethrown. Cancellation requested at one of the explicit checks throws `OperationCanceledException`.

### GetNextActivitiesAsync

```csharp
public Task<List<Activity>> GetNextActivitiesAsync(
    Workflow workflow,
    string activityId,
    ExecutionContext context,
    CancellationToken cancellationToken = default)
```

Calls `ResolveBranchesAsync`, takes the distinct `ToActivityId` values from its selected transitions, and returns matching entries from `workflow.Activities`. The returned activities follow the order of `workflow.Activities`, not transition priority. Duplicate targets produce one activity, and a selected transition whose target is absent from the workflow does not produce an item.

The same argument and cancellation exceptions as `ResolveBranchesAsync` apply. An empty list is returned when no branch is selected or no selected target ID corresponds to an activity.

### ValidateTransitionExpressions

```csharp
public List<TransitionEvaluationError> ValidateTransitionExpressions(
    Workflow workflow)
```

Checks the syntax of every transition with a non-null `ConditionExpression`, including default transitions that also have an expression. It does not execute expressions against an `ExecutionContext`.

The method returns one `TransitionEvaluationError` for each reported validation message. An empty list means no syntax errors were reported. A null workflow throws `ArgumentNullException`; invalid expressions are returned as data rather than thrown.

## BranchingResult

`BranchingResult` describes resolution for one source activity.

| Member | Meaning |
| --- | --- |
| `ActivityId` | ID of the activity whose outgoing transitions were resolved. |
| `SelectedTransitions` | Matching conditional transitions, followed by unconditional transitions, or the chosen default. |
| `SkippedTransitions` | Conditional transitions that evaluated to `false` or failed evaluation. Unused defaults are not included. |
| `EvaluationErrors` | Expression failures captured while resolving individual transitions. |
| `AnyConditionMatched` | `true` when at least one conditional transition was selected. It remains `false` for unconditional and default selections. |
| `UsedDefaultTransition` | `true` only when a default transition was selected as the fallback. |
| `HasSelectedBranches` | Computed as `SelectedTransitions.Count > 0`. |
| `HasEvaluationErrors` | Computed as `EvaluationErrors.Count > 0`. |

The three list properties are initialized to mutable empty lists and have `init` setters. `BranchingResult.Empty(activityId)` creates a result with the supplied activity ID and all flags and lists empty. `ToString()` includes the activity ID, both flags, and details from the first evaluation error, if one exists.

Each `TransitionEvaluationError` contains:

| Member | Meaning |
| --- | --- |
| `TransitionId` | ID of the transition whose expression failed. |
| `Expression` | The expression that was being validated or evaluated. |
| `ErrorMessage` | The validation or evaluation error message. |

## Usage

```csharp
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using Microsoft.Extensions.Logging;
using WorkflowExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole());

var service = new ConditionalBranchingService(
    loggerFactory.CreateLogger<ConditionalBranchingService>());

var workflow = new Workflow
{
    Id = "order-processing",
    Name = "Order processing",
    Activities = new List<Activity>
    {
        new() { Id = "review", Name = "Review" },
        new() { Id = "approve", Name = "Approve" },
        new() { Id = "manual-review", Name = "Manual review" }
    },
    Transitions = new List<Transition>
    {
        new()
        {
            Id = "approve-large-order",
            FromActivityId = "review",
            ToActivityId = "approve",
            ConditionExpression = "${amount} >= 100",
            Priority = 10
        },
        new()
        {
            Id = "review-fallback",
            FromActivityId = "review",
            ToActivityId = "manual-review",
            IsDefault = true
        }
    }
};

var context = new WorkflowExecutionContext
{
    WorkflowInstanceId = "order-42",
    Variables = new Dictionary<string, object?>
    {
        ["amount"] = 150
    }
};

var validationErrors = service.ValidateTransitionExpressions(workflow);
if (validationErrors.Count > 0)
{
    throw new InvalidOperationException("The workflow contains invalid conditions.");
}

BranchingResult result = await service.ResolveBranchesAsync(
    workflow,
    "review",
    context);

foreach (Transition transition in result.SelectedTransitions)
{
    Console.WriteLine($"Selected {transition.Id}");
}

List<Activity> nextActivities = await service.GetNextActivitiesAsync(
    workflow,
    "review",
    context);
```

In this example, `approve-large-order` is selected, `AnyConditionMatched` is `true`, and the default transition is not used.
