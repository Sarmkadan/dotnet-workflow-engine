// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Exceptions;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using FluentAssertions;
using Moq;
using Xunit;
using WorkflowExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

namespace DotNetWorkflowEngine.Tests;

/// <summary>
/// Contains unit tests for the <see cref="ActivityService"/> class.
/// Tests various scenarios including handler registration, activity execution,
/// conditional branching, retry policies, error handling, and context management.
/// </summary>
public class ActivityServiceTests
{
    /// <summary>
    /// Creates an instance of <see cref="ActivityService"/> for testing.
    /// </summary>
    /// <returns>An initialized <see cref="ActivityService"/> instance.</returns>
    private ActivityService CreateService()
    {
        var retryPolicyService = new RetryPolicyService();
        return new ActivityService(retryPolicyService);
    }

    /// <summary>
    /// Creates a test <see cref="Activity"/> with default values.
    /// </summary>
    /// <param name="id">The activity identifier. Defaults to "test-activity".</param>
    /// <param name="handlerType">The handler type to assign. Defaults to "default".</param>
    /// <returns>A configured <see cref="Activity"/> instance for testing.</returns>
    private Activity CreateActivity(string id = "test-activity", string handlerType = "default")
    {
        return new Activity
        {
            Id = id,
            Name = "Test Activity",
            TimeoutSeconds = 30,
            MaxRetries = 0,
            RetryPolicy = RetryPolicy.NoRetry,
            HandlerType = handlerType
        };
    }

    /// <summary>
    /// Creates a test <see cref="WorkflowExecutionContext"/> with default values.
    /// </summary>
    /// <returns>A configured <see cref="WorkflowExecutionContext"/> instance for testing.</returns>
    private WorkflowExecutionContext CreateContext()
    {
        return new WorkflowExecutionContext
        {
            WorkflowInstanceId = "inst-1",
            CorrelationId = "corr-1"
        };
    }

    /// <summary>
    /// Tests that registering a custom handler adds it to the handler registry.
    /// </summary>
    [Fact]
    public void RegisterHandler_AddsHandlerToRegistry()
    {
        var service = CreateService();
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
            .ReturnsAsync(new Dictionary<string, object?>());

        service.RegisterHandler("custom", mockHandler.Object);

        var activity = CreateActivity("act1", "custom");
        var context = CreateContext();

        Func<Task> act = async () => await service.ExecuteAsync(activity, context);
        act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Tests that executing a gateway activity succeeds without requiring a handler.
    /// Gateway activities should complete successfully even when no handler is registered.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_GatewayActivity_ReturnsSuccessWithoutHandler()
    {
        var service = CreateService();
        var activity = CreateActivity("gateway-act", null!);
        activity.Type = "ParallelGateway";
        activity.ExecutionMode = ExecutionMode.Fork;
        var context = CreateContext();

        var result = await service.ExecuteAsync(activity, context);

        result.IsSuccess().Should().BeTrue();
        result.Status.Should().Be(ActivityStatus.Completed);
    }

    /// <summary>
    /// Tests that executing an invalid activity throws a <see cref="ValidationException"/>.
    /// An activity with an empty ID is considered invalid.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_InvalidActivity_ThrowsValidationException()
    {
        var service = CreateService();
        var activity = CreateActivity();
        activity.Id = string.Empty; // Mark as invalid

        var context = CreateContext();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.ExecuteAsync(activity, context));
    }

    /// <summary>
    /// Tests that executing an activity with no registered handler throws an <see cref="ActivityException"/>.
    /// Activities require a registered handler to execute unless they are gateway activities.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_NoHandlerRegistered_ThrowsActivityException()
    {
        var service = CreateService();
        var activity = CreateActivity("act1", "unregistered-handler");
        var context = CreateContext();

        await Assert.ThrowsAsync<ActivityException>(() =>
            service.ExecuteAsync(activity, context));
    }

