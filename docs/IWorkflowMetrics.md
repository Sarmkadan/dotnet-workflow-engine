# IWorkflowMetrics

`IWorkflowMetrics` defines the contract for collecting, aggregating, and exposing runtime telemetry about workflow and activity executions within the workflow engine. It tracks execution counts, success/failure rates, duration statistics, and error distributions, and supports snapshotting the current state for external consumption or persistence.

## API

### Properties

| Member | Type | Description |
|---|---|---|
| `TotalWorkflowsExecuted` | `int` | The total number of workflow executions that have been recorded, regardless of outcome. |
| `SuccessfulWorkflows` | `int` | The number of workflow executions that completed without error. |
| `FailedWorkflows` | `int` | The number of workflow executions that terminated due to an unhandled error or explicit failure. |
| `SuccessRate` | `double` | The ratio of `SuccessfulWorkflows` to `TotalWorkflowsExecuted`. Returns `0.0` when no workflows have been executed. |
| `AverageWorkflowDurationMs` | `long` | The mean duration of recorded workflow executions, in milliseconds. |
| `MinWorkflowDurationMs` | `long` | The shortest duration observed among recorded workflow executions, in milliseconds. |
| `MaxWorkflowDurationMs` | `long` | The longest duration observed among recorded workflow executions, in milliseconds. |
| `TotalActivitiesExecuted` | `int` | The total number of individual activity executions recorded. |
| `SuccessfulActivities` | `int` | The number of activity executions that completed successfully. |
| `FailedActivities` | `int` | The number of activity executions that failed. |
| `AverageActivityDurationMs` | `long` | The mean duration of recorded activity executions, in milliseconds. |
| `ErrorCount` | `Dictionary<string, int>` | A breakdown of error occurrences keyed by error type name or error code. The dictionary is a live reference; modifications to the returned instance may affect internal state depending on implementation. |
| `LastUpdated` | `DateTime` | The timestamp of the most recent call to any recording method (`RecordWorkflowExecution`, `RecordActivityExecution`, or `RecordError`). |
| `SnapshotTime` | `DateTime` | The timestamp at which the last snapshot was generated via `GetMetricsAsync`. |

### Constructor

| Signature | Description |
|---|---|
| `WorkflowMetrics()` | Initializes a new instance with all counters and aggregates set to zero, `LastUpdated` and `SnapshotTime` set to `DateTime.MinValue`, and an empty `ErrorCount` dictionary. |

### Methods

| Signature | Returns | Description |
|---|---|---|
| `void RecordWorkflowExecution(bool success, long durationMs)` | `void` | Records a single workflow execution outcome. `success` indicates whether the workflow completed successfully; `durationMs` is the elapsed time in milliseconds. Updates `TotalWorkflowsExecuted`, `SuccessfulWorkflows` or `FailedWorkflows`, and recalculates `SuccessRate`, `AverageWorkflowDurationMs`, `MinWorkflowDurationMs`, and `MaxWorkflowDurationMs`. Sets `LastUpdated` to `DateTime.UtcNow`. |
| `void RecordActivityExecution(bool success, long durationMs)` | `void` | Records a single activity execution outcome. `success` indicates whether the activity completed successfully; `durationMs` is the elapsed time in milliseconds. Updates `TotalActivitiesExecuted`, `SuccessfulActivities` or `FailedActivities`, and recalculates `AverageActivityDurationMs`. Sets `LastUpdated` to `DateTime.UtcNow`. |
| `void RecordError(string errorType)` | `void` | Increments the count for the specified `errorType` in the `ErrorCount` dictionary. If the key does not exist, it is added with a count of `1`. Sets `LastUpdated` to `DateTime.UtcNow`. Passing `null` or an empty string may result in an `ArgumentNullException` or `ArgumentException` depending on implementation. |
| `Task<WorkflowMetricsSnapshot> GetMetricsAsync()` | `Task<WorkflowMetricsSnapshot>` | Asynchronously captures a point-in-time snapshot of all current metric values and returns it wrapped in a `WorkflowMetricsSnapshot` object. Updates `SnapshotTime` to `DateTime.UtcNow` before returning. The returned task is typically completed synchronously but is exposed as `Task` for consistency with async consumers. |
| `void Reset()` | `void` | Resets all counters, aggregates, and the error dictionary to their initial state. `LastUpdated` and `SnapshotTime` are set to `DateTime.MinValue`. |

## Usage

### Example 1: Recording workflow and activity executions inline

```csharp
IWorkflowMetrics metrics = new WorkflowMetrics();
var sw = Stopwatch.StartNew();

try
{
    // Execute workflow logic...
    sw.Stop();
    metrics.RecordWorkflowExecution(success: true, durationMs: sw.ElapsedMilliseconds);
}
catch (Exception ex)
{
    sw.Stop();
    metrics.RecordWorkflowExecution(success: false, durationMs: sw.ElapsedMilliseconds);
    metrics.RecordError(ex.GetType().Name);
}

// Record an activity within the workflow
var activitySw = Stopwatch.StartNew();
bool activityOk = ExecuteSomeActivity();
activitySw.Stop();
metrics.RecordActivityExecution(success: activityOk, durationMs: activitySw.ElapsedMilliseconds);

Console.WriteLine($"Current success rate: {metrics.SuccessRate:P2}");
```

### Example 2: Periodic snapshotting and reset

```csharp
IWorkflowMetrics metrics = new WorkflowMetrics();
// ... multiple executions recorded over time ...

async Task PersistMetricsPeriodically(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromMinutes(5), ct);

        WorkflowMetricsSnapshot snapshot = await metrics.GetMetricsAsync();
        await SaveSnapshotToDatabase(snapshot, ct);

        // Optionally reset for the next aggregation window
        metrics.Reset();
    }
}
```

## Notes

- **Thread safety:** The default `WorkflowMetrics` implementation is not guaranteed to be thread-safe. Concurrent calls to `RecordWorkflowExecution`, `RecordActivityExecution`, `RecordError`, `Reset`, or `GetMetricsAsync` from multiple threads may produce corrupted counters or inconsistent aggregates unless external synchronization is applied.
- **Duration statistics:** `MinWorkflowDurationMs` is initialized to `long.MaxValue` internally and replaced on the first recorded execution. Querying it before any workflow has been recorded returns `long.MaxValue` (or `0` depending on implementation choice). Consumers should guard against this by checking `TotalWorkflowsExecuted > 0`.
- **SuccessRate precision:** `SuccessRate` is a floating-point calculation. When `TotalWorkflowsExecuted` is zero, the value is `0.0` rather than `double.NaN`.
- **ErrorCount dictionary:** The returned dictionary reference may be the internal backing store. To avoid unintended mutation, consumers should treat it as read-only or copy it before modification.
- **Snapshot immutability:** `WorkflowMetricsSnapshot` is a separate, immutable record of the state at the time `GetMetricsAsync` is called. Subsequent recordings do not affect a snapshot that has already been taken.
- **Reset behavior:** After `Reset`, all aggregate properties return to their initial zero-equivalent states. Any snapshot taken before the reset remains unchanged.
