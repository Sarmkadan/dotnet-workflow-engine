# dotnet-workflow-engine

![CI](https://github.com/sarmkadan/dotnet-workflow-engine/actions/workflows/build.yml/badge.svg)
![License](https://img.shields.io/github/license/sarmkadan/dotnet-workflow-engine)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)

A powerful, enterprise-grade visual workflow engine for .NET with BPMN-like DSL, parallel execution, retry policies, comprehensive audit trails, and extensible architecture. Designed for complex business process automation, microservice orchestration, and event-driven workflows.

## Table of Contents

- [Project Overview](#project-overview)
- [Architecture](#architecture)
- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Usage Examples](#usage-examples)
- [API Reference](#api-reference)
- [Configuration](#configuration)
- [CLI Reference](#cli-reference)
- [Troubleshooting](#troubleshooting)
- [Testing](#testing)
- [Performance](#performance)
- [Related Projects](#related-projects)
- [Contributing](#contributing)
- [License](#license)

## Project Overview

**dotnet-workflow-engine** is a modern, high-performance workflow orchestration framework built on .NET 10. It enables developers to define, execute, and monitor complex business processes using a declarative, code-first approach or a visual DSL.

### Why dotnet-workflow-engine?

- **Enterprise-Ready**: Built with production concerns in mind - error handling, retry policies, audit trails, monitoring
- **Flexible Execution**: Support for sequential, parallel, and conditional workflow execution
- **Extensible**: Plugin architecture for custom activities and validators
- **Observable**: Comprehensive metrics, logging, and audit trail for regulatory compliance
- **Resilient**: Built-in retry policies, circuit breakers, and error recovery mechanisms
- **Scalable**: Async-first design with background job processing via Hangfire integration
- **Developer-Friendly**: Fluent builder API, comprehensive CLI, rich REST API

### Use Cases

- **Order Processing**: Multi-step order fulfillment with parallel payment and shipping
- **Approval Workflows**: Document approval chains with conditional routing
- **Data Pipeline Orchestration**: ETL processes with retry and error handling
- **Business Process Automation**: Complex multi-actor workflows with audit trails
- **Microservice Orchestration**: Coordinate operations across multiple services
- **Compliance Automation**: Automated compliance checks with full audit trail

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    REST API Layer                            │
│  (WorkflowController, InstanceController, AuditController)   │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────┴────────────────────────────────────────┐
│                  Service Layer                               │
│  ├─ WorkflowExecutionService (Orchestration)               │
│  ├─ WorkflowDefinitionService (Workflow Management)        │
│  ├─ ActivityService (Activity Execution)                   │
│  ├─ AuditService (Audit Trail Management)                 │
│  └─ RetryPolicyService (Resilience)                        │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────┴────────────────────────────────────────┐
│            Domain Models & Event System                      │
│  ├─ Workflow, Activity, Transition Models                   │
│  ├─ ExecutionContext (Runtime State)                        │
│  ├─ EventBus (Pub/Sub for Workflow Events)                 │
│  └─ Enums (Status, ExecutionMode, RetryPolicy)             │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────┴────────────────────────────────────────┐
│              Data Access Layer                               │
│  ├─ DatabaseContext (Entity Framework Core)                │
│  ├─ Repository Pattern (Workflow, Instance, Audit)         │
│  └─ Database (SQL Server, PostgreSQL, SQLite)              │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────┴────────────────────────────────────────┐
│          Cross-Cutting Concerns                             │
│  ├─ Caching (Redis, In-Memory)                             │
│  ├─ Background Jobs (Hangfire)                             │
│  ├─ Monitoring (Prometheus Metrics)                         │
│  ├─ Middleware (Error Handling, Rate Limiting, Logging)    │
│  └─ Security (Authorization, Encryption)                   │
└─────────────────────────────────────────────────────────────┘
```

### Component Details

| Component | Responsibility |
|-----------|-----------------|
| **REST API** | Expose workflows, instances, and audit trails via HTTP endpoints |
| **Execution Service** | Orchestrate workflow execution, manage state transitions |
| **Activity Service** | Execute individual workflow activities, validate inputs |
| **Event Bus** | Publish/subscribe pattern for workflow events |
| **Audit Service** | Maintain immutable audit trail for compliance |
| **Retry Service** | Implement exponential backoff and retry policies |
| **Database Layer** | Persist workflows, instances, and audit logs |
| **Caching Layer** | Cache frequently accessed workflows and definitions |
| **Background Jobs** | Async processing via Hangfire integration |
| **Monitoring** | Prometheus metrics for performance tracking |

## Features

- **BPMN-like DSL**: Define workflows with activities, transitions, and gateways
- **Parallel Execution**: Execute multiple activities concurrently with synchronization
- **Sequential & Conditional Routing**: Support for complex decision logic
- **Retry Policies**: Exponential backoff, max retries, custom retry logic
- **Audit Trail**: Immutable log of all workflow actions for compliance
- **REST API**: Full REST API for workflow management and execution
- **CLI Support**: Command-line interface for workflow operations
- **Event-Driven**: Pub/sub system for workflow lifecycle events
- **Background Jobs**: Integration with Hangfire for async processing
- **Caching**: Redis and in-memory caching for performance
- **Monitoring**: Prometheus metrics and health checks
- **Error Handling**: Comprehensive error handling with custom exceptions
- **Authorization**: Role-based access control for workflows
- **Expression Evaluation**: Evaluate conditions and variable assignments
- **Webhook Support**: Call external systems during workflow execution
- **Type-Safe**: Strongly typed with full C# generics support

## Installation

### Prerequisites

- .NET 10 SDK or later
- SQL Server, PostgreSQL, or SQLite
- Optional: Redis (for caching)
- Optional: Hangfire Dashboard (for background jobs)

### Method 1: NuGet Package

```bash
dotnet add package DotNetWorkflowEngine
```

### Method 2: Clone from Source

```bash
git clone https://github.com/Sarmkadan/dotnet-workflow-engine.git
cd dotnet-workflow-engine
dotnet restore
dotnet build
```

### Method 3: Docker

Build the Docker image:

```bash
docker build -t dotnet-workflow-engine .
```

Run the container:

```bash
docker run -p 8080:8080 dotnet-workflow-engine
```

### Method 4: Docker Compose

Start the application with its dependencies (Database and Redis):

```bash
docker-compose up -d
```

The application will be accessible at `http://localhost:8080`.


## Quick Start

### 1. Create a Workflow Definition

```csharp
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Utilities;

var workflow = new Workflow
{
    Id = Guid.NewGuid(),
    Name = "OrderProcessing",
    Version = 1,
    Status = WorkflowStatus.Active,
    Activities = new List<Activity>
    {
        new Activity 
        { 
            Id = "validate_order",
            Name = "Validate Order",
            ActivityType = "ValidationActivity",
            Timeout = TimeSpan.FromSeconds(30)
        },
        new Activity 
        { 
            Id = "process_payment",
            Name = "Process Payment",
            ActivityType = "PaymentActivity",
            RetryPolicy = RetryPolicy.Exponential,
            MaxRetries = 3
        },
        new Activity 
        { 
            Id = "ship_order",
            Name = "Ship Order",
            ActivityType = "ShippingActivity"
        }
    },
    Transitions = new List<Transition>
    {
        new Transition 
        { 
            Id = "t1",
            SourceActivityId = "validate_order",
            TargetActivityId = "process_payment"
        },
        new Transition 
        { 
            Id = "t2",
            SourceActivityId = "process_payment",
            TargetActivityId = "ship_order"
        }
    }
};
```

### 2. Register Services

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddWorkflowEngine(builder.Configuration)
    .AddDbContext<DatabaseContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")))
    .AddHangfire(config => config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")))
    .AddCaching(builder.Configuration);

var app = builder.Build();
app.UseWorkflowEngine();
```

### 3. Execute a Workflow

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IWorkflowExecutionService _executionService;

    public OrderController(IWorkflowExecutionService executionService)
    {
        _executionService = executionService;
    }

    [HttpPost("process")]
    public async Task<ActionResult> ProcessOrder(OrderRequest request)
    {
        var context = new ExecutionContext
        {
            WorkflowId = Guid.Parse("workflow-id"),
            InstanceId = Guid.NewGuid(),
            Variables = new Dictionary<string, object>
            {
                { "OrderId", request.OrderId },
                { "Amount", request.Amount },
                { "CustomerId", request.CustomerId }
            }
        };

        var result = await _executionService.ExecuteAsync(context);
        
        return Ok(new 
        { 
            InstanceId = result.InstanceId,
            Status = result.Status 
        });
    }
}
```

## Usage Examples

Practical usage snippets:

- **BasicUsage.cs** - Minimal setup and first workflow execution
- **AdvancedUsage.cs** - Configuration, custom options, retry policies, and error handling
- **IntegrationExample.cs** - Wiring the workflow engine into ASP.NET Core Dependency Injection

Complete examples for specific scenarios are also available in the `examples/` directory:

- **OrderProcessingExample.cs** - Multi-step order workflow with validation, payment, and shipping
- **ApprovalChainExample.cs** - Document approval process with multiple reviewers
- **ParallelExecutionExample.cs** - Parallel task execution and synchronization
- **ErrorHandlingExample.cs** - Custom retry policies and error recovery
- **WebhookIntegrationExample.cs** - Calling external systems during execution
- **CustomActivityExample.cs** - Extending with custom activities
- **EventDrivenExample.cs** - Event-driven workflow patterns
- **MonitoringExample.cs** - Metrics collection and health checks

## API Reference

### Workflow Management

#### List Workflows
```http
GET /api/workflows
Authorization: Bearer <token>
```

#### Get Workflow Details
```http
GET /api/workflows/{id}
Authorization: Bearer <token>
```

#### Create Workflow
```http
POST /api/workflows
Content-Type: application/json
Authorization: Bearer <token>
```

## WorkflowAuthorizationHandlerExtensions

Extension methods for `WorkflowAuthorizationHandler` that provide convenient utilities for common authorization scenarios in workflow applications. These methods simplify checking user claims, roles, and retrieving user information from the authorization context.

### Usage Example

```csharp
using DotNetWorkflowEngine.Security;
using Microsoft.AspNetCore.Authorization;

// In your authorization handler
public class WorkflowAuthorizationHandler : AuthorizationHandler<WorkflowRequirement>
{
    private readonly IUserService _userService;

    public WorkflowAuthorizationHandler(IUserService userService)
    {
        _userService = userService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WorkflowRequirement requirement)
    {
        // Check if user has required claim
        if (this.HasRequiredClaim(context, "workflow:execute"))
        {
            context.Succeed(requirement);
            return;
        }

        // Check if user has specific role
        if (this.HasRequiredRole(context, "WorkflowAdmin"))
        {
            context.Succeed(requirement);
            return;
        }

        // Get user information for logging/auditing
        var userId = this.GetUserId(context);
        var userEmail = this.GetUserEmail(context);
        var userName = this.GetUserName(context);

        // Check for specific claim value
        if (this.HasRequiredClaimValue(context, "department", "engineering"))
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail();
    }
}
```

The extension methods include:
- `HasRequiredClaim()` - Check if user has a specific claim type
- `HasRequiredClaimValue()` - Check if user has a claim with specific type and value
- `HasRequiredRole()` - Check if user has a specific role
- `GetUserId()` - Retrieve the authenticated user's ID
- `GetUserEmail()` - Retrieve the authenticated user's email
- `GetUserName()` - Retrieve the authenticated user's name

### RetryPolicyConfigTestsExtensions

Provides extension methods for testing `RetryPolicyConfig` configurations and behaviors. This class offers utilities to create different retry policy configurations (exponential backoff, fixed delay, no retry) and verify their behavior through fluent assertions.

### Usage Example

```csharp
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Tests;
using FluentAssertions;
using Xunit;

public class RetryPolicyTests
{
    [Fact]
    public void TestRetryPolicyConfigurations()
    {
        // Create different retry policy configurations
        var exponentialConfig = new RetryPolicyConfigTestsExtensions().CreateExponentialBackoff(
            maxAttempts: 5,
            initialDelayMs: 100
        );

        var fixedConfig = new RetryPolicyConfigTestsExtensions().CreateFixedDelay(
            maxAttempts: 3,
            delayMs: 500
        );

        var noRetryConfig = new RetryPolicyConfigTestsExtensions().CreateNoRetry(
            maxAttempts: 1
        );

        // Verify configuration properties
        exponentialConfig.PolicyType.Should().Be(RetryPolicy.ExponentialBackoff);
        exponentialConfig.MaxAttempts.Should().Be(5);
        exponentialConfig.InitialDelayMs.Should().Be(100);

        fixedConfig.PolicyType.Should().Be(RetryPolicy.FixedDelay);
        fixedConfig.MaxAttempts.Should().Be(3);
        fixedConfig.DelayMs.Should().Be(500);

        noRetryConfig.MaxAttempts.Should().Be(1);

        // Test retry behavior
        var config = exponentialConfig;
        new RetryPolicyConfigTestsExtensions().ShouldRetry_WhenMaxAttemptsExhausted_ShouldReturnFalse(
            config,
            exhaustedAttempt: 6
        );

        new RetryPolicyConfigTestsExtensions().ShouldRetry_WithRetryableExceptionType_ShouldReturnTrue(
            "IOException"
        );

        // Test delay calculation
        var expectedDelay = new RetryPolicyConfigTestsExtensions().CalculateExpectedExponentialDelay(
            attempt: 3,
            initialDelayMs: 1000
        );
        expectedDelay.Should().Be(4000);

        // Test max delay constraint
        var constrainedConfig = new RetryPolicyConfig
        {
            MaxAttempts = 5,
            MaxDelayMs = 10000
        };
        new RetryPolicyConfigTestsExtensions().CalculateDelayMs_WithMaxDelayConstraint_ShouldNotExceedMax(
            constrainedConfig,
            attempt: 10,
            expectedMaxDelay: 10000
        );
    }
}
```

The extension methods include:
- `CreateExponentialBackoff()` - Create a retry policy with exponential backoff
- `CreateFixedDelay()` - Create a retry policy with fixed delay between attempts
- `CreateNoRetry()` - Create a retry policy that doesn't retry (max attempts = 1)
- `ShouldRetry_WhenMaxAttemptsExhausted_ShouldReturnFalse()` - Verify retry policy correctly identifies when max attempts are exhausted
- `ShouldRetry_WithRetryableExceptionType_ShouldReturnTrue()` - Verify retry policy correctly identifies retryable exception types
- `CalculateExpectedExponentialDelay()` - Calculate expected delay for exponential backoff at a specific attempt
- `CalculateDelayMs_WithMaxDelayConstraint_ShouldNotExceedMax()` - Verify delay calculation respects max delay constraint

### Workflow Instances

#### Execute Workflow
```http
POST /api/workflows/{id}/execute
Content-Type: application/json
Authorization: Bearer <token>
```

#### Get Instance Status
```http
GET /api/instances/{instanceId}
Authorization: Bearer <token>
```

#### List Instance Activities
```http
GET /api/instances/{instanceId}/activities
Authorization: Bearer <token>
```

### Audit Trail

#### Get Audit Logs
```http
GET /api/audit?workflowId={id}&startDate={date}&pageSize=50
Authorization: Bearer <token>
```

See `docs/api-reference.md` for complete API documentation.

## Configuration

The dotnet-workflow-engine supports comprehensive configuration through the standard .NET configuration system using `IOptions` pattern. All configuration is optional with sensible defaults provided.

### Basic Configuration

Configure the workflow engine in `appsettings.json`:

```json
{
  "WorkflowEngine": {
    "ConnectionString": "Server=localhost;Database=WorkflowEngine;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;",
    "DefaultActivityTimeoutSeconds": 300,
    "EnableAuditLogging": true,
    "MaxConcurrentWorkflows": 100,
    "ValidateWorkflowsOnLoad": true
  },
  "Database": {
    "ConnectionString": "Server=localhost;Database=WorkflowEngine;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;",
    "Provider": "SqlServer"
  }
}
```

### Complete Configuration Reference

All available configuration options with their default values:

```json
{
  "WorkflowEngine": {
    // Core engine configuration
    "ConnectionString": "Server=localhost;Database=WorkflowEngine;...",
    "DefaultRetryPolicy": {
      "MaxRetries": 3,
      "InitialDelayMilliseconds": 1000,
      "MaxDelayMilliseconds": 300000,
      "BackoffFactor": 2
    },
    "EnableAuditLogging": true,
    "MaxConcurrentWorkflows": 100,
    "DefaultActivityTimeoutSeconds": 300,
    "ValidateWorkflowsOnLoad": true,

    // Infrastructure configuration
    "EnableMetrics": true,
    "EnableBackgroundJobs": true,
    "EnableAuditTrail": true,

    // Caching configuration
    "CachingEnabled": true,
    "CacheProvider": "Memory",
    "RedisConnectionString": "localhost:6379,password=your-redis-password",
    "DefaultCacheExpiration": "01:00:00",
    "UseDistributedCache": false,

    // Middleware configuration
    "EnableRequestLogging": true,
    "LogRequestBody": false,
    "LogResponseBody": false,
    "EnableRateLimiting": true,
    "RateLimit": {
      "MaxRequests": 100,
      "WindowSeconds": 60,
      "RetryAfterSeconds": 60
    },
    "EnableCors": true,

    // Security configuration
    "EnableWebhookValidation": true,
    "WebhookSecret": "your-webhook-secret-key",
    "EnableActivityValidation": true,
    "EnableWorkflowValidation": true,

    // Expression evaluation
    "EnableExpressionEvaluation": true,
    "MaxExpressionDepth": 20,
    "MaxWorkflowVariables": 1000,
    "MaxWorkflowDepth": 50,

    // Execution configuration
    "ExecutionMode": "Sequential",
    "EnableParallelExecution": true,
    "MaxParallelActivities": 10,
    "EnableConditionalBranching": true,
    "EnableErrorRecovery": true,
    "EnableCircuitBreaker": true,
    "CircuitBreaker": {
      "FailureThreshold": 5,
      "SamplingDurationSeconds": 60,
      "MinimumThroughput": 10,
      "BreakDurationSeconds": 30
    },

    // Audit trail configuration
    "EnableImmutableAuditTrail": true,
    "AuditTrailRetentionDays": 365,

    // Health and monitoring
    "EnableHealthChecks": true,
    "HealthCheckIntervalSeconds": 30,
    "EnablePrometheusMetrics": false,
    "MetricsPort": 9090
  }
}
```

### Using Configuration in Code

#### Option 1: Using IConfiguration (Recommended)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddWorkflowEngine(builder.Configuration.GetSection("WorkflowEngine"))
    .AddDbContext<DatabaseContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();
app.UseWorkflowEngine();
```

#### Option 2: Using Action Configuration

```csharp
builder.Services.AddWorkflowEngine(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.DefaultActivityTimeoutSeconds = 600;
    options.MaxConcurrentWorkflows = 200;
    options.EnableAuditLogging = true;
    options.CachingEnabled = true;
    options.CacheProvider = "Redis";
    options.RedisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString");
});
```

#### Option 3: Using IOptions Pattern

```csharp
// In your service
public class MyWorkflowService
{
    private readonly DotnetWorkflowEngineOptions _options;
    
    public MyWorkflowService(IOptions<DotnetWorkflowEngineOptions> options)
    {
        _options = options.Value;
        
        // Access configuration
        var timeout = _options.DefaultActivityTimeoutSeconds;
        var auditEnabled = _options.EnableAuditLogging;
    }
}
```

### Configuration Validation

The workflow engine validates all configuration options on startup using FluentValidation. Invalid configurations will throw a `ValidationException` with detailed error messages.

### Environment-Specific Configuration

Use standard .NET environment variables and configuration providers:

```bash
# appsettings.Development.json
{
  "WorkflowEngine": {
    "EnableDebugLogging": true,
    "MaxConcurrentWorkflows": 50
  }
}

# appsettings.Production.json
{
  "WorkflowEngine": {
    "MaxConcurrentWorkflows": 500,
    "EnableDebugLogging": false
  }
}
```

### Sensitive Data

For production environments, use:
- Environment variables
- Azure Key Vault
- AWS Secrets Manager
- User secrets (development only)

```bash
# Using environment variables
# export WorkflowEngine__ConnectionString="Server=..."
```

See `appsettings.example.json` for a complete configuration template with all available options.

### Configuration Best Practices

1. **Never hardcode sensitive data** - Use configuration providers
2. **Validate on startup** - Invalid configurations will fail fast
3. **Use environment-specific files** - appsettings.{Environment}.json
4. **Monitor configuration** - Log configuration at startup for debugging
5. **Document changes** - Update configuration when making breaking changes

### Configuration Hot Reload

The workflow engine supports configuration hot reload in development:

```csharp
builder.Services.AddWorkflowEngine(builder.Configuration.GetSection("WorkflowEngine"))
    .AddOptions<DotnetWorkflowEngineOptions>()
    .Bind(builder.Configuration.GetSection("WorkflowEngine"))
    .ValidateOnStart();
```

See `docs/configuration.md` for advanced configuration scenarios and best practices.

## CLI Reference

```bash
# Workflow operations
dotnet run -- workflow list
dotnet run -- workflow get <id>
dotnet run -- workflow create ./definitions/workflow.json
dotnet run -- workflow validate ./definitions/workflow.json

# Instance operations
dotnet run -- instance execute <workflow-id> --variables '{"key":"value"}'
dotnet run -- instance get <instance-id>
dotnet run -- instance cancel <instance-id>

# Audit operations
dotnet run -- audit list --limit 100
dotnet run -- audit export --output ./audit.csv

# Health & Monitoring
dotnet run -- health check
dotnet run -- metrics show
```

See `docs/cli-reference.md` for complete CLI documentation.

## Troubleshooting

Common issues and solutions are documented in `docs/troubleshooting.md`:

- Workflow execution timeouts
- Database connection failures
- Redis cache issues
- Out of memory with large workflows
- Activities not executing in parallel
- Performance optimization tips

## Testing

Run the full test suite:

```bash
dotnet test
```

Run with code coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage"
```

Run a specific test project:

```bash
dotnet test tests/dotnet-workflow-engine.Tests/
```

## Performance

Benchmarks measured on a single core (AMD Ryzen 9, .NET 10, Release build):

| Scenario | Throughput | Latency (p99) |
|---|---|---|
| Sequential workflow execution | ~12,000 events/sec | <8 ms |
| Parallel branch coordination | ~4,500 workflows/sec | <22 ms |
| Workflow state transition | — | <2 ms |
| Audit trail write (SQLite) | — | <10 ms |
| Audit trail write (SQL Server) | — | <18 ms |
| Workflow definition load (cached) | — | <1 ms |
| Cold start (service boot) | — | <450 ms |

Key performance characteristics:

- **Async-first execution**: All I/O operations are fully async; no thread blocking under load
- **Redis caching**: Workflow definitions cached in Redis reduce database reads by ~95% under steady-state traffic
- **Parallel overhead**: Forking into N parallel branches adds <2 ms per branch regardless of N
- **Retry backoff**: Exponential backoff does not block worker threads; retries are re-queued via Hangfire

To run benchmarks locally:

```bash
cd dotnet-workflow-engine.Benchmarks
dotnet run -c Release -- all
```

## Running Benchmarks

For detailed performance measurements, run the benchmarks project:

```bash
# Navigate to benchmarks directory
cd dotnet-workflow-engine.Benchmarks

# Run all benchmarks (Release mode recommended)
dotnet run -c Release -- all

# Run specific benchmark
cd dotnet-workflow-engine.Benchmarks
dotnet run -c Release -- ActivityExecutionBenchmarks
```

See the [benchmarks README](dotnet-workflow-engine.Benchmarks/README.md) for more details.

## Related Projects

- [dotnet-event-bus](https://github.com/sarmkadan/dotnet-event-bus) - In-process and distributed event bus for .NET - pub/sub, request/reply, dead letter, polymorphic handlers
- [dotnet-distributed-lock](https://github.com/sarmkadan/dotnet-distributed-lock) - Distributed locking library for .NET - Redis, SQLite, PostgreSQL backends with fencing tokens and auto-renewal

### Integration Examples

**Publish workflow lifecycle events via dotnet-event-bus**

```csharp
// Register both libraries and wire up cross-cutting event publishing
services.AddEventBus(options => options.UseInProcess());
services.AddWorkflowEngine(configuration)
    .OnActivityCompleted(async (ctx, sp) =>
    {
        var bus = sp.GetRequiredService<IEventBus>();
        await bus.PublishAsync(new ActivityCompletedEvent(ctx.InstanceId, ctx.CurrentActivity));
    });
```

**Prevent duplicate workflow execution with dotnet-distributed-lock**

```csharp
// Acquire a distributed lock before starting a workflow instance
var lockKey = $"workflow:{workflowId}:{correlationId}";
await using var handle = await lockProvider.AcquireAsync(lockKey, TimeSpan.FromMinutes(5));
var result = await executionService.ExecuteAsync(new ExecutionContext
{
    WorkflowId = workflowId,
    Variables = variables
});
```

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch
3. Make your changes and add tests
4. Ensure all tests pass: `dotnet test`
5. Commit with meaningful message
6. Push to branch and open a Pull Request

See `docs/contributing.md` for detailed guidelines.

## License

MIT License © 2026 Vladyslav Zaiets

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**

[Portfolio](https://sarmkadan.com) | [GitHub](https://github.com/Sarmkadan) | [Telegram](https://t.me/sarmkadan)
