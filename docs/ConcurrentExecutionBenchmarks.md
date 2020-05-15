# ConcurrentExecutionBenchmarks

A benchmarking utility for evaluating the performance and scalability of the workflow engine when executing multiple workflows concurrently. It measures throughput, latency, and resource utilization under increasing workloads to identify bottlenecks and validate thread-safety guarantees.

## API

### `Setup`
Initializes the benchmark environment, including any required services, configurations, or test data. This method should be called before any execution benchmarks to ensure a clean and consistent state.

- **Parameters**: None
- **Return value**: `void`
- **Exceptions**: May throw if initialization fails (e.g., dependency injection errors, configuration issues).

---

### `Execute_10_Concurrent_Workflows`
Executes 10 workflows concurrently and measures performance metrics such as execution time, throughput, and resource usage.

- **Parameters**: None
- **Return value**: `Task`
- **Exceptions**: Propagates exceptions from individual workflow executions or engine failures.

---

### `Execute_50_Concurrent_Workflows`
Executes 50 workflows concurrently and measures performance metrics. Intended to test moderate concurrency scenarios.

- **Parameters**: None
- **Return value**: `Task`
- **Exceptions**: Propagates exceptions from individual workflow executions or engine failures.

---
### `Execute_100_Concurrent_Workflows`
Executes 100 workflows concurrently and measures performance metrics. Designed to evaluate scalability under higher concurrency.

- **Parameters**: None
- **Return value**: `Task`
- **Exceptions**: Propagates exceptions from individual workflow executions or engine failures.

---
### `Execute_200_Concurrent_Workflows`
Executes 200 workflows concurrently and measures performance metrics. Used to assess behavior under significant load.

- **Parameters**: None
- **Return value**: `Task`
- **Exceptions**: Propagates exceptions from individual workflow executions or engine failures.

---
### `Execute_500_Concurrent_Workflows`
Executes 500 workflows concurrently and measures performance metrics. Tests extreme concurrency scenarios and potential thread-safety limits.

- **Parameters**: None
- **Return value**: `Task`
- **Exceptions**: Propagates exceptions from individual workflow executions or engine failures.

---
### `Get_Statistics_With_1000_Instances`
Retrieves aggregated performance statistics after processing 1000 workflow instances. Useful for validating system behavior under sustained load.

- **Parameters**: None
- **Return value**: `Task`
- **Exceptions**: May throw if statistics collection fails or if required data is unavailable.

---
### `ExecuteAsync`
Executes a single workflow asynchronously with optional parameters and returns a dictionary of execution results or metadata.

- **Parameters**:
  - `workflowId` (`string`): Unique identifier for the workflow instance.
  - `options` (`Dictionary<string, object?>?`, optional): Additional execution options (e.g., timeouts, retries).
- **Return value**: `Task<Dictionary<string, object?>>`
  - Returns a dictionary containing execution results, status, or error details.
- **Exceptions**: May throw if workflow execution fails or parameters are invalid.

## Usage

### Example 1: Basic Benchmark Execution
