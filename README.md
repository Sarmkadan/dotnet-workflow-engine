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

## RetryPolicyConfig

The `RetryPolicyConfig` class defines configuration for activity retry behavior, enabling automatic retries on transient failures with configurable policies. It supports no retry, fixed delay, linear backoff, and exponential backoff strategies with jitter for distributed system resilience.

Example usage:

```csharp
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Enums;
using System;

// Create a no-retry policy (default behavior)
var noRetryConfig = RetryPolicyConfig.CreateNoRetry();
Console.WriteLine($"No retry policy: {noRetryConfig.PolicyType}, MaxAttempts: {noRetryConfig.MaxAttempts}");

// Create a fixed delay retry policy
var fixedDelayConfig = RetryPolicyConfig.CreateFixedDelay(maxAttempts: 5, delayMs: 2000);
fixedDelayConfig.PolicyType = RetryPolicy.FixedDelay;
fixedDelayConfig.RetryableExceptionTypes = new List<string> { "TimeoutException", "HttpRequestException" };
Console.WriteLine($"Fixed delay retry: {fixedDelayConfig.PolicyType}, Delay: {fixedDelayConfig.InitialDelayMs}ms, MaxAttempts: {fixedDelayConfig.MaxAttempts}");

// Create an exponential backoff retry policy
var exponentialConfig = RetryPolicyConfig.CreateExponentialBackoff(
    maxAttempts: 10,
    initialDelayMs: 1000,
    maxDelayMs: 300000,
    jitterFactor: 0.2
);
exponentialConfig.BackoffMultiplier = 2.5;
Console.WriteLine($"Exponential backoff: {exponentialConfig.PolicyType}, Initial: {exponentialConfig.InitialDelayMs}ms, Max: {exponentialConfig.MaxDelayMs}ms, Multiplier: {exponentialConfig.BackoffMultiplier}");

// Use the CalculateDelayMs method to get delay for a specific attempt
var delayForAttempt3 = exponentialConfig.CalculateDelayMs(3);
Console.WriteLine($"Delay for attempt 3: {delayForAttempt3}ms");

// Use the ShouldRetry method to check if another attempt should be made
var shouldRetry = exponentialConfig.ShouldRetry(5, "TimeoutException");
Console.WriteLine($"Should retry on TimeoutException (attempt 5): {shouldRetry}");

// Create a linear backoff retry policy
var linearConfig = new RetryPolicyConfig
{
    PolicyType = RetryPolicy.LinearBackoff,
    MaxAttempts = 8,
    InitialDelayMs = 1500,
    BackoffMultiplier = 1.0,
    JitterFactor = 0.1,
    RetryableExceptionTypes = new List<string> { "SqlException", "IOException" },
    RetryOnTimeout = true
};

// Calculate delay for attempt 4 with linear backoff
var linearDelay = linearConfig.CalculateDelayMs(4);
Console.WriteLine($"Linear backoff delay for attempt 4: {linearDelay}ms");
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

## Transition

The `Transition` class represents a directed edge connecting two activities in a workflow definition. Transitions define the flow of execution between activities and support both default (unconditional) and conditional routing based on expressions. Transitions can be prioritized and labeled for better workflow organization and debugging.

Example usage:

```csharp
using DotNetWorkflowEngine.Models;
using System;

// Create a default transition between two activities
var defaultTransition = new Transition
{
    Id = "validate_to_process_payment",
    FromActivityId = "validate-order",
    ToActivityId = "process-payment",
    IsDefault = true,
    Priority = 1,
    Label = "Default payment path"
};

// Create a conditional transition with an expression
var conditionalTransition = new Transition
{
    Id = "high_value_to_approval",
    FromActivityId = "validate-order",
    ToActivityId = "require-approval",
    ConditionExpression = "context.GetVariable<decimal>(\"orderTotal\") > 1000",
    Label = "High value order requires approval",
    Priority = 2
};

// Use factory methods for common scenarios
var defaultTransition2 = Transition.CreateDefault("check-inventory", "process-payment");
var conditionalTransition2 = Transition.CreateConditional(
    "validate-order",
    "reject-order",
    "!context.GetVariable<bool>(\"isValid\")"
);

// Validate transition configuration
if (defaultTransition.Validate(out var errors))
{
    Console.WriteLine("Transition is valid!");
}
else
{
    Console.WriteLine("Validation errors:");
    foreach (var error in errors)
    {
        Console.WriteLine($" - {error}");
    }
}

// Access transition properties
Console.WriteLine($"Transition: {defaultTransition.Id}");
Console.WriteLine($"From: {defaultTransition.FromActivityId}");
Console.WriteLine($"To: {defaultTransition.ToActivityId}");
Console.WriteLine($"Is default: {defaultTransition.IsDefault}");
Console.WriteLine($"Priority: {defaultTransition.Priority}");
Console.WriteLine($"Created at: {defaultTransition.CreatedAt:O}");
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

## AuditLogEntry

The `AuditLogEntry` class represents a structured audit log entry for tracking workflow events, state changes, activity executions, and errors. It provides a standardized format for recording workflow execution history with detailed context including previous and current states, timestamps, actors, and severity levels. Audit logs are essential for debugging workflow execution, compliance tracking, and monitoring workflow health.

Example usage:

