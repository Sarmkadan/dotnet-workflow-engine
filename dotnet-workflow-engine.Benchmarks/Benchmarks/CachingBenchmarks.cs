using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using Microsoft.Extensions.Caching.Memory;

namespace DotNetWorkflowEngine.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for caching performance.
/// Measures throughput of workflow definition caching operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net10_0)]
public class CachingBenchmarks
{
    private WorkflowDefinitionService _definitionService;
    private IMemoryCache _memoryCache;
    private Workflow _workflow;
    private Workflow _largeWorkflow;
    private const string CacheKeyPrefix = "workflow_definition:";

    [GlobalSetup]
    public void Setup()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 1024,
            CompactionPercentage = 0.2
        });

        _definitionService = new WorkflowDefinitionService();

        _workflow = CreateSmallWorkflow();
        _largeWorkflow = CreateLargeWorkflow();

        _definitionService.AddWorkflow(_workflow);
        _definitionService.AddWorkflow(_largeWorkflow);
    }

    [Benchmark]
    public void Cache_Small_Workflow_Definition()
    {
        var cacheKey = $"{CacheKeyPrefix}{_workflow.Id}";
        _memoryCache.Set(cacheKey, _workflow, TimeSpan.FromHours(1));
    }

    [Benchmark]
    public void Cache_Large_Workflow_Definition()
    {
        var cacheKey = $"{CacheKeyPrefix}{_largeWorkflow.Id}";
        _memoryCache.Set(cacheKey, _largeWorkflow, TimeSpan.FromHours(1));
    }

    [Benchmark]
    public bool Get_Cached_Small_Workflow()
    {
        var cacheKey = $"{CacheKeyPrefix}{_workflow.Id}";
        return _memoryCache.TryGetValue(cacheKey, out _);
    }

    [Benchmark]
    public bool Get_Cached_Large_Workflow()
    {
        var cacheKey = $"{CacheKeyPrefix}{_largeWorkflow.Id}";
        return _memoryCache.TryGetValue(cacheKey, out _);
    }

    [Benchmark]
    public void Get_Missing_Workflow_From_Cache()
    {
        var cacheKey = $"{CacheKeyPrefix}missing-workflow-id";
        _ = _memoryCache.TryGetValue(cacheKey, out _);
    }

    [Benchmark]
    public void Cache_Multiple_Workflows()
    {
        var cacheKey = $"{CacheKeyPrefix}multi:{Guid.NewGuid()}";
        _memoryCache.Set(cacheKey, _workflow, TimeSpan.FromHours(1));
    }

    [Benchmark]
    public void Remove_Workflow_From_Cache()
    {
        var cacheKey = $"{CacheKeyPrefix}{_workflow.Id}";
        _memoryCache.Remove(cacheKey);
    }

    [Benchmark]
    public void Clear_Entire_Cache()
    {
        // MemoryCache doesn't have a direct clear method, so we simulate cache pressure
        // In real scenarios, this would be handled by cache eviction policies
        ((MemoryCache)_memoryCache).Compact(1.0); // Compact 100% of cache
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
