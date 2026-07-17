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
    await cacheService.SetAsync(`key-{item}`, item, TimeSpan.FromMinutes(10));
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

## WorkflowInstanceController

The `WorkflowInstanceController` class provides REST API endpoints for managing workflow instances through HTTP endpoints. It handles execution, state transitions, retry logic, and instance lifecycle operations including creation, retrieval, listing, retrying, terminating, and retrieving execution history for workflow instances. All endpoints require authentication and audit-log mutations.

The controller exposes endpoints for executing workflows, retrieving instance details, listing instances with filtering and pagination, retrying failed instances, terminating running instances, and retrieving execution history for debugging purposes.

Example usage:

```csharp
using DotNetWorkflowEngine.Controllers;
using DotNetWorkflowEngine.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Setup services (typically via DI in ASP.NET Core)
var services = new ServiceCollection();
services.AddWorkflowServices();
services.AddControllers();
services.AddLogging();
var serviceProvider = services.BuildServiceProvider();

// Create controller instance with required services
var executionService = serviceProvider.GetRequiredService<WorkflowExecutionService>();
var auditService = serviceProvider.GetRequiredService<AuditService>();
var logger = serviceProvider.GetRequiredService<ILogger<WorkflowInstanceController>>();
var controller = new WorkflowInstanceController(executionService, auditService, logger);

// Example: Execute a workflow instance
var executeResult = await controller.ExecuteWorkflow("order-processing-workflow", 
    new Dictionary<string, object> { { "orderId", "order-2024-001" }, { "customerId", "cust-12345" } });
if (executeResult is AcceptedResult acceptedResult)
{
    var instanceData = acceptedResult.Value as dynamic;
    Console.WriteLine($"Workflow instance created: {instanceData?.instanceId}");
    Console.WriteLine($"Status: {instanceData?.Status}");
}

// Example: Get a specific workflow instance
var instanceResult = await controller.GetInstance("wf-order-processing-001");
if (instanceResult is OkObjectResult okResult)
{
    var instance = okResult.Value as WorkflowInstance;
    Console.WriteLine($"Retrieved instance: {instance?.Id} - Status: {instance?.Status}");
}

// Example: List workflow instances with filtering and pagination
var listResult = await controller.ListInstances(
    workflowId: "order-processing-workflow",
    status: "Active",
    skip: 0,
    take: 50);
if (listResult is OkObjectResult okListResult)
{
    var instances = okListResult.Value as List<WorkflowInstance>;
    Console.WriteLine($"Found {instances?.Count} active instances for order-processing-workflow");
}

// Example: Retry a failed workflow instance
var retryResult = await controller.RetryInstance("wf-order-processing-failed-001");
if (retryResult is AcceptedResult acceptedRetryResult)
{
    var instance = acceptedRetryResult.Value as WorkflowInstance;
    Console.WriteLine($"Retry initiated for instance: {instance?.Id}");
}

// Example: Terminate a running workflow instance
var terminateResult = await controller.TerminateInstance("wf-order-processing-long-running-001", "Timeout exceeded");
if (terminateResult is NoContentResult)
{
    Console.WriteLine("Workflow instance terminated successfully");
}

// Example: Get execution history for a workflow instance
var historyResult = await controller.GetInstanceHistory("wf-order-processing-001");
if (historyResult is OkObjectResult okHistoryResult)
{
    var history = okHistoryResult.Value as List<ActivityResult>;
    Console.WriteLine($"Execution history contains {history?.Count} activity results");
}
```

## WorkflowController

The `WorkflowController` class provides a REST API controller for managing workflow definitions through HTTP endpoints. It exposes CRUD operations for workflow definitions, allowing clients to create, read, update, and delete workflows via standard REST conventions. The controller integrates with the workflow engine's services to provide complete workflow lifecycle management through a web interface.

The controller supports operations for managing workflow definitions including listing all workflows, retrieving specific workflows by ID, creating new workflow definitions, updating existing ones, and deleting workflows that are no longer needed.

Example usage:

```csharp

## ValidationFilterValidation

The `ValidationFilterValidation` static class provides comprehensive validation extension methods for `ValidationErrorResponse` and validation-related types. It helps validate filter responses, validation attributes, and common .NET types like strings and DateTime values, ensuring data integrity throughout the workflow engine.

This validation utility is particularly useful for API controllers and filter middleware that need to validate incoming data and provide detailed error responses to clients.

Example usage:

```csharp
using DotNetWorkflowEngine.Filters;
using DotNetWorkflowEngine.Models;
using System;
using System.Collections.Generic;