```csharp
using DotNetWorkflowEngine.Models;
using System;
using System.Collections.Generic;

// Create a basic audit log entry
var entry = new AuditLogEntry(
    workflowInstanceId: "wf-order-processing-001",
    eventType: "WorkflowStarted",
    description: "Workflow instance created and started execution"
);
entry.Id = "log-7f3b9c2e-4567-89ab-cdef-123456789abc";
entry.ActivityId = "act-initialize";
entry.Severity = "Info";
entry.Actor = "system@company.com";
entry.CorrelationId = "corr-7f3b9c2e-4567-89ab-cdef-123456789abc";
entry.PreviousState = new Dictionary<string, object?> { { "status", "Created" } };
entry.CurrentState = new Dictionary<string, object?> { { "status", "Running" } };
entry.Details = new Dictionary<string, object?> { { "initiatedBy", "automation-service" } };

Console.WriteLine($"Audit Entry: {entry.Id}");
Console.WriteLine($"Timestamp: {entry.GetFormattedTimestamp()}");
Console.WriteLine($"Event: {entry.EventType}");
Console.WriteLine($"Description: {entry.Description}");

// Use factory methods for common scenarios
var activityEntry = AuditLogEntry.CreateActivityExecution(
    workflowInstanceId: "wf-order-processing-001",
    activityId: "act-validate-order",
    status: "Completed"
);
activityEntry.Severity = "Info";
activityEntry.Actor = "validation-service";

var stateChangeEntry = AuditLogEntry.CreateStateChange(
    workflowInstanceId: "wf-order-processing-001",
    previousState: "Running",
    currentState: "Completed",
    reason: "All activities completed successfully"
);
stateChangeEntry.Severity = "Info";

var errorEntry = AuditLogEntry.CreateError(
    workflowInstanceId: "wf-order-processing-001",
    activityId: "act-process-payment",
    errorMessage: "Payment gateway timeout after 30 seconds",
    correlationId: "corr-7f3b9c2e-4567-89ab-cdef-123456789abc"
);
errorEntry.Actor = "payment-gateway";

Console.WriteLine($"\nActivity Execution Log: {activityEntry.Description}");
Console.WriteLine($"State Change Log: {stateChangeEntry.Description}");
Console.WriteLine($"Error Log: {errorEntry.Description}");
```

## BranchingResult

The `BranchingResult` class describes the outcome of conditional branch resolution for a single activity. It is produced by `ConditionalBranchingService.ResolveBranchesAsync` after evaluating all outgoing transition expressions from a completed activity. This result helps determine which path(s) the workflow should follow based on the evaluation of transition conditions.

Example usage:

```csharp
using DotNetWorkflowEngine.Models;
using System;
using System.Collections.Generic;

// Simulate a completed activity with conditional transitions
var activity = new Activity
{
    Id = "order-validation",
    Name = "Validate Order",
    Type = "Validation"
};

// Create a branching result for the completed activity
var branchingResult = new BranchingResult
{
    ActivityId = activity.Id,
    AnyConditionMatched = true,
    UsedDefaultTransition = false,
    SelectedTransitions = new List<Transition>
    {
        new Transition
        {
            Id = "trans-valid-order",
            FromActivityId = activity.Id,
            ToActivityId = "process-payment",
            Condition = "context.GetVariable<bool>(\"isValid\")"
        }
    },
    SkippedTransitions = new List<Transition>
    {
        new Transition
        {
            Id = "trans-invalid-order",
            FromActivityId = activity.Id,
            ToActivityId = "reject-order",
            Condition = "!context.GetVariable<bool>(\"isValid\")"
        }
    },
    EvaluationErrors = new List<TransitionEvaluationError>()
};

// Check which transitions were selected
Console.WriteLine($"Activity '{branchingResult.ActivityId}' completed with {branchingResult.SelectedTransitions.Count} selected branches");
Console.WriteLine($"Skipped {branchingResult.SkippedTransitions.Count} branches due to condition mismatch");

if (branchingResult.AnyConditionMatched)
{
    Console.WriteLine("At least one conditional expression matched");
}

if (branchingResult.UsedDefaultTransition)
{
    Console.WriteLine("Default transition was used");
}

// Access the selected transition IDs
foreach (var transition in branchingResult.SelectedTransitions)
{
    Console.WriteLine($"Following transition: {transition.Id} -> {transition.ToActivityId}");
}

// Check for evaluation errors
if (branchingResult.HasEvaluationErrors)
{
    Console.WriteLine("Errors occurred during evaluation:");
    foreach (var error in branchingResult.EvaluationErrors)
    {
        Console.WriteLine($"  Transition '{error.TransitionId}': {error.ErrorMessage}");
    }
}

// Create an empty result for activities with no outgoing transitions
var emptyResult = BranchingResult.Empty("standalone-activity");
Console.WriteLine($"Empty result for activity with no transitions: {emptyResult.ActivityId}");
```

## ActivityResult

The `ActivityResult` class represents the outcome of an activity execution within a workflow. It tracks execution status, timing, output data, error information, retry attempts, and metadata. This class is used throughout the workflow engine to communicate the result of activity execution back to the workflow instance and to determine subsequent workflow transitions.

Example usage:

