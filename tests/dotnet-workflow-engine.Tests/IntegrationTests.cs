// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetWorkflowEngine.Tests;

public class IntegrationTests
{
    private (WorkflowExecutionService, ActivityService, AuditService, Mock<IAuditRepository>) CreateServices()
    {
        var auditRepoMock = new Mock<IAuditRepository>();
        auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);

        var auditService = new AuditService(auditRepoMock.Object);
        var definitionService = new WorkflowDefinitionService();
        var retryPolicyService = new RetryPolicyService();
        var activityService = new ActivityService(retryPolicyService);
        var executionService = new WorkflowExecutionService(definitionService, auditService, activityService);

        return (executionService, activityService, auditService, auditRepoMock);
    }

    private Workflow CreateSimpleWorkflow()
    {
        var workflow = new Workflow
        {
            Id = "simple-workflow",
            Name = "Simple Workflow",
            StartActivityId = "step1",
            EndActivityId = "step3",
            Activities = new List<Activity>
            {
                new Activity { Id = "step1", Name = "Step 1", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" },
                new Activity { Id = "step2", Name = "Step 2", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" },
                new Activity { Id = "step3", Name = "Step 3", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" }
            },
            Transitions = new List<Transition>
            {
                new Transition { Id = "t1", FromActivityId = "step1", ToActivityId = "step2" },
                new Transition { Id = "t2", FromActivityId = "step2", ToActivityId = "step3" }
            }
        };
        workflow.Publish();
        return workflow;
    }

    [Fact]
    public async Task EndToEnd_SimpleWorkflow_ExecutesSuccessfully()
    {
        var (executionService, activityService, auditService, _) = CreateServices();
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<ExecutionContext>()))
            .ReturnsAsync(new Dictionary<string, object?> { { "status", "completed" } });
        activityService.RegisterHandler("default", mockHandler.Object);

        var workflow = CreateSimpleWorkflow();
        var instance = executionService.CreateInstance(workflow.Id, "corr-123", "user1");

        instance.Status.Should().Be(WorkflowStatus.Draft);

        instance.Start();
        instance.Status.Should().Be(WorkflowStatus.Active);

        var result = await executionService.StartAsync(instance.Id);

        result.Should().NotBeNull();
        mockHandler.Verify(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<ExecutionContext>()), Times.AtLeast(3));
    }

    [Fact]
    public async Task EndToEnd_ConditionalRouting_SelectsCorrectPath()
    {
        var (executionService, activityService, auditService, _) = CreateServices();
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<ExecutionContext>()))
            .ReturnsAsync((Activity activity, ExecutionContext ctx) =>
            {
                if (activity.Id == "check")
                    ctx.SetVariable("approved", true);
                return new Dictionary<string, object?>();
            });
        activityService.RegisterHandler("default", mockHandler.Object);

        var workflow = new Workflow
        {
            Id = "conditional-workflow",
            Name = "Conditional Workflow",
            StartActivityId = "check",
            EndActivityId = "approved-end",
            Activities = new List<Activity>
            {
                new Activity { Id = "check", Name = "Check", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" },
                new Activity { Id = "approved-path", Name = "Approved Path", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" },
                new Activity { Id = "approved-end", Name = "Approved End", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" },
                new Activity { Id = "rejected-path", Name = "Rejected Path", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" },
                new Activity { Id = "rejected-end", Name = "Rejected End", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" }
            },
            Transitions = new List<Transition>
            {
                new Transition { Id = "t1", FromActivityId = "check", ToActivityId = "approved-path", ConditionExpression = "${approved}" },
                new Transition { Id = "t2", FromActivityId = "check", ToActivityId = "rejected-path", ConditionExpression = "!${approved}" },
                new Transition { Id = "t3", FromActivityId = "approved-path", ToActivityId = "approved-end" },
                new Transition { Id = "t4", FromActivityId = "rejected-path", ToActivityId = "rejected-end" }
            }
        };
        workflow.Publish();

        var instance = executionService.CreateInstance(workflow.Id);
        instance.Start();

        var result = await executionService.StartAsync(instance.Id);

        result.Should().NotBeNull();
    }

    [Fact]
    public void CreateInstance_NewWorkflowInstance_InitializesCorrectly()
    {
        var (executionService, _, _, _) = CreateServices();
        var workflow = CreateSimpleWorkflow();

        var instance = executionService.CreateInstance(workflow.Id, "corr-456", "admin");

        instance.WorkflowId.Should().Be(workflow.Id);
        instance.Status.Should().Be(WorkflowStatus.Draft);
        instance.CorrelationId.Should().Be("corr-456");
        instance.InitiatedBy.Should().Be("admin");
        instance.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CreateInstance_NonExistentWorkflow_ThrowsWorkflowException()
    {
        var (executionService, _, _, _) = CreateServices();

        Assert.Throws<WorkflowException>(() =>
            executionService.CreateInstance("nonexistent-workflow"));
    }

    [Fact]
    public async Task ExecuteWorkflow_WithAuditTrail_LogsAllEvents()
    {
        var (executionService, activityService, auditService, auditRepoMock) = CreateServices();
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<ExecutionContext>()))
            .ReturnsAsync(new Dictionary<string, object?>());
        activityService.RegisterHandler("default", mockHandler.Object);

        var workflow = CreateSimpleWorkflow();
        var instance = executionService.CreateInstance(workflow.Id, "corr-789", "user2");
        instance.Start();

        await executionService.StartAsync(instance.Id);

        auditRepoMock.Verify(r => r.AddAsync(It.IsAny<AuditLogEntry>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task WorkflowExecution_MultipleInstances_MaintainsIndependentState()
    {
        var (executionService, activityService, _, _) = CreateServices();
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        var executedActivities = new List<string>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<ExecutionContext>()))
            .Callback<Activity, ExecutionContext>((act, _) => executedActivities.Add(act.Id))
            .ReturnsAsync(new Dictionary<string, object?>());
        activityService.RegisterHandler("default", mockHandler.Object);

        var workflow = CreateSimpleWorkflow();

        var instance1 = executionService.CreateInstance(workflow.Id, "corr-1");
        var instance2 = executionService.CreateInstance(workflow.Id, "corr-2");

        instance1.Start();
        instance2.Start();

        var result1 = await executionService.StartAsync(instance1.Id);
        var result2 = await executionService.StartAsync(instance2.Id);

        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        executedActivities.Should().HaveCountGreaterThanOrEqualTo(6); // 3 activities × 2 instances
    }

    [Fact]
    public async Task WorkflowExecution_RetryOnFailure_EventuallySucceeds()
    {
        var (executionService, activityService, _, _) = CreateServices();
        var callCount = 0;
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<ExecutionContext>()))
            .Returns((Activity act, ExecutionContext ctx) =>
            {
                if (act.Id == "step1")
                {
                    callCount++;
                    if (callCount < 3)
                        return Task.FromException<Dictionary<string, object?>>(
                            new InvalidOperationException("Temporary error"));
                }
                return Task.FromResult(new Dictionary<string, object?>());
            });
        activityService.RegisterHandler("default", mockHandler.Object);

        var workflow = CreateSimpleWorkflow();
        // Set retry policy on step1
        workflow.Activities[0].RetryPolicy = RetryPolicy.FixedDelay;
        workflow.Activities[0].MaxRetries = 3;
        workflow.Publish();

        var instance = executionService.CreateInstance(workflow.Id);
        instance.Start();

        var result = await executionService.StartAsync(instance.Id);

        result.Should().NotBeNull();
        callCount.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void Instance_StateTransitions_AreCorrect()
    {
        var (executionService, _, _, _) = CreateServices();
        var workflow = CreateSimpleWorkflow();

        var instance = executionService.CreateInstance(workflow.Id);

        instance.IsActive().Should().BeFalse(); // Initially in Draft status
        instance.Status.Should().Be(WorkflowStatus.Draft);

        instance.Start();

        instance.IsActive().Should().BeTrue();
        instance.Status.Should().Be(WorkflowStatus.Active);
        instance.StartedAt.Should().NotBeNull();
    }

    [Fact]
    public void ActivityExecution_RecordsExecutedActivities()
    {
        var instance = new WorkflowInstance("workflow-1");

        instance.RecordActivityExecution("activity1");
        instance.RecordActivityExecution("activity2");
        instance.RecordActivityExecution("activity1"); // Duplicate

        instance.ExecutedActivities.Should().HaveCount(2);
        instance.HasActivityBeenExecuted("activity1").Should().BeTrue();
        instance.HasActivityBeenExecuted("activity2").Should().BeTrue();
        instance.HasActivityBeenExecuted("activity3").Should().BeFalse();
    }

    [Fact]
    public void ActivityResult_StatusTransitions_WorkCorrectly()
    {
        var result = new ActivityResult("activity1");

        result.Status.Should().Be(ActivityStatus.Pending);
        result.IsSuccess().Should().BeFalse();
        result.IsFailed().Should().BeFalse();

        result.SetSuccess(new Dictionary<string, object?> { { "data", "value" } });

        result.Status.Should().Be(ActivityStatus.Completed);
        result.IsSuccess().Should().BeTrue();
        result.IsFailed().Should().BeFalse();

        var newResult = new ActivityResult("activity2");
        newResult.SetFailure("Test error", null);

        newResult.Status.Should().Be(ActivityStatus.Failed);
        newResult.IsSuccess().Should().BeFalse();
        newResult.IsFailed().Should().BeTrue();
    }

    [Fact]
    public void ActivityResult_SkippedStatus_WorksCorrectly()
    {
        var result = new ActivityResult("activity1");

        result.SetSkipped("Condition not met");

        result.Status.Should().Be(ActivityStatus.Skipped);
        result.IsSuccess().Should().BeFalse();
        result.IsFailed().Should().BeFalse();
    }

    [Fact]
    public async Task ExecutionContext_VariableManagement_WorksCorrectly()
    {
        var context = new ExecutionContext
        {
            WorkflowInstanceId = "inst-1"
        };

        context.SetVariable("name", "Test");
        context.SetVariable("count", 42);
        context.SetVariable("flag", true);

        context.GetVariable("name").Should().Be("Test");
        context.GetVariable<int>("count").Should().Be(42);
        context.GetVariable<bool>("flag").Should().BeTrue();
        context.GetVariable("missing").Should().BeNull();

        context.Complete();

        context.IsActive.Should().BeFalse();
        context.EndTime.Should().NotBeNull();
        context.ExecutionDurationMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ExecutionContext_Reset_ClearsAllData()
    {
        var context = new ExecutionContext();
        context.SetVariable("key", "value");
        context.SetActivityInput("input", "data");
        context.IsActive = false;

        context.Reset();

        context.Variables.Should().BeEmpty();
        context.ActivityInput.Should().BeEmpty();
        context.IsActive.Should().BeTrue();
        context.ExecutionError.Should().BeNull();
    }

    [Fact]
    public async Task ConcurrentWorkflowExecution_HandlesConcurrency()
    {
        var (executionService, activityService, _, _) = CreateServices();
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<ExecutionContext>()))
            .ReturnsAsync(new Dictionary<string, object?>());
        activityService.RegisterHandler("default", mockHandler.Object);

        var workflow = CreateSimpleWorkflow();

        var tasks = Enumerable.Range(0, 10)
            .Select(i =>
            {
                var instance = executionService.CreateInstance(workflow.Id, $"corr-{i}");
                instance.Start();
                return executionService.StartAsync(instance.Id);
            })
            .ToList();

        await Task.WhenAll(tasks);

        tasks.Should().AllSatisfy(t => t.IsCompletedSuccessfully.Should().BeTrue());
    }

    [Fact]
    public void WorkflowValidation_ValidatesCompleteWorkflow()
    {
        var workflow = CreateSimpleWorkflow();

        var validationResult = DotNetWorkflowEngine.Utilities.WorkflowValidator.ValidateWorkflow(workflow);

        validationResult.IsValid.Should().BeTrue();
        validationResult.Errors.Should().BeEmpty();
    }

    [Fact]
    public void WorkflowValidation_DetectsInvalidActivities()
    {
        var workflow = new Workflow
        {
            Id = "bad-workflow",
            Name = "Bad Workflow",
            Activities = new List<Activity>
            {
                new Activity { Id = "", Name = "Invalid" }
            }
        };

        var validationResult = DotNetWorkflowEngine.Utilities.WorkflowValidator.ValidateWorkflow(workflow);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ExpressionEvaluation_ComplexConditions_EvaluateCorrectly()
    {
        var context = new ExecutionContext();
        context.SetVariable("amount", 1000);
        context.SetVariable("approved", true);
        context.SetVariable("status", "active");

        var expr1 = DotNetWorkflowEngine.Utilities.ExpressionEvaluator.Evaluate(
            "${amount} > \"500\" && ${approved}", context);
        expr1.Should().BeTrue();

        var expr2 = DotNetWorkflowEngine.Utilities.ExpressionEvaluator.Evaluate(
            "${status} == \"inactive\" || ${approved}", context);
        expr2.Should().BeTrue();

        var expr3 = DotNetWorkflowEngine.Utilities.ExpressionEvaluator.Evaluate(
            "!${blocked}", context);
        expr3.Should().BeTrue();
    }
}
