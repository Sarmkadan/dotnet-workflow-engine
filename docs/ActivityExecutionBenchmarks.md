# ActivityExecutionBenchmarks

`ActivityExecutionBenchmarks` is a benchmarking utility designed to measure the performance of workflow activity executions under various retry configurations. It provides a set of asynchronous methods that invoke activities and return timing or result data, enabling developers to compare the overhead introduced by different retry policies.

## API

### `public void Setup`
Prepares the benchmark environment by initializing required services, registering activity types, and resetting internal state.  
- **Parameters:** None.  
- **Return value:** None.  
- **Exceptions:**  
  - `InvalidOperationException` if called after the benchmark has already been executed without a intervening reset.  
  - `ObjectDisposedException` if the underlying workflow host has been disposed.

### `public async Task Execute_Simple_Activity`
Executes a baseline activity that contains no retry logic. Useful for measuring the raw execution cost of an activity.  
- **Parameters:** None.  
- **Return value:** A `Task` that completes when the activity finishes.  
- **Exceptions:**  
  - `ActivityExecutionException` if the activity throws an unhandled exception.  
  - `InvalidOperationException` if `Setup` has not been called prior to invocation.

### `public async Task Execute_Activity_With_Retry_Policy`
Executes an activity using a custom retry policy supplied by the benchmark (e.g., exponential back‑off).  
- **Parameters:** None.  
- **Return value:** A `Task` that completes when the activity succeeds or the retry policy aborts.  
- **Exceptions:**  
  - `ActivityExecutionException` if all retry attempts fail.  
  - `InvalidOperationException` if `Setup` has not been called.

### `public async Task Execute_Activity_With_Fixed_Retry`
Executes an activity with a fixed‑interval retry policy (e.g., retry three times every 100 ms).  
- **Parameters:** None.  
- **Return value:** A `Task` that completes when the activity succeeds or the fixed retry limit is reached.  
- **Exceptions:**  
  - `ActivityExecutionException` if the activity fails after all fixed retries.  
  - `InvalidOperationException` if `Setup` has not been called.

### `public async Task Execute_Activity_With_No_Retry`
Executes an activity with retry logic explicitly disabled.  
- **Parameters:** None.  
- **Return value:** A `Task` that completes when the activity finishes (or fails on the first attempt).  
- **Exceptions:**  
  - `ActivityExecutionException` if the activity throws an exception.  
  - `InvalidOperationException` if `Setup` has not been called.

### `public async Task<Dictionary<string, object?>> ExecuteAsync()`
Executes the benchmark activity and returns its output dictionary. This overload supplies no explicit input arguments.  
- **Parameters:** None.  
- **Return value:** A `Task<Dictionary<string, object?>>` containing the activity’s output parameters upon successful completion.  
- **Exceptions:**  
  - `ActivityExecutionException` if the activity fails.  
  - `InvalidOperationException` if `Setup` has not been called.  
  - `ObjectDisposedException` if the workflow host has been disposed.

### `public async Task<Dictionary<string, object?>> ExecuteAsync(IDictionary<string, object?> inputs)`
Executes the benchmark activity with the supplied input arguments.  
- **Parameters:**  
  - `inputs`: A dictionary of parameter names to values that are passed to the activity.  
- **Return value:** A `Task<Dictionary<string, object?>>` containing the activity’s output parameters upon successful completion.  
- **Exceptions:**  
  - `ArgumentNullException` if `inputs` is `null`.  
  - `ActivityExecutionException` if the activity fails.  
  - `InvalidOperationException` if `Setup` has not been called.  
  - `ObjectDisposedException` if the workflow host has been disposed.

## Usage

```csharp
using System.Threading.Tasks;
using DotNetWorkflowEngine.Benchmarks;

public class BenchmarkRunner
{
    public async Task RunSimpleActivityBenchmark()
    {
        var bench = new ActivityExecutionBenchmarks();
        bench.Setup();                                 // prepare the environment
        await bench.Execute_Simple_Activity();         // measure baseline execution
        // additional timing or metrics collection can be added here
    }
}
```

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetWorkflowEngine.Benchmarks;

public class BenchmarkRunnerWithInputs
{
    public async Task<Dictionary<string, object?>> RunActivityWithRetry()
    {
        var bench = new ActivityExecutionBenchmarks();
        bench.Setup();

        var inputs = new Dictionary<string, object?>
        {
            { "payload", "test data" },
            { "count", 42 }
        };

        // Execute the activity using the default retry policy and supplied inputs
        return await bench.ExecuteAsync(inputs);
    }
}
```

## Notes

- **Statefulness:** `Setup` mutates internal state (service registrations, activity definitions). Calling it multiple times without a reset may lead to duplicate registrations and consequently `InvalidOperationException`.  
- **Thread‑safety:** The benchmark instance is **not** thread‑safe. Concurrent calls to `Setup` or any of the execution methods from multiple threads can corrupt internal state. For parallel benchmarks, create separate instances of `ActivityExecutionBenchmarks` for each thread.  
- **Exception propagation:** All execution methods propagate `ActivityExecutionException` unchanged; callers should handle it if they wish to distinguish benchmark failures from expected activity failures.  
- **Resource disposal:** The underlying workflow host implements `IDisposable`. After a benchmark sequence is complete, consider disposing the `ActivityExecutionBenchmarks` instance (if it implements `IDisposable`) or explicitly resetting state to free resources.  
- **Input validation:** The overload of `ExecuteAsync` that accepts an `IDictionary<string, object?>` throws `ArgumentNullException` when the argument is `null`; empty dictionaries are permitted and result in the activity being invoked with no inputs.  
- **Performance measurement:** These methods are intended for use with a benchmarking framework (e.g., BenchmarkDotNet). The returned `Task` does not include timing information; timing should be measured externally around the method call.
