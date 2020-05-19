# WorkflowDefinitionBenchmarks

Benchmark suite for measuring the performance of workflow definition operations in the dotnet‑workflow‑engine library. The class contains a set of methods that exercise adding, retrieving, validating, and traversing small, medium, and large workflow definitions under controlled conditions.

## API

### `public void Setup`
**Purpose:** Initializes the benchmark fixture by creating an instance of the workflow engine and loading the workflow definition templates used by the subsequent benchmark methods.  
**Parameters:** None.  
**Return Value:** None.  
**Exceptions:**  
- `InvalidOperationException` if the engine cannot be instantiated or if required resources are missing.  

### `public void Add_Small_Workflow`
**Purpose:** Measures the time required to add a small workflow definition to the engine’s repository.  
**Parameters:** None.  
**Return Value:** None.  
**Exceptions:**  
- `ArgumentException` if the small workflow definition is malformed.  
- `InvalidOperationException` if `Setup` has not been called prior to execution.  

### `public void Get_Small_Workflow`
**Purpose:** Measures the time required to retrieve a previously added small workflow definition from the repository.  
**Parameters:** None.  
**Return Value:** None.  
**Exceptions:**  
- `KeyNotFoundException` if the small workflow definition has not been added.  
- `InvalidOperationException` if `Setup` has not been called.  

### `public void Validate_Small_Workflow`
**Purpose:** Measures the time required to validate the structural correctness of a small workflow definition.  
**Parameters:** None.  
**Return Value:** None.  
**Exceptions:**  
- `InvalidOperationException` if the workflow definition is not present or if `Setup` has not been called.  

### `public void Get_Next_Activities_Small_Workflow`
**Purpose:** Measures the time required to compute the set of next possible activities for a given state within a small workflow definition.  
**Parameters:** None.  
**Return Value:** None.  
**Exceptions:**  
- `InvalidOperationException` if the workflow definition is not present, if `Setup` has not been called, or if the requested state does not exist.  

### `public void Add_Medium_Workflow`
**Purpose:** Measures the time required to add a medium workflow definition to the engine’s repository.  
**Parameters:** None.  
**Return Value:** None.  
**Exceptions:**  
- `ArgumentException` if the medium workflow definition is malformed.  
- `InvalidOperationException` if `Setup` has not been called.  

### `public void Get_Medium_Workflow`
**Purpose:** Measures the time required to retrieve a previously added medium workflow definition from the repository.  
**Parameters:** None.  
**Return Value:** None.  
**Exceptions:**  
- `KeyNotFoundException` if the medium workflow definition has not been added.  
- `InvalidOperationException` if `Setup` has not been called.  

### `public void Validate_Medium_Workflow`
**Purpose:** Measures the time required to validate the structural correctness of a medium workflow definition.  
**Parameters:** None.  
**Return Value:** None.  
**Exceptions:**  
- `InvalidOperationException` if the workflow definition is not present or if `Setup` has not been called.  

### `public void Get_Next_Activities_Medium_Workflow`
**Purpose:** Measures the time required to compute the set of next possible activities for a given state within a medium workflow definition.  
**Parameters:** None.  
**Return Value:** None.  
**Exceptions:**  
- `InvalidOperationException` if the workflow definition is not present, if `Setup` has not been called, or if the requested state does not exist.  

### `public void Add_Large_Workflow`
**Purpose:** Measures the time required to add a large workflow definition to the engine’s repository.  
**Parameters:** None.  
**Return Value:** None.  
**Exceptions:**  
- `ArgumentException` if the large workflow definition is malformed.  
- `InvalidOperationException` if `Setup` has not been called.  

### `public void Get_Large_Workflow`
**Purpose:** Measures the time required to retrieve a previously added large workflow definition from the repository.  
**Parameters:** None.  
**Return Value:** None.  
**Exceptions:**  
- `KeyNotFoundException` if the large workflow definition has not been added.  
- `InvalidOperationException` if `Setup` has not been called.  

### `public void Validate_Large_Workflow`
**Purpose:** Measures the time required to validate the structural correctness of a large workflow definition.  
**Parameters:** None.  
**Return Value:** None.  
**Exceptions:**  
- `InvalidOperationException` if the workflow definition is not present or if `Setup` has not been called.  

### `public void Get_Next_Activities_Large_Workflow`
**Purpose:** Measures the time required to compute the set of next possible activities for a given state within a large workflow definition.  
**Parameters:** None.  
**Return Value:** None.  
**Exceptions:**  
- `InvalidOperationException` if the workflow definition is not present, if `Setup` has not been called, or if the requested state does not exist.  

## Usage

```csharp
using BenchmarkDotNet.Running;
using DotNetWorkflowEngine.Benchmarks;

// Run all benchmarks in the class
var summary = BenchmarkRunner.Run<WorkflowDefinitionBenchmarks>();
```

```csharp
using DotNetWorkflowEngine.Benchmarks;

// Manual execution for debugging or profiling
var bench = new WorkflowDefinitionBenchmarks();
bench.Setup();               // prepare the engine and templates
bench.Add_Small_Workflow();  // execute a single benchmark iteration
bench.Get_Small_Workflow();
bench.Validate_Small_Workflow();
bench.Get_Next_Activities_Small_Workflow();
// … repeat for medium and large variants as needed
```

## Notes

- All benchmark methods assume that `Setup` has been invoked successfully beforehand; calling any of the workflow‑specific methods prior to `Setup` will result in an `InvalidOperationException`.  
- The class is **not thread‑safe**. The benchmark methods mutate internal state (e.g., adding workflow definitions) and are intended to be executed sequentially by a benchmark runner such as BenchmarkDotNet. Concurrent invocation from multiple threads may lead to undefined behavior or incorrect measurements.  
- Memory consumption grows with the size of the workflow definition; the *Large* workflow benchmarks may allocate significant heap space and should be run in an environment with sufficient memory to avoid garbage‑collection interference.  
- If a workflow definition fails validation (e.g., due to missing required elements), the corresponding `Validate_*` method will throw an `InvalidOperationException`; this is considered a test failure rather than a performance measurement.  
- The benchmark methods return `void` because their primary purpose is to allow the benchmarking framework to measure execution time; any result data is intentionally ignored to keep the measurement focused on the operation itself.
