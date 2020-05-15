# WorkflowExecutionBenchmarks

`WorkflowExecutionBenchmarks` is a performance benchmarking class for the `dotnet-workflow-engine` project. It measures the execution characteristics of various workflow patterns—sequential, parallel, conditional, and multi-instance—using BenchmarkDotNet. The class provides setup methods to prepare workflow definitions and instances, as well as benchmark targets that exercise the engine's execution pipeline under controlled, repeatable conditions.

## API

### public void Setup()
Prepares the benchmarking environment before any benchmarks run. This method initializes shared resources, configures the workflow engine, and ensures that all necessary infrastructure is in a clean state. It is invoked once per benchmark run by the BenchmarkDotNet harness.

- **Parameters:** None.
- **Return value:** None.
- **Exceptions:** May throw if the workflow engine fails to initialize or if required configuration is missing.

### public async Task Execute_Sequential_Workflow()
Benchmarks the execution of a workflow composed of steps that run one after another in a predetermined order. Measures the overhead and throughput of sequential step dispatch.

- **Parameters:** None.
- **Return value:** A `Task` representing the asynchronous benchmark operation.
- **Exceptions:** Propagates any exception thrown by the workflow engine during sequential execution.

### public async Task Execute_Parallel_Workflow()
Benchmarks the execution of a workflow where multiple independent branches run concurrently. Captures the engine's ability to schedule and join parallel paths.

- **Parameters:** None.
- **Return value:** A `Task` representing the asynchronous benchmark operation.
- **Exceptions:** Propagates any exception thrown by the workflow engine during parallel execution, including aggregate exceptions from concurrent branches.

### public async Task Execute_Conditional_Workflow()
Benchmarks the execution of a workflow containing branching logic, where the path taken depends on runtime-evaluated conditions. Measures the cost of condition evaluation and dynamic route selection.

- **Parameters:** None.
- **Return value:** A `Task` representing the asynchronous benchmark operation.
- **Exceptions:** Propagates any exception thrown by the workflow engine during conditional execution.

### public void Create_Workflow_Instance()
Creates a single workflow instance from a predefined definition and stores it for subsequent benchmark iterations. This method isolates instance creation cost from execution cost.

- **Parameters:** None.
- **Return value:** None.
- **Exceptions:** May throw if the workflow definition is invalid or if instance creation fails due to engine state.

### public async Task Execute_Workflow_With_Multiple_Instances()
Benchmarks the execution of several workflow instances concurrently or in rapid succession. Evaluates the engine's behavior under multi-instance load, including resource contention and scheduling fairness.

- **Parameters:** None.
- **Return value:** A `Task` representing the asynchronous benchmark operation.
- **Exceptions:** Propagates exceptions from any failing instance, potentially wrapped in an aggregate exception.

### public async Task<Dictionary<string, object?>> ExecuteAsync()
Executes a generic workflow run and returns a dictionary of output values produced by the workflow. This method serves as a functional benchmark that also validates correctness by capturing results.

- **Parameters:** None.
- **Return value:** A `Task` that resolves to a `Dictionary<string, object?>` containing named outputs from the workflow execution. Keys correspond to step or workflow output names; values are the produced results, which may be null.
- **Exceptions:** Propagates any exception thrown during workflow execution. The dictionary may be incomplete or empty if execution fails before producing outputs.

## Usage

### Example 1: Running a Sequential Workflow Benchmark
```csharp
using BenchmarkDotNet.Running;

// Run the sequential workflow benchmark directly
var summary = BenchmarkRunner.Run<WorkflowExecutionBenchmarks>(
    benchmarks => benchmarks.Execute_Sequential_Workflow()
);
```

### Example 2: Validating Workflow Outputs in a Functional Benchmark
```csharp
var benchmarks = new WorkflowExecutionBenchmarks();
benchmarks.Setup();
benchmarks.Create_Workflow_Instance();

Dictionary<string, object?> outputs = await benchmarks.ExecuteAsync();

foreach (var kvp in outputs)
{
    Console.WriteLine($"Output '{kvp.Key}': {kvp.Value ?? "(null)"}");
}
```

## Notes

- **Setup ordering:** `Setup()` must complete before any benchmark method is invoked. The BenchmarkDotNet harness guarantees this, but manual invocation requires calling `Setup()` first.
- **Instance reuse:** `Create_Workflow_Instance()` prepares a workflow instance that may be reused across multiple iterations of `Execute_Workflow_With_Multiple_Instances()` or `ExecuteAsync()`. Modifying shared instance state between calls can skew benchmark results.
- **Thread safety:** The class is designed for use within the BenchmarkDotNet framework, which typically runs benchmarks single-threaded per iteration. Concurrent access to instance state from multiple threads is not guaranteed to be safe unless the underlying workflow engine explicitly supports it.
- **Exception propagation:** All `Execute_*` methods propagate exceptions directly. Benchmarks that throw will be recorded as failed by BenchmarkDotNet. Ensure the workflow definitions used are valid to avoid spurious failures.
- **Output dictionary nullability:** `ExecuteAsync()` returns a dictionary with `object?` values. Consumers must perform null checks before dereferencing values, as workflow steps may produce null outputs by design.