```csharp
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Enums;
using System;
using System.Collections.Generic;

// Create an activity result for a payment processing activity
var paymentResult = new ActivityResult("process-payment")
{
    StartTime = DateTime.UtcNow,
    AttemptNumber = 1,
    TotalAttempts = 3,
    Metadata = new Dictionary<string, object?>
    {
        { "retryPolicy", "exponential-backoff" },
        { "maxRetries", 3 }
    }
};

try
{
    // Simulate payment processing logic
    var paymentSuccess = ProcessPayment("ORD-12345", 299.99m);
    
    if (paymentSuccess)
    {
        // Mark as successful with output data
        var output = new Dictionary<string, object?>
        {
            { "transactionId", "txn-pay-7f3b9c2e" },
            { "amount", 299.99m },
            { "paymentMethod", "credit-card" },
            { "customerId", "CUST-67890" },
            { "status", "completed" }
        };
        
        paymentResult.SetSuccess(output);
        Console.WriteLine($"Payment processed successfully in {paymentResult.ExecutionDurationMs}ms");
    }
    else
    {
        throw new InvalidOperationException("Payment gateway declined the transaction");
    }
}
catch (Exception ex)
{
    // Mark as failed with error details
    paymentResult.SetFailure(
        errorMessage: "Payment processing failed",
        stackTrace: ex.StackTrace
    );
    Console.WriteLine($"Payment failed: {paymentResult.ErrorMessage}");
}

// Check result status
if (paymentResult.IsSuccess())
{
    Console.WriteLine($"Transaction ID: {paymentResult.GetOutput<string>("transactionId")}");
    Console.WriteLine($"Amount: {paymentResult.GetOutput<decimal>("amount"):C}");
}
else if (paymentResult.IsFailed())
{
    Console.WriteLine($"Error: {paymentResult.ErrorMessage}");
    Console.WriteLine($"Stack trace available: {!string.IsNullOrEmpty(paymentResult.StackTrace)}");
}

// Access timing information
Console.WriteLine($"Execution started: {paymentResult.StartTime:O}");
Console.WriteLine($"Execution ended: {paymentResult.EndTime?.ToString("O") ?? "not completed"}");
Console.WriteLine($"Duration: {paymentResult.ExecutionDurationMs}ms");

// Access metadata
Console.WriteLine($"Retry policy: {paymentResult.Metadata.GetValueOrDefault("retryPolicy")}");

// Helper method for demo
static bool ProcessPayment(string orderId, decimal amount)
{
    // Simulate payment processing
    return true;
}
```

## Activity

The `Activity` class represents a single unit of work within a workflow definition. It encapsulates all configuration needed to execute a task, including timeouts, retry policies, input/output mappings, and execution modes. Activities can represent tasks, events, or gateways (fork/join points) and support conditional execution through expressions.

Example usage:

```csharp
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Enums;
using System;
using System.Collections.Generic;

// Create a payment processing activity with retry policy
var paymentActivity = new Activity
{
    Id = "process-payment",
    Name = "Process Payment",
    Description = "Processes customer payment using configured payment gateway",
    Type = "PaymentProcessing",
    ExecutionMode = ExecutionMode.Sequential,
    HandlerType = "DotNetWorkflowEngine.Handlers.PaymentHandler",
    
    // Configuration
    TimeoutSeconds = 45,
    MaxRetries = 3,
    RetryPolicy = RetryPolicy.ExponentialBackoff,
    
    // Input parameters for the handler
    InputParameters = new Dictionary<string, object?>
    {
        { "orderId", "ORD-12345" },
        { "amount", 299.99m },
        { "customerId", "CUST-67890" },
        { "paymentMethod", "credit-card" }
    },
    
    // Output mapping to workflow context
    OutputMapping = new Dictionary<string, string>
    {
        { "transactionId", "paymentTransactionId" },
        { "status", "paymentStatus" }
    },
    
    // Conditional execution
    ConditionExpression = "context.GetVariable<bool>(\"isPaymentEnabled\")",
    IsOptional = false,
    
    // Message correlation (for event-based activities)
    MessageName = "PaymentProcessed",
    CorrelationProperty = "orderId",
    
    // Custom metadata
    Metadata = new Dictionary<string, object?>
    {
        { "category", "financial" },
        { "priority", "high" },
        { "createdBy", "payment-service" }
    }
};

// Set additional input parameters dynamically
paymentActivity.SetInputParameter("retryCount", 0);
paymentActivity.SetInputParameter("timeoutOverride", 60);

// Access input parameters
var orderId = paymentActivity.GetInputParameter("orderId") as string;
var amount = paymentActivity.GetInputParameter("amount") as decimal?;

// Add output mappings dynamically
paymentActivity.AddOutputMapping("gatewayResponse", "paymentGatewayResponse");

// Validate the activity configuration
if (paymentActivity.Validate(out var errors))
{
    Console.WriteLine("Activity configuration is valid!");
}
else
{
    Console.WriteLine("Validation errors:");
    foreach (var error in errors)
    {
        Console.WriteLine($" - {error}");
    }
}

// Check activity type
if (paymentActivity.IsGateway())
{
    Console.WriteLine("This is a gateway activity");
}

if (paymentActivity.RequiresHandler())
{
    Console.WriteLine("This activity requires a handler implementation");
}

Console.WriteLine($"Activity created at: {paymentActivity.CreatedAt:O}");
```

## MonitoringExample

The `MonitoringExample` class provides endpoints for monitoring workflow engine metrics, health checks, and system resource usage. It demonstrates how to expose workflow execution statistics, audit trail data, performance trends, and system health information through a REST API.

Example usage:

