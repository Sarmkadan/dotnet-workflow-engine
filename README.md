# dotnet-workflow-engine

A powerful, enterprise-grade visual workflow engine for .NET with BPMN-like DSL, parallel execution, retry policies, comprehensive audit trails, and extensible architecture. Designed for complex business process automation, microservice orchestration, and event-driven workflows.

![Version](https://img.shields.io/badge/version-1.2.0-blue)
![License](https://img.shields.io/badge/license-MIT-green)
![.NET](https://img.shields.io/badge/.NET-10.0-purple)

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

```bash
docker pull sarmkadan/dotnet-workflow-engine:latest
docker run -p 5000:80 sarmkadan/dotnet-workflow-engine:latest
```

### Method 4: Docker Compose

```bash
git clone https://github.com/Sarmkadan/dotnet-workflow-engine.git
cd dotnet-workflow-engine
docker-compose up -d
```

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

Complete examples for all major workflows are available in the `examples/` directory:

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

Configure the workflow engine in `appsettings.json`:

```json
{
  "WorkflowEngine": {
    "DefaultExecutionMode": "Sequential",
    "MaxConcurrentActivities": 10,
    "DefaultTimeout": "00:05:00",
    "EnableAuditTrail": true,
    "EnableMetrics": true,
    "CachingEnabled": true,
    "CacheProvider": "Redis"
  },
  "Database": {
    "ConnectionString": "Server=localhost;Database=WorkflowEngine;",
    "Provider": "SqlServer"
  }
}
```

See `docs/configuration.md` for all configuration options.

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
