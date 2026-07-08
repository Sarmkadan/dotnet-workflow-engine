using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using ExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

namespace DotNetWorkflowEngine.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for activity execution performance.
/// Measures throughput of basic activity execution with different retry policies.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net10_0)]
public class ActivityExecutionBenchmarks
{
    private ActivityService _activityService;
    private RetryPolicyService _retryPolicyService;
    private Activity _simpleActivity;
    private Activity _retryActivity;
    private ExecutionContext _context;

    [GlobalSetup]
    public void Setup()
    {
        _retryPolicyService = new RetryPolicyService();
        _activityService = new ActivityService(_retryPolicyService);

        // Register a simple handler
        _activityService.RegisterHandler("Simple", new SimpleActivityHandler());
        _activityService.RegisterHandler("Retry", new FailingActivityHandler());

        // Create a simple activity
        _simpleActivity = new Activity
        {
            Id = "simple_activity",
            Name = "Simple Activity",
            HandlerType = "Simple",
            Type = "TestActivity"
        };

        // Create an activity with retry policy
        _retryActivity = new Activity
        {
            Id = "retry_activity",
            Name = "Retry Activity",
            HandlerType = "Retry",
            Type = "TestActivity",
            RetryPolicy = RetryPolicy.ExponentialBackoff,
            MaxRetries = 3
        };

        _context = new ExecutionContext
        {
            WorkflowInstanceId = Guid.NewGuid().ToString(),
            ActivityId = "test_activity"
        };
    }

    [Benchmark]
    public async Task Execute_Simple_Activity()
    {
        await _activityService.ExecuteAsync(_simpleActivity, _context);
    }

    [Benchmark]
    public async Task Execute_Activity_With_Retry_Policy()
    {
        await _activityService.ExecuteAsync(_retryActivity, _context);
    }

    [Benchmark]
    public async Task Execute_Activity_With_Fixed_Retry()
    {
        var activity = new Activity
        {
            Id = "fixed_retry_activity",
            Name = "Fixed Retry Activity",
            HandlerType = "Retry",
            Type = "TestActivity",
            RetryPolicy = RetryPolicy.FixedDelay,
            MaxRetries = 2
        };

        await _activityService.ExecuteAsync(activity, _context);
    }

    [Benchmark]
    public async Task Execute_Activity_With_No_Retry()
    {
        var activity = new Activity
        {
            Id = "no_retry_activity",
            Name = "No Retry Activity",
            HandlerType = "Retry",
            Type = "TestActivity",
            RetryPolicy = RetryPolicy.NoRetry,
            MaxRetries = 0
        };

        await _activityService.ExecuteAsync(activity, _context);
    }

    /// <summary>
    /// Simple activity handler that always succeeds.
    /// </summary>
    private class SimpleActivityHandler : ActivityService.IActivityHandler
    {
        public async Task<Dictionary<string, object?>> ExecuteAsync(Activity activity, ExecutionContext context)
        {
            await Task.Delay(1); // Simulate some work
            return new Dictionary<string, object?> { { "Result", "Success" } };
        }
    }

    /// <summary>
    /// Activity handler that always fails to test retry behavior.
    /// </summary>
    private class FailingActivityHandler : ActivityService.IActivityHandler
    {
        public async Task<Dictionary<string, object?>> ExecuteAsync(Activity activity, ExecutionContext context)
        {
            await Task.Delay(1); // Simulate some work
            throw new Exception("Activity failed");
        }
    }
}
