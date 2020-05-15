# CachingBenchmarks

A benchmarking utility for evaluating the performance of workflow definition caching mechanisms in the dotnet-workflow-engine project. This class measures cache hit/miss rates, insertion throughput, and eviction behavior under various workload sizes and concurrency scenarios.

## API

### `void Setup()`
Initializes the benchmark environment by clearing any existing cache state and preparing measurement infrastructure. This method must be called before any other benchmark method to ensure a clean baseline.

**Parameters**: None
**Return value**: None
**Exceptions**: May throw if cache initialization fails or if required system resources are unavailable.

---

### `void Cache_Small_Workflow_Definition()`
Benchmarks the time and memory cost of inserting a small workflow definition (≤1KB) into the cache. Measures cache insertion latency and verifies successful storage.

**Parameters**: None
**Return value**: None
**Exceptions**: Throws if the workflow definition cannot be serialized or if cache insertion fails due to capacity constraints.

---

### `void Cache_Large_Workflow_Definition()`
Benchmarks the time and memory cost of inserting a large workflow definition (>10KB) into the cache. Evaluates cache behavior under higher memory pressure and potential eviction scenarios.

**Parameters**: None
**Return value**: None
**Exceptions**: Throws if the workflow definition exceeds cache size limits or if serialization/deserialization fails.

---
### `bool Get_Cached_Small_Workflow()`
Measures the latency and success rate of retrieving a previously cached small workflow definition. Validates cache hit behavior and deserialization correctness.

**Parameters**: None
**Return value**:
- `true` if the workflow is retrieved successfully from cache.
- `false` if the workflow is missing or deserialization fails.
**Exceptions**: May throw if cache access fails due to corruption or if deserialization encounters invalid data.

---
### `bool Get_Cached_Large_Workflow()`
Measures the latency and success rate of retrieving a previously cached large workflow definition. Assesses cache performance under higher memory footprint scenarios.

**Parameters**: None
**Return value**:
- `true` if the workflow is retrieved successfully from cache.
- `false` if the workflow is missing or deserialization fails.
**Exceptions**: May throw if cache access fails due to corruption or if deserialization encounters invalid data.

---
### `void Get_Missing_Workflow_From_Cache()`
Benchmarks the behavior and latency when attempting to retrieve a workflow definition that does not exist in the cache. Measures miss penalty and verifies correct error handling.

**Parameters**: None
**Return value**: None
**Exceptions**: May throw if cache access fails due to corruption or if the benchmark setup is invalid.

---
### `void Cache_Multiple_Workflows()`
Evaluates cache insertion throughput and eviction behavior when inserting multiple workflow definitions concurrently. Measures contention, memory usage, and potential cache thrashing.

**Parameters**: None
**Return value**: None
**Exceptions**: Throws if cache capacity is exceeded or if concurrent insertion fails due to synchronization issues.

---
### `void Remove_Workflow_From_Cache()`
Benchmarks the latency and correctness of explicitly removing a workflow definition from the cache. Validates eviction path and memory reclamation.

**Parameters**: None
**Return value**: None
**Exceptions**: May throw if the workflow is not present in the cache or if cache modification fails.

---
### `void Clear_Entire_Cache()`
Benchmarks the time and memory impact of clearing all cached workflow definitions. Measures cache invalidation overhead and resource reclamation.

**Parameters**: None
**Return value**: None
**Exceptions**: May throw if cache access fails due to corruption or if cleanup operations fail.

## Usage
