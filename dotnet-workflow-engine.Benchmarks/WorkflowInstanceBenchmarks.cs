using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Enums;

namespace DotNetWorkflowEngine.Benchmarks;

[MemoryDiagnoser]
public class WorkflowInstanceBenchmarks
{
    private WorkflowInstance _instance = null!;
    private List<string> _activityIds = new();

    [Params(10, 100, 1000)]
    public int ActivityCount;

    [GlobalSetup]
    public void Setup()
    {
        _instance = new WorkflowInstance("test-workflow");
        _instance.Start();
        
        _activityIds = new List<string>();
        for (int i = 0; i < ActivityCount; i++)
        {
            var id = $"activity-{i}";
            _activityIds.Add(id);
            _instance.RecordActivityExecution(id);
            _instance.SetContextVariable($"key-{i}", i);
            _instance.Metadata[$"meta-{i}"] = i;
        }
    }

    [Benchmark]
    public void RecordActivityExecution()
    {
        _instance.RecordActivityExecution("new-activity");
    }

    [Benchmark]
    public WorkflowInstance Clone()
    {
        return _instance.Clone();
    }

    [Benchmark]
    public void TransitionToArchived()
    {
        // Must be in active state for transition to archived
        var instance = new WorkflowInstance("test-workflow");
        instance.Start();
        instance.TransitionTo(WorkflowStatus.Archived);
    }
}
