using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;

namespace DotNetWorkflowEngine.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for concurrent workflow execution.
/// Measures scalability and thread safety under load.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net10_0)]
public class ConcurrentExecutionBenchmarks
{
    private WorkflowExecutionService _executionService;
    private WorkflowDefinitionService _definitionService;
    private AuditService _auditService;
    private ActivityService _activityService;
    private Workflow _sequentialWorkflow;

    [GlobalSetup]
    public void Setup()
    {
        // Setup services
        var retryPolicyService = new RetryPolicyService();
        _activityService = new ActivityService(retryPolicyService);
        _auditService = new AuditService(null); // Null audit service for benchmarks
        _definitionService = new WorkflowDefinitionService();
        _executionService = new WorkflowExecutionService(_definitionService, _auditService, _activityService);

        // Register a simple handler
        _activityService.RegisterHandler("Simple", new SimpleActivityHandler());

        // Create a simple sequential workflow
        _sequentialWorkflow = CreateSequentialWorkflow();
        _definitionService.AddWorkflow(_sequentialWorkflow);
    }

    [Benchmark]
    public async Task Execute_10_Concurrent_Workflows()
    {
        await ExecuteConcurrentWorkflows(10);
    }

    [Benchmark]
    public async Task Execute_50_Concurrent_Workflows()
    {
        await ExecuteConcurrentWorkflows(50);
    }

    [Benchmark]
    public async Task Execute_100_Concurrent_Workflows()
    {
        await ExecuteConcurrentWorkflows(100);
    }

    [Benchmark]
    public async Task Execute_200_Concurrent_Workflows()
    {
        await ExecuteConcurrentWorkflows(200);
    }

    [Benchmark]
    public async Task Execute_500_Concurrent_Workflows()
    {
        await ExecuteConcurrentWorkflows(500);
    }

    [Benchmark]
    public async Task Get_Statistics_With_1000_Instances()
    {
        // First create many instances
        for (int i = 0; i < 1000; i++)
        {
            var instance = _executionService.CreateInstance(_sequentialWorkflow.Id);
            await _executionService.StartAsync(instance.Id);
        }

        // Then measure statistics retrieval
        _ = _executionService.GetStatistics();
    }

    private async Task ExecuteConcurrentWorkflows(int count)
    {
        var tasks = new List<Task>();
        for (int i = 0; i < count; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var instance = _executionService.CreateInstance(_sequentialWorkflow.Id);
                await _executionService.StartAsync(instance.Id);
            }));
        }
        await Task.WhenAll(tasks);
    }

    private Workflow CreateSequentialWorkflow()
    {
        var workflow = new Workflow
        {
            Id = Guid.NewGuid().ToString(),
            Name = "SequentialWorkflow",
            Version = 1,
            Status = WorkflowStatus.Active,
            StartActivityId = "start_activity",
            EndActivityId = "end_activity"
        };

        workflow.Activities.Add(new Activity
        {
            Id = "start_activity",
            Name = "Start Activity",
            HandlerType = "Simple",
            Type = "StartActivity"
        });

        workflow.Activities.Add(new Activity
        {
            Id = "activity1",
            Name = "Activity 1",
            HandlerType = "Simple",
            Type = "TestActivity"
        });

        workflow.Activities.Add(new Activity
        {
            Id = "activity2",
            Name = "Activity 2",
            HandlerType = "Simple",
            Type = "TestActivity"
        });

        workflow.Activities.Add(new Activity
        {
            Id = "end_activity",
            Name = "End Activity",
            Type = "EndActivity"
        });

        workflow.Transitions.Add(new Transition
        {
            Id = "t1",
            FromActivityId = "start_activity",
            ToActivityId = "activity1"
        });

        workflow.Transitions.Add(new Transition
        {
            Id = "t2",
            FromActivityId = "activity1",
            ToActivityId = "activity2"
        });

        workflow.Transitions.Add(new Transition
        {
            Id = "t3",
            FromActivityId = "activity2",
            ToActivityId = "end_activity"
        });

        return workflow;
    }

    /// <summary>
    /// Simple activity handler for benchmarks.
    /// </summary>
    private class SimpleActivityHandler : ActivityService.IActivityHandler
    {
        public async Task<Dictionary<string, object?>> ExecuteAsync(Activity activity, DotNetWorkflowEngine.Models.ExecutionContext context)
        {
            await Task.Delay(1); // Simulate minimal work
            return new Dictionary<string, object?> { { "Result", "Success" } };
        }
    }
}
