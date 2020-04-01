using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using DotNetWorkflowEngine.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// This example demonstrates the basic usage of dotnet-workflow-engine v2.0
// It shows how to create a simple workflow, register services, and execute it

var builder = Host.CreateApplicationBuilder(args);

// Configure services
builder.Services
    .AddWorkflowEngine(builder.Configuration)
    .AddDbContext<DatabaseContext>(options =>
        options.UseSqlite("Data Source=workflow.db"))
    .AddLogging();

var host = builder.Build();

// Create a simple workflow definition
var workflow = new Workflow
{
    Id = Guid.Parse("12345678-1234-1234-1234-123456789abc"),
    Name = "SimpleApprovalWorkflow",
    Version = 1,
    Status = WorkflowStatus.Active,
    Description = "A simple approval workflow with validation and notification",
    Activities = new List<Activity>
    {
        new Activity
        {
            Id = "start",
            Name = "Start Workflow",
            ActivityType = "StartActivity",
            Display = new ActivityDisplay
            {
                PositionX = 50,
                PositionY = 100,
                Color = "#4CAF50"
            }
        },
        new Activity
        {
            Id = "validate_request",
            Name = "Validate Request",
            ActivityType = "ValidatorActivity",
            Timeout = TimeSpan.FromSeconds(30),
            Display = new ActivityDisplay
            {
                PositionX = 200,
                PositionY = 100,
                Color = "#FF9800"
            }
        },
        new Activity
        {
            Id = "approve",
            Name = "Get Approval",
            ActivityType = "ApprovalActivity",
            Timeout = TimeSpan.FromHours(24),
            Display = new ActivityDisplay
            {
                PositionX = 350,
                PositionY = 100,
                Color = "#2196F3"
            }
        },
        new Activity
        {
            Id = "notify",
            Name = "Send Notification",
            ActivityType = "EmailActivity",
            Timeout = TimeSpan.FromSeconds(60),
            Display = new ActivityDisplay
            {
                PositionX = 500,
                PositionY = 100,
                Color = "#9C27B0"
            }
        },
        new Activity
        {
            Id = "end",
            Name = "End Workflow",
            ActivityType = "EndActivity",
            Display = new ActivityDisplay
            {
                PositionX = 650,
                PositionY = 100,
                Color = "#795548"
            }
        }
    },
    Transitions = new List<Transition>
    {
        new Transition
        {
            Id = "t1",
            SourceActivityId = "start",
            TargetActivityId = "validate_request",
            Display = new TransitionDisplay
            {
                Path = "M 100 125 L 200 125",
                Label = "Start"
            }
        },
        new Transition
        {
            Id = "t2",
            SourceActivityId = "validate_request",
            TargetActivityId = "approve",
            Display = new TransitionDisplay
            {
                Path = "M 250 125 L 350 125",
                Label = "Valid?"
            }
        },
        new Transition
        {
            Id = "t3",
            SourceActivityId = "approve",
            TargetActivityId = "notify",
            Display = new TransitionDisplay
            {
                Path = "M 400 125 L 500 125",
                Label = "Approved"
            }
        },
        new Transition
        {
            Id = "t4",
            SourceActivityId = "notify",
            TargetActivityId = "end",
            Display = new TransitionDisplay
            {
                Path = "M 550 125 L 650 125",
                Label = "Complete"
            }
        }
    }
};

// Register the workflow
var workflowService = host.Services.GetRequiredService<IWorkflowDefinitionService>();
await workflowService.SaveAsync(workflow);

Console.WriteLine("✅ Workflow registered successfully!");

// Execute the workflow
var executionService = host.Services.GetRequiredService<IWorkflowExecutionService>();

var executionContext = new ExecutionContext
{
    WorkflowId = workflow.Id,
    InstanceId = Guid.NewGuid(),
    Variables = new Dictionary<string, object>
    {
        { "RequestId", Guid.NewGuid() },
        { "Requester", "john.doe@example.com" },
        { "Amount", 1500.00m },
        { "ApprovalRequired", true }
    }
};

Console.WriteLine($"🚀 Starting workflow execution...");
Console.WriteLine($"Workflow ID: {workflow.Id}");
Console.WriteLine($"Instance ID: {executionContext.InstanceId}");

var result = await executionService.ExecuteAsync(executionContext);

Console.WriteLine($"\n✅ Workflow execution completed!");
Console.WriteLine($"Status: {result.Status}");
Console.WriteLine($"Instance ID: {result.InstanceId}");
Console.WriteLine($"Completed Activities: {result.CompletedActivities.Count}");
Console.WriteLine($"Variables:");
foreach (var variable in result.Variables)
{
    Console.WriteLine($"  - {variable.Key}: {variable.Value}");
}

// Wait for user input before exiting
Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();

await host.StopAsync();
