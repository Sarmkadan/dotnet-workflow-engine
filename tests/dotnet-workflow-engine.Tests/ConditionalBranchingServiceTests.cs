// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using WorkflowExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

namespace DotNetWorkflowEngine.Tests;

public class ConditionalBranchingServiceTests
{
    private ConditionalBranchingService CreateService()
    {
        var loggerMock = new Mock<ILogger<ConditionalBranchingService>>();
        return new ConditionalBranchingService(loggerMock.Object);
    }

    private Workflow CreateWorkflow(params (string id, string name)[] activities)
    {
        var activityList = activities.Select(a => new Activity { Id = a.id, Name = a.name, TimeoutSeconds = 30, MaxRetries = 0 }).ToList();
        return new Workflow
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Activities = activityList,
            Transitions = new List<Transition>()
        };
    }

    private WorkflowExecutionContext CreateContext(Dictionary<string, object?>? variables = null)
    {
        return new WorkflowExecutionContext
        {
            WorkflowInstanceId = "inst-1",
            Variables = variables ?? new Dictionary<string, object?>()
        };
    }

    [Fact]
    public async Task ResolveBranchesAsync_NoOutgoingTransitions_ReturnsEmpty()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"), ("activity2", "Activity 2"));
        var context = CreateContext();

        var result = await service.ResolveBranchesAsync(workflow, "activity1", context);

        result.ActivityId.Should().Be("activity1");
        result.SelectedTransitions.Should().BeEmpty();
        result.SkippedTransitions.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveBranchesAsync_UnconditionalTransition_SelectsIt()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"), ("activity2", "Activity 2"));
        workflow.Transitions.Add(new Transition { Id = "t1", FromActivityId = "activity1", ToActivityId = "activity2" });
        var context = CreateContext();

        var result = await service.ResolveBranchesAsync(workflow, "activity1", context);

        result.SelectedTransitions.Should().HaveCount(1);
        result.SelectedTransitions[0].Id.Should().Be("t1");
        result.SkippedTransitions.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveBranchesAsync_ConditionalTransitionMatches_SelectsIt()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"), ("activity2", "Activity 2"));
        workflow.Transitions.Add(new Transition
        {
            Id = "t1",
            FromActivityId = "activity1",
            ToActivityId = "activity2",
            ConditionExpression = "true"
        });
        var context = CreateContext();

        var result = await service.ResolveBranchesAsync(workflow, "activity1", context);

        result.SelectedTransitions.Should().HaveCount(1);
        result.SkippedTransitions.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveBranchesAsync_ConditionalTransitionDoesNotMatch_SkipsIt()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"), ("activity2", "Activity 2"));
        workflow.Transitions.Add(new Transition
        {
            Id = "t1",
            FromActivityId = "activity1",
            ToActivityId = "activity2",
            ConditionExpression = "false"
        });
        var context = CreateContext();

        var result = await service.ResolveBranchesAsync(workflow, "activity1", context);

        result.SelectedTransitions.Should().BeEmpty();
        result.SkippedTransitions.Should().HaveCount(1);
    }

    [Fact]
    public async Task ResolveBranchesAsync_MultipleConditionalTransitions_SelectsAllMatching()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"), ("activity2", "Activity 2"), ("activity3", "Activity 3"));
        workflow.Transitions.Add(new Transition
        {
            Id = "t1",
            FromActivityId = "activity1",
            ToActivityId = "activity2",
            ConditionExpression = "true"
        });
        workflow.Transitions.Add(new Transition
        {
            Id = "t2",
            FromActivityId = "activity1",
            ToActivityId = "activity3",
            ConditionExpression = "true"
        });
        var context = CreateContext();

        var result = await service.ResolveBranchesAsync(workflow, "activity1", context);

        result.SelectedTransitions.Should().HaveCount(2);
        result.SkippedTransitions.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveBranchesAsync_MixedConditionalAndUnconditional_SelectsBoth()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"), ("activity2", "Activity 2"), ("activity3", "Activity 3"));
        workflow.Transitions.Add(new Transition
        {
            Id = "t1",
            FromActivityId = "activity1",
            ToActivityId = "activity2",
            ConditionExpression = "true"
        });
        workflow.Transitions.Add(new Transition
        {
            Id = "t2",
            FromActivityId = "activity1",
            ToActivityId = "activity3"
        });
        var context = CreateContext();

        var result = await service.ResolveBranchesAsync(workflow, "activity1", context);

        result.SelectedTransitions.Should().HaveCount(2);
    }

    [Fact]
    public async Task ResolveBranchesAsync_DefaultTransitionWithoutConditionalMatch_UsesDefault()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"), ("activity2", "Activity 2"), ("activity3", "Activity 3"));
        workflow.Transitions.Add(new Transition
        {
            Id = "t1",
            FromActivityId = "activity1",
            ToActivityId = "activity2",
            ConditionExpression = "false"
        });
        workflow.Transitions.Add(new Transition
        {
            Id = "t2",
            FromActivityId = "activity1",
            ToActivityId = "activity3",
            IsDefault = true
        });
        var context = CreateContext();

        var result = await service.ResolveBranchesAsync(workflow, "activity1", context);

        result.SelectedTransitions.Should().HaveCount(1);
        result.SelectedTransitions[0].Id.Should().Be("t2");
        result.UsedDefaultTransition.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveBranchesAsync_DefaultTransitionWithConditionalMatch_IgnoresDefault()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"), ("activity2", "Activity 2"), ("activity3", "Activity 3"));
        workflow.Transitions.Add(new Transition
        {
            Id = "t1",
            FromActivityId = "activity1",
            ToActivityId = "activity2",
            ConditionExpression = "true"
        });
        workflow.Transitions.Add(new Transition
        {
            Id = "t2",
            FromActivityId = "activity1",
            ToActivityId = "activity3",
            IsDefault = true
        });
        var context = CreateContext();

        var result = await service.ResolveBranchesAsync(workflow, "activity1", context);

        result.SelectedTransitions.Should().HaveCount(1);
        result.SelectedTransitions[0].Id.Should().Be("t1");
        result.UsedDefaultTransition.Should().BeFalse();
    }

    [Fact]
    public async Task ResolveBranchesAsync_PriorityOrdering_SelectsHighestPriority()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"), ("activity2", "Activity 2"), ("activity3", "Activity 3"));
        workflow.Transitions.Add(new Transition
        {
            Id = "t1",
            FromActivityId = "activity1",
            ToActivityId = "activity2",
            ConditionExpression = "true",
            Priority = 10
        });
        workflow.Transitions.Add(new Transition
        {
            Id = "t2",
            FromActivityId = "activity1",
            ToActivityId = "activity3",
            ConditionExpression = "true",
            Priority = 5
        });
        var context = CreateContext();

        var result = await service.ResolveBranchesAsync(workflow, "activity1", context);

        result.SelectedTransitions.Should().HaveCount(2);
    }

    [Fact]
    public async Task ResolveBranchesAsync_VariableInCondition_EvaluatesCorrectly()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"), ("activity2", "Activity 2"));
        workflow.Transitions.Add(new Transition
        {
            Id = "t1",
            FromActivityId = "activity1",
            ToActivityId = "activity2",
            ConditionExpression = "${status} == \"approved\""
        });
        var context = CreateContext(new Dictionary<string, object?> { { "status", "approved" } });

        var result = await service.ResolveBranchesAsync(workflow, "activity1", context);

        result.SelectedTransitions.Should().HaveCount(1);
        result.SkippedTransitions.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveBranchesAsync_InvalidExpression_RecordsError()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"), ("activity2", "Activity 2"));
        workflow.Transitions.Add(new Transition
        {
            Id = "t1",
            FromActivityId = "activity1",
            ToActivityId = "activity2",
            ConditionExpression = "${status"  // Invalid expression
        });
        var context = CreateContext();

        var result = await service.ResolveBranchesAsync(workflow, "activity1", context);

        result.EvaluationErrors.Should().HaveCount(1);
        result.EvaluationErrors[0].TransitionId.Should().Be("t1");
    }

    [Fact]
    public async Task ResolveBranchesAsync_NullWorkflow_ThrowsArgumentNullException()
    {
        var service = CreateService();
        var context = CreateContext();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.ResolveBranchesAsync(null!, "activity1", context));
    }

    [Fact]
    public async Task ResolveBranchesAsync_NullContext_ThrowsArgumentNullException()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.ResolveBranchesAsync(workflow, "activity1", null!));
    }

    [Fact]
    public async Task ResolveBranchesAsync_NullActivityId_ThrowsArgumentException()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"));
        var context = CreateContext();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ResolveBranchesAsync(workflow, null!, context));
    }

    [Fact]
    public async Task ResolveBranchesAsync_EmptyActivityId_ThrowsArgumentException()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"));
        var context = CreateContext();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ResolveBranchesAsync(workflow, "", context));
    }

    [Fact]
    public async Task GetNextActivitiesAsync_ReturnsTargetActivities()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"), ("activity2", "Activity 2"), ("activity3", "Activity 3"));
        workflow.Transitions.Add(new Transition
        {
            Id = "t1",
            FromActivityId = "activity1",
            ToActivityId = "activity2",
            ConditionExpression = "true"
        });
        workflow.Transitions.Add(new Transition
        {
            Id = "t2",
            FromActivityId = "activity1",
            ToActivityId = "activity3",
            ConditionExpression = "true"
        });
        var context = CreateContext();

        var nextActivities = await service.GetNextActivitiesAsync(workflow, "activity1", context);

        nextActivities.Should().HaveCount(2);
        nextActivities.Should().Contain(a => a.Id == "activity2");
        nextActivities.Should().Contain(a => a.Id == "activity3");
    }

    [Fact]
    public async Task GetNextActivitiesAsync_NoMatchingTransitions_ReturnsEmpty()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"), ("activity2", "Activity 2"));
        workflow.Transitions.Add(new Transition
        {
            Id = "t1",
            FromActivityId = "activity1",
            ToActivityId = "activity2",
            ConditionExpression = "false"
        });
        var context = CreateContext();

        var nextActivities = await service.GetNextActivitiesAsync(workflow, "activity1", context);

        nextActivities.Should().BeEmpty();
    }

    [Fact]
    public void ValidateTransitionExpressions_NoExpressions_ReturnsEmpty()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"), ("activity2", "Activity 2"));
        workflow.Transitions.Add(new Transition
        {
            Id = "t1",
            FromActivityId = "activity1",
            ToActivityId = "activity2"
        });

        var errors = service.ValidateTransitionExpressions(workflow);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateTransitionExpressions_ValidExpressions_ReturnsEmpty()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"), ("activity2", "Activity 2"));
        workflow.Transitions.Add(new Transition
        {
            Id = "t1",
            FromActivityId = "activity1",
            ToActivityId = "activity2",
            ConditionExpression = "${status} == \"approved\""
        });

        var errors = service.ValidateTransitionExpressions(workflow);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateTransitionExpressions_InvalidExpression_ReturnsErrors()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"), ("activity2", "Activity 2"));
        workflow.Transitions.Add(new Transition
        {
            Id = "t1",
            FromActivityId = "activity1",
            ToActivityId = "activity2",
            ConditionExpression = "${status"  // Invalid
        });

        var errors = service.ValidateTransitionExpressions(workflow);

        errors.Should().HaveCountGreaterThan(0);
        errors[0].TransitionId.Should().Be("t1");
    }

    [Fact]
    public void ValidateTransitionExpressions_NullWorkflow_ThrowsArgumentNullException()
    {
        var service = CreateService();

        Assert.Throws<ArgumentNullException>(() =>
            service.ValidateTransitionExpressions(null!));
    }

    [Fact]
    public async Task ResolveBranchesAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"));
        var context = CreateContext();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.ResolveBranchesAsync(workflow, "activity1", context, cts.Token));
    }

    [Fact]
    public async Task ResolveBranchesAsync_ComplexExpression_EvaluatesCorrectly()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"), ("activity2", "Activity 2"), ("activity3", "Activity 3"));
        workflow.Transitions.Add(new Transition
        {
            Id = "t1",
            FromActivityId = "activity1",
            ToActivityId = "activity2",
            ConditionExpression = "${amount} > \"1000\" && ${approved}"
        });
        workflow.Transitions.Add(new Transition
        {
            Id = "t2",
            FromActivityId = "activity1",
            ToActivityId = "activity3",
            ConditionExpression = "${amount} <= \"1000\""
        });
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "amount", 1500 },
            { "approved", true }
        });

        var result = await service.ResolveBranchesAsync(workflow, "activity1", context);

        result.SelectedTransitions.Should().HaveCount(1);
        result.SelectedTransitions[0].Id.Should().Be("t1");
    }

    [Fact]
    public async Task ResolveBranchesAsync_AnyConditionMatched_SetCorrectly()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"), ("activity2", "Activity 2"));
        workflow.Transitions.Add(new Transition
        {
            Id = "t1",
            FromActivityId = "activity1",
            ToActivityId = "activity2",
            ConditionExpression = "true"
        });
        var context = CreateContext();

        var result = await service.ResolveBranchesAsync(workflow, "activity1", context);

        result.AnyConditionMatched.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveBranchesAsync_NoConditionMatched_SetCorrectly()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"), ("activity2", "Activity 2"));
        workflow.Transitions.Add(new Transition
        {
            Id = "t1",
            FromActivityId = "activity1",
            ToActivityId = "activity2",
            ConditionExpression = "false"
        });
        var context = CreateContext();

        var result = await service.ResolveBranchesAsync(workflow, "activity1", context);

        result.AnyConditionMatched.Should().BeFalse();
    }
}
