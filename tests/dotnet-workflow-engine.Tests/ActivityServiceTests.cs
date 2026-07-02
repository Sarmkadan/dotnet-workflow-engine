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

public class ActivityServiceTests
{
    private ActivityService CreateService()
    {
        var retryPolicyService = new RetryPolicyService();
        return new ActivityService(retryPolicyService);
    }

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

    private WorkflowExecutionContext CreateContext()
    {
        return new WorkflowExecutionContext
        {
            WorkflowInstanceId = "inst-1",
            CorrelationId = "corr-1"
        };
    }

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

    [Fact]
    public async Task ExecuteAsync_GatewayActivity_ReturnsSuccessWithoutHandler()
    {
        var service = CreateService();
        var activity = CreateActivity("gateway-act", null!);
        activity.ActivityType = "ParallelGateway";
        var context = CreateContext();

        var result = await service.ExecuteAsync(activity, context);

        result.IsSuccess().Should().BeTrue();
        result.Status.Should().Be(ActivityStatus.Completed);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidActivity_ThrowsValidationException()
    {
        var service = CreateService();
        var activity = CreateActivity();
        activity.Validate = _ => false; // Mark as invalid

        var context = CreateContext();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.ExecuteAsync(activity, context));
    }

    [Fact]
    public async Task ExecuteAsync_NoHandlerRegistered_ThrowsActivityException()
    {
        var service = CreateService();
        var activity = CreateActivity("act1", "unregistered-handler");
        var context = CreateContext();

        await Assert.ThrowsAsync<ActivityException>(() =>
            service.ExecuteAsync(activity, context));
    }

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

    [Fact]
    public async Task ExecuteAsync_HandlerNullEmptyType_ThrowsActivityException()
    {
        var service = CreateService();
        var activity = CreateActivity("act1", "");
        var context = CreateContext();

        await Assert.ThrowsAsync<ActivityException>(() =>
            service.ExecuteAsync(activity, context));
    }

    [Fact]
    public async Task ExecuteAsync_ActivityWithNoHandler_SucceedsIfNotRequired()
    {
        var service = CreateService();
        var activity = CreateActivity("simple-act");
        activity.ActivityType = "SimpleAction";  // Mark as not requiring handler
        activity.HandlerType = null;
        activity.RequiresHandler = () => false;
        var context = CreateContext();

        var result = await service.ExecuteAsync(activity, context);

        result.IsSuccess().Should().BeTrue();
        result.Status.Should().Be(ActivityStatus.Completed);
    }

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
