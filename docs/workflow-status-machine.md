# Workflow status machine

`WorkflowStatusMachine` defines the allowed lifecycle changes for `WorkflowStatus` values. The status enum is shared by workflow definitions and workflow instances, while the state machine is used by `WorkflowInstance.TransitionTo` to validate instance status changes.

The types documented here are implemented in:

- `Enums/WorkflowStatus.cs`
- `Models/WorkflowStatusMachine.cs`

## WorkflowStatus values

| Value | Numeric value | Meaning |
| --- | ---: | --- |
| `Draft` | `0` | The workflow has not been published. New workflow instances also start in this status. |
| `Active` | `1` | The workflow is active and can be instantiated. For an instance, execution is active. |
| `Deprecated` | `2` | The workflow definition is deprecated, but existing instances may continue. This value has no valid state-machine transitions. |
| `Archived` | `3` | The workflow is archived and cannot create new instances. The state machine treats it as a terminal instance status. |
| `Suspended` | `4` | Instance execution is paused. |
| `WaitingForMessage` | `5` | The instance is waiting for a correlated message. |
| `Cancelled` | `6` | The instance was cancelled by a user request. The state machine treats it as terminal. |

Because the enum serves both definitions and instances, not every value participates in the instance state machine. In particular, `Deprecated` is meaningful for a workflow definition but is neither the source nor target of any transition in `WorkflowStatusMachine`.

## Valid transitions

| Current status | Valid target statuses | Typical reason |
| --- | --- | --- |
| `Draft` | `Active` | Start or publish the workflow. |
| `Active` | `WaitingForMessage` | A message catch event was reached. |
| `Active` | `Suspended` | Execution was manually paused. |
| `Active` | `Archived` | Execution completed normally. |
| `Active` | `Cancelled` | Execution was manually cancelled. |
| `WaitingForMessage` | `Active` | The awaited message arrived and execution resumed. |
| `WaitingForMessage` | `Suspended` | Execution was paused while waiting. |
| `WaitingForMessage` | `Cancelled` | Execution was cancelled while waiting. |
| `Suspended` | `Active` | Execution was manually resumed. |
| `Suspended` | `WaitingForMessage` | A message catch event was reached while suspended. |
| `Suspended` | `Archived` | Execution completed after being suspended or resumed. |
| `Suspended` | `Cancelled` | Execution was cancelled after suspension. |
| `Deprecated` | None | The status does not participate in the state machine. |
| `Archived` | None | Terminal status. |
| `Cancelled` | None | Terminal status. |

All transitions not listed in the table are invalid. This includes transitions to the same status, every transition to or from `Deprecated`, returning an archived or cancelled instance to another status, and moving directly from `Draft` to any status other than `Active`.

## Public API

### IsValidTransition

```csharp
public static bool IsValidTransition(
    WorkflowStatus from,
    WorkflowStatus to)
```

Returns `true` only when the exact `(from, to)` pair is in the valid transition set. `Archived` and `Cancelled` always return `false` as a source, and `Draft` accepts only `Active` as its target. The method does not throw for an invalid transition; it returns `false`.

Enum casts are also accepted by the CLR even when the numeric value is not declared by `WorkflowStatus`. Such an unknown value has no entry in the transition set, so this method returns `false`.

### GetValidTransitionsFrom

```csharp
public static IEnumerable<WorkflowStatus> GetValidTransitionsFrom(
    WorkflowStatus status)
```

Enumerates the allowed targets for `status`. `Draft` yields only `Active`. `Deprecated`, `Archived`, `Cancelled`, and unknown enum values yield an empty sequence.

The result is produced lazily. Callers should treat it as the set of allowed targets rather than rely on its enumeration order, because the underlying rules are stored in a `HashSet`.

### GetTransitionMatrix

```csharp
public static string GetTransitionMatrix()
```

Returns a newline-separated diagnostic representation of the stored rules. It includes one line for each source that has an entry in the transition set: `Draft`, `Active`, `Suspended`, and `WaitingForMessage`. It omits `Deprecated`, `Archived`, and `Cancelled` because those statuses have no outgoing rule.

Source lines are ordered by the enum's numeric value. Target names reflect `HashSet` enumeration and should not be parsed as a stable serialization format.

## Using the state machine

Use `WorkflowInstance.TransitionTo` when changing an instance status. It calls `WorkflowStatusMachine.IsValidTransition` and throws `StateException` when the requested change is invalid.

```csharp
using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;

var instance = new WorkflowInstance();

if (WorkflowStatusMachine.IsValidTransition(
    instance.Status,
    WorkflowStatus.Active))
{
    instance.TransitionTo(WorkflowStatus.Active);
}

IEnumerable<WorkflowStatus> nextStatuses =
    WorkflowStatusMachine.GetValidTransitionsFrom(instance.Status);
```

`Workflow.Status` and `WorkflowInstance.Status` have public setters. Assigning either property directly bypasses `WorkflowStatusMachine`; validation occurs only when code calls the state-machine API or an operation such as `WorkflowInstance.TransitionTo` that uses it.
