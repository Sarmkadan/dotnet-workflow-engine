# DotNet Workflow Engine

A powerful, production-ready visual workflow engine for .NET with BPMN-like DSL, parallel execution support, retry policies, and comprehensive audit trails.

## Features

- **BPMN-like DSL** - Define workflows using familiar BPMN concepts (activities, transitions, gateways)
- **Parallel Execution** - Support for parallel activity execution and fork/join patterns
- **Retry Policies** - Built-in retry mechanisms with exponential backoff, fixed delay, and linear backoff strategies
- **Audit Trail** - Comprehensive audit logging of all workflow events and state changes
- **Type-Safe** - Full type safety with C# and .NET 10
- **Fluent API** - Build workflows using a fluent builder pattern
- **Extensible** - Register custom activity handlers for domain-specific logic
- **Async/Await** - Full async support throughout the engine

## Quick Start

### Installation

```bash
dotnet add package DotNetWorkflowEngine
```

### Basic Usage

```csharp
using DotNetWorkflowEngine.Configuration;
using DotNetWorkflowEngine.Services;
using DotNetWorkflowEngine.Utilities;
using Microsoft.Extensions.DependencyInjection;

// Configure services
var services = new ServiceCollection();
services.AddWorkflowEngine(options =>
{
    options.EnableAuditLogging = true;
    options.DefaultActivityTimeoutSeconds = 300;
});

var provider = services.BuildServiceProvider();

// Get services
var workflowService = provider.GetRequiredService<WorkflowDefinitionService>();
var executionService = provider.GetRequiredService<WorkflowExecutionService>();

// Build a workflow
var builder = WorkflowBuilder.CreateSerial(
    "order-processing",
    "Order Processing Workflow",
    workflowService,
    "Validate Order",
    "Process Payment",
    "Ship Order"
);

var workflow = builder.BuildAndRegister();

// Publish and execute
workflowService.PublishWorkflow(workflow.Id);
var instance = executionService.CreateInstance(workflow.Id);
await executionService.StartAsync(instance.Id);
```

## Architecture

### Core Components

- **Workflow** - Represents a workflow definition with activities and transitions
- **WorkflowInstance** - Runtime instance of an executing workflow
- **Activity** - Individual task/step in a workflow
- **Transition** - Connection between activities with optional conditions
- **ExecutionContext** - Runtime context containing variables and state
- **AuditLogEntry** - Immutable record of workflow events

### Services

- **WorkflowDefinitionService** - Manages workflow definitions
- **WorkflowExecutionService** - Executes workflows and manages instances
- **ActivityService** - Executes activities with retry logic
- **RetryPolicyService** - Manages retry configurations
- **AuditService** - Tracks and retrieves workflow events

### Data Layer

- **WorkflowRepository** - Persists workflow definitions
- **WorkflowInstanceRepository** - Persists workflow instances
- **AuditRepository** - Persists audit logs
- **DatabaseContext** - Manages connections and transactions

## Key Features

### Retry Policies

```csharp
var retryService = provider.GetRequiredService<RetryPolicyService>();

// Exponential backoff
var config = RetryPolicyConfig.CreateExponentialBackoff(
    maxAttempts: 5,
    initialDelayMs: 1000,
    maxDelayMs: 300000
);

// Fixed delay
var config = RetryPolicyConfig.CreateFixedDelay(
    maxAttempts: 3,
    delayMs: 2000
);
```

### Audit Logging

```csharp
var auditService = provider.GetRequiredService<AuditService>();
var auditLog = auditService.GetAuditLog(instanceId);
var recentEntries = auditService.GetRecentAuditLog(instanceId, count: 10);
var csvExport = auditService.ExportAuditLogAsCsv(instanceId);
```

### Custom Activity Handlers

```csharp
class EmailActivityHandler : ActivityService.IActivityHandler
{
    public async Task<Dictionary<string, object?>> ExecuteAsync(
        Activity activity, 
        ExecutionContext context)
    {
        var email = context.GetActivityInput("email");
        // Send email logic
        return new Dictionary<string, object?> 
        { 
            ["sent"] = true,
            ["timestamp"] = DateTime.UtcNow
        };
    }
}

activityService.RegisterHandler("SendEmail", new EmailActivityHandler());
```

### Workflow Builder

```csharp
var workflow = new WorkflowBuilder("my-workflow", "My Workflow", workflowService)
    .WithDescription("A complex workflow")
    .AddTaskActivity("task1", "First Task", "Handler1")
    .AddTaskActivity("task2", "Second Task", "Handler2")
    .AddTaskActivity("task3", "Third Task", "Handler3")
    .AddTransition("task1", "task2")
    .AddTransition("task2", "task3", condition: "${approved}")
    .WithStartActivity("task1")
    .WithEndActivity("task3")
    .BuildAndRegister();
```

## Configuration

```csharp
services.AddWorkflowEngine(options =>
{
    options.ConnectionString = "Data Source=workflow.db";
    options.EnableAuditLogging = true;
    options.MaxConcurrentWorkflows = 100;
    options.DefaultActivityTimeoutSeconds = 300;
    options.ValidateWorkflowsOnLoad = true;
    options.DefaultRetryPolicy = RetryPolicyConfig.CreateExponentialBackoff(3);
});
```

## File Structure

```
DotNetWorkflowEngine/
├── Models/                 # Domain models
├── Services/              # Business logic services
├── Data/
│   ├── Repositories/      # Data access layer
│   └── Context/           # Database context
├── Configuration/         # DI and configuration
├── Enums/                 # Enumerations
├── Constants/             # Shared constants
├── Exceptions/            # Custom exceptions
├── Utilities/             # Helper utilities
└── Program.cs             # Entry point
```

## License

MIT License - See LICENSE file for details

## Author

Vladyslav Zaiets
https://sarmkadan.com

## Contributing

Contributions are welcome! Please ensure all code follows the project style and includes proper documentation.