```csharp
using DotNetWorkflowEngine.Examples;
using DotNetWorkflowEngine.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

// Setup services (typically via DI)
var services = new ServiceCollection();
services.AddWorkflowServices();
services.AddLogging();
var serviceProvider = services.BuildServiceProvider();

var executionService = serviceProvider.GetRequiredService<IWorkflowExecutionService>();
var auditService = serviceProvider.GetRequiredService<IAuditService>();
var logger = serviceProvider.GetRequiredService<ILogger<MonitoringExample>>();

// Create monitoring example instance
var monitoringExample = new MonitoringExample(
    executionService,
    auditService,
    logger
);

// Get workflow metrics for today
var metrics = await monitoringExample.GetMetrics("today");
Console.WriteLine($"Total workflows: {metrics.Value.workflowMetrics.totalInstances}");
Console.WriteLine($"Completed: {metrics.Value.workflowMetrics.completedInstances}");
Console.WriteLine($"Success rate: {metrics.Value.successRate:P}");

// Get health status
var health = await monitoringExample.GetHealthStatus();
Console.WriteLine($"System status: {health.Value.status}");
Console.WriteLine($"Database: {health.Value.components.database}");
Console.WriteLine($"Cache: {health.Value.components.cache}");

// Get audit statistics
var auditStats = await monitoringExample.GetAuditStatistics("week");
Console.WriteLine($"Total audit entries: {auditStats.Value.totalEntries}");
Console.WriteLine($"Top 5 users: {string.Join(", ", auditStats.Value.topUsers.Select(u => u.userId))}");

// Get performance trends
var trends = await monitoringExample.GetPerformanceTrends(30);
Console.WriteLine($"Performance trends for last 30 days: {trends.Value.Count} data points");

// Get slowest workflows
var slowWorkflows = await monitoringExample.GetSlowestWorkflows(5);
Console.WriteLine($"Slowest workflows:");
foreach (var workflow in slowWorkflows.Value)
{
    Console.WriteLine($"  - {workflow.instanceId}: {workflow.executionTimeSeconds}s");
}

// Get failed workflows
var failedWorkflows = await monitoringExample.GetFailedWorkflows(7);
Console.WriteLine($"Total failed workflows: {failedWorkflows.Value.totalFailed}");
Console.WriteLine($"Failures by activity: {string.Join(", ", failedWorkflows.Value.failuresByActivity.Select(f => f.activityId))}");

// Get resource usage
var resourceUsage = monitoringExample.GetResourceUsage();
Console.WriteLine($"Memory usage: {resourceUsage.Value.memory.workingSetMb} MB");
Console.WriteLine($"CPU time: {resourceUsage.Value.cpu.totalProcessorTime}s");
```

## ErrorHandlingMiddleware

The `ErrorHandlingMiddleware` class provides global exception handling for ASP.NET Core applications that use the workflow engine. It catches all unhandled exceptions and converts them to standardized JSON error responses with consistent structure, ensuring consistent error handling across all API endpoints.

Example usage:

```csharp
using DotNetWorkflowEngine.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddWorkflowServices();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Add the error handling middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

## ErrorHandlingExample

The `ErrorHandlingExample` class demonstrates comprehensive error handling patterns in workflow execution, including retry policies, fallback activities, and graceful degradation. This example shows how to build resilient workflows that can recover from transient failures and provide meaningful error information.

Example usage:

```csharp
using DotNetWorkflowEngine.Examples;
using DotNetWorkflowEngine.Services;
using Microsoft.Extensions.DependencyInjection;

// Setup services (typically via DI)
var services = new ServiceCollection();
services.AddWorkflowServices();
var serviceProvider = services.BuildServiceProvider();

var workflowService = serviceProvider.GetRequiredService<IWorkflowDefinitionService>();
var executionService = serviceProvider.GetRequiredService<IWorkflowExecutionService>();
var retryService = serviceProvider.GetRequiredService<IRetryPolicyService>();

// Create error handling example instance
var errorHandlingExample = new ErrorHandlingExample(
    workflowService, 
    executionService, 
    retryService
);

// Initialize workflow with error handling configuration
var initResult = await errorHandlingExample.InitializeWorkflow();
Console.WriteLine($"Workflow initialized: {initResult.Value}");

// Process data with comprehensive error handling
var processResult = await errorHandlingExample.ProcessData(new ProcessingRequest
{
    DataSourceUrl = "https://api.example.com/data",
    ProcessingRules = new Dictionary<string, object>
    {
        { "timeout", 30 },
        { "retryPolicy", "exponential" },
        { "validationStrict", true }
    }
});

Console.WriteLine($"Processing started: {processResult.Value}");

// Get error handling details after execution
var instanceId = processResult.Value.InstanceId;
var errorInfo = await errorHandlingExample.GetErrorInfo(instanceId);

Console.WriteLine($"Status: {errorInfo.Value.status}");
Console.WriteLine($"Retry attempts: {errorInfo.Value.retryCount}");
Console.WriteLine($"Fallback used: {errorInfo.Value.fallbackUsed}");
```

## CustomActivityExample

The `CustomActivityExample` class demonstrates how to create and use custom activity implementations for domain-specific workflow logic. It provides examples of three custom activities: `CustomSmsActivity` for sending SMS notifications, `CustomImageProcessingActivity` for image processing tasks, and `CustomReportGenerationActivity` for generating reports.

Example usage:

```csharp
using DotNetWorkflowEngine.Examples;
using DotNetWorkflowEngine.Models;
using System;
using System.Threading.Tasks;

