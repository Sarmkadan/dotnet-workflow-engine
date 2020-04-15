using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using ExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

namespace DotNetWorkflowEngine.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for workflow execution performance.
/// Measures throughput of complete workflow execution with different topologies.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net100)]
public class WorkflowExecutionBenchmarks
{
    private WorkflowExecutionService _executionService;
    private WorkflowDefinitionService _definitionService;
    private AuditService _auditService;
    private ActivityService _activityService;
    private Workflow _sequentialWorkflow;
    private Workflow _parallelWorkflow;
    private Workflow _conditionalWorkflow;

    [GlobalSetup]
    public void Setup()
    {
        // Setup services
        var retryPolicyService = new RetryPolicyService();
        _activityService = new ActivityService(retryPolicyService);
        _auditService = new AuditService(null); // Null audit service for benchmarks
        _definitionService = new WorkflowDefinitionService(null);
        _executionService = new WorkflowExecutionService(_definitionService, _auditService, _activityService);

        // Register a simple handler
        _activityService.RegisterHandler("Simple", new SimpleActivityHandler());

        // Create a simple sequential workflow: Start -> Activity1 -> Activity2 -> End
        _sequentialWorkflow = CreateSequentialWorkflow();
        _definitionService.AddWorkflow(_sequentialWorkflow);

        // Create a parallel workflow: Start -> Fork -> Activity1 & Activity2 (parallel) -> Join -> End
        _parallelWorkflow = CreateParallelWorkflow();
        _definitionService.AddWorkflow(_parallelWorkflow);

        // Create a conditional workflow with branching
        _conditionalWorkflow = CreateConditionalWorkflow();
        _definitionService.AddWorkflow(_conditionalWorkflow);
    }

    [Benchmark]
    public async Task Execute_Sequential_Workflow()
    {
        var instance = _executionService.CreateInstance(_sequentialWorkflow.Id);
        await _executionService.StartAsync(instance.Id);

        // Continue execution through all activities
        var workflow = _definitionService.GetWorkflow(_sequentialWorkflow.Id);
        if (workflow != null)
        {
            await ExecuteNextActivities(instance, workflow, workflow.StartActivityId!);
        }
    }

    [Benchmark]
    public async Task Execute_Parallel_Workflow()
    {
        var instance = _executionService.CreateInstance(_parallelWorkflow.Id);
        await _executionService.StartAsync(instance.Id);

        // Continue execution through parallel branches
        var workflow = _definitionService.GetWorkflow(_parallelWorkflow.Id);
        if (workflow != null)
        {
            await ExecuteNextActivities(instance, workflow, workflow.StartActivityId!);
        }
    }

    [Benchmark]
    public async Task Execute_Conditional_Workflow()
    {
        var instance = _executionService.CreateInstance(_conditionalWorkflow.Id);
        await _executionService.StartAsync(instance.Id);

        // Continue execution through conditional branches
        var workflow = _definitionService.GetWorkflow(_conditionalWorkflow.Id);
        if (workflow != null)
        {
            await ExecuteNextActivities(instance, workflow, workflow.StartActivityId!);
        }
    }

    [Benchmark]
    public void Create_Workflow_Instance()
    {
        _executionService.CreateInstance(_sequentialWorkflow.Id, "benchmark-correlation", "BenchmarkRunner");
    }

