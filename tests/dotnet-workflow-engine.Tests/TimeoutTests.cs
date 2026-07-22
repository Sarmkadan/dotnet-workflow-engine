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

/// <summary>
/// Contains unit tests for verifying timeout functionality in activities.
/// Tests various scenarios including activities that timeout, complete successfully,
/// and activities where timeout is not exceeded.
/// </summary>
public class TimeoutTests
{
    /// <summary>
    /// Tests that an activity with a timeout that is exceeded results in a timeout status.
    /// Verifies that the activity status is set to Timeout, the IsTimeout method returns true,
    /// and an error message containing "timed out" is present.
    /// </summary>
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

    /// <summary>
    /// Tests that an activity without a timeout (TimeoutSeconds = 0) completes successfully.
    /// Verifies that the activity status is Completed, IsTimeout returns false,
    /// and IsSuccess returns true.
    /// </summary>
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

    /// <summary>
    /// Tests that an activity with a timeout that is not exceeded completes successfully.
    /// Verifies that the activity status is Completed, IsTimeout returns false,
    /// and IsSuccess returns true when the activity completes within the timeout period.
    /// </summary>
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

/// <summary>
/// A slow activity handler that introduces a configurable delay before completing.
/// Used to test timeout scenarios where activities take longer than the configured timeout.
/// </summary>
public class SlowHandler : ActivityService.IActivityHandler
{
    private readonly TimeSpan _delay;

    /// <summary>
    /// Initializes a new instance of the SlowHandler with a specified delay duration.
    /// </summary>
    /// <param name="delay">The time span to wait before completing the activity.</param>
    public SlowHandler(TimeSpan delay)
    {
        _delay = delay;
    }

    /// <summary>
    /// Executes the activity with the configured delay.
    /// </summary>
    /// <param name="activity">The activity to execute.</param>
    /// <param name="context">The execution context containing workflow instance information.</param>
    /// <returns>A dictionary of output values from the activity execution.</returns>
    public async Task<System.Collections.Generic.Dictionary<string, object?>> ExecuteAsync(Activity activity, ExecutionContext context)
    {
        await Task.Delay(_delay);
        return new System.Collections.Generic.Dictionary<string, object?>();
    }
}

/// <summary>
/// A fast activity handler that completes immediately without any delay.
/// Used to test successful activity execution scenarios.
/// </summary>
public class FastHandler : ActivityService.IActivityHandler
{
    /// <summary>
    /// Executes the activity immediately without any delay.
    /// </summary>
    /// <param name="activity">The activity to execute.</param>
    /// <param name="context">The execution context containing workflow instance information.</param>
    /// <returns>A dictionary of output values from the activity execution.</returns>
    public async Task<System.Collections.Generic.Dictionary<string, object?>> ExecuteAsync(Activity activity, ExecutionContext context)
    {
        await Task.CompletedTask;
        return new System.Collections.Generic.Dictionary<string, object?>();
    }
}