// Create a custom activity workflow controller
var customActivityExample = new CustomActivityExample(
    workflowService: workflowDefinitionService,
    executionService: workflowExecutionService,
    activityService: activityService
);

// Initialize workflow with custom activities
var initResult = await customActivityExample.InitializeWorkflow();
Console.WriteLine($"Workflow initialized: {initResult.Value}");

// Execute workflow with custom activities
var executionResult = await customActivityExample.ExecuteWithCustomActivities(new CustomActivityRequest
{
    PhoneNumber = "+1234567890",
    Message = "Your order has been processed successfully",
    ImageUrl = "https://example.com/image.jpg",
    ProcessingType = "resize",
    ReportType = "detailed",
    Format = "pdf"
});

Console.WriteLine($"Workflow execution started: {executionResult.Value}");

// Get execution results
var instanceId = executionResult.Value.InstanceId;
var results = await customActivityExample.GetExecutionResults(instanceId);
Console.WriteLine($"SMS sent: {results.Value.Results.SmsResult}");
Console.WriteLine($"Image processed: {results.Value.Results.ImageResult}");
Console.WriteLine($"Report generated: {results.Value.Results.ReportResult}");
```

## OrderProcessingExample

The `OrderProcessingExample` class demonstrates a complete order processing workflow that handles order validation, tax calculation, payment processing, shipment preparation, and customer notification. This example shows how to create a multi-step workflow with conditional transitions and audit trail tracking.

Example usage:

```csharp
using DotNetWorkflowEngine.Examples;
using DotNetWorkflowEngine.Models;
using Microsoft.Extensions.DependencyInjection;

// Setup services (typically via DI)
var services = new ServiceCollection();
services.AddWorkflowServices();
var serviceProvider = services.BuildServiceProvider();

var workflowService = serviceProvider.GetRequiredService<IWorkflowDefinitionService>();
var executionService = serviceProvider.GetRequiredService<IWorkflowExecutionService>();
var auditService = serviceProvider.GetRequiredService<IAuditService>();

// Create the order processing example instance
var orderProcessingExample = new OrderProcessingExample(
    workflowService,
    executionService,
    auditService
);

// Initialize the workflow definition
var initResult = await orderProcessingExample.InitializeWorkflow();
Console.WriteLine($"Workflow initialized: {initResult.Value.message}");

// Process a customer order
var orderRequest = new OrderRequest
{
    OrderId = "ORD-2024-001",
    CustomerId = "CUST-12345",
    Amount = 299.99m,
    ShippingAddress = "123 Main St, Anytown, USA 12345",
    Items = new List<OrderItem>
    {
        new OrderItem { ProductId = "PROD-001", Quantity = 2, UnitPrice = 49.99m },
        new OrderItem { ProductId = "PROD-002", Quantity = 1, UnitPrice = 99.99m }
    }
};

var processResult = await orderProcessingExample.ProcessOrder(orderRequest);
Console.WriteLine($"Order processing started: {processResult.Value.instanceId}");

// Check order status
var statusResult = await orderProcessingExample.GetOrderStatus(processResult.Value.instanceId);
Console.WriteLine($"Order status: {statusResult.Value.status}");

// Get order summary
var summaryResult = await orderProcessingExample.GetOrderSummary(processResult.Value.instanceId);
Console.WriteLine($"Order summary: {summaryResult.Value.orderId}, Duration: {summaryResult.Value.duration}");

// Export audit trail
var exportResult = await orderProcessingExample.ExportAuditTrail(processResult.Value.instanceId);
Console.WriteLine("Audit trail exported successfully");
```

## ParallelExecutionExample

The `ParallelExecutionExample` class demonstrates how to execute multiple independent workflow activities concurrently to improve performance and reduce total processing time. This example creates a workflow that validates inventory, checks payment, retrieves shipping quotes, and applies promotions simultaneously before combining results and continuing with sequential processing.

Example usage:

```csharp
using DotNetWorkflowEngine.Examples;
using Microsoft.Extensions.DependencyInjection;

// Setup services (typically via DI)
var services = new ServiceCollection();
services.AddWorkflowServices();
var serviceProvider = services.BuildServiceProvider();

var workflowService = serviceProvider.GetRequiredService<IWorkflowDefinitionService>();
var executionService = serviceProvider.GetRequiredService<IWorkflowExecutionService>();

// Create the parallel execution example instance
var parallelExample = new ParallelExecutionExample(workflowService, executionService);

// Initialize the parallel workflow
var initResult = await parallelExample.InitializeWorkflow();
Console.WriteLine($"Workflow initialized: {initResult.Value.message}");

// Execute an order with parallel processing
var orderData = new OrderData
{
    OrderId = "ORD-2024-001",
    Items = new List<OrderItem>
    {
        new OrderItem { ProductId = "PROD-001", Quantity = 2, Price = 49.99m },
        new OrderItem { ProductId = "PROD-002", Quantity = 1, Price = 99.99m }
    },
    ShippingAddress = "123 Main St, Anytown, USA 12345",
    PaymentMethod = "Credit Card",
    CustomerEmail = "customer@example.com"
};

var executionResult = await parallelExample.ExecuteParallelOrder(orderData);
Console.WriteLine($"Order processing started: {executionResult.Value.instanceId}");

// Get combined results from all parallel activities
var instanceId = executionResult.Value.instanceId;
var results = await parallelExample.GetParallelResults(instanceId);

