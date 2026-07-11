# ExpressionEvaluationBenchmarks

The `ExpressionEvaluationBenchmarks` class provides a set of benchmark methods for measuring the performance of expression evaluation and activity execution within the workflow engine. It is designed to be used with a benchmarking framework (e.g., BenchmarkDotNet) to assess the throughput and latency of evaluating simple, complex, and variable‑based conditions, as well as executing activities that depend on those conditions. The class exposes both synchronous boolean evaluation methods and asynchronous activity execution methods, all of which rely on a shared setup state.

## API

### `public void Setup()`

Initializes the benchmark state. This method must be called once before any benchmark methods are invoked. It sets up the workflow definition, activity instances, and any required expression trees or variable stores.  
**Parameters:** None.  
**Return value:** None.  
**Throws:** May throw if the workflow or expression configuration is invalid (e.g., missing required dependencies).

### `public async Task Execute_Activity_With_True_Condition()`

Executes an activity whose associated condition evaluates to `true`. The activity is expected to run to completion.  
**Parameters:** None.  
**Return value:** A `Task` representing the asynchronous operation.  
**Throws:** If the condition evaluation fails or the activity throws an exception during execution.

### `public async Task Execute_Activity_With_False_Condition()`

Executes an activity whose associated condition evaluates to `false`. The activity is expected to be skipped or not executed.  
**Parameters:** None.  
**Return value:** A `Task` representing the asynchronous operation.  
**Throws:** If the condition evaluation fails or the engine encounters an unexpected state.

### `public async Task Execute_Activity_With_Complex_Condition()`

Executes an activity whose condition involves multiple sub‑expressions, variable references, and logical operators.  
**Parameters:** None.  
**Return value:** A `Task` representing the asynchronous operation.  
**Throws:** If the complex expression cannot be parsed or evaluated, or if the activity execution fails.

### `public bool Evaluate_Simple_True_Condition()`

Evaluates a simple expression that is known to always return `true` (e.g., a literal `true`).  
**Parameters:** None.  
**Return value:** `true` if the expression evaluates successfully; otherwise `false`.  
**Throws:** Never throws under normal circumstances; may throw if the expression tree is malformed (should not happen after `Setup`).

### `public bool Evaluate_Simple_False_Condition()`

Evaluates a simple expression that is known to always return `false` (e.g., a literal `false`).  
**Parameters:** None.  
**Return value:** `false` if the expression evaluates successfully; otherwise `true` (indicating an evaluation error).  
**Throws:** Never throws under normal circumstances; may throw if the expression tree is malformed.

### `public bool Evaluate_Variable_Reference_Condition()`

Evaluates an expression that references a variable from the workflow context. The variable’s value is set during `Setup`.  
**Parameters:** None.  
**Return value:** The boolean result of the variable‑based expression.  
**Throws:** If the variable is not defined or its value cannot be resolved.

### `public bool Evaluate_Complex_Expression()`

Evaluates a multi‑part expression combining literals, variables, and logical/relational operators.  
**Parameters:** None.  
**Return value:** The boolean result of the complex expression.  
**Throws:** If any sub‑expression fails to evaluate (e.g., type mismatch, missing variable).

### `public async Task<Dictionary<string, object?>> ExecuteAsync()`

Executes the entire workflow (or a predefined workflow instance) and returns a dictionary of output variables and their values.  
**Parameters:** None.  
**Return value:** A `Task<Dictionary<string, object?>>` that resolves to a dictionary mapping output variable names to their values (or `null` if a variable is not set).  
**Throws:** If the workflow execution fails at any point (e.g., activity exception, invalid state transition).

## Usage

The following examples demonstrate how to use the `ExpressionEvaluationBenchmarks` class in a test or benchmarking context.

### Example 1: Running individual benchmark methods

```csharp
using System;
using System.Threading.Tasks;
using WorkflowEngine.Benchmarks;

public class BenchmarkRunner
{
    public static async Task Main()
    {
        var benchmarks = new ExpressionEvaluationBenchmarks();
        benchmarks.Setup();

        // Evaluate simple conditions
        bool simpleTrue = benchmarks.Evaluate_Simple_True_Condition();
        bool simpleFalse = benchmarks.Evaluate_Simple_False_Condition();
        Console.WriteLine($"Simple true: {simpleTrue}, Simple false: {simpleFalse}");

        // Execute an activity with a true condition
        await benchmarks.Execute_Activity_With_True_Condition();

        // Execute the full workflow and inspect outputs
        var outputs = await benchmarks.ExecuteAsync();
        foreach (var kvp in outputs)
        {
            Console.WriteLine($"{kvp.Key} = {kvp.Value}");
        }
    }
}
```

### Example 2: Using with BenchmarkDotNet

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using WorkflowEngine.Benchmarks;

[MemoryDiagnoser]
public class WorkflowBenchmarks
{
    private ExpressionEvaluationBenchmarks _benchmarks = new();

    [GlobalSetup]
    public void Setup() => _benchmarks.Setup();

    [Benchmark]
    public bool EvaluateSimpleTrue() => _benchmarks.Evaluate_Simple_True_Condition();

    [Benchmark]
    public bool EvaluateComplex() => _benchmarks.Evaluate_Complex_Expression();

    [Benchmark]
    public async Task ExecuteWithTrueCondition() => await _benchmarks.Execute_Activity_With_True_Condition();

    [Benchmark]
    public async Task<Dictionary<string, object?>> ExecuteWorkflow() => await _benchmarks.ExecuteAsync();
}

public class Program
{
    public static void Main() => BenchmarkRunner.Run<WorkflowBenchmarks>();
}
```

## Notes

- **Edge cases:**  
  - `Evaluate_Simple_False_Condition` returns `false` on success; a return value of `true` indicates an evaluation error.  
  - `Evaluate_Variable_Reference_Condition` and `Evaluate_Complex_Expression` may throw if the referenced variable is missing or the expression contains unsupported operations.  
  - The asynchronous methods (`Execute_Activity_*`, `ExecuteAsync`) will throw if the workflow definition is incomplete or if an activity throws an unhandled exception.  
  - `ExecuteAsync` returns a dictionary that may contain `null` values for output variables that were never assigned.

- **Thread safety:**  
  Instances of `ExpressionEvaluationBenchmarks` are **not thread‑safe**. The `Setup` method modifies internal state that is subsequently read by all benchmark methods. Concurrent calls to `Setup` or any benchmark method from multiple threads will produce undefined behavior. Each benchmark thread should use its own instance, or the instance must be used in a single‑threaded context (as is typical with benchmarking frameworks).
