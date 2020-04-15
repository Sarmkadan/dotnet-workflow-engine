using DotNetWorkflowEngine.Configuration;
using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// This example demonstrates the basic usage of dotnet-workflow-engine
// It shows how to create a simple workflow, register services, and execute it

var builder = Host.CreateApplicationBuilder(args);

// Configure services
builder.Services.AddWorkflowEngine("Data Source=workflow.db");

var host = builder.Build();

// Create a simple workflow definition
var definitionService = host.Services.GetRequiredService<WorkflowDefinitionService>();
var workflow = definitionService.CreateWorkflow(
    "approval-workflow",
    "SimpleApprovalWorkflow",
    "A simple approval workflow with validation and notification");

definitionService.AddActivity(workflow.Id, new Activity
{
    Id = "start",
    Name = "Start Workflow",
    Type = "StartActivity"
});

definitionService.AddActivity(workflow.Id, new Activity
{
    Id = "validate_request",
    Name = "Validate Request",
    Type = "ValidatorActivity",
    TimeoutSeconds = 30
});

definitionService.AddActivity(workflow.Id, new Activity
{
    Id = "approve",
    Name = "Get Approval",
    Type = "ApprovalActivity",
    TimeoutSeconds = 86400
});

definitionService.AddActivity(workflow.Id, new Activity
{
    Id = "notify",
    Name = "Send Notification",
    Type = "EmailActivity",
    TimeoutSeconds = 60
});

definitionService.AddActivity(workflow.Id, new Activity
{
    Id = "end",
    Name = "End Workflow",
    Type = "EndActivity"
});

definitionService.AddTransition(workflow.Id, new Transition
{
    Id = "t1",
    FromActivityId = "start",
    ToActivityId = "validate_request",
    Label = "Start"
});

definitionService.AddTransition(workflow.Id, new Transition
{
    Id = "t2",
    FromActivityId = "validate_request",
    ToActivityId = "approve",
    Label = "Valid?"
});

definitionService.AddTransition(workflow.Id, new Transition
{
    Id = "t3",
    FromActivityId = "approve",
    ToActivityId = "notify",
    Label = "Approved"
});

definitionService.AddTransition(workflow.Id, new Transition
{
    Id = "t4",
    FromActivityId = "notify",
    ToActivityId = "end",
    Label = "Complete"
});

definitionService.SetStartActivity(workflow.Id, "start");
definitionService.SetEndActivity(workflow.Id, "end");
definitionService.PublishWorkflow(workflow.Id);

Console.WriteLine("Workflow registered successfully!");

// Execute the workflow
var executionService = host.Services.GetRequiredService<WorkflowExecutionService>();

var instance = executionService.CreateInstance(workflow.Id, initiatedBy: "john.doe@example.com");
instance.SetContextVariable("RequestId", Guid.NewGuid());
instance.SetContextVariable("Requester", "john.doe@example.com");
instance.SetContextVariable("Amount", 1500.00m);
instance.SetContextVariable("ApprovalRequired", true);

Console.WriteLine("Starting workflow execution...");
Console.WriteLine($"Workflow ID: {workflow.Id}");
Console.WriteLine($"Instance ID: {instance.Id}");

var result = await executionService.StartAsync(instance.Id);

Console.WriteLine("\nWorkflow execution completed!");
Console.WriteLine($"Status: {result.Status}");
Console.WriteLine($"Instance ID: {result.Id}");
Console.WriteLine($"Executed Activities: {result.ExecutedActivities.Count}");
Console.WriteLine("Context:");
foreach (var variable in result.Context)
{
    Console.WriteLine($"  - {variable.Key}: {variable.Value}");
}

// Wait for user input before exiting
Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();

await host.StopAsync();
