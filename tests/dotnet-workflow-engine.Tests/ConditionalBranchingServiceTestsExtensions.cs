// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using WorkflowExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

namespace DotNetWorkflowEngine.Tests;

/// <summary>
/// Extension methods for <see cref="ConditionalBranchingServiceTests"/> to provide fluent assertions
/// and helper methods for testing conditional branching scenarios.
/// </summary>
public static class ConditionalBranchingServiceTestsExtensions
{
    /// <summary>
    /// Asserts that the result contains exactly one selected transition with the specified ID.
    /// </summary>
    public static async Task<BranchingResult> ShouldHaveSingleSelectedTransitionAsync(
        this Task<BranchingResult> task,
        string expectedTransitionId)
    {
        var result = await task;
        result.SelectedTransitions.Should().HaveCount(1, "Expected exactly one selected transition");
        result.SelectedTransitions[0].Id.Should().Be(expectedTransitionId, "Transition ID mismatch");
        return result;
    }

    /// <summary>
    /// Asserts that the result contains exactly one skipped transition with the specified ID.
    /// </summary>
    public static async Task<BranchingResult> ShouldHaveSingleSkippedTransitionAsync(
        this Task<BranchingResult> task,
        string expectedTransitionId)
    {
        var result = await task;
        result.SkippedTransitions.Should().HaveCount(1, "Expected exactly one skipped transition");
        result.SkippedTransitions[0].Id.Should().Be(expectedTransitionId, "Transition ID mismatch");
        return result;
    }

    /// <summary>
    /// Asserts that the result contains the specified number of selected transitions.
    /// </summary>
    public static async Task<BranchingResult> ShouldHaveSelectedTransitionsCountAsync(
        this Task<BranchingResult> task,
        int expectedCount)
    {
        var result = await task;
        result.SelectedTransitions.Should().HaveCount(expectedCount, $"Expected {expectedCount} selected transitions");
        return result;
    }

    /// <summary>
    /// Asserts that the result contains the specified number of skipped transitions.
    /// </summary>
    public static async Task<BranchingResult> ShouldHaveSkippedTransitionsCountAsync(
        this Task<BranchingResult> task,
        int expectedCount)
    {
        var result = await task;
        result.SkippedTransitions.Should().HaveCount(expectedCount, $"Expected {expectedCount} skipped transitions");
        return result;
    }

    /// <summary>
    /// Asserts that the result has no evaluation errors.
    /// </summary>
    public static async Task<BranchingResult> ShouldHaveNoErrorsAsync(
        this Task<BranchingResult> task)
    {
        var result = await task;
        result.EvaluationErrors.Should().BeEmpty("Expected no evaluation errors");
        return result;
    }

    /// <summary>
    /// Asserts that the result has the specified number of evaluation errors.
    /// </summary>
    public static async Task<BranchingResult> ShouldHaveErrorsCountAsync(
        this Task<BranchingResult> task,
        int expectedCount)
    {
        var result = await task;
        result.EvaluationErrors.Should().HaveCount(expectedCount, $"Expected {expectedCount} evaluation errors");
        return result;
    }

    /// <summary>
    /// Asserts that the result indicates a default transition was used.
    /// </summary>
    public static async Task<BranchingResult> ShouldHaveUsedDefaultTransitionAsync(
        this Task<BranchingResult> task)
    {
        var result = await task;
        result.UsedDefaultTransition.Should().BeTrue("Expected default transition to be used");
        return result;
    }

    /// <summary>
    /// Asserts that the result indicates no default transition was used.
    /// </summary>
    public static async Task<BranchingResult> ShouldNotHaveUsedDefaultTransitionAsync(
        this Task<BranchingResult> task)
    {
        var result = await task;
        result.UsedDefaultTransition.Should().BeFalse("Expected no default transition to be used");
        return result;
    }

    /// <summary>
    /// Asserts that the result indicates at least one condition matched.
    /// </summary>
    public static async Task<BranchingResult> ShouldHaveAnyConditionMatchedAsync(
        this Task<BranchingResult> task)
    {
        var result = await task;
        result.AnyConditionMatched.Should().BeTrue("Expected at least one condition to match");
        return result;
    }

