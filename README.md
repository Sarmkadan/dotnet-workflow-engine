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