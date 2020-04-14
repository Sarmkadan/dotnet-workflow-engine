using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using DotNetWorkflowEngine.Enums;
using Microsoft.Extensions.DependencyInjection;

// Configuration with custom options, retry policies, and error handling
var services = new ServiceCollection();

// Configure with specific options
services.AddWorkflowEngine(options => {
    options.DefaultExecutionMode = ExecutionMode.Sequential;
    options.MaxConcurrentActivities = 5;
});

var provider = services.BuildServiceProvider();
var executionService = provider.GetRequiredService<IWorkflowExecutionService>();

// Workflow with retry policy
var workflow = new Workflow {
    Name = "AdvancedWorkflow",
    Activities = new List<Activity> {
        new Activity { 
            Id = "retryable_task", 
            Name = "Task with Retry", 
            ActivityType = "ReliableActivity",
            RetryPolicy = RetryPolicy.Exponential,
            MaxRetries = 3 
        }
    }
};

// Execute with custom variables
var context = new ExecutionContext { 
    WorkflowId = workflow.Id,
    Variables = new Dictionary<string, object> { { "Input", "Value" } }
};

try {
    var result = await executionService.ExecuteAsync(context);
    Console.WriteLine($"Workflow status: {result.Status}");
}
catch (Exception ex) {
    Console.WriteLine($"Workflow execution failed: {ex.Message}");
}