    [Benchmark]
    public async Task Execute_Workflow_With_Multiple_Instances()
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var instance = _executionService.CreateInstance(_sequentialWorkflow.Id);
                await _executionService.StartAsync(instance.Id);
            }));
        }
        await Task.WhenAll(tasks);
    }

    private async Task ExecuteNextActivities(WorkflowInstance instance, Workflow workflow, string currentActivityId)
    {
        var nextActivities = workflow.GetNextActivities(currentActivityId);
        foreach (var activity in nextActivities)
        {
            await _executionService.ExecuteActivityAsync(instance, activity.Id);
        }
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
            ActivityType = "StartActivity"
        });

        workflow.Activities.Add(new Activity
        {
            Id = "activity1",
            Name = "Activity 1",
            HandlerType = "Simple",
            ActivityType = "TestActivity"
        });

        workflow.Activities.Add(new Activity
        {
            Id = "activity2",
            Name = "Activity 2",
            HandlerType = "Simple",
            ActivityType = "TestActivity"
        });

        workflow.Activities.Add(new Activity
        {
            Id = "end_activity",
            Name = "End Activity",
            ActivityType = "EndActivity"
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

    private Workflow CreateParallelWorkflow()
    {
        var workflow = new Workflow
        {
            Id = Guid.NewGuid().ToString(),
            Name = "ParallelWorkflow",
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
            ActivityType = "StartActivity"
        });

        workflow.Activities.Add(new Activity
        {
            Id = "fork_activity",
            Name = "Fork Activity",
            ExecutionMode = ExecutionMode.Fork,
            ActivityType = "ForkGateway"
        });

        workflow.Activities.Add(new Activity
        {
            Id = "parallel_activity1",
            Name = "Parallel Activity 1",
            HandlerType = "Simple",
            ActivityType = "TestActivity"
        });

        workflow.Activities.Add(new Activity
        {
            Id = "parallel_activity2",
            Name = "Parallel Activity 2",
            HandlerType = "Simple",
            ActivityType = "TestActivity"
        });

        workflow.Activities.Add(new Activity
        {
            Id = "join_activity",
            Name = "Join Activity",
            ActivityType = "JoinGateway"
        });

        workflow.Activities.Add(new Activity
        {
            Id = "end_activity",
            Name = "End Activity",
            ActivityType = "EndActivity"
        });

        workflow.Transitions.Add(new Transition
        {
            Id = "t1",
            FromActivityId = "start_activity",
            ToActivityId = "fork_activity"
        });

        workflow.Transitions.Add(new Transition
        {
            Id = "t2",
            FromActivityId = "fork_activity",
            ToActivityId = "parallel_activity1"
        });

        workflow.Transitions.Add(new Transition
        {
            Id = "t3",
            FromActivityId = "fork_activity",
            ToActivityId = "parallel_activity2"
        });

        workflow.Transitions.Add(new Transition
        {
            Id = "t4",
            FromActivityId = "parallel_activity1",
            ToActivityId = "join_activity"
        });

        workflow.Transitions.Add(new Transition
        {
            Id = "t5",
            FromActivityId = "parallel_activity2",
            ToActivityId = "join_activity"
        });

        workflow.Transitions.Add(new Transition
        {
            Id = "t6",
            FromActivityId = "join_activity",
            ToActivityId = "end_activity"
        });

        return workflow;
    }

    private Workflow CreateConditionalWorkflow()
    {
        var workflow = new Workflow
        {
            Id = Guid.NewGuid().ToString(),
            Name = "ConditionalWorkflow",
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
            ActivityType = "StartActivity"
        });

        workflow.Activities.Add(new Activity
        {
            Id = "condition_activity",
            Name = "Condition Activity",
            ConditionExpression = "${shouldProceed}",
            ActivityType = "ConditionGateway"
        });

        workflow.Activities.Add(new Activity
        {
            Id = "path1_activity",
            Name = "Path 1 Activity",
            HandlerType = "Simple",
            ActivityType = "TestActivity"
        });

        workflow.Activities.Add(new Activity
        {
            Id = "path2_activity",
            Name = "Path 2 Activity",
            HandlerType = "Simple",
            ActivityType = "TestActivity"
        });

        workflow.Activities.Add(new Activity
        {
            Id = "end_activity",
            Name = "End Activity",
            ActivityType = "EndActivity"
        });

        workflow.Transitions.Add(new Transition
        {
            Id = "t1",
            FromActivityId = "start_activity",
            ToActivityId = "condition_activity"
        });

        workflow.Transitions.Add(new Transition
        {
            Id = "t2",
            FromActivityId = "condition_activity",
            ToActivityId = "path1_activity"
        });

        workflow.Transitions.Add(new Transition
        {
            Id = "t3",
            FromActivityId = "condition_activity",
            ToActivityId = "path2_activity"
        });

        workflow.Transitions.Add(new Transition
        {
            Id = "t4",
            FromActivityId = "path1_activity",
            ToActivityId = "end_activity"
        });

        workflow.Transitions.Add(new Transition
        {
            Id = "t5",
            FromActivityId = "path2_activity",
            ToActivityId = "end_activity"
        });

        return workflow;
    }

    /// <summary>
    /// Simple activity handler for benchmarks.
    /// </summary>
    private class SimpleActivityHandler : ActivityService.IActivityHandler
    {
        public async Task<Dictionary<string, object?>> ExecuteAsync(Activity activity, ExecutionContext context)
        {
            await Task.Delay(1); // Simulate minimal work
            return new Dictionary<string, object?> { { "Result", "Success" } };
        }
    }
}