// Create a validation error response
var validationResponse = new ValidationErrorResponse
{
    Message = "Validation failed",
    Errors = new List<KeyValuePair<string, string[]>>
    {
        new KeyValuePair<string, string[]>("email", new[] { "Email is required" }),
        new KeyValuePair<string, string[]>("age", new[] { "Age must be at least 18" })
    },
    Timestamp = DateTime.UtcNow
};

// Validate the response
var validationProblems = validationResponse.Validate();

if (validationProblems.Count > 0)
{
    Console.WriteLine("Validation problems found:");
    foreach (var problem in validationProblems)
    {
        Console.WriteLine($"- {problem}");
    }
}

// Check if valid using the IsValid extension method
bool isValid = validationResponse.IsValid();
Console.WriteLine($"Is valid: {isValid}");

// Use EnsureValid to throw an exception if invalid
try
{
    validationResponse.EnsureValid();
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}

// Validate a string value
string? userInput = "   ";
var stringProblems = userInput.Validate("userInput");
Console.WriteLine($"String validation problems: {stringProblems.Count}");

// Validate a DateTime value
DateTime invalidDate = default;
var dateProblems = invalidDate.Validate("expirationDate");
Console.WriteLine($"DateTime validation problems: {dateProblems.Count}");
```

## ErrorHandlingExampleValidation

The `ErrorHandlingExampleValidation` static class provides validation helpers for error handling workflow data models. It offers extension methods to validate `ProcessingRequest` instances and their processing rules, ensuring data integrity before workflow execution.

This validation utility is useful for validating processing requests with configurable constraints, checking rule types, and validating positive integer values within processing rules.

Example usage:

```csharp
using DotNetWorkflowEngine.Examples;
using System;
using System.Collections.Generic;

// Create a valid processing request
var request = new ProcessingRequest
{
    DataSourceUrl = "https://api.example.com/data",
    ProcessingRules = new Dictionary<string, object>
    {
        { "timeout", 30 },
        { "maxRetries", "3" },
        { "batchSize", 100 }
    }
};

// Validate the request - returns list of problems (empty if valid)
var problems = request.Validate();
Console.WriteLine($"Validation problems: {problems.Count}"); // 0

// Check if valid using IsValid extension method
bool isValid = request.IsValid();
Console.WriteLine($"Is valid: {isValid}"); // True

// Use EnsureValid to throw if invalid
request.EnsureValid(); // No exception thrown

// Validate with length and count constraints
var constrainedProblems = request.Validate(
    maxDataSourceUrlLength: 200,
    maxProcessingRulesCount: 10
);
Console.WriteLine($"Constrained validation problems: {constrainedProblems.Count}"); // 0

// Validate processing rules specifically
var ruleProblems = request.ValidateProcessingRules();
Console.WriteLine($"Rule validation problems: {ruleProblems.Count}"); // 0

// Validate a specific rule type
var typeProblems = request.ProcessingRules.ValidateRuleType("timeout", typeof(int));
Console.WriteLine($"Type validation problems: {typeProblems.Count}"); // 0

// Validate a positive integer rule with minimum value
var positiveIntProblems = request.ProcessingRules.ValidatePositiveIntegerRule("batchSize", minValue: 1);
Console.WriteLine($"Positive integer validation problems: {positiveIntProblems.Count}"); // 0
```

## HealthController

The `HealthController` provides health monitoring endpoints for the workflow engine, implementing liveness and readiness probes suitable for container orchestration systems like Kubernetes. It exposes three endpoints for monitoring application health:

- **Liveness** (`GET /health/liveness`): Verifies the application process is running without performing dependency checks
- **Readiness** (`GET /health/readiness`): Checks if the application is ready to receive traffic by validating critical dependencies (database, cache, metrics)
- **Health** (`GET /health`): Provides comprehensive health status including system resources, database connectivity, cache availability, and configuration validation

The controller returns structured JSON responses with status information, timestamps, and detailed component checks.

Example usage:

```csharp
using DotNetWorkflowEngine.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

// Setup services (typically via DI in ASP.NET Core)
var services = new ServiceCollection();
services.AddWorkflowServices();
services.AddControllers();
services.AddLogging();
var serviceProvider = services.BuildServiceProvider();

// Create controller instance with required services
var metrics = serviceProvider.GetRequiredService<IWorkflowMetrics>();
var options = serviceProvider.GetRequiredService<IOptions<WorkflowEngineOptions>>();
var logger = serviceProvider.GetRequiredService<ILogger<HealthController>>();
var controller = new HealthController(serviceProvider, metrics, options, logger);

// Example: Check liveness (application is running)
var livenessResult = await controller.Liveness();
if (livenessResult is OkObjectResult okLivenessResult)
{
    var response = okLivenessResult.Value as dynamic;
    Console.WriteLine($"Liveness status: {response.status}");
    Console.WriteLine($"Uptime: {response.uptime}");
}