Console.WriteLine($"Order ID: {results.Value.orderId}");
Console.WriteLine($"Inventory valid: {results.Value.inventoryValid}");
Console.WriteLine($"Payment valid: {results.Value.paymentValid}");
Console.WriteLine($"Shipping cost: {results.Value.shippingCost}");
Console.WriteLine($"Applied promotion: {results.Value.appliedPromotion}");
Console.WriteLine($"Final total: {results.Value.finalTotal}");
Console.WriteLine($"Processing status: {results.Value.processingStatus}");
```

## ApprovalChainExample

The `ApprovalChainExample` class demonstrates a multi-level document approval workflow that requires sequential approval from manager, director, and optionally CFO (for amounts over $10,000). This example showcases conditional workflow routing, state management across multiple approval stages, and notification activities for approval/rejection outcomes.

Example usage:

```csharp
using DotNetWorkflowEngine.Examples;
using Microsoft.Extensions.DependencyInjection;

// Setup services (typically via DI)
var services = new ServiceCollection();
services.AddWorkflowServices();
var serviceProvider = services.BuildServiceProvider();

var workflowService = serviceProvider.GetRequiredService<IWorkflowDefinitionService>();
var executionService = serviceProvider.GetRequiredService<IWorkflowExecutionService>();

// Create the approval chain example instance
var approvalExample = new ApprovalChainExample(workflowService, executionService);

// Initialize the workflow definition
var initResult = await approvalExample.InitializeWorkflow();
Console.WriteLine($"Workflow initialized: {initResult.Value}");

// Submit a document for approval
var submissionResult = await approvalExample.SubmitForApproval(new DocumentSubmission
{
    DocumentId = "DOC-2024-001",
    Title = "Q3 Marketing Budget Approval",
    Amount = 15000.00m,
    SubmittedBy = "john.doe@company.com"
});

Console.WriteLine($"Document submitted: {submissionResult.Value.instanceId}");

// Manager approves the document
var managerApproval = await approvalExample.ApproveDocument(submissionResult.Value.instanceId, new ApprovalDecision
{
    ApprovedBy = "manager@company.com",
    Comments = "Budget approved within department limits"
});

Console.WriteLine(managerApproval.Value.message);

// Director approves the document
var directorApproval = await approvalExample.ApproveDocument(submissionResult.Value.instanceId, new ApprovalDecision
{
    ApprovedBy = "director@company.com",
    Comments = "Budget approved for Q3"
});

Console.WriteLine(directorApproval.Value.message);

// CFO approves the document (required for amounts > $10,000)
var cfoApproval = await approvalExample.ApproveDocument(submissionResult.Value.instanceId, new ApprovalDecision
{
    ApprovedBy = "cfo@company.com",
    Comments = "Executive approval for large budget"
});

Console.WriteLine(cfoApproval.Value.message);

// Document is now fully approved and will be archived
```

## WorkflowDefinitionBenchmarks

The `WorkflowDefinitionBenchmarks` class provides performance benchmarks for workflow definition operations including loading, validation, and graph traversal. It measures the workflow engine's performance characteristics for workflow definition operations across different workflow sizes (small, medium, and large) to evaluate the efficiency of workflow definition management and validation.

Example usage:

```csharp
using DotNetWorkflowEngine.Benchmarks.Benchmarks;
using DotNetWorkflowEngine.Models;

// Create benchmark instance
var benchmarks = new WorkflowDefinitionBenchmarks();

// Setup the benchmark environment
benchmarks.Setup();

// Benchmark adding a small workflow definition
var addSmallResult = benchmarks.Add_Small_Workflow();
Console.WriteLine("Small workflow added successfully");

// Benchmark retrieving a small workflow definition
var getSmallResult = benchmarks.Get_Small_Workflow();
Console.WriteLine("Small workflow retrieved successfully");

// Benchmark validating a small workflow definition
var validateSmallResult = benchmarks.Validate_Small_Workflow();
Console.WriteLine("Small workflow validated successfully");

// Benchmark getting next activities from a small workflow
var nextActivitiesSmallResult = benchmarks.Get_Next_Activities_Small_Workflow();
Console.WriteLine("Next activities retrieved from small workflow");

// Benchmark adding a medium workflow definition (20 activities)
var addMediumResult = benchmarks.Add_Medium_Workflow();
Console.WriteLine("Medium workflow added successfully");

// Benchmark adding a large workflow definition (100 activities)
var addLargeResult = benchmarks.Add_Large_Workflow();
Console.WriteLine("Large workflow added successfully");

// Benchmark retrieving workflow definitions
var getMediumResult = benchmarks.Get_Medium_Workflow();
var getLargeResult = benchmarks.Get_Large_Workflow();
Console.WriteLine("Medium and large workflows retrieved successfully");

// Benchmark validating workflow definitions
var validateMediumResult = benchmarks.Validate_Medium_Workflow();
var validateLargeResult = benchmarks.Validate_Large_Workflow();
Console.WriteLine("Medium and large workflows validated successfully");