    /// <summary>
    /// Asserts that the result indicates no conditions matched.
    /// </summary>
    public static async Task<BranchingResult> ShouldNotHaveAnyConditionMatchedAsync(
        this Task<BranchingResult> task)
    {
        var result = await task;
        result.AnyConditionMatched.Should().BeFalse("Expected no conditions to match");
        return result;
    }

    /// <summary>
    /// Asserts that the result contains transitions with the specified IDs in the given order.
    /// </summary>
    public static async Task<BranchingResult> ShouldHaveSelectedTransitionsInOrderAsync(
        this Task<BranchingResult> task,
        params string[] expectedTransitionIds)
    {
        var result = await task;
        result.SelectedTransitions.Should().HaveCount(expectedTransitionIds.Length,
            $"Expected {expectedTransitionIds.Length} selected transitions");

        for (int i = 0; i < expectedTransitionIds.Length; i++)
        {
            result.SelectedTransitions[i].Id.Should().Be(expectedTransitionIds[i],
                $"Transition at index {i} should have ID '{expectedTransitionIds[i]}'");
        }

        return result;
    }

    /// <summary>
    /// Asserts that the result contains skipped transitions with the specified IDs.
    /// </summary>
    public static async Task<BranchingResult> ShouldHaveSkippedTransitionsAsync(
        this Task<BranchingResult> task,
        params string[] expectedTransitionIds)
    {
        var result = await task;
        result.SkippedTransitions.Should().HaveCount(expectedTransitionIds.Length,
            $"Expected {expectedTransitionIds.Length} skipped transitions");

        foreach (var expectedId in expectedTransitionIds)
        {
            result.SkippedTransitions.Should().Contain(t => t.Id == expectedId,
                $"Expected to find skipped transition with ID '{expectedId}'");
        }

        return result;
    }

    /// <summary>
    /// Creates a workflow with the specified activities.
    /// </summary>
    public static Workflow CreateWorkflow(
        string workflowId,
        params (string id, string name)[] activities)
    {
        var activityList = activities.Select(a => new Activity { Id = a.id, Name = a.name, TimeoutSeconds = 30, MaxRetries = 0 }).ToList();
        return new Workflow
        {
            Id = workflowId,
            Name = "Test Workflow",
            Activities = activityList,
            Transitions = new List<Transition>()
        };
    }

    /// <summary>
    /// Creates a workflow execution context with the specified variables.
    /// </summary>
    public static WorkflowExecutionContext CreateContext(
        Dictionary<string, object?>? variables = null)
    {
        return new WorkflowExecutionContext
        {
            WorkflowInstanceId = "inst-1",
            Variables = variables ?? new Dictionary<string, object?>()
        };
    }

    /// <summary>
    /// Creates a ConditionalBranchingService for testing.
    /// </summary>
    public static ConditionalBranchingService CreateService()
    {
        var loggerMock = new Mock<ILogger<ConditionalBranchingService>>();
        return new ConditionalBranchingService(loggerMock.Object);
    }

    /// <summary>
    /// Creates a workflow with the specified activities and transitions.
    /// </summary>
    public static Workflow CreateWorkflowWithTransitions(
        string workflowId,
        params (string id, string name, string toActivityId, string? conditionExpression, bool isDefault)[] transitions)
    {
        var workflow = CreateWorkflow(workflowId, transitions.Select(t => (t.id, t.name)).ToArray());

        foreach (var transition in transitions)
        {
            workflow.Transitions.Add(new Transition
            {
                Id = transition.id,
                FromActivityId = transition.id,
                ToActivityId = transition.toActivityId,
                ConditionExpression = transition.conditionExpression,
                IsDefault = transition.isDefault
            });
        }

        return workflow;
    }

    /// <summary>
    /// Validates that the workflow has valid transition expressions.
    /// </summary>
    public static void ShouldHaveValidTransitionExpressions(
        this Workflow workflow)
    {
        var service = CreateService();
        var errors = service.ValidateTransitionExpressions(workflow);
        errors.Should().BeEmpty("Expected all transition expressions to be valid");
    }

    /// <summary>
    /// Validates that the workflow has the specified number of invalid transition expressions.
    /// </summary>
    public static void ShouldHaveInvalidTransitionExpressions(
        this Workflow workflow,
        int expectedErrorCount)
    {
        var service = CreateService();
        var errors = service.ValidateTransitionExpressions(workflow);
        errors.Should().HaveCount(expectedErrorCount, $"Expected {expectedErrorCount} invalid transition expressions");
    }
}