// Example: Check readiness (application is ready to receive traffic)
var readinessResult = await controller.Readiness();
if (readinessResult is OkObjectResult okReadinessResult)
{
    var response = okReadinessResult.Value as dynamic;
    Console.WriteLine($"Readiness status: {response.status}");
    Console.WriteLine($"Components: {string.Join(", ", response.components.Keys)}");
}

// Example: Get comprehensive health status
var healthResult = await controller.Health();
if (healthResult is OkObjectResult okHealthResult)
{
    var response = okHealthResult.Value as dynamic;
    Console.WriteLine($"Overall health: {response.overallStatus}");
    Console.WriteLine($"Status: {response.status}");
    Console.WriteLine($"Timestamp: {response.timestamp}");
    Console.WriteLine($"Components checked: {response.components.Count}");
}
```

```csharp

## AuditController

The `AuditController` class provides REST API endpoints for audit trail management and compliance reporting. It offers comprehensive read-only access to all audit log entries generated by the workflow engine, enabling debugging, monitoring, and regulatory compliance. Audit logs are immutable and cannot be modified or deleted through these endpoints.

The controller supports filtering audit logs by workflow ID, instance ID, action type, actor, and date range. It provides endpoints for retrieving individual log entries, workflow-specific logs, instance-specific logs, summary statistics, and exporting logs in various formats (JSON, CSV).

Example usage:

```csharp
using DotNetWorkflowEngine.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Setup services (typically via DI in ASP.NET Core)
var services = new ServiceCollection();
services.AddWorkflowServices();
services.AddControllers();
services.AddLogging();
var serviceProvider = services.BuildServiceProvider();

// Create controller instance with required services
var auditService = serviceProvider.GetRequiredService<AuditService>();
var auditRepository = serviceProvider.GetRequiredService<IAuditRepository>();
var logger = serviceProvider.GetRequiredService<ILogger<AuditController>>();
var controller = new AuditController(auditService, auditRepository, logger);

// Example: Get all audit logs with filtering and pagination
var allLogsResult = await controller.GetAuditLogs(
    workflowId: "order-processing-workflow",
    action: "ActivityExecuted",
    executedBy: "user@company.com",
    fromDate: DateTime.UtcNow.AddDays(-7),
    toDate: DateTime.UtcNow,
    skip: 0,
    take: 100
);
if (allLogsResult is OkObjectResult okResult)
{
    var logs = okResult.Value as IEnumerable<AuditLogEntry>;
    Console.WriteLine($"Found {logs?.Count()} audit log entries");
}

// Example: Get audit logs for a specific workflow
var workflowLogsResult = await controller.GetWorkflowAuditLog("order-processing-workflow", skip: 0, take: 50);
if (workflowLogsResult is OkObjectResult workflowOkResult)
{
    var workflowLogs = workflowOkResult.Value as IEnumerable<AuditLogEntry>;
    Console.WriteLine($"Workflow has {workflowLogs?.Count()} audit entries");
}

// Example: Get audit logs for a specific workflow instance
var instanceLogsResult = await controller.GetInstanceAuditLog("wf-order-processing-001", skip: 0, take: 50);
if (instanceLogsResult is OkObjectResult instanceOkResult)
{
    var instanceLogs = instanceOkResult.Value as IEnumerable<AuditLogEntry>;
    Console.WriteLine($"Instance has {instanceLogs?.Count()} audit entries");
}

// Example: Get a specific audit log entry by ID
var logEntryResult = await controller.GetAuditLogEntry("audit-entry-12345");
if (logEntryResult is OkObjectResult entryOkResult)
{
    var entry = entryOkResult.Value as AuditLogEntry;
    Console.WriteLine($"Retrieved audit entry: {entry?.Description}");
}

// Example: Get audit statistics for monitoring
var statsResult = await controller.GetAuditStatistics(
    fromDate: DateTime.UtcNow.AddDays(-30),
    toDate: DateTime.UtcNow
);
if (statsResult is OkObjectResult statsOkResult)
{
    var stats = statsOkResult.Value as dynamic;
    Console.WriteLine($"Total entries: {stats.totalEntries}");
    Console.WriteLine($"Actions by type: {string.Join(", ", stats.entriesByAction.Select(kvp => $"${kvp.Key}: {kvp.Value}"))}");
}

// Example: Export audit logs in CSV format
var exportResult = await controller.ExportAuditLogs(
    format: "csv",
    workflowId: "order-processing-workflow",
    fromDate: DateTime.UtcNow.AddDays(-7)
);
if (exportResult is FileResult fileResult)
{
    var fileContent = await new System.IO.StreamReader(fileResult.FileStream).ReadToEndAsync();
    Console.WriteLine($"Exported {fileContent.Split('\n').Length - 1} CSV lines");
}
```

## WorkflowController

