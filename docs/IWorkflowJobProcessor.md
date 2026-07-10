# IWorkflowJobProcessor

Defines a contract for processing workflow jobs in the dotnet-workflow-engine. Implementations are responsible for enqueuing, tracking, and executing workflow-related jobs with configurable retry and priority semantics.

## API

### `Id`
Gets the unique identifier for this job processor instance.
Type: `string?`
Remarks: May be null if the processor has not been initialized.

### `WorkflowId`
Gets the identifier of the workflow this processor is associated with.
Type: `string?`
Remarks: May be null if the processor is not bound to a specific workflow.

### `InstanceId`
Gets the identifier of the workflow instance being processed.
Type: `string?`
Remarks: May be null if the processor is not currently processing an instance.

### `InputData`
Gets the input data dictionary for the current job.
Type: `Dictionary<string, object>?`
Remarks: May be null if no input data is associated with the job.

### `CreatedAt`
Gets the timestamp when this processor was created.
Type: `DateTime`
Remarks: Read-only; set at construction.

### `ScheduledFor`
Gets or sets the timestamp when the next retry or processing should occur.
Type: `DateTime?`
Remarks: Null indicates immediate processing or no scheduled retry.

### `RetryCount`
Gets the number of times this job has been retried.
Type: `int`
Remarks: Incremented on each retry; starts at 0.

### `Priority`
Gets or sets the processing priority of the job.
Type: `string?`
Remarks: Format and semantics are implementation-defined.

### `TotalProcessed`
Gets the total number of jobs successfully processed.
Type: `int`
Remarks: Read-only; updated by the processor.

### `TotalFailed`
Gets the total number of jobs that failed processing.
Type: `int`
Remarks: Read-only; updated by the processor.

### `TotalRetried`
Gets the total number of jobs that were retried.
Type: `int`
Remarks: Read-only; updated by the processor.

### `LastProcessedAt`
Gets the timestamp of the last successful processing.
Type: `DateTime?`
Remarks: Null if no job has been processed yet.

### `AvgProcessingTime`
Gets the average processing time across all processed jobs.
Type: `TimeSpan`
Remarks: Zero if no jobs have been processed.

### `WorkflowJobProcessor`
Gets the underlying processor instance.
Type: `WorkflowJobProcessor`
Remarks: Provides access to the concrete processor implementation.

### `EnqueueAsync()`
Enqueues the current job for processing.
Returns: `Task`
Remarks:
- May throw `InvalidOperationException` if the processor is not in a valid state.
- May throw `ArgumentNullException` if required data is missing.
- May throw `OperationCanceledException` if cancellation is requested.

### `GetPendingCountAsync()`
Gets the number of pending jobs for this processor.
Returns: `Task<int>`
Remarks:
- Returns 0 if no pending jobs exist.
- May throw `InvalidOperationException` if the processor is not initialized.

### `GetStatsAsync()`
Gets aggregated statistics for this processor.
Returns: `Task<JobProcessorStats>`
Remarks:
- Returns a snapshot of current statistics.
- May throw `InvalidOperationException` if the processor is not initialized.

## Usage

### Enqueuing a workflow job
