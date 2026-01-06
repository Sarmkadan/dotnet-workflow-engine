// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Getting Started with dotnet-workflow-engine

This guide will walk you through setting up and running your first workflow with dotnet-workflow-engine.

## Prerequisites

- .NET 10 SDK (download from [dotnet.microsoft.com](https://dotnet.microsoft.com))
- A code editor (Visual Studio, VS Code, or JetBrains Rider)
- A database (SQL Server, PostgreSQL, or SQLite)
- Optional: Redis for caching

## Step 1: Create a New Project

```bash
dotnet new webapi -n MyWorkflowApp
cd MyWorkflowApp
```

## Step 2: Install NuGet Package

```bash
dotnet add package DotNetWorkflowEngine
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Hangfire
```

## Step 3: Configure appsettings.json

Update `appsettings.json` with your database connection:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=WorkflowDb;Integrated Security=true;"
  },
  "WorkflowEngine": {
    "DefaultExecutionMode": "Sequential",
    "MaxConcurrentActivities": 5,
    "DefaultTimeout": "00:05:00",
    "EnableAuditTrail": true,
    "EnableMetrics": false
  }
}
```

## Step 4: Configure Program.cs

```csharp
using DotNetWorkflowEngine.Configuration;
using DotNetWorkflowEngine.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Workflow Engine
builder.Services.AddWorkflowEngine(builder.Configuration);

// Add Database Context
builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Run database migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    context.Database.Migrate();
}

app.Run();
```

## Step 5: Create Your First Workflow

Create `Models/OrderWorkflow.cs`:

```csharp
using DotNetWorkflowEngine.Models;

public class OrderWorkflow
{
    public static Workflow CreateOrderProcessingWorkflow()
    {
        return new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "OrderProcessing",
            Version = 1,
            Description = "Basic order processing workflow",
            Status = WorkflowStatus.Active,
            Activities = new List<Activity>
            {
                new Activity
                {
                    Id = "validate",
                    Name = "Validate Order",
                    ActivityType = "Validation",
                    Timeout = TimeSpan.FromSeconds(30)
                },
                new Activity
                {
                    Id = "payment",
                    Name = "Process Payment",
                    ActivityType = "Payment",
                    Timeout = TimeSpan.FromSeconds(60),
                    RetryPolicy = RetryPolicy.Exponential,
                    MaxRetries = 3
                },
                new Activity
                {
                    Id = "shipping",
                    Name = "Ship Order",
                    ActivityType = "Shipping",
                    Timeout = TimeSpan.FromSeconds(45)
                }
            },
            Transitions = new List<Transition>
            {
                new Transition 
                { 
                    Id = "t1",
                    SourceActivityId = "validate",
                    TargetActivityId = "payment"
                },
                new Transition 
                { 
                    Id = "t2",
                    SourceActivityId = "payment",
                    TargetActivityId = "shipping"
                }
            }
        };
    }
}
```

## Step 6: Create a Controller

Create `Controllers/OrderController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using DotNetWorkflowEngine.Services;
using DotNetWorkflowEngine.Models;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IWorkflowDefinitionService _workflowService;
    private readonly IWorkflowExecutionService _executionService;

    public OrderController(
        IWorkflowDefinitionService workflowService,
        IWorkflowExecutionService executionService)
    {
        _workflowService = workflowService;
        _executionService = executionService;
    }

    [HttpPost("create-workflow")]
    public async Task<ActionResult> CreateWorkflow()
    {
        var workflow = OrderWorkflow.CreateOrderProcessingWorkflow();
        await _workflowService.CreateWorkflowAsync(workflow);
        
        return Ok(new { workflowId = workflow.Id });
    }

    [HttpPost("process")]
    public async Task<ActionResult> ProcessOrder([FromBody] ProcessOrderRequest request)
    {
        // Get workflow
        var workflows = await _workflowService.GetWorkflowsByNameAsync("OrderProcessing");
        if (workflows == null || !workflows.Any())
            return BadRequest("Workflow not found");

        var workflow = workflows.First();

        // Create execution context
        var context = new ExecutionContext
        {
            WorkflowId = workflow.Id,
            InstanceId = Guid.NewGuid(),
            Variables = new Dictionary<string, object>
            {
                { "OrderId", request.OrderId },
                { "Amount", request.Amount }
            }
        };

        // Execute workflow
        var result = await _executionService.ExecuteAsync(context);

        return Ok(new
        {
            instanceId = result.InstanceId,
            status = result.Status
        });
    }

    [HttpGet("{instanceId}")]
    public async Task<ActionResult> GetStatus(Guid instanceId)
    {
        var instance = await _executionService.GetInstanceAsync(instanceId);
        if (instance == null)
            return NotFound();

        return Ok(instance);
    }
}

public class ProcessOrderRequest
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
```

## Step 7: Run Your Application

```bash
dotnet run
```

Visit `http://localhost:5000/swagger` to see the Swagger UI.

## Step 8: Test the Workflow

```bash
# Create the workflow
curl -X POST http://localhost:5000/api/order/create-workflow

# Process an order
curl -X POST http://localhost:5000/api/order/process \
  -H "Content-Type: application/json" \
  -d '{"orderId":"ORD-001","amount":99.99}'

# Get instance status
curl http://localhost:5000/api/order/instance-id
```

## Next Steps

- Read the [Architecture Guide](architecture.md) to understand the system design
- Explore [Examples](../examples/) for more complex workflows
- Check [Configuration Guide](configuration.md) for tuning options
- Review [API Reference](api-reference.md) for all available endpoints

## Troubleshooting

### Database Connection Failed
- Ensure SQL Server is running
- Verify the connection string in appsettings.json
- Run migrations: `dotnet ef database update`

### Port Already in Use
```bash
# Change the port in appsettings.json
"Kestrel": {
  "Endpoints": {
    "Http": {
      "Url": "http://localhost:5001"
    }
  }
}
```

### Build Fails
```bash
# Clean and restore
dotnet clean
dotnet restore
dotnet build
```

For more troubleshooting, see [Troubleshooting Guide](troubleshooting.md).