The `WorkflowController` class provides a REST API controller for managing workflow definitions through HTTP endpoints. It exposes CRUD operations for workflow definitions, allowing clients to create, read, update, and delete workflows via standard REST conventions. The controller integrates with the workflow engine's services to provide complete workflow lifecycle management through a web interface.

The controller supports operations for managing workflow definitions including listing all workflows, retrieving specific workflows by ID, creating new workflow definitions, updating existing ones, and deleting workflows that are no longer needed.

Example usage:

```csharp

## ValidationFilterValidation

The `ValidationFilterValidation` static class provides comprehensive validation extension methods for `ValidationErrorResponse` and validation-related types. It helps validate filter responses, validation attributes, and common .NET types like strings and DateTime values, ensuring data integrity throughout the workflow engine.

This validation utility is particularly useful for API controllers and filter middleware that need to validate incoming data and provide detailed error responses to clients.

Example usage:

```csharp
using DotNetWorkflowEngine.Filters;
using DotNetWorkflowEngine.Models;
using System;
using System.Collections.Generic;

// Create a validation error response
var validationResponse = new ValidationErrorResponse
{
    Message = "Validation failed",
    Errors = new List<KeyValuePair<string, string[]>>
    {
        new KeyValuePair<string, string[]>("email", new[] { "Email is required" }),
        new KeyValuePair<string, string[]>("age", new[] { "Age must be at least 18" })
    },
    Timestamp = DateTime.UtcNow
};

// Validate the response
var validationProblems = validationResponse.Validate();

if (validationProblems.Count > 0)
{
    Console.WriteLine("Validation problems found:");
    foreach (var problem in validationProblems)
    {
        Console.WriteLine($"- {problem}");
    }
}

// Check if valid using the IsValid extension method
bool isValid = validationResponse.IsValid();
Console.WriteLine($"Is valid: {isValid}");

// Use EnsureValid to throw an exception if invalid
try
{
    validationResponse.EnsureValid();
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}

// Validate a string value
string? userInput = "   ";
var stringProblems = userInput.Validate("userInput");
Console.WriteLine($"String validation problems: {stringProblems.Count}");

// Validate a DateTime value
DateTime invalidDate = default;
var dateProblems = invalidDate.Validate("expirationDate");
Console.WriteLine($"DateTime validation problems: {dateProblems.Count}");
```

## HealthController

The `HealthController` provides health monitoring endpoints for the workflow engine, implementing liveness and readiness probes suitable for container orchestration systems like Kubernetes. It exposes three endpoints for monitoring application health:

- **Liveness** (`GET /health/liveness`): Verifies the application process is running without performing dependency checks
- **Readiness** (`GET /health/readiness`): Checks if the application is ready to receive traffic by validating critical dependencies (database, cache, metrics)
- **Health** (`GET /health`): Provides comprehensive health status including system resources, database connectivity, cache availability, and configuration validation

The controller returns structured JSON responses with status information, timestamps, and detailed component checks.

Example usage:

```csharp
using DotNetWorkflowEngine.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

// Setup services (typically via DI in ASP.NET Core)
var services = new ServiceCollection();
services.AddWorkflowServices();
services.AddControllers();
services.AddLogging();
var serviceProvider = services.BuildServiceProvider();

// Create controller instance with required services
var metrics = serviceProvider.GetRequiredService<IWorkflowMetrics>();
var options = serviceProvider.GetRequiredService<IOptions<WorkflowEngineOptions>>();
var logger = serviceProvider.GetRequiredService<ILogger<HealthController>>();
var controller = new HealthController(serviceProvider, metrics, options, logger);

// Example: Check liveness (application is running)
var livenessResult = await controller.Liveness();
if (livenessResult is OkObjectResult okLivenessResult)
{
    var response = okLivenessResult.Value as dynamic;
    Console.WriteLine($"Liveness status: {response.status}");
    Console.WriteLine($"Uptime: {response.uptime}");
}

// Example: Check readiness (application is ready to receive traffic)
var readinessResult = await controller.Readiness();
if (readinessResult is OkObjectResult okReadinessResult)
{
    var response = okReadinessResult.Value as dynamic;
    Console.WriteLine($"Readiness status: {response.status}");
    Console.WriteLine($"Components: {string.Join(", ", response.components.Keys)}");
}

// Example: Get comprehensive health status
var healthResult = await controller.Health();
if (healthResult is OkObjectResult okHealthResult)
{
    var response = okHealthResult.Value as dynamic;
    Console.WriteLine($"Overall health: {response.overallStatus}");
    Console.WriteLine($"Status: {response.status}");
    Console.WriteLine($"Timestamp: {response.timestamp}");
    Console.WriteLine($"Components checked: {response.components.Count}");
}
```

```csharp