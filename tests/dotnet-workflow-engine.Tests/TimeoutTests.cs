// =============================================================================
// Author: Test
// Simple test to verify timeout functionality
// ====================================================================

using System;
using System.Threading.Tasks;
using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using Xunit;
using ExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

namespace DotNetWorkflowEngine.Tests;

public class TimeoutTests
{
    [Fact]
    public async Task Activity_WithTimeout_ShouldTimeoutWhenExceeded()
    {
        // Arrange
        var retryPolicyService = new RetryPolicyService();
        var activityService = new ActivityService(retryPolicyService);

        var slowHandler = new SlowHandler(TimeSpan.FromSeconds(2));
        activityService.RegisterHandler("slowHandler", slowHandler);

        var activity = new Activity
        {
            Id = "test-timeout",
            Name = "Test Timeout Activity",
            HandlerType = "slowHandler",
            TimeoutSeconds = 1, // 1 second timeout
            MaxRetries = 0,
            RetryPolicy = RetryPolicy.NoRetry
        };

        var context = new ExecutionContext
        {
            WorkflowInstanceId = "inst-1",
            CorrelationId = "corr-1"
        };

        // Act
        var result = await activityService.ExecuteAsync(activity, context);

        // Assert
        Assert.Equal(ActivityStatus.Timeout, result.Status);
        Assert.True(result.IsTimeout());
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("timed out", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Activity_WithoutTimeout_ShouldCompleteSuccessfully()
    {
        // Arrange
        var retryPolicyService = new RetryPolicyService();
        var activityService = new ActivityService(retryPolicyService);

        var fastHandler = new FastHandler();
        activityService.RegisterHandler("fastHandler", fastHandler);

        var activity = new Activity
        {
            Id = "test-no-timeout",
            Name = "Test No Timeout Activity",
            HandlerType = "fastHandler",
            TimeoutSeconds = 0, // No timeout
            MaxRetries = 0,
            RetryPolicy = RetryPolicy.NoRetry
        };

        var context = new ExecutionContext
        {
            WorkflowInstanceId = "inst-2",
            CorrelationId = "corr-2"
        };

        // Act
        var result = await activityService.ExecuteAsync(activity, context);

        // Assert
        Assert.Equal(ActivityStatus.Completed, result.Status);
        Assert.False(result.IsTimeout());
        Assert.True(result.IsSuccess());
    }

    [Fact]
    public async Task Activity_WithTimeoutNotExceeded_ShouldCompleteSuccessfully()
    {
        // Arrange
        var retryPolicyService = new RetryPolicyService();
        var activityService = new ActivityService(retryPolicyService);

        var fastHandler = new FastHandler();
        activityService.RegisterHandler("fastHandler2", fastHandler);

        var activity = new Activity
        {
            Id = "test-timeout-not-exceeded",
            Name = "Test Timeout Not Exceeded Activity",
            HandlerType = "fastHandler2",
            TimeoutSeconds = 5, // 5 second timeout (plenty of time)
            MaxRetries = 0,
            RetryPolicy = RetryPolicy.NoRetry
        };

        var context = new ExecutionContext
        {
            WorkflowInstanceId = "inst-3",
            CorrelationId = "corr-3"
        };

        // Act
        var result = await activityService.ExecuteAsync(activity, context);

        // Assert
        Assert.Equal(ActivityStatus.Completed, result.Status);
        Assert.False(result.IsTimeout());
        Assert.True(result.IsSuccess());
    }
}

public class SlowHandler : ActivityService.IActivityHandler
{
    private readonly TimeSpan _delay;

    public SlowHandler(TimeSpan delay)
    {
        _delay = delay;
    }

    public async Task<System.Collections.Generic.Dictionary<string, object?>> ExecuteAsync(Activity activity, ExecutionContext context)
    {
        await Task.Delay(_delay);
        return new System.Collections.Generic.Dictionary<string, object?>();
    }
}

public class FastHandler : ActivityService.IActivityHandler
{
    public async Task<System.Collections.Generic.Dictionary<string, object?>> ExecuteAsync(Activity activity, ExecutionContext context)
    {
        await Task.CompletedTask;
        return new System.Collections.Generic.Dictionary<string, object?>();
    }
}