// Benchmark getting next activities from larger workflows
var nextActivitiesMediumResult = benchmarks.Get_Next_Activities_Medium_Workflow();
var nextActivitiesLargeResult = benchmarks.Get_Next_Activities_Large_Workflow();
Console.WriteLine("Next activities retrieved from medium and large workflows");
```

## ActivityExecutionBenchmarks

The `ActivityExecutionBenchmarks` class provides performance benchmarks for measuring activity execution throughput with different retry policies and execution scenarios. It benchmarks basic activity execution, activities with retry policies (exponential backoff, fixed delay, no retry), and measures the workflow engine's performance characteristics for individual activity execution under various workload scenarios.

Example usage:

```csharp
using DotNetWorkflowEngine.Benchmarks.Benchmarks;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using ExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

// Create benchmark instance
var benchmarks = new ActivityExecutionBenchmarks();

// Setup the benchmark environment
benchmarks.Setup();

// Benchmark simple activity execution (no retry)
var simpleResult = await benchmarks.Execute_Simple_Activity();
Console.WriteLine($"Simple activity completed in: {simpleResult.TotalTime}");

// Benchmark activity with exponential backoff retry policy
var retryResult = await benchmarks.Execute_Activity_With_Retry_Policy();
Console.WriteLine($"Activity with retry policy completed in: {retryResult.TotalTime}");

// Benchmark activity with fixed delay retry policy
var fixedRetryResult = await benchmarks.Execute_Activity_With_Fixed_Retry();
Console.WriteLine($"Activity with fixed retry completed in: {fixedRetryResult.TotalTime}");

// Benchmark activity with no retry policy
var noRetryResult = await benchmarks.Execute_Activity_With_No_Retry();
Console.WriteLine($"Activity with no retry completed in: {noRetryResult.TotalTime}");

// Execute activity and get output dictionary
var activity = new Activity
{
    Id = "test-activity",
    Name = "Test Activity",
    HandlerType = "Simple",
    Type = "TestActivity"
};

var context = new ExecutionContext
{
    WorkflowInstanceId = Guid.NewGuid().ToString(),
    ActivityId = "test-activity"
};

var output = await benchmarks.ExecuteAsync(activity, context);
Console.WriteLine($"Activity output: {output.Count} items");
```

## ExpressionEvaluationBenchmarks

The `ExpressionEvaluationBenchmarks` class provides performance benchmarks for measuring expression evaluation and conditional logic execution within workflows. It benchmarks the evaluation of simple boolean conditions (`true`/`false`), variable references, and complex expressions involving multiple variables and logical operators. These benchmarks help measure the workflow engine's performance characteristics for condition evaluation, which is critical for conditional workflow routing and activity execution.

Example usage:

```csharp
using DotNetWorkflowEngine.Benchmarks.Benchmarks;
using DotNetWorkflowEngine.Models;
using ExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

// Create benchmark instance
var benchmarks = new ExpressionEvaluationBenchmarks();

// Setup the benchmark environment
benchmarks.Setup();

// Benchmark execution of activity with true condition
var trueResult = await benchmarks.Execute_Activity_With_True_Condition();
Console.WriteLine($"True condition activity executed successfully");

// Benchmark execution of activity with false condition
var falseResult = await benchmarks.Execute_Activity_With_False_Condition();
Console.WriteLine($"False condition activity executed successfully");

// Benchmark execution of activity with complex condition expression
var complexResult = await benchmarks.Execute_Activity_With_Complex_Condition();
Console.WriteLine($"Complex condition activity executed successfully");

// Benchmark simple condition evaluation returning true
var simpleTrueResult = benchmarks.Evaluate_Simple_True_Condition();
Console.WriteLine($"Simple true condition evaluated: {simpleTrueResult}");

// Benchmark simple condition evaluation returning false
var simpleFalseResult = benchmarks.Evaluate_Simple_False_Condition();
Console.WriteLine($"Simple false condition evaluated: {simpleFalseResult}");

// Benchmark variable reference condition evaluation
var variableResult = benchmarks.Evaluate_Variable_Reference_Condition();
Console.WriteLine($"Variable reference condition evaluated: {variableResult}");

// Benchmark complex expression evaluation
var complexExprResult = benchmarks.Evaluate_Complex_Expression();
Console.WriteLine($"Complex expression evaluated: {complexExprResult}");

// Execute activity and get output dictionary
var activity = new Activity
{
    Id = "test-activity",
    Name = "Test Activity",
    ConditionExpression = "${orderAmount} > 1000",
    HandlerType = "Simple",
    Type = "TestActivity"
};

var context = new ExecutionContext
{
    WorkflowInstanceId = Guid.NewGuid().ToString(),
    ActivityId = "test-activity",
    Variables = new Dictionary<string, object?>
    {
        { "orderAmount", 1500 }
    }
};

var output = await benchmarks.ExecuteAsync(activity, context);
Console.WriteLine($"Activity output: {output.Count} items");
```

## ConcurrentExecutionBenchmarks

The `ConcurrentExecutionBenchmarks` class provides performance benchmarks for concurrent workflow execution scenarios. It measures the workflow engine's scalability, thread safety, and performance under load by executing multiple workflow instances simultaneously. These benchmarks help identify bottlenecks in workflow execution, activity handling, and state management when processing high volumes of concurrent workflows.

Example usage:

```csharp
using DotNetWorkflowEngine.Benchmarks.Benchmarks;
using BenchmarkDotNet.Running;

// Run benchmarks to measure concurrent workflow execution performance
var summary = BenchmarkRunner.Run<ConcurrentExecutionBenchmarks>();

// Or run specific benchmarks programmatically
var benchmarks = new ConcurrentExecutionBenchmarks();

// Setup the benchmark environment
benchmarks.Setup();

