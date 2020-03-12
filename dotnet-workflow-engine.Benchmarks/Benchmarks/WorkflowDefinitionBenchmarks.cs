using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;

namespace DotNetWorkflowEngine.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for workflow definition operations.
/// Measures performance of workflow definition loading, validation, and traversal.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net10_0)]
public class WorkflowDefinitionBenchmarks
{
    private WorkflowDefinitionService _definitionService;
    private Workflow _smallWorkflow;
    private Workflow _mediumWorkflow;
    private Workflow _largeWorkflow;

    [GlobalSetup]
    public void Setup()
    {
        _definitionService = new WorkflowDefinitionService();

        _smallWorkflow = CreateSmallWorkflow();
        _definitionService.AddWorkflow(_smallWorkflow);

        _mediumWorkflow = CreateMediumWorkflow();
        _definitionService.AddWorkflow(_mediumWorkflow);

        _largeWorkflow = CreateLargeWorkflow();
        _definitionService.AddWorkflow(_largeWorkflow);
    }

    [Benchmark]
    public void Add_Small_Workflow()
    {
        var workflow = CreateSmallWorkflow();
        _definitionService.AddWorkflow(workflow);
    }

    [Benchmark]
    public void Get_Small_Workflow()
    {
        _ = _definitionService.GetWorkflow(_smallWorkflow.Id);
    }

    [Benchmark]
    public void Validate_Small_Workflow()
    {
        _ = _smallWorkflow.Validate(out var errors);
    }

    [Benchmark]
    public void Get_Next_Activities_Small_Workflow()
    {
        var workflow = _definitionService.GetWorkflow(_smallWorkflow.Id);
        if (workflow != null)
        {
            _ = workflow.GetNextActivities("start_activity");
        }
    }

    [Benchmark]
    public void Add_Medium_Workflow()
    {
        var workflow = CreateMediumWorkflow();
        _definitionService.AddWorkflow(workflow);
    }

    [Benchmark]
    public void Get_Medium_Workflow()
    {
        _ = _definitionService.GetWorkflow(_mediumWorkflow.Id);
    }

    [Benchmark]
    public void Validate_Medium_Workflow()
    {
        _ = _mediumWorkflow.Validate(out var errors);
    }

    [Benchmark]
    public void Get_Next_Activities_Medium_Workflow()
    {
        var workflow = _definitionService.GetWorkflow(_mediumWorkflow.Id);
        if (workflow != null)
        {
            _ = workflow.GetNextActivities("start_activity");
        }
    }

    [Benchmark]
    public void Add_Large_Workflow()
    {
        var workflow = CreateLargeWorkflow();
        _definitionService.AddWorkflow(workflow);
    }

    [Benchmark]
    public void Get_Large_Workflow()
    {
        _ = _definitionService.GetWorkflow(_largeWorkflow.Id);
    }

    [Benchmark]
    public void Validate_Large_Workflow()
    {
        _ = _largeWorkflow.Validate(out var errors);
    }

    [Benchmark]
    public void Get_Next_Activities_Large_Workflow()
    {
        var workflow = _definitionService.GetWorkflow(_largeWorkflow.Id);
        if (workflow != null)
        {
            _ = workflow.GetNextActivities("start_activity");
        }
    }

    private Workflow CreateSmallWorkflow()
    {
        var workflow = new Workflow
        {
            Id = Guid.NewGuid().ToString(),
            Name = "SmallWorkflow",
            Version = 1,
            Status = WorkflowStatus.Active,
            StartActivityId = "start_activity",
            EndActivityId = "end_activity"
        };

        workflow.Activities.Add(new Activity
        {
            Id = "start_activity",
            Name = "Start Activity"
        });

        workflow.Activities.Add(new Activity
        {
            Id = "activity1",
            Name = "Activity 1"
        });

        workflow.Activities.Add(new Activity
        {
            Id = "end_activity",
            Name = "End Activity"
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
            ToActivityId = "end_activity"
        });

        return workflow;
    }

    private Workflow CreateMediumWorkflow()
    {
        var workflow = new Workflow
        {
            Id = Guid.NewGuid().ToString(),
            Name = "MediumWorkflow",
            Version = 1,
            Status = WorkflowStatus.Active,
            StartActivityId = "start_activity",
            EndActivityId = "end_activity"
        };

        // Create a chain of 20 activities
        for (int i = 1; i <= 20; i++)
        {
            workflow.Activities.Add(new Activity
            {
                Id = $"activity{i}",
                Name = $"Activity {i}"
            });
        }

        workflow.Activities.Add(new Activity
        {
            Id = "end_activity",
            Name = "End Activity"
        });

        // Create transitions between all activities
        for (int i = 1; i <= 20; i++)
        {
            workflow.Transitions.Add(new Transition
            {
                Id = $"t{i}",
                FromActivityId = i == 1 ? "start_activity" : $"activity{i-1}",
                ToActivityId = i == 20 ? "end_activity" : $"activity{i}"
            });
        }

        return workflow;
    }

    private Workflow CreateLargeWorkflow()
    {
        var workflow = new Workflow
        {
            Id = Guid.NewGuid().ToString(),
            Name = "LargeWorkflow",
            Version = 1,
            Status = WorkflowStatus.Active,
            StartActivityId = "start_activity",
            EndActivityId = "end_activity"
        };

        // Create a chain of 100 activities
        for (int i = 1; i <= 100; i++)
        {
            workflow.Activities.Add(new Activity
            {
                Id = $"activity{i}",
                Name = $"Activity {i}"
            });
        }

        workflow.Activities.Add(new Activity
        {
            Id = "end_activity",
            Name = "End Activity"
        });

        // Create transitions between all activities
        for (int i = 1; i <= 100; i++)
        {
            workflow.Transitions.Add(new Transition
            {
                Id = $"t{i}",
                FromActivityId = i == 1 ? "start_activity" : $"activity{i-1}",
                ToActivityId = i == 100 ? "end_activity" : $"activity{i}"
            });
        }

        return workflow;
    }
}