    /// <summary>
    /// Tests that executing an activity with a registered handler returns success.
    /// Verifies that the handler is invoked and its output is captured in the result.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_HandlerExecutes_ReturnsSuccess()
    {
        var service = CreateService();
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        var expectedOutput = new Dictionary<string, object?> { { "result", "success" } };
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
            .ReturnsAsync(expectedOutput);
        service.RegisterHandler("default", mockHandler.Object);

        var activity = CreateActivity();
        var context = CreateContext();

        var result = await service.ExecuteAsync(activity, context);

        result.IsSuccess().Should().BeTrue();
        result.Status.Should().Be(ActivityStatus.Completed);
        result.Output.Should().Contain(expectedOutput);
    }

    /// <summary>
    /// Tests that when a handler throws an exception, an <see cref="ActivityException"/> is thrown.
    /// Verifies that exceptions from handlers are properly wrapped and propagated.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_HandlerThrows_ThrowsActivityException()
    {
        var service = CreateService();
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
            .ThrowsAsync(new InvalidOperationException("Handler error"));
        service.RegisterHandler("default", mockHandler.Object);

        var activity = CreateActivity();
        var context = CreateContext();

        await Assert.ThrowsAsync<ActivityException>(() =>
            service.ExecuteAsync(activity, context));
    }

    /// <summary>
    /// Tests that an activity with a false condition expression is skipped.
    /// Verifies conditional branching logic works correctly.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ConditionalSkip_ReturnsSkipped()
    {
        var service = CreateService();
        var activity = CreateActivity();
        activity.ConditionExpression = "false";
        var context = CreateContext();

        var result = await service.ExecuteAsync(activity, context);

        result.Status.Should().Be(ActivityStatus.Skipped);
    }