// Execute benchmarks for different concurrency levels
var smallLoadResult = await benchmarks.Execute_10_Concurrent_Workflows();
var mediumLoadResult = await benchmarks.Execute_50_Concurrent_Workflows();
var largeLoadResult = await benchmarks.Execute_100_Concurrent_Workflows();

// Get statistics for 1000 workflow instances
var statsResult = await benchmarks.Get_Statistics_With_1000_Instances();

// Access benchmark results (via BenchmarkDotNet)
Console.WriteLine($"10 workflows completed in: {smallLoadResult.TotalTime}");
Console.WriteLine($"50 workflows completed in: {mediumLoadResult.TotalTime}");
Console.WriteLine($"100 workflows completed in: {largeLoadResult.TotalTime}");
```

## WorkflowExecutionBenchmarks

The `WorkflowExecutionBenchmarks` class provides performance benchmarks for measuring workflow execution throughput across different workflow topologies. It benchmarks sequential, parallel, and conditional workflow execution patterns to evaluate the engine's performance characteristics under various workload scenarios. The class measures complete workflow execution including instance creation, activity execution, and state management.

Example usage:

```csharp
using DotNetWorkflowEngine.Benchmarks.Benchmarks;
using DotNetWorkflowEngine.Services;
using Microsoft.Extensions.DependencyInjection;

// Setup services (typically via DI)
var services = new ServiceCollection();
services.AddWorkflowServices();
var serviceProvider = services.BuildServiceProvider();

var executionService = serviceProvider.GetRequiredService<IWorkflowExecutionService>();
var definitionService = serviceProvider.GetRequiredService<IWorkflowDefinitionService>();
var activityService = serviceProvider.GetRequiredService<IActivityService>();

// Create benchmarks instance
var benchmarks = new WorkflowExecutionBenchmarks();
benchmarks.Setup(); // Initialize workflows and services

// Benchmark sequential workflow execution
var sequentialResult = await benchmarks.Execute_Sequential_Workflow();
Console.WriteLine($"Sequential workflow completed in: {sequentialResult.TotalTime}");

// Benchmark parallel workflow execution
var parallelResult = await benchmarks.Execute_Parallel_Workflow();
Console.WriteLine($"Parallel workflow completed in: {parallelResult.TotalTime}");

// Benchmark conditional workflow execution
var conditionalResult = await benchmarks.Execute_Conditional_Workflow();
Console.WriteLine($"Conditional workflow completed in: {conditionalResult.TotalTime}");

// Benchmark workflow instance creation performance
benchmarks.Create_Workflow_Instance();
Console.WriteLine("Workflow instance created");

// Benchmark execution with multiple concurrent instances
var multiInstanceResult = await benchmarks.Execute_Workflow_With_Multiple_Instances();
Console.WriteLine("100 workflow instances executed concurrently");

// Access benchmark statistics
var stats = new Dictionary<string, object?> 
{
    { "Sequential", sequentialResult.TotalTime.TotalMilliseconds },
    { "Parallel", parallelResult.TotalTime.TotalMilliseconds },
    { "Conditional", conditionalResult.TotalTime.TotalMilliseconds }
};
```

## CachingBenchmarks

The `CachingBenchmarks` class provides performance benchmarks for measuring caching throughput of workflow definitions. It benchmarks workflow definition caching operations including small and large workflow definitions, cache retrieval, cache removal, and cache clearing scenarios. These benchmarks help evaluate the workflow engine's caching performance characteristics and memory efficiency when handling workflow definitions of varying sizes.

Example usage:

```csharp
using DotNetWorkflowEngine.Benchmarks.Benchmarks;
using Microsoft.Extensions.Caching.Memory;

// Create benchmark instance
var benchmarks = new CachingBenchmarks();

// Setup the benchmark environment
benchmarks.Setup();

// Benchmark caching a small workflow definition
var smallWorkflowResult = benchmarks.Cache_Small_Workflow_Definition();
Console.WriteLine("Small workflow cached successfully");

// Benchmark caching a large workflow definition (100 activities)
var largeWorkflowResult = benchmarks.Cache_Large_Workflow_Definition();
Console.WriteLine("Large workflow cached successfully");

// Benchmark retrieving a cached small workflow
var cachedSmallResult = benchmarks.Get_Cached_Small_Workflow();
Console.WriteLine($"Small workflow retrieved from cache: {cachedSmallResult}");

// Benchmark retrieving a cached large workflow
var cachedLargeResult = benchmarks.Get_Cached_Large_Workflow();
Console.WriteLine($"Large workflow retrieved from cache: {cachedLargeResult}");

// Benchmark retrieving a missing workflow from cache (should return false)
var missingResult = benchmarks.Get_Missing_Workflow_From_Cache();
Console.WriteLine("Missing workflow lookup completed");

// Benchmark caching multiple workflows
var multipleResult = benchmarks.Cache_Multiple_Workflows();
Console.WriteLine("Multiple workflows cached successfully");

// Benchmark removing a workflow from cache
benchmarks.Remove_Workflow_From_Cache();
Console.WriteLine("Workflow removed from cache");

// Benchmark clearing the entire cache
benchmarks.Clear_Entire_Cache();
Console.WriteLine("Cache cleared successfully");

// Access the MemoryCache instance for custom operations
var memoryCache = benchmarks.GetCache(); // Helper method to access the cache
Console.WriteLine("Cache operations completed");
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