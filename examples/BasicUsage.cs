using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using Microsoft.Extensions.DependencyInjection;

// Minimal setup for running a simple sequential workflow
var services = new ServiceCollection();
services.AddWorkflowEngine(); // Assumes basic default configuration

var provider = services.BuildServiceProvider();
var executionService = provider.GetRequiredService<IWorkflowExecutionService>();

// Define a simple workflow with one activity
var workflow = new Workflow {
    Name = "SimpleWorkflow",
    Activities = new List<Activity> {
        new Activity { Id = "act1", Name = "Task 1", ActivityType = "SimpleActivity" }
    }
};

// Execute
var context = new ExecutionContext { WorkflowId = workflow.Id };
var result = await executionService.ExecuteAsync(context);
Console.WriteLine($"Workflow finished with status: {result.Status}");