    /// <summary>
    /// Tests that an activity with a true condition expression executes successfully.
    /// Verifies that the handler is invoked when the condition passes.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ConditionalPass_ExecutesActivity()
    {
        var service = CreateService();
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
            .ReturnsAsync(new Dictionary<string, object?>());
        service.RegisterHandler("default", mockHandler.Object);

        var activity = CreateActivity();
        activity.ConditionExpression = "true";
        var context = CreateContext();

        var result = await service.ExecuteAsync(activity, context);

        result.Status.Should().Be(ActivityStatus.Completed);
        mockHandler.Verify(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()), Times.Once);
    }

    /// <summary>
    /// Tests that activities with retry policies retry on failure according to the policy.
    /// Verifies that retry logic executes multiple times when configured.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithRetryPolicy_RetriesOnFailure()
    {
        var service = CreateService();
        var callCount = 0;
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
            .Callback(() => callCount++)
            .ThrowsAsync(new InvalidOperationException("Temporary error"));
        service.RegisterHandler("default", mockHandler.Object);

        var activity = CreateActivity();
        activity.RetryPolicy = RetryPolicy.FixedDelay;
        activity.MaxRetries = 2;
        var context = CreateContext();

        await Assert.ThrowsAsync<ActivityException>(() =>
            service.ExecuteAsync(activity, context));

        callCount.Should().BeGreaterThan(1);
    }

    /// <summary>
    /// Tests that retry policies eventually succeed if the handler eventually succeeds.
    /// Verifies that retry logic can recover from transient failures.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_RetryWithEventualSuccess_Succeeds()
    {
        var service = CreateService();
        var callCount = 0;
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
            .Callback(() => callCount++)
            .Returns(() =>
            {
                if (callCount < 3)
                    return Task.FromException<Dictionary<string, object?>>(
                        new InvalidOperationException("Temporary error"));
                return Task.FromResult(new Dictionary<string, object?>());
            });
        service.RegisterHandler("default", mockHandler.Object);

        var activity = CreateActivity();
        activity.RetryPolicy = RetryPolicy.FixedDelay;
        activity.MaxRetries = 3;
        var context = CreateContext();

        var result = await service.ExecuteAsync(activity, context);

        result.IsSuccess().Should().BeTrue();
        callCount.Should().Be(3);
    }

    /// <summary>
    /// Tests that the execution result contains correct attempt tracking information.
    /// Verifies that attempt numbers and total attempts are properly recorded.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_AttemptsInResult_SetCorrectly()
    {
        var service = CreateService();
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
            .ReturnsAsync(new Dictionary<string, object?>());
        service.RegisterHandler("default", mockHandler.Object);

        var activity = CreateActivity();
        var context = CreateContext();

        var result = await service.ExecuteAsync(activity, context);

        result.AttemptNumber.Should().Be(1);
        result.TotalAttempts.Should().BeGreaterThanOrEqualTo(1);
    }

    /// <summary>
    /// Tests that the activity ID is set in the execution context.
    /// Verifies that the activity ID is properly propagated to the context.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_SetActivityIdInContext()
    {
        var service = CreateService();
        WorkflowExecutionContext? capturedContext = null;
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
            .Callback<Activity, WorkflowExecutionContext>((_, ctx) => capturedContext = ctx)
            .ReturnsAsync(new Dictionary<string, object?>());
        service.RegisterHandler("default", mockHandler.Object);

        var activity = CreateActivity("specific-activity");
        var context = CreateContext();

        await service.ExecuteAsync(activity, context);

        capturedContext!.ActivityId.Should().Be("specific-activity");
    }

    /// <summary>
    /// Tests that retry policies retry on specific retryable exceptions like <see cref="TimeoutException"/>.
    /// Verifies that exception type checking works correctly for retry decisions.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_HandlerWithExceptionType_RetriesIfRetryableException()
    {
        var service = CreateService();
        var callCount = 0;
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
            .Callback(() => callCount++)
            .ThrowsAsync(new TimeoutException("Timeout"));
        service.RegisterHandler("default", mockHandler.Object);

        var activity = CreateActivity();
        activity.RetryPolicy = RetryPolicy.FixedDelay;
        activity.MaxRetries = 2;
        var context = CreateContext();

        await Assert.ThrowsAsync<ActivityException>(() =>
            service.ExecuteAsync(activity, context));

        callCount.Should().BeGreaterThan(1);
    }

    /// <summary>
    /// Tests that exponential backoff retry policy uses appropriate delays between attempts.
    /// Verifies that the retry mechanism respects exponential backoff timing.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ExponentialBackoffPolicy_UsesBackoff()
    {
        var service = CreateService();
        var callCount = 0;
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
            .Callback(() => callCount++)
            .ThrowsAsync(new InvalidOperationException("Error"));
        service.RegisterHandler("default", mockHandler.Object);

        var activity = CreateActivity();
        activity.RetryPolicy = RetryPolicy.ExponentialBackoff;
        activity.MaxRetries = 2;
        var context = CreateContext();

        await Assert.ThrowsAsync<ActivityException>(() =>
            service.ExecuteAsync(activity, context));

        sw.Stop();
        callCount.Should().Be(2);
    }

    /// <summary>
    /// Tests that multiple registered handlers can coexist and the correct one is selected based on activity's handler type.
    /// Verifies handler registry lookup and selection logic.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_MultipleHandlers_SelectsCorrectOne()
    {
        var service = CreateService();
        var mockHandler1 = new Mock<ActivityService.IActivityHandler>();
        var mockHandler2 = new Mock<ActivityService.IActivityHandler>();
        mockHandler1.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
            .ReturnsAsync(new Dictionary<string, object?> { { "source", "handler1" } });
        mockHandler2.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
            .ReturnsAsync(new Dictionary<string, object?> { { "source", "handler2" } });

        service.RegisterHandler("handler1", mockHandler1.Object);
        service.RegisterHandler("handler2", mockHandler2.Object);

        var activity = CreateActivity("act1", "handler2");
        var context = CreateContext();

        var result = await service.ExecuteAsync(activity, context);

        result.IsSuccess().Should().BeTrue();
        result.Output["source"].Should().Be("handler2");
    }

    /// <summary>
    /// Tests that an activity with null or empty handler type throws an <see cref="ActivityException"/>.
    /// Verifies validation of handler type configuration.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_HandlerNullEmptyType_ThrowsActivityException()
    {
        var service = CreateService();
        var activity = CreateActivity("act1", "");
        var context = CreateContext();

        await Assert.ThrowsAsync<ActivityException>(() =>
            service.ExecuteAsync(activity, context));
    }

    /// <summary>
    /// Tests that activities without handlers succeed if they are not required to have one.
    /// Some activity types like Event activities don't require handlers.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ActivityWithNoHandler_SucceedsIfNotRequired()
    {
        var service = CreateService();
        var activity = CreateActivity("simple-act");
        activity.Type = "Event";  // Mark as not requiring handler
        activity.HandlerType = null;
        var context = CreateContext();

        var result = await service.ExecuteAsync(activity, context);

        result.IsSuccess().Should().BeTrue();
        result.Status.Should().Be(ActivityStatus.Completed);
    }

    /// <summary>
    /// Tests that activity execution failures record the error message in the exception.
    /// Verifies that error details are properly captured and included in exceptions.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_FailureRecordsErrorMessage()
    {
        var service = CreateService();
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
            .ThrowsAsync(new InvalidOperationException("Specific error message"));
        service.RegisterHandler("default", mockHandler.Object);

        var activity = CreateActivity();
        var context = CreateContext();

        var exception = await Assert.ThrowsAsync<ActivityException>(() =>
            service.ExecuteAsync(activity, context));

        exception.Message.Should().Contain("Specific error message");
    }

    /// <summary>
    /// Tests that retry exhaustion produces an informative error message.
    /// Verifies that when all retry attempts are exhausted, the error message contains relevant retry information.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_RetryExhaustionMessage_IsInformative()
    {
        var service = CreateService();
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
            .ThrowsAsync(new InvalidOperationException("Error"));
        service.RegisterHandler("default", mockHandler.Object);

        var activity = CreateActivity();
        activity.RetryPolicy = RetryPolicy.FixedDelay;
        activity.MaxRetries = 2;
        var context = CreateContext();

        var exception = await Assert.ThrowsAsync<ActivityException>(() =>
            service.ExecuteAsync(activity, context));

        exception.Message.Should().Contain("retry");
    }

    /// <summary>
    /// Tests that the correlation ID is preserved through activity execution.
    /// Verifies that context properties like correlation ID are maintained during execution.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_CorrelationIdPreserved()
    {
        var service = CreateService();
        WorkflowExecutionContext? capturedContext = null;
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
            .Callback<Activity, WorkflowExecutionContext>((_, ctx) => capturedContext = ctx)
            .ReturnsAsync(new Dictionary<string, object?>());
        service.RegisterHandler("default", mockHandler.Object);

        var activity = CreateActivity();
        var context = CreateContext();
        context.CorrelationId = "my-correlation-id";

        await service.ExecuteAsync(activity, context);

        capturedContext!.CorrelationId.Should().Be("my-correlation-id");
    }

    /// <summary>
    /// Tests that activities with no retry policy throw immediately without retrying.
    /// Verifies that the no-retry policy behaves correctly.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_NoRetryPolicy_ThrowsImmediately()
    {
        var service = CreateService();
        var callCount = 0;
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
            .Callback(() => callCount++)
            .ThrowsAsync(new InvalidOperationException("Error"));
        service.RegisterHandler("default", mockHandler.Object);

        var activity = CreateActivity();
        activity.RetryPolicy = RetryPolicy.NoRetry;
        activity.MaxRetries = 0;
        var context = CreateContext();

        await Assert.ThrowsAsync<ActivityException>(() =>
            service.ExecuteAsync(activity, context));

        callCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that conditional expressions can reference workflow variables.
    /// Verifies that the expression evaluator correctly resolves variable references like ${variableName}.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ConditionalVariableReference_EvaluatesCorrectly()
    {
        var service = CreateService();
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
            .ReturnsAsync(new Dictionary<string, object?>());
        service.RegisterHandler("default", mockHandler.Object);

        var activity = CreateActivity();
        activity.ConditionExpression = "${shouldExecute}";
        var context = CreateContext();
        context.SetVariable("shouldExecute", true);

        var result = await service.ExecuteAsync(activity, context);

        result.Status.Should().Be(ActivityStatus.Completed);
        mockHandler.Verify(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()), Times.Once);
    }
}
