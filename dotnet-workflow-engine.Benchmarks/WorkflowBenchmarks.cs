using BenchmarkDotNet.Attributes;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Enums;

namespace DotNetWorkflowEngine.Benchmarks;

[MemoryDiagnoser]
public class WorkflowBenchmarks
{
    [Params(10, 100, 1000)]
    public int ActivityCount;

    private Workflow _workflow = null!;
    private string _targetActivityId = null!;

    [GlobalSetup]
    public void Setup()
    {
        _workflow = new Workflow
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            StartActivityId = "activity-0",
            EndActivityId = $"activity-{ActivityCount - 1}"
        };

        for (int i = 0; i < ActivityCount; i++)
        {
            _workflow.Activities.Add(new Activity
            {
                Id = $"activity-{i}",
                Name = $"Activity {i}"
            });
        }

        for (int i = 0; i < ActivityCount - 1; i++)
        {
            _workflow.Transitions.Add(new Transition
            {
                Id = $"trans-{i}",
                FromActivityId = $"activity-{i}",
                ToActivityId = $"activity-{i + 1}"
            });
        }

        _targetActivityId = $"activity-{ActivityCount / 2}";
    }

    [Benchmark]
    public bool Validate()
    {
        return _workflow.Validate(out _);
    }

    [Benchmark]
    public List<Activity> GetNextActivities()
    {
        return _workflow.GetNextActivities(_targetActivityId);
    }

    [Benchmark]
    public List<Activity> GetPreviousActivities()
    {
        return _workflow.GetPreviousActivities(_targetActivityId);
    }

    [Benchmark]
    public Workflow CloneWithVersion()
    {
        return _workflow.CloneWithVersion(2);
    }
}
