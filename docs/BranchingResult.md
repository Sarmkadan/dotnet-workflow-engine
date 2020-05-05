# BranchingResult

Represents the outcome of evaluating branching conditions within a workflow activity, containing the selected transitions, skipped transitions, and any evaluation errors encountered during the process.

## API

### `public string ActivityId`
The unique identifier of the workflow activity that produced this branching result.

### `public List<Transition> SelectedTransitions`
Gets the list of transitions that were selected based on the evaluated conditions. Transitions in this list met their conditions and are eligible for execution.

### `public List<Transition> SkippedTransitions`
Gets the list of transitions that were skipped due to unmet conditions or other evaluation criteria. These transitions were considered but did not qualify for execution.

### `public List<TransitionEvaluationError> EvaluationErrors`
Gets the list of errors encountered during the evaluation of transition conditions. Each error provides details about why a transition could not be evaluated or selected.

### `public bool AnyConditionMatched`
Indicates whether any transition condition evaluated to `true` during the branching process. Useful for determining if the branching logic produced any viable transitions.

### `public bool UsedDefaultTransition`
Indicates whether the default transition was used as a fallback when no other transitions met their conditions. This flag helps distinguish between explicit selections and default behavior.

### `public static BranchingResult Empty`
A static instance representing an empty result with no selected transitions, skipped transitions, or errors. Useful as a default or sentinel value.

### `public string TransitionId`
The unique identifier of the transition associated with this result. This is typically set when the result pertains to a specific transition evaluation.

### `public string Expression`
The expression that was evaluated to produce this branching result. This may represent the condition or logic used to determine transition eligibility.

### `public string ErrorMessage`
A message describing any error that occurred during the evaluation or selection process. This complements the `EvaluationErrors` list for scenarios where a single error summary is sufficient.

## Usage

### Example 1: Evaluating Branching Conditions
