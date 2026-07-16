# dotnet-workflow-engine

[![Build Status](https://dev.azure.com/.../dotnet-workflow-engine/_apis/build/status/...)](https://dev.azure.com/.../dotnet-workflow-engine/_build/latest?definitionId=...)

The dotnet-workflow-engine is a lightweight, extensible workflow engine written in C#. It supports parallel execution, conditional branching, retry policies, and stateful activities. The engine is designed to be easily integrated into existing .NET applications and can be extended with custom activity handlers.

## Architecture

See [docs/architecture.md](docs/architecture.md) for the full picture: module breakdown,
the execution flow (definition -> publish -> instance -> recursive graph walk), design
decisions with their trade-offs, extension points (`IActivityHandler`, `IEventBus`,
`IAuditRepository`, `IOutputFormatter`) and the honest list of current limitations.
Short version: everything is in-memory today, handlers plug in per activity type, retry
and routing are handled centrally by the engine.

## AdvancedIntegrationTests

The `AdvancedIntegrationTests` class contains comprehensive integration tests for verifying advanced workflow engine scenarios and complex workflows. These tests cover a range of features, including parallel workflow execution, error handling with retry policies, state preservation across activities, and conditional routing.

Example usage:
```csharp
var advancedTests = new AdvancedIntegrationTests();
await advancedTests.ComplexWorkflow_WithParallelPaths_ExecutesSuccessfully();
await advancedTests.WorkflowWithErrorHandling_RecoverableError_CompletesSuccessfully();
await advancedTests.LongRunningWorkflow_PreservesStateAcrossActivities();
await advancedTests.WorkflowWithMultipleInstances_EachMaintainsOwnState();
await advancedTests.WorkflowWithConditionalRouting_SelectsCorrectPathBasedOnContext();
advancedTests.WorkflowLifecycle_FullCycle_StateTransitionsCorrectly();
await advancedTests.ActivityWithTimeout_CompletesWithinTimeLimit();
advancedTests.WorkflowBuilder_CreateSerialWorkflow_BuildsValidWorkflow();
advancedTests.WorkflowSerialization_RoundTrip_PreservesStructure();
```

## SerializationHelperTests

The `SerializationHelperTests` class demonstrates how to use the `SerializationHelper` utility to serialize and deserialize workflow engine objects, perform deep cloning, merge objects, and validate JSON. It covers typical scenarios such as converting activities to JSON, handling nulls, and ensuring data integrity across round‑trips.

Example usage:
```csharp
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Utilities;

// Create an activity instance
var activity = new Activity
{
    Id = "act-1",
    Name = "Sample Activity",
    TimeoutSeconds = 45,
    MaxRetries = 3
};

// Serialize to JSON
string json = SerializationHelper.ToJson(activity);
Console.WriteLine(json); // {"id":"act-1","name":"Sample Activity","timeoutSeconds":45,"maxRetries":3}

// Deserialize back to an object
var deserialized = SerializationHelper.FromJson<Activity>(json);
Console.WriteLine(deserialized?.Name); // Sample Activity

// Deep clone the activity
var clone = SerializationHelper.DeepClone(activity);
clone!.Name = "Cloned Activity";
Console.WriteLine(activity.Name); // Sample Activity (unchanged)

// Merge two activities (second overrides first)
var updated = new Activity { Id = "act-1", Name = "Updated Activity", MaxRetries = 5 };
var merged = SerializationHelper.Merge(activity, updated);
Console.WriteLine(merged.Name); // Updated Activity
Console.WriteLine(merged.MaxRetries); // 5
```

## IWebhookHandler

The `IWebhookHandler` interface defines the contract for handling webhook events from external services. It provides properties to configure webhook endpoints, event subscriptions, authentication, and custom headers, along with methods to process incoming webhook payloads and track delivery attempts.

Example usage:

```csharp
using DotNetWorkflowEngine.Integration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Create a webhook handler for workflow events
var webhookHandler = new WebhookHandler
{
    Id = "wh-7f3b9c2e",
    Url = "https://api.example.com/webhooks/workflow-events",
    Events = new List<string> { "WorkflowStarted", "WorkflowCompleted", "WorkflowFailed" },
    Secret = "my-webhook-secret-key",
    CustomHeaders = new Dictionary<string, string>
    {
        { "X-Custom-Header", "custom-value" },
        { "Authorization", "Bearer token-here" }
    },
    Active = true,
    EventType = "WorkflowEvent",
    WorkflowId = "wf-order-processing",
    Timestamp = DateTime.UtcNow
};

// Process a webhook event
var webhookEvent = new WebhookEvent
{
    Id = "evt-12345",
    WebhookId = webhookHandler.Id,
    EventType = "WorkflowStarted",
    AttemptedAt = DateTime.UtcNow,
    Data = new Dictionary<string, object>
    {
        { "workflowId", "wf-order-processing" },
        { "instanceId", "inst-abc123" },
        { "timestamp", DateTime.UtcNow.ToString("o") }
    }
};

// Track webhook delivery attempt
var deliveryAttempt = new WebhookDeliveryAttempt
{
    Id = "att-98765",
    WebhookId = webhookHandler.Id,
    EventType = "WorkflowStarted",
    AttemptedAt = DateTime.UtcNow,
    StatusCode = 200,
    Success = true,
    ErrorMessage = null
};

Console.WriteLine($"Webhook configured for events: {string.Join(", ", webhookHandler.Events)}");
Console.WriteLine($"Webhook active: {webhookHandler.Active}");
Console.WriteLine($"Last delivery status: {(deliveryAttempt.Success ? "Success" : "Failed")}");
```

## IHttpClientFactory

The `IHttpClientFactory` interface provides a standardized way to create and manage HTTP clients with built-in connection pooling, timeout management, and retry policies. It simplifies external API communication by allowing named clients with custom configurations including base URLs, default headers, and retry settings.

Example usage:

```csharp
using DotNetWorkflowEngine.Integration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

// Create HTTP client configurations
var defaultConfig = new HttpClientConfig
{
    BaseUrl = "https://api.example.com/v1",
    TimeoutSeconds = 60,
    DefaultHeaders = new Dictionary<string, string>
    {
        { "Accept", "application/json" },
        { "User-Agent", "dotnet-workflow-engine" }
    },
    MaxRetries = 5,
    RetryDelayMs = 2000
};

var analyticsConfig = new HttpClientConfig
{
    BaseUrl = "https://analytics.example.com/api",
    TimeoutSeconds = 30,
    DefaultHeaders = new Dictionary<string, string>
    {
        { "Authorization", "Bearer analytics-token-123" },
        { "X-API-Key", "analytics-secret" }
    },
    MaxRetries = 3,
    RetryDelayMs = 1000
};

// Register clients (typically done in DI setup)
var httpClientFactory = new StandardHttpClientFactory(
    // In real usage, this would be injected via DI
    new HttpClient(),
    new Logger<StandardHttpClientFactory>()
);

httpClientFactory.RegisterClient("default", defaultConfig);
httpClientFactory.RegisterClient("analytics", analyticsConfig);

// Get configured clients
var defaultClient = httpClientFactory.GetClient("default");
var analyticsClient = httpClientFactory.GetClient("analytics");

// Use with retry helper methods
var response = await defaultClient.GetWithRetryAsync("/workflows");
if (response.IsSuccessStatusCode)
{
    var content = await response.Content.ReadAsStringAsync();
    Console.WriteLine(content);
}

// Make POST request with retry
var postResponse = await analyticsClient.PostWithRetryAsync(
    "/events",
    new StringContent("{\"eventType\":\"workflow.started\"}", System.Text.Encoding.UTF8, "application/json")
);
```

## WorkflowValidatorTests

`WorkflowValidatorTests` provides a suite of unit tests that verify the correctness of the workflow validation logic. Each public method exercises a specific validation rule, ensuring that workflows, activities, and transitions are checked for required fields, consistency, and logical correctness.

Typical usage involves creating an instance of the test class and invoking the desired test methods directly (for example, when debugging or running tests programmatically):

```csharp
using DotNetWorkflowEngine.Tests;

// Create the test class instance
var validatorTests = new WorkflowValidatorTests();

// Run a selection of validation scenarios
validatorTests.ValidateWorkflow_ValidWorkflow_ReturnsValid();
validatorTests.ValidateWorkflow_MissingId_ReturnsError();
validatorTests.ValidateWorkflow_MissingName_ReturnsError();
validatorTests.ValidateWorkflow_NoActivities_ReturnsError();
validatorTests.ValidateWorkflow_InvalidActivity_ReturnsError();
validatorTests.ValidateWorkflow_StartActivityNotFound_ReturnsError();
validatorTests.ValidateWorkflow_EndActivityNotFound_ReturnsError();
validatorTests.ValidateWorkflow_NoStartActivity_ReturnsWarning();
validatorTests.ValidateWorkflow_InvalidTransition_ReturnsError();

validatorTests.ValidateActivity_ValidActivity_ReturnsValid();
validatorTests.ValidateActivity_MissingId_ReturnsError();
validatorTests.ValidateActivity_MissingName_ReturnsError();
validatorTests.ValidateActivity_InvalidTimeout_ReturnsError();
validatorTests.ValidateActivity_NegativeRetries_ReturnsError();
validatorTests.ValidateActivity_RetriesWithoutPolicy_ReturnsWarning();

validatorTests.ValidateTransition_ValidTransition_ReturnsValid();
validatorTests.ValidateTransition_MissingFromActivity_ReturnsError();
validatorTests.ValidateTransition_MissingToActivity_ReturnsError();
validatorTests.ValidateTransition_FromActivityNotFound_ReturnsError();
validatorTests.ValidateTransition_ToActivityNotFound_ReturnsError();
```

These calls execute the underlying assertions (via FluentAssertions) and will throw if any validation rule fails, making them useful for ad‑hoc verification during development.

## IWorkflowJobProcessor

The `IWorkflowJobProcessor` interface provides a background job processing mechanism for executing workflow instances asynchronously. It manages a queue of workflow jobs, processes them with automatic retry on failure, and tracks statistics about job execution. This interface is typically used to decouple workflow execution from the main request/response cycle, enabling better scalability and reliability.

Example usage:

```csharp
using DotNetWorkflowEngine.BackgroundJobs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Create a workflow job to be processed in the background
var job = new WorkflowJob
{
    Id = "job-7f3b9c2e-4567-89ab-cdef-123456789abc",
    WorkflowId = "order-processing-workflow",
    InstanceId = null, // Will be set after processing starts
    InputData = new Dictionary<string, object>
    {
        { "orderId", "ORD-12345" },
        { "customerId", "CUST-67890" },
        { "amount", 149.99 },
        { "priority", "high" }
    },
    CreatedAt = DateTime.UtcNow,
    ScheduledFor = null, // Process immediately
    RetryCount = 0,
    Priority = "high"
};

// Get the job processor (typically injected via DI)
var jobProcessor = serviceProvider.GetRequiredService<IWorkflowJobProcessor>();

// Enqueue the job for background processing
await jobProcessor.EnqueueAsync(job);

Console.WriteLine($"Job enqueued: {job.Id}");

// Check how many jobs are pending
var pendingCount = await jobProcessor.GetPendingCountAsync();
Console.WriteLine($"Pending jobs: {pendingCount}");

// Get processing statistics
var stats = await jobProcessor.GetStatsAsync();
Console.WriteLine($"Total processed: {stats.TotalProcessed}");
Console.WriteLine($"Total failed: {stats.TotalFailed}");
Console.WriteLine($"Total retried: {stats.TotalRetried}");
Console.WriteLine($"Last processed: {stats.LastProcessedAt}");
Console.WriteLine($"Average processing time: {stats.AvgProcessingTime.TotalMilliseconds}ms");
```

## ValidationFilter

The `ValidationFilter` is an action filter that validates model state using DataAnnotations validation attributes. It automatically validates incoming request data against validation attributes on model classes and returns a structured error response when validation fails. The filter integrates with ASP.NET Core's validation pipeline and can be applied to controller actions or Razor Pages.

Example usage:

```csharp
using DotNetWorkflowEngine.Filters;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

public class CreateWorkflowRequest
{
    [Required(ErrorMessage = "Workflow name is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters")]
    public string? Name { get; set; }

    [Range(1, 365, ErrorMessage = "Timeout must be between 1 and 365 days")]
    public int TimeoutDays { get; set; }

    [AllowedValues("Draft", "Active", "Completed", "Archived", ErrorMessage = "Status must be one of: Draft, Active, Completed, Archived")]
    public string? Status { get; set; }
}

public class WorkflowController : ControllerBase
{
    [HttpPost("workflows")]
    [ValidationFilter]
    public async Task<IActionResult> CreateWorkflow([FromBody] CreateWorkflowRequest request)
    {
        // If we reach here, model validation passed
        return Ok(new { message = "Workflow created successfully", workflowId = Guid.NewGuid() });
    }
}

// Example of handling validation errors
public class CustomValidationFilter : ValidationFilter
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(kvp => kvp.Value?.Errors.Count > 0)
                .Select(kvp => new KeyValuePair<string, string[]>
                (
                    kvp.Key,
                    kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                ))
                .ToList();

            context.Result = new BadRequestObjectResult(new
            {
                message = "Validation failed",
                errors = errors,
                timestamp = DateTime.UtcNow
            });
            return;
        }

        await base.OnActionExecutionAsync(context, next);
    }
}
```

## AuditServiceTests

The `AuditServiceTests` class contains unit tests that verify the audit logging functionality of the workflow engine. It tests instance creation logging, activity tracking, and comprehensive filtering capabilities for retrieving audit logs by instance ID, activity ID, event type, severity, date range, actor, and pagination.

Example usage:

```csharp

## ExecutionContext

The `ExecutionContext` class provides runtime information and control for workflow and activity execution. It tracks workflow instance identifiers, maintains execution state and variables, handles activity input/output, and manages execution lifecycle including timing and error states.


Example usage:

```csharp
using DotNetWorkflowEngine.Models;
using System;
using System.Collections.Generic;

// Create an execution context for a workflow instance
var context = new ExecutionContext
{
    WorkflowInstanceId = "wf-order-processing-001",
    CorrelationId = "corr-7f3b9c2e-4567-89ab-cdef-123456789abc",
    ActivityId = "act-validate-order"
};

// Set variables for the workflow execution
context.SetVariable("customerId", "CUST-12345");
context.SetVariable("orderTotal", 299.99);
context.SetVariable("isPriority", true);

// Set activity input parameters
context.SetActivityInput("orderId", "ORD-54321");
context.SetActivityInput("validationRules", new List<string> { "fraud-check", "inventory-valid" });

// Access variables and input
var customerId = context.GetVariable<string>("customerId");
var orderTotal = context.GetVariable<decimal>("orderTotal");
var orderId = context.GetActivityInput("orderId");

// Track workflow state
context.State["orderStatus"] = "validating";
context.State["validationAttempts"] = 1;

// Mark activity as completed
context.SetActivityOutput("validationResult", "approved");
context.SetActivityOutput("riskScore", 0.15);

// Complete the execution
context.Complete();

Console.WriteLine($"Workflow {context.WorkflowInstanceId} completed in {context.ExecutionDurationMs}ms");
Console.WriteLine($"Status: {(context.IsActive ? "Active" : "Completed")}");
Console.WriteLine($"Order total: {orderTotal:C}");
Console.WriteLine($"Validation result: {context.GetActivityOutput("validationResult")}");
```

## WorkflowInstance

The `WorkflowInstance` class represents a runtime instance of a workflow definition. It tracks the execution state, context, and lifecycle of a workflow as it progresses through its activities. Each workflow instance maintains its own execution history, including completed activities, active activities, and any errors that may have occurred during execution.

Example usage:

```csharp
using DotNetWorkflowEngine.Models;
using System;
using System.Collections.Generic;

// Create a new workflow instance
var workflowInstance = new WorkflowInstance
{
    Id = "wf-inst-order-processing-001",
    WorkflowId = "order-processing-workflow",
    Status = WorkflowStatus.Created,
    CurrentActivityId = null,
    ExecutedActivities = new List<string>(),
    ActiveActivities = new List<string>(),
    Context = new Dictionary<string, object?>
    {
        { "customerId", "CUST-12345" },
        { "orderTotal", 299.99 },
        { "priority", true }
    },
    CreatedAt = DateTime.UtcNow,
    StartedAt = null,
    CompletedAt = null,
    ExecutionTimeMs = 0,
    ErrorMessage = null,
    CorrelationId = "corr-7f3b9c2e-4567-89ab-cdef-123456789abc",
    Metadata = new Dictionary<string, object?>
    {
        { "initiatedBy", "user@company.com" },
        { "source", "api" }
    },
    InitiatedBy = "user@company.com"
};

// Start the workflow instance
workflowInstance.Start();

Console.WriteLine($"Workflow instance created: {workflowInstance.Id}");
Console.WriteLine($"Status: {workflowInstance.Status}");
Console.WriteLine($"Created at: {workflowInstance.CreatedAt}");

// Simulate some activities being executed
workflowInstance.ExecutedActivities.Add("validate-order");
workflowInstance.ExecutedActivities.Add("check-inventory");
workflowInstance.ActiveActivities.Add("process-payment");
workflowInstance.CurrentActivityId = "process-payment";

// Update context during execution
workflowInstance.Context["paymentStatus"] = "pending";

// Complete the workflow successfully
workflowInstance.Complete();

Console.WriteLine($"Workflow completed: {workflowInstance.Id}");
Console.WriteLine($"Status: {workflowInstance.Status}");
Console.WriteLine($"Completed at: {workflowInstance.CompletedAt}");
Console.WriteLine($"Execution time: {workflowInstance.ExecutionTimeMs}ms");

// Alternatively, handle a failure
var failedInstance = new WorkflowInstance
{
    Id = "wf-inst-payment-failed-002",
    WorkflowId = "payment-workflow",
    Status = WorkflowStatus.Running,
    CurrentActivityId = "process-payment",
    ExecutedActivities = new List<string> { "validate-order" },
    ActiveActivities = new List<string> { "process-payment" },
    Context = new Dictionary<string, object?>
    {
        { "orderId", "ORD-54321" },
        { "amount", 99.99 }
    },
    CreatedAt = DateTime.UtcNow.AddMinutes(-5),
    StartedAt = DateTime.UtcNow.AddMinutes(-5),
    ExecutionTimeMs = 1250
};

failedInstance.Fail("Payment gateway timeout");

Console.WriteLine($"Workflow failed: {failedInstance.Id}");
Console.WriteLine($"Error: {failedInstance.ErrorMessage}");
Console.WriteLine($"Status: {failedInstance.Status}");
```

## Workflow

The `Workflow` class represents a workflow definition as a directed graph of activities connected by transitions. It provides methods to validate the workflow structure, navigate the activity graph, and publish the workflow for execution.

Example usage:

```csharp
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Enums;
using System;
using System.Collections.Generic;

// Create a workflow definition
var workflow = new Workflow
{
    Id = "order-processing-workflow",
    Name = "Order Processing Workflow",
    Description = "Processes customer orders through validation, inventory check, and payment",
    Version = 1,
    Status = WorkflowStatus.Draft,
    CreatedAt = DateTime.UtcNow,
    ModifiedAt = DateTime.UtcNow,
    CreatedBy = "admin@company.com",
    ModifiedBy = "admin@company.com",
    
    // Define activities
    Activities = new List<Activity>
    {
        new Activity
        {
            Id = "validate-order",
            Name = "Validate Order",
            Type = "Validation",
            TimeoutSeconds = 30,
            MaxRetries = 3
        },
        new Activity
        {
            Id = "check-inventory",
            Name = "Check Inventory",
            Type = "InventoryCheck",
            TimeoutSeconds = 60,
            MaxRetries = 2
        },
        new Activity
        {
            Id = "process-payment",
            Name = "Process Payment",
            Type = "PaymentProcessing",
            TimeoutSeconds = 45,
            MaxRetries = 3
        },
        new Activity
        {
            Id = "fulfill-order",
            Name = "Fulfill Order",
            Type = "Fulfillment",
            TimeoutSeconds = 120,
            MaxRetries = 1
        }
    },
    
    // Define transitions between activities
    Transitions = new List<Transition>
    {
        new Transition { FromActivityId = "validate-order", ToActivityId = "check-inventory" },
        new Transition { FromActivityId = "check-inventory", ToActivityId = "process-payment" },
        new Transition { FromActivityId = "process-payment", ToActivityId = "fulfill-order" }
    },
    
    StartActivityId = "validate-order",
    EndActivityId = "fulfill-order"
};

// Validate the workflow definition
if (workflow.Validate(out var errors))
{
    Console.WriteLine("Workflow is valid!");
    
    // Publish the workflow to make it available for execution
    workflow.Publish();
    Console.WriteLine($"Workflow published with status: {workflow.Status}");
}
else
{
    Console.WriteLine("Workflow validation failed:");
    foreach (var error in errors)
    {
        Console.WriteLine($"  - {error}");
    }
}

// Navigate the workflow graph
var nextActivities = workflow.GetNextActivities("validate-order");
Console.WriteLine($"Activities after 'validate-order': {nextActivities.Count}");

var previousActivities = workflow.GetPreviousActivities("process-payment");
Console.WriteLine($"Activities before 'process-payment': {previousActivities.Count}");

// Check if workflow is published
if (workflow.IsPublished)
{
    Console.WriteLine("Workflow is ready for execution!");
}
```

## CommandContext

`CommandContext` provides runtime information about a CLI command execution, including the command name, arguments, options, output format preferences, and execution context. It is used by CLI handlers to access command-line parameters and user-specific settings during workflow engine operations.

Example usage:

```csharp
using DotNetWorkflowEngine.Services;
using DotNetWorkflowEngine.Tests;
using Moq;

// Create mock repository and service instance
var mockAuditRepository = new Mock<IAuditRepository>();
var auditService = new AuditService(mockAuditRepository.Object);

// Log workflow instance creation
await auditService.LogInstanceCreated("workflow-instance-123", "user@company.com");

// Log activity completion
await auditService.LogActivityCompleted("workflow-instance-123", "activity-456", "user@company.com");

// Log activity failure
await auditService.LogActivityFailed("workflow-instance-123", "activity-789", "Task failed", "user@company.com");

// Retrieve all audit logs
var (allLogs, totalCount) = await auditService.GetFilteredAuditLogsAsync();
Console.WriteLine($"Total logs: {totalCount}");

// Filter logs by instance ID
var (instanceLogs, instanceCount) = await auditService.GetFilteredAuditLogsAsync(
    instanceId: "workflow-instance-123"
);

// Filter logs by activity ID
var (activityLogs, activityCount) = await auditService.GetFilteredAuditLogsAsync(
    activityId: "activity-456"
);

// Filter logs by event type
var (failedLogs, failedCount) = await auditService.GetFilteredAuditLogsAsync(
    eventType: "ActivityFailed"
);

// Filter logs by severity
var (errorLogs, errorCount) = await auditService.GetFilteredAuditLogsAsync(
    severity: "Error"
);

// Filter logs by date range
var fromDate = DateTime.UtcNow.AddDays(-7);
var toDate = DateTime.UtcNow;
var (recentLogs, recentCount) = await auditService.GetFilteredAuditLogsAsync(
    fromDate: fromDate,
    toDate: toDate
);

// Filter logs by actor
var (userLogs, userCount) = await auditService.GetFilteredAuditLogsAsync(
    actor: "user@company.com"
);

// Get paginated results
var (page1, pageTotal) = await auditService.GetFilteredAuditLogsAsync(
    skip: 0,
    take: 50
);

// Export audit logs to CSV
var csvData = await auditService.ExportAuditLogAsCsv("workflow-instance-123");
Console.WriteLine(csvData);

```csharp
using DotNetWorkflowEngine.Cli;
using System;
using System.Collections.Generic;

// Create a command context for a CLI command execution
var context = new CommandContext
{
    CommandName = "workflow run",
    Arguments = new List<string> { "--workflow-id", "wf-12345", "--timeout", "300" },
    Options = new Dictionary<string, string>
    {
        { "workflow-id", "wf-12345" },
        { "timeout", "300" },
        { "output-format", "json" }
    },
    OutputFormat = "json",
    IsVerbose = true,
    ExecutingUser = "admin@company.com"
};

// Access command properties
Console.WriteLine($"Executing command: {context.CommandName}");
Console.WriteLine($"Verbose mode: {context.IsVerbose}");
Console.WriteLine($"Output format: {context.OutputFormat}");
Console.WriteLine($"Executing user: {context.ExecutingUser}");

// Access arguments and options
Console.WriteLine($"Arguments count: {context.Arguments.Count}");
foreach (var arg in context.Arguments)
{
    Console.WriteLine($"  Argument: {arg}");
}

// Check for options
if (context.HasFlag("verbose"))
{
    Console.WriteLine("Verbose logging enabled");
}

var workflowId = context.GetOption("workflow-id");
Console.WriteLine($"Workflow ID: {workflowId}");

var timeout = context.GetOption("timeout");
Console.WriteLine($"Timeout: {timeout} seconds");

// Validate arguments
if (context.ValidateArguments())
{
    Console.WriteLine("Arguments are valid");
}
else
{
    Console.WriteLine("Arguments validation failed");
}
```