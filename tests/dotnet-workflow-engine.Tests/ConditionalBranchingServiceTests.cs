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

/// <summary>
/// Provides unit tests for the <see cref="ConditionalBranchingService"/> class,
/// verifying branch resolution, activity navigation, and transition expression validation.
/// </summary>
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

    /// <summary>Verifies that ResolveBranchesAsync returns no transitions when an activity has no outgoing transitions.</summary>
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

    /// <summary>Verifies that ResolveBranchesAsync selects an unconditional transition.</summary>
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

    /// <summary>Verifies that ResolveBranchesAsync selects a conditional transition when the condition evaluates to true.</summary>
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

    /// <summary>Verifies that ResolveBranchesAsync skips a conditional transition when the condition evaluates to false.</summary>
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

    /// <summary>Verifies that ResolveBranchesAsync selects multiple conditional transitions when all conditions evaluate to true.</summary>
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

    /// <summary>Verifies that ResolveBranchesAsync selects both conditional and unconditional transitions when the conditional one matches.</summary>
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

    /// <summary>Verifies that ResolveBranchesAsync selects the default transition when no conditional transitions match.</summary>
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

    /// <summary>Verifies that ResolveBranchesAsync ignores the default transition when a conditional transition matches.</summary>
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

    /// <summary>Verifies that ResolveBranchesAsync selects multiple transitions according to their priority, even if they have matching conditions.</summary>
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

    /// <summary>Verifies that ResolveBranchesAsync evaluates conditional expressions containing variables correctly.</summary>
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

    /// <summary>Verifies that ResolveBranchesAsync records an error when a conditional expression is invalid.</summary>
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

    /// <summary>Verifies that ResolveBranchesAsync throws an ArgumentNullException when the workflow is null.</summary>
    [Fact]
    public async Task ResolveBranchesAsync_NullWorkflow_ThrowsArgumentNullException()
    {
        var service = CreateService();
        var context = CreateContext();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.ResolveBranchesAsync(null!, "activity1", context));
    }

    /// <summary>Verifies that ResolveBranchesAsync throws an ArgumentNullException when the context is null.</summary>
    [Fact]
    public async Task ResolveBranchesAsync_NullContext_ThrowsArgumentNullException()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.ResolveBranchesAsync(workflow, "activity1", null!));
    }

    /// <summary>Verifies that ResolveBranchesAsync throws an ArgumentException when the activity ID is null.</summary>
    [Fact]
    public async Task ResolveBranchesAsync_NullActivityId_ThrowsArgumentException()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"));
        var context = CreateContext();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ResolveBranchesAsync(workflow, null!, context));
    }

    /// <summary>Verifies that ResolveBranchesAsync throws an ArgumentException when the activity ID is empty.</summary>
    [Fact]
    public async Task ResolveBranchesAsync_EmptyActivityId_ThrowsArgumentException()
    {
        var service = CreateService();
        var workflow = CreateWorkflow(("activity1", "Activity 1"));
        var context = CreateContext();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ResolveBranchesAsync(workflow, "", context));
    }

    /// <summary>Verifies that GetNextActivitiesAsync returns the expected target activities for an activity with multiple matching transitions.</summary>
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

    /// <summary>Verifies that GetNextActivitiesAsync returns an empty collection when no transitions match.</summary>
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

    /// <summary>Verifies that ValidateTransitionExpressions returns no errors when there are no transition expressions.</summary>
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

    /// <summary>Verifies that ValidateTransitionExpressions returns no errors when transition expressions are valid.</summary>
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

    /// <summary>Verifies that ValidateTransitionExpressions returns errors for invalid transition expressions.</summary>
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

    /// <summary>Verifies that ValidateTransitionExpressions throws an ArgumentNullException when the workflow is null.</summary>
    [Fact]
    public void ValidateTransitionExpressions_NullWorkflow_ThrowsArgumentNullException()
    {
        var service = CreateService();

        Assert.Throws<ArgumentNullException>(() =>
            service.ValidateTransitionExpressions(null!));
    }

    /// <summary>Verifies that ResolveBranchesAsync throws an OperationCanceledException when cancellation is requested.</summary>
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

    /// <summary>Verifies that ResolveBranchesAsync correctly evaluates complex conditional expressions with multiple variables.</summary>
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

    /// <summary>Verifies that ResolveBranchesAsync sets the AnyConditionMatched property correctly when a condition is matched.</summary>
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

    /// <summary>Verifies that ResolveBranchesAsync sets the AnyConditionMatched property correctly when no condition is matched.</summary>
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
