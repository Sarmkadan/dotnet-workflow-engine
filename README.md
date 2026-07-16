# dotnet-workflow-engine

[![Build Status](https://dev.azure.com/.../dotnet-workflow-engine/_apis/build/status/...)](https://dev.azure.com/.../dotnet-workflow-engine/_build/latest?definitionId=...)

The dotnet-workflow-engine is a lightweight, extensible workflow engine written in C#. It supports parallel execution, conditional branching, retry policies, and stateful activities. The engine is designed to be easily integrated into existing .NET applications and can be extended with custom activity handlers.

## Architecture

See [docs/architecture.md](docs/architecture.md) for the full picture: module breakdown, the execution flow (definition -> publish -> instance -> recursive graph walk), design decisions with their trade-offs, extension points (`IActivityHandler`, `IEventBus`, `IAuditRepository`, `IOutputFormatter`) and the honest list of current limitations.
Short version: everything is in-memory today, handlers plug in per activity type, retry and routing are handled centrally by the engine.

## WorkflowExecutionService

The `WorkflowExecutionService` is the core execution engine for the workflow system. It manages the complete lifecycle of workflow instances from creation to completion, including:

- Creating new instances from published workflow definitions
- Starting and executing workflows by running activities in sequence
- Handling parallel execution with fork/join patterns
- Managing suspended workflows waiting for external messages
- Tracking instance state and providing comprehensive querying capabilities
- Providing statistics on workflow execution status

The service maintains all active instances in memory and persists audit logs for all workflow events.

Example usage:

```csharp
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Setup services (typically via DI)
var services = new ServiceCollection();
services.AddWorkflowServices();
var serviceProvider = services.BuildServiceProvider();

var executionService = serviceProvider.GetRequiredService<IWorkflowExecutionService>();
var definitionService = serviceProvider.GetRequiredService<IWorkflowDefinitionService>();
var auditService = serviceProvider.GetRequiredService<IAuditService>();

// Create a new workflow instance
var workflowInstance = executionService.CreateInstance(
workflowId: "order-processing-workflow",
correlationId: "order-2024-001",
initiatedBy: "order-service@company.com"
);

Console.WriteLine($"Created workflow instance: {workflowInstance.Id}");

// Start the workflow execution
var startedInstance = await executionService.StartAsync(workflowInstance.Id);
Console.WriteLine($"Workflow started: {startedInstance.Status}");

// Execute a specific activity
await executionService.ExecuteActivityAsync(startedInstance, "validate-order");
Console.WriteLine("Validation activity completed");

// Get the current instance state
var currentInstance = executionService.GetInstance(workflowInstance.Id);
Console.WriteLine($"Current status: {currentInstance?.Status}");

// Get statistics
var stats = executionService.GetStatistics();
Console.WriteLine($"Total instances: {stats.Total}, Active: {stats.Active}, Completed: {stats.Completed}, Failed: {stats.Failed}");

// Complete the workflow
if (currentInstance != null && currentInstance.Status == WorkflowStatus.Completed)
{
    executionService.CompleteInstance(currentInstance.Id);
    Console.WriteLine("Workflow completed successfully");
}

// Get instances by workflow
var workflowInstances = executionService.GetInstancesByWorkflow("order-processing-workflow");
Console.WriteLine($"Found {workflowInstances.Count} instances for this workflow");

// Get active instances
var activeInstances = executionService.GetActiveInstances();
Console.WriteLine($"Active instances: {activeInstances.Count}");
```

## ICacheService

The `ICacheService` interface provides a unified abstraction for caching operations across different cache implementations. It supports both in-memory and distributed caching strategies, allowing the workflow engine to adapt to various deployment scenarios without changing application code.

The interface includes methods for basic CRUD operations (`GetAsync`, `SetAsync`, `RemoveAsync`, `ExistsAsync`) and a convenience method (`GetOrLoadAsync`) that combines retrieval with fallback loading for efficient data access patterns.

Example usage with MemoryCacheService:

```csharp
using DotNetWorkflowEngine.Caching;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

// Setup services with in-memory caching (default)
var services = new ServiceCollection();
services.AddWorkflowServices(); // Uses MemoryCacheService by default
var serviceProvider = services.BuildServiceProvider();

// Resolve the cache service
var cacheService = serviceProvider.GetRequiredService<ICacheService>();

// Get a cached value (returns null if not found)
var cachedData = await cacheService.GetAsync<string>("user-session-123");
Console.WriteLine($"Cached data: {cachedData ?? "null"}");

// Check if a key exists in cache
bool exists = await cacheService.ExistsAsync("user-session-123");
Console.WriteLine($"Key exists: {exists}");

// Set a value in cache with expiration
await cacheService.SetAsync(
  "user-session-123",
  "user-data-456",
  TimeSpan.FromMinutes(30)
);
Console.WriteLine("Value cached successfully");

// Remove a value from cache
await cacheService.RemoveAsync("user-session-123");
Console.WriteLine("Value removed from cache");

// Get or load with fallback - efficient pattern for expensive operations
var workflowData = await cacheService.GetOrLoadAsync(
  "workflow-definition-789",
  async () => {
    Console.WriteLine("Loading workflow from database...");
    await Task.Delay(50); // Simulate database call
    return "workflow-definition-content";
  },
  TimeSpan.FromHours(1)
);
Console.WriteLine($"Workflow data: {workflowData}");

// Set multiple values
foreach (var item in new[] { "item1", "item2", "item3" })
{
  await cacheService.SetAsync($"key-{item}", item, TimeSpan.FromMinutes(10));
}
Console.WriteLine("Multiple values cached");

// Check existence of multiple keys
foreach (var item in new[] { "key-item1", "key-item2", "key-item4" })
{
  bool keyExists = await cacheService.ExistsAsync(item);
  Console.WriteLine($"Key {item} exists: {keyExists}");
}
```

Example usage with DistributedCacheService:

```csharp
using DotNetWorkflowEngine.Caching;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

// Setup services with distributed caching (e.g., Redis)
var services = new ServiceCollection();
services.AddWorkflowServices(cacheProvider: "Redis");
services.AddStackExchangeRedisCache(options =>
{
  options.Configuration = "localhost:6379";
});
var serviceProvider = services.BuildServiceProvider();

// Resolve the distributed cache service
var cacheService = serviceProvider.GetRequiredService<ICacheService>();

// Get a cached value from distributed cache
var cachedConfig = await cacheService.GetAsync<WorkflowConfig>("app-config-v2");
Console.WriteLine($"Config from distributed cache: {cachedConfig?.Version}");

// Set a configuration value with expiration
var newConfig = new WorkflowConfig { Version = "2.0", Settings = "..." };
await cacheService.SetAsync("app-config-v2", newConfig, TimeSpan.FromHours(24));
Console.WriteLine("Config cached in distributed cache");

// Use GetOrLoadAsync with distributed cache for shared data
var sharedData = await cacheService.GetOrLoadAsync(
  "shared-workflow-state",
  async () => {
    Console.WriteLine("Fetching shared state from API...");
    await Task.Delay(100);
    return new SharedWorkflowState { State = "Running", Timestamp = DateTime.UtcNow };
  },
  TimeSpan.FromMinutes(5)
);
Console.WriteLine($"Shared state: {sharedData.State}");
```

## ActivityService

The `ActivityService` manages the execution of workflow activities, including activity handler registration, conditional execution, retry policies, and validation. It serves as the central service for executing activities within workflows, supporting both standard activities and gateway activities (fork/join points).

The service handles:

- Registration and lookup of activity handlers by type
- Execution of activities with configurable retry policies (exponential backoff, fixed delay, linear backoff, or no retry)
- Conditional activity execution based on expressions
- Activity validation before execution
- Gateway activity handling (fork/join patterns)

Example usage:

```csharp
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Setup services (typically via DI)
var services = new ServiceCollection();
services.AddWorkflowServices();
var serviceProvider = services.BuildServiceProvider();

// Get the activity service and retry policy service
var activityService = serviceProvider.GetRequiredService<IActivityService>();
var retryPolicyService = serviceProvider.GetRequiredService<IRetryPolicyService>();

// Register a custom activity handler
activityService.RegisterHandler("custom-handler", new CustomActivityHandler());

// Get registered handler types
var handlers = activityService.GetRegisteredHandlerTypes();
Console.WriteLine($"Registered handlers: {string.Join(", ", handlers)}");

// Validate an activity before execution
var activity = new Activity
{
    Id = "validate-order",
    Name = "Validate Order",
    Type = "Validation",
    HandlerType = "custom-handler",
    TimeoutSeconds = 30,
    MaxRetries = 3,
    RetryPolicy = RetryPolicy.ExponentialBackoff
};

if (activityService.ValidateActivity(activity, out var errors))
{
    Console.WriteLine("Activity is valid");
}
else
{
    Console.WriteLine($"Validation errors: {string.Join(", ", errors)}");
}

// Execute the activity with execution context
var context = new ExecutionContext
{
    WorkflowInstanceId = "wf-order-processing-001",
    CorrelationId = "corr-7f3b9c2e-4567-89ab-cdef-123456789abc"
};

// Set variables for conditional execution
context.SetVariable("isValid", true);

var result = await activityService.ExecuteAsync(activity, context);

if (result.IsSuccess())
{
    Console.WriteLine($"Activity completed successfully in {result.ExecutionDurationMs}ms");
    Console.WriteLine($"Output: {result.GetOutputs().Count} items");
}
else if (result.IsFailed())
{
    Console.WriteLine($"Activity failed: {result.ErrorMessage}");
}
else if (result.IsSkipped())
{
    Console.WriteLine($"Activity skipped: {result.SkipReason}");
}

// Example custom activity handler implementation
public class CustomActivityHandler : ActivityService.IActivityHandler
{
    public async Task<Dictionary<string, object?>> ExecuteAsync(Activity activity, ExecutionContext context)
    {
        // Perform activity logic here
        var output = new Dictionary<string, object?>();
        output["result"] = "success";
        output["processedAt"] = DateTime.UtcNow;

        await Task.Delay(100); // Simulate work
        return output;
    }
}
```

## WorkflowRepository

The `WorkflowRepository` class provides data access methods for workflow persistence and querying. It serves as the primary interface for storing, retrieving, updating, and deleting workflow definitions and instances in the underlying data store. The repository supports both basic CRUD operations and advanced query patterns including pagination, filtering by status, searching by name, and retrieving workflows with activity counts.

Example usage:

```csharp
using DotNetWorkflowEngine.Data.Repositories;
using DotNetWorkflowEngine.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Create repository instance (typically via dependency injection)
var repository = new WorkflowRepository();

// Add a new workflow definition
var newWorkflow = new Workflow
{
    Id = "order-processing-workflow",
    Name = "Order Processing Workflow",
    Description = "Processes customer orders through validation, inventory check, and payment",
    Version = 1,
    Status = WorkflowStatus.Draft,
    CreatedAt = DateTime.UtcNow,
    ModifiedAt = DateTime.UtcNow,
    CreatedBy = "admin@company.com",
    ModifiedBy = "admin@company.com"
};

await repository.AddAsync(newWorkflow);
Console.WriteLine($"Added workflow: {newWorkflow.Id}");

// Get a workflow by ID
var retrievedWorkflow = await repository.GetByIdAsync("order-processing-workflow");
if (retrievedWorkflow != null)
{
    Console.WriteLine($"Retrieved workflow: {retrievedWorkflow.Name} (v{retrievedWorkflow.Version}");
}

// Check if a workflow exists
bool exists = await repository.ExistsAsync("order-processing-workflow");
Console.WriteLine($"Workflow exists: {exists}");

// Get all workflows
var allWorkflows = await repository.GetAllAsync();
Console.WriteLine($"Total workflows: {allWorkflows.Count}");

// Get workflows with pagination
var pagedResult = await repository.GetPagedAsync(page: 1, pageSize: 10);
Console.WriteLine($"Page 1: {pagedResult.Items.Count} workflows, Total: {pagedResult.Total} workflows");

// Search workflows by name
var searchResults = await repository.SearchByNameAsync("order");
Console.WriteLine($"Workflows matching 'order': {searchResults.Count}");

// Get workflows by status
var draftWorkflows = await repository.GetByStatusAsync(WorkflowStatus.Draft);
Console.WriteLine($"Draft workflows: {draftWorkflows.Count}");

// Get active workflows
var activeWorkflows = await repository.GetActiveWorkflowsAsync();
Console.WriteLine($"Active workflows: {activeWorkflows.Count}");

// Update a workflow
if (retrievedWorkflow != null)
{
    retrievedWorkflow.Status = WorkflowStatus.Published;
    retrievedWorkflow.ModifiedAt = DateTime.UtcNow;
    retrievedWorkflow.ModifiedBy = "admin@company.com";

    await repository.UpdateAsync(retrievedWorkflow);
    Console.WriteLine("Workflow updated successfully");
}

// Get workflows created since a specific date
var recentWorkflows = await repository.GetCreatedSinceAsync(DateTime.UtcNow.AddDays(-7));
Console.WriteLine($"Workflows created in last 7 days: {recentWorkflows.Count}");

// Get workflows with activity counts
var workflowsWithCounts = await repository.GetWithActivityCountAsync();
foreach (var (workflow, activityCount) in workflowsWithCounts)
{
    Console.WriteLine($"Workflow '{workflow.Name}' has {activityCount} activities");
}

// Count total workflows
var totalCount = await repository.CountAsync();
Console.WriteLine($"Total workflow count: {totalCount}");

// Delete a workflow
// await repository.DeleteAsync("temp-workflow");

// Clear all workflows (use with caution!)
// await repository.ClearAsync();
```

## NoOpCacheService

The `NoOpCacheService` is a no-operation cache implementation that does nothing when caching is disabled. It implements the `ICacheService` interface to maintain consistency in the dependency injection container but provides no actual caching functionality. All operations return default values or complete immediately without performing any work.

This service is useful when you want to disable caching without changing the code that depends on `ICacheService`. It bypasses all cache operations, ensuring that every call to retrieve or store data results in the actual operation being performed.

Example usage:

```csharp
using DotNetWorkflowEngine.Caching;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

// Setup services with caching disabled
var services = new ServiceCollection();
services.AddWorkflowServices(cacheProvider: "NoOp"); // Explicitly use NoOp cache
var serviceProvider = services.BuildServiceProvider();

// Resolve the cache service
var cacheService = serviceProvider.GetRequiredService<ICacheService>();

// Try to get a value (returns null/default)
var cachedValue = await cacheService.GetAsync<string>("non-existent-key");
Console.WriteLine($"Cached value: {cachedValue}"); // null

// Check if key exists (always returns false)
bool exists = await cacheService.ExistsAsync("non-existent-key");
Console.WriteLine($"Key exists: {exists}"); // false

// Set a value (does nothing)
await cacheService.SetAsync("test-key", "test-value", TimeSpan.FromMinutes(5));
Console.WriteLine("Value set (no-op)");

// Remove a value (does nothing)
await cacheService.RemoveAsync("test-key");
Console.WriteLine("Value removed (no-op)");

// Clear cache (does nothing)
await cacheService.ClearAsync();
Console.WriteLine("Cache cleared (no-op)");

// Get or load with fallback (always executes provider)
var result = await cacheService.GetOrLoadAsync(
    "data-key",
    async () => {
        Console.WriteLine("Loading data from source...");
        await Task.Delay(100); // Simulate loading
        return "actual-data";
    },
    TimeSpan.FromMinutes(5)
);
Console.WriteLine($"Result: {result}"); // "actual-data"
```

## ICacheService

The `ICacheService` interface provides a unified abstraction for caching operations across different cache implementations. It supports both in-memory and distributed caching strategies, allowing the workflow engine to adapt to various deployment scenarios without changing application code.

The interface includes methods for basic CRUD operations (`GetAsync`, `SetAsync`, `RemoveAsync`, `ExistsAsync`) and a convenience method (`GetOrLoadAsync`) that combines retrieval with fallback loading for efficient data access patterns.

Example usage with MemoryCacheService:

```csharp
using DotNetWorkflowEngine.Caching;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

// Setup services with in-memory caching (default)
var services = new ServiceCollection();
services.AddWorkflowServices(); // Uses MemoryCacheService by default
var serviceProvider = services.BuildServiceProvider();

// Resolve the cache service
var cacheService = serviceProvider.GetRequiredService<ICacheService>();

// Get a cached value (returns null if not found)
var cachedData = await cacheService.GetAsync<string>("user-session-123");
Console.WriteLine($"Cached data: {cachedData ?? "null"}");

// Check if a key exists in cache
bool exists = await cacheService.ExistsAsync("user-session-123");
Console.WriteLine($"Key exists: {exists}");

// Set a value in cache with expiration
await cacheService.SetAsync(
  "user-session-123",
  "user-data-456",
  TimeSpan.FromMinutes(30)
);
Console.WriteLine("Value cached successfully");

// Remove a value from cache
await cacheService.RemoveAsync("user-session-123");
Console.WriteLine("Value removed from cache");

// Get or load with fallback - efficient pattern for expensive operations
var workflowData = await cacheService.GetOrLoadAsync(
  "workflow-definition-789",
  async () => {
    Console.WriteLine("Loading workflow from database...");
    await Task.Delay(50); // Simulate database call
    return "workflow-definition-content";
  },
  TimeSpan.FromHours(1)
);
Console.WriteLine($"Workflow data: {workflowData}");

// Set multiple values
foreach (var item in new[] { "item1", "item2", "item3" })
{
  await cacheService.SetAsync($"key-{item}", item, TimeSpan.FromMinutes(10));
}
Console.WriteLine("Multiple values cached");

// Check existence of multiple keys
foreach (var item in new[] { "key-item1", "key-item2", "key-item4" })
{
  bool keyExists = await cacheService.ExistsAsync(item);
  Console.WriteLine($"Key {item} exists: {keyExists}");
}
```

Example usage with DistributedCacheService:

```csharp
using DotNetWorkflowEngine.Caching;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

// Setup services with distributed caching (e.g., Redis)
var services = new ServiceCollection();
services.AddWorkflowServices(cacheProvider: "Redis");
services.AddStackExchangeRedisCache(options =>
{
  options.Configuration = "localhost:6379";
});
var serviceProvider = services.BuildServiceProvider();

// Resolve the distributed cache service
var cacheService = serviceProvider.GetRequiredService<ICacheService>();

// Get a cached value from distributed cache
var cachedConfig = await cacheService.GetAsync<WorkflowConfig>("app-config-v2");
Console.WriteLine($"Config from distributed cache: {cachedConfig?.Version}");

// Set a configuration value with expiration
var newConfig = new WorkflowConfig { Version = "2.0", Settings = "..." };
await cacheService.SetAsync("app-config-v2", newConfig, TimeSpan.FromHours(24));
Console.WriteLine("Config cached in distributed cache");

// Use GetOrLoadAsync with distributed cache for shared data
var sharedData = await cacheService.GetOrLoadAsync(
  "shared-workflow-state",
  async () => {
    Console.WriteLine("Fetching shared state from API...");
    await Task.Delay(100);
    return new SharedWorkflowState { State = "Running", Timestamp = DateTime.UtcNow };
  },
  TimeSpan.FromMinutes(5)
);
Console.WriteLine($"Shared state: {sharedData.State}");
```

## CollectionExtensions

The `CollectionExtensions` class provides a set of extension methods for working with collections, lists, dictionaries, and enumerables. These methods offer safe access to collection elements, filtering capabilities, transformation utilities, and common operations that help prevent null reference exceptions and simplify collection manipulation.

Key features include safe first-element retrieval, null filtering, batching, dictionary conversion, and element comparison operations.

Example usage:

```csharp
using DotNetWorkflowEngine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

// Create sample data for demonstration
var numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
var strings = new List<string?> { "hello", null, "world", null, "dotnet", "workflow", null };
var people = new List<Person> 
{
    new Person { Id = 1, Name = "Alice", Age = 30 },
    new Person { Id = 2, Name = "Bob", Age = 25 },
    new Person { Id = 3, Name = "Charlie", Age = 35 }
};

var dict1 = new Dictionary<int, string> { { 1, "one" }, { 2, "two" } };
var dict2 = new Dictionary<int, string> { { 3, "three" }, { 2, "TWO" } };

// SafeFirst - safely get first element or default
var firstNumber = numbers.SafeFirst();
Console.WriteLine($"First number: {firstNumber}"); // 1

var emptyList = new List<int>();
var firstEmpty = emptyList.SafeFirst(-1);
Console.WriteLine($"First of empty list: {firstEmpty}"); // -1

// WhereNotNull - filter out null elements
var nonNullStrings = strings.WhereNotNull().ToList();
Console.WriteLine($"Non-null strings: {string.Join(", ", nonNullStrings)}"); // hello, world, dotnet, workflow

// IsNullOrEmpty - check if collection is null or empty
bool isEmpty = emptyList.IsNullOrEmpty();
Console.WriteLine($"Is empty list null or empty: {isEmpty}"); // True

bool isNull = ((List<int>)null).IsNullOrEmpty();
Console.WriteLine($"Is null collection null or empty: {isNull}"); // True

// Batch - split collection into chunks
var batches = numbers.Batch(3).ToList();
Console.WriteLine($"Batches: {batches.Count}"); // 4 batches: [1,2,3], [4,5,6], [7,8,9], [10]

// ToSafeDictionary - convert collection to dictionary with duplicate key checking
var peopleDict = people.ToSafeDictionary(p => p.Id);
Console.WriteLine($"People dictionary count: {peopleDict.Count}"); // 3

// ContainsSameElements - compare collections regardless of order
var numbers2 = new List<int> { 3, 1, 2 };
bool sameElements = numbers.Take(3).ContainsSameElements(numbers2);
Console.WriteLine($"Same elements: {sameElements}"); // True

// TryGetValue - safely get dictionary value without exceptions
var dict = new Dictionary<int, string> { { 1, "one" }, { 2, "two" } };
var value = dict.TryGetValue(2, "default");
Console.WriteLine($"Value for key 2: {value}"); // "two"
var missingValue = dict.TryGetValue(99, "not-found");
Console.WriteLine($"Value for missing key: {missingValue}"); // "not-found"

// Merge - combine multiple dictionaries
var mergedDict = dict1.Merge(dict2);
Console.WriteLine($"Merged dictionary count: {mergedDict.Count}"); // 3 (key 2 overwritten)

// Flatten - flatten nested collections
var nested = new List<List<int>> { new List<int> { 1, 2 }, new List<int> { 3, 4 } };
var flattened = nested.Flatten().ToList();
Console.WriteLine($"Flattened: {string.Join(", ", flattened)}"); // 1, 2, 3, 4

// DistinctOrdered - remove duplicates while preserving order
var withDuplicates = new List<int> { 1, 2, 2, 3, 1, 4, 5, 3 };
var distinct = withDuplicates.DistinctOrdered().ToList();
Console.WriteLine($"Distinct ordered: {string.Join(", ", distinct)}"); // 1, 2, 3, 4, 5

// AddAndReturn - add item and return collection for chaining
var list = new List<string>();
list.AddAndReturn("first").AddAndReturn("second").AddAndReturn("third");
Console.WriteLine($"Chained adds: {string.Join(", ", list)}"); // first, second, third

// Partition - split collection based on predicate
var (even, odd) = numbers.Partition(n => n % 2 == 0);
Console.WriteLine($"Even numbers: {string.Join(", ", even)}"); // 2, 4, 6, 8, 10
Console.WriteLine($"Odd numbers: {string.Join(", ", odd)}"); // 1, 3, 5, 7, 9

// Example class for demonstration
public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}
```

## StringExtensions

The `StringExtensions` class provides a comprehensive set of extension methods for common string manipulation operations used throughout the workflow engine. It includes utilities for case conversion (PascalCase, snake_case, kebab-case), validation (email, URL), text processing (truncation, whitespace handling, repetition), and specialized parsing (substring extraction, smart splitting).

Example usage:

```csharp
using DotNetWorkflowEngine.Utilities;
using System;
using System.Linq;

// Case conversion examples
string pascalCase = "hello-world".ToPascalCase(); // "HelloWorld"
string snakeCase = "HelloWorld".ToSnakeCase(); // "hello_world"
string kebabCase = "HelloWorld".ToKebabCase(); // "hello-world"

// Validation examples
bool isValidEmail = "test@example.com".IsValidEmail(); // true
bool isValidUrl = "https://example.com".IsValidUrl(); // true

// Text processing examples
string truncated = "Hello World".Truncate(5); // "Hello"
string withEllipsis = "Hello World".Truncate(5, "..."); // "He..."
string noWhitespace = "hello world".RemoveWhitespace(); // "helloworld"
string normalized = "  hello   world  ".NormalizeWhitespace(); // "hello world"
string repeated = "ab".Repeat(3); // "ababab"

// Safe operations examples
string safeSubstring = "Hello World".SafeSubstring(6, 5); // "World"
string? extracted = "prefix[start]middle[end]suffix".ExtractBetween("[", "]"); // "start"

// Smart splitting example
var parts = "a,b,\"c,d\",e".SmartSplit(","); // ["a", "b", "\"c,d\"", "e"]

// Complex example combining multiple operations
string workflowName = "process-order-workflow";
string pascalWorkflow = workflowName.ToPascalCase(); // "ProcessOrderWorkflow"
string kebabWorkflow = pascalWorkflow.ToKebabCase(); // "process-order-workflow"
bool isValid = kebabWorkflow.IsValidUrl(); // false
```

## SerializationHelper

The `SerializationHelper` class provides a comprehensive set of utilities for JSON serialization and deserialization operations. It standardizes JSON handling across the application with consistent options for property naming, null handling, and type conversion. The helper includes methods for converting objects to JSON strings, parsing JSON back to objects, deep cloning, merging objects, and validating JSON content.

Example usage:

```csharp
using DotNetWorkflowEngine.Utilities;
using System;
using System.Collections.Generic;
using System.Text.Json;

// Create sample data for demonstration
var person = new Person
{
    Id = 1,
    Name = "John Doe",
    Age = 30,
    Address = new Address { Street = "123 Main St", City = "New York", ZipCode = "10001" },
    Tags = new List<string> { "developer", "csharp", "workflow" }
};

var workflowConfig = new WorkflowConfig
{
    Id = "config-001",
    Name = "Order Processing",
    Enabled = true,
    RetryPolicy = new RetryPolicyConfig { MaxRetries = 3, InitialDelaySeconds = 1 },
    Settings = new Dictionary<string, object> { { "timeout", 30 }, { "retry", true } }
};

// Serialize an object to JSON string (compact format)
string jsonCompact = SerializationHelper.ToJson(person);
Console.WriteLine(jsonCompact);

// Serialize an object to pretty-printed JSON (human-readable)
string jsonPretty = SerializationHelper.ToJsonPretty(person);
Console.WriteLine(jsonPretty);

// Deserialize JSON string back to an object
var deserializedPerson = SerializationHelper.FromJson<Person>(jsonCompact);
Console.WriteLine($"Deserialized: {deserializedPerson?.Name}");

// Safely deserialize JSON (returns null on error instead of throwing)
string invalidJson = "{ invalid: json }";
var safeResult = SerializationHelper.TryFromJson<Person>(invalidJson);
Console.WriteLine($"Safe deserialization result: {safeResult}"); // null

// Deep clone an object by serializing and deserializing
var personClone = SerializationHelper.DeepClone(person);
Console.WriteLine($"Clone equals original: {personClone?.Id == person.Id}"); // True

// Merge two objects (later values override earlier ones)
var person2 = new Person { Id = 1, Name = "John Updated", Age = 31 };
var mergedPerson = SerializationHelper.Merge(person, person2);
Console.WriteLine($"Merged name: {mergedPerson?.Name}"); // "John Updated"

// Convert between JSON element and typed objects
JsonElement jsonElement = SerializationHelper.ToJsonElement(person);
var fromElement = SerializationHelper.FromJsonElement<Person>(jsonElement);
Console.WriteLine($"From element: {fromElement?.Name}");

// Validate JSON content
bool isValid = SerializationHelper.IsValidJson(jsonCompact);
Console.WriteLine($"Is valid JSON: {isValid}"); // True

bool isInvalid = SerializationHelper.IsValidJson("not json");
Console.WriteLine($"Is invalid JSON: {isInvalid}"); // False

// Pretty-print existing JSON string
string minifiedJson = "{\"name\":\"test\",\"value\":123}";
string prettyJson = SerializationHelper.PrettyPrintJson(minifiedJson);
Console.WriteLine(prettyJson);

// Minify JSON string (remove whitespace)
string prettyJsonInput = "{ \"name\": \"test\", \"value\": 123 }";
string minified = SerializationHelper.MinifyJson(prettyJsonInput);
Console.WriteLine(minified);

// Deserialize to dictionary for untyped data
string dictJson = "{\"key1\":\"value1\",\"key2\":123}";
var dictionary = SerializationHelper.FromJsonToDict(dictJson);
Console.WriteLine($"Dictionary count: {dictionary?.Count}"); // 2

// Example classes for demonstration
public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
    public Address Address { get; set; }
    public List<string> Tags { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string ZipCode { get; set; }
}

public class WorkflowConfig
{
    public string Id { get; set; }
    public string Name { get; set; }
    public bool Enabled { get; set; }
    public RetryPolicyConfig RetryPolicy { get; set; }
    public Dictionary<string, object> Settings { get; set; }
}

public class RetryPolicyConfig
{
    public int MaxRetries { get; set; }
    public int InitialDelaySeconds { get; set; }
}
```

## IWorkflowMetrics

The `IWorkflowMetrics` interface provides comprehensive metrics and monitoring capabilities for tracking workflow execution statistics. It collects and exposes detailed metrics including workflow execution counts, success/failure rates, durations, activity statistics, error tracking, and snapshot timestamps. This interface is essential for monitoring system health, performance analysis, and capacity planning.

Example usage:

```csharp
using DotNetWorkflowEngine.Monitoring;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

// Setup services (typically via DI)
var services = new ServiceCollection();
services.AddWorkflowServices();
var serviceProvider = services.BuildServiceProvider();

// Resolve the metrics service
var metricsService = serviceProvider.GetRequiredService<IWorkflowMetrics>();

// Record a successful workflow execution
metricsService.RecordWorkflowExecution(isSuccess: true, durationMs: 1500);

// Record a failed workflow execution
metricsService.RecordWorkflowExecution(isSuccess: false, durationMs: 800);

// Record a successful activity execution
metricsService.RecordActivityExecution(isSuccess: true, durationMs: 250);

// Record an activity failure with error details
metricsService.RecordActivityExecution(isSuccess: false, durationMs: 120);
metricsService.RecordError("database-connection-failed", "Failed to connect to database: timeout");

// Get current metrics snapshot
var metrics = await metricsService.GetMetricsAsync();

Console.WriteLine($"Total workflows executed: {metrics.TotalWorkflowsExecuted}");
Console.WriteLine($"Successful workflows: {metrics.SuccessfulWorkflows}");
Console.WriteLine($"Failed workflows: {metrics.FailedWorkflows}");
Console.WriteLine($"Success rate: {metrics.SuccessRate:P2}");
Console.WriteLine($"Average workflow duration: {metrics.AverageWorkflowDurationMs}ms");
Console.WriteLine($"Min workflow duration: {metrics.MinWorkflowDurationMs}ms");
Console.WriteLine($"Max workflow duration: {metrics.MaxWorkflowDurationMs}ms");
Console.WriteLine($"Total activities executed: {metrics.TotalActivitiesExecuted}");
Console.WriteLine($"Successful activities: {metrics.SuccessfulActivities}");
Console.WriteLine($"Failed activities: {metrics.FailedActivities}");
Console.WriteLine($"Average activity duration: {metrics.AverageActivityDurationMs}ms");

// Display error statistics
foreach (var error in metrics.ErrorCount.OrderByDescending(kv => kv.Value).Take(5))
{
    Console.WriteLine($"Error '{error.Key}': {error.Value} occurrences");
}

Console.WriteLine($"Last updated: {metrics.LastUpdated}");
Console.WriteLine($"Snapshot time: {metrics.SnapshotTime}");

// Reset metrics (typically used for testing or when starting fresh)
metricsService.Reset();
Console.WriteLine("Metrics reset completed");
```

## Activity

The `Activity` class represents a single unit of work within a workflow definition. It encapsulates all configuration needed to execute a task, including timeouts, retry policies, input/output mappings, and execution modes. Activities can represent tasks, events, or gateways (fork/join points) and support conditional execution through expressions.

## DotnetWorkflowEngineOptions

The `DotnetWorkflowEngineOptions` class provides configuration options for the dotnet-workflow-engine using the IOptions pattern. It controls core engine behavior, infrastructure settings, caching configuration, middleware options, security parameters, and execution policies. This class is typically configured via dependency injection in your application's startup.

Example usage:

```csharp
using DotNetWorkflowEngine.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// Configure options in appsettings.json or via code
var configuration = new ConfigurationBuilder()
.AddJsonFile("appsettings.json")
.Build();

// Setup services with configuration
var services = new ServiceCollection();

services.Configure<DotnetWorkflowEngineOptions>(configuration.GetSection("WorkflowEngine"));
services.AddWorkflowServices();

var serviceProvider = services.BuildServiceProvider();

// Access configured options
var options = serviceProvider.GetRequiredService<IOptions<DotnetWorkflowEngineOptions>>().Value;

Console.WriteLine($"Connection String: {options.ConnectionString}");
Console.WriteLine($"Max Concurrent Workflows: {options.MaxConcurrentWorkflows}");
Console.WriteLine($"Enable Audit Logging: {options.EnableAuditLogging}");
Console.WriteLine($"Caching Enabled: {options.CachingEnabled}");
Console.WriteLine($"Cache Provider: {options.CacheProvider}");

// Configure specific options programmatically
services.Configure<DotnetWorkflowEngineOptions>(options =>
{
    options.ConnectionString = "Host=localhost;Database=workflow_engine;Username=postgres;Password=secret";
    options.MaxConcurrentWorkflows = 200;
    options.DefaultActivityTimeoutSeconds = 600;
    options.EnableMetrics = true;
    options.EnableCaching = true;
    options.CacheProvider = "Redis";
    options.RedisConnectionString = "localhost:6379";
    options.UseDistributedCache = true;
    options.EnableAuditLogging = true;
    options.EnableAuditTrail = true;
    options.EnableRequestLogging = true;
    options.EnableRateLimiting = true;
    options.MaxConcurrentWorkflows = 150;
    options.DefaultRetryPolicy = new RetryPolicyConfig
    {
        MaxRetries = 3,
        InitialDelaySeconds = 1,
        MaxDelaySeconds = 30,
        BackoffType = "Exponential"
    };
});
```

## DependencyInjection

The `DependencyInjection` class provides extension methods for registering workflow engine services in the .NET dependency injection container. It centralizes the configuration of all services, middleware, and filters needed to run the workflow engine. The primary method `AddWorkflowEngine()` registers core services, while additional methods like `AddWorkflowEngineCors()`, `AddWorkflowEngineAuthentication()`, and `UseWorkflowEngine()` configure specific features.

The middleware configuration class `WorkflowEngineMiddlewareOptions` controls request logging, rate limiting, CORS, and other HTTP middleware settings. You can customize these options when calling `UseWorkflowEngine()` to tailor the engine's behavior to your application's needs.

Example usage:

```csharp
using DotNetWorkflowEngine.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

// Setup services in Program.cs
var builder = WebApplication.CreateBuilder(args);

// Configure workflow engine services with optional settings
builder.Services.AddWorkflowEngine(options =>
{
    options.ConnectionString = "Host=localhost;Database=workflow_engine";
    options.EnableAuditLogging = true;
    options.EnableMetrics = true;
    options.MaxConcurrentWorkflows = 100;
});

// Add CORS policy
builder.Services.AddWorkflowEngineCors();

// Add authentication with JWT
builder.Services.AddWorkflowEngineAuthentication(
    "your-secret-key-here-at-least-32-characters-long");

var app = builder.Build();

// Configure middleware with custom options
app.UseWorkflowEngine(new WorkflowEngineMiddlewareOptions
{
    EnableRequestLogging = true,
    LogRequestBody = true,
    LogResponseBody = false,
    EnableRateLimiting = true,
    RateLimit = new RateLimitConfiguration
    {
        MaxRequests = 200,
        WindowSeconds = 30,
        RetryAfterSeconds = 30
    },
    EnableCors = true
});

Console.WriteLine("Workflow engine services configured successfully");
```

## AuditRepository

The `AuditRepository` class provides data access methods for audit log persistence and querying. It serves as the primary interface for storing, retrieving, updating, and deleting audit log entries that track all workflow events including activity executions, state changes, errors, and completions. The repository supports comprehensive querying capabilities including filtering by workflow instance, activity, event type, severity level, date ranges, and pagination.

Example usage:

```csharp
using DotNetWorkflowEngine.Data.Repositories;
using DotNetWorkflowEngine.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Create repository instance (typically via dependency injection)
var repository = new AuditRepository();

// Add a new audit entry
var newEntry = new AuditLogEntry
{
    Id = Guid.NewGuid().ToString(),
    WorkflowInstanceId = "wf-order-processing-001",
    ActivityId = "validate-order",
    EventType = "ActivityStarted",
    Description = "Starting order validation activity",
    Severity = "Information",
    Actor = "order-service@company.com",
    Timestamp = DateTime.UtcNow,
    Metadata = new Dictionary<string, object?> { { "orderId", "order-2024-001" } }
};

await repository.AddAsync(newEntry);
Console.WriteLine($"Added audit entry: {newEntry.Id}");

// Get an audit entry by ID
var entryById = await repository.GetByIdAsync(newEntry.Id);
if (entryById != null)
{
    Console.WriteLine($"Retrieved entry: {entryById.Description}");
}

// Check if an audit entry exists
bool exists = await repository.ExistsAsync(newEntry.Id);
Console.WriteLine($"Entry exists: {exists}");

// Get all audit entries
var allEntries = await repository.GetAllAsync();
Console.WriteLine($"Total audit entries: {allEntries.Count}");

// Get audit entries for a specific workflow instance
var instanceEntries = await repository.GetByInstanceIdAsync("wf-order-processing-001");
Console.WriteLine($"Entries for instance: {instanceEntries.Count}");

// Get audit entries by event type
var startedEntries = await repository.GetByEventTypeAsync("ActivityStarted");
Console.WriteLine($"ActivityStarted events: {startedEntries.Count}");

// Get audit entries by severity level
var errorEntries = await repository.GetBySeverityAsync("Error");
Console.WriteLine($"Error events: {errorEntries.Count}");

// Get error audit entries
var errors = await repository.GetErrorsAsync();
Console.WriteLine($"Total errors: {errors.Count}");

// Get audit entries within a date range
var recentEntries = await repository.GetByDateRangeAsync(
    DateTime.UtcNow.AddHours(-1),
    DateTime.UtcNow
);
Console.WriteLine($"Recent entries: {recentEntries.Count}");

// Get recent audit entries for an instance
var recentForInstance = await repository.GetRecentForInstanceAsync("wf-order-processing-001", 5);
Console.WriteLine($"Recent entries for instance: {recentForInstance.Count}");

// Get audit entries by activity ID
var activityEntries = await repository.GetByActivityIdAsync("validate-order");
Console.WriteLine($"Entries for activity: {activityEntries.Count}");

// Get audit entries with pagination
var pagedResult = await repository.GetPagedAsync(page: 1, pageSize: 20);
Console.WriteLine($"Page 1: {pagedResult.Items.Count} entries, Total: {pagedResult.Total} entries");

// Count total audit entries
var totalCount = await repository.CountAsync();
Console.WriteLine($"Total audit count: {totalCount}");

// Get filtered and paginated audit entries
var filteredResult = await repository.GetFilteredAndPagedAsync(
    workflowId: "order-processing",
    instanceId: "wf-order-processing-001",
    eventType: "ActivityStarted",
    severity: "Information",
    fromDate: DateTime.UtcNow.AddDays(-1),
    take: 50
);
Console.WriteLine($"Filtered entries: {filteredResult.Items.Count} of {filteredResult.Total}");

// Update an audit entry (typically immutable, but supported for metadata updates)
if (entryById != null)
{
    entryById.Description = "Updated description";
    await repository.UpdateAsync(entryById);
    Console.WriteLine("Audit entry updated");
}

// Delete an audit entry
// await repository.DeleteAsync(newEntry.Id);

// Clear audit log for a specific instance
// await repository.ClearInstanceAsync("wf-order-processing-001");

// Clear all audit logs (use with caution!)
// await repository.ClearAsync();
```