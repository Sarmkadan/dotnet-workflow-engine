// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

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
/// <remarks>
/// This class provides a fluent API for testing <see cref="ConditionalBranchingService"/> behavior,
/// including transition selection, error handling, and expression evaluation validation.
/// </remarks>
public static class ConditionalBranchingServiceTestsExtensions
{
    /// <summary>
    /// Asserts that the result contains exactly one selected transition with the specified ID.
    /// </summary>
    /// <param name="task">The task returning the branching result to assert.</param>
    /// <param name="expectedTransitionId">The expected transition ID.</param>
    /// <returns>The original <see cref="BranchingResult"/> for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="task"/> is <see langword="null"/>.</exception>
    public static async Task<BranchingResult> ShouldHaveSingleSelectedTransitionAsync(
        this Task<BranchingResult> task,
        string expectedTransitionId)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        var result = await task;
        result.SelectedTransitions.Should().HaveCount(1, "Expected exactly one selected transition");
        result.SelectedTransitions[0].Id.Should().Be(expectedTransitionId, "Transition ID mismatch");
        return result;
    }

    /// <summary>
    /// Asserts that the result contains exactly one skipped transition with the specified ID.
    /// </summary>
    /// <param name="task">The task returning the branching result to assert.</param>
    /// <param name="expectedTransitionId">The expected transition ID.</param>
    /// <returns>The original <see cref="BranchingResult"/> for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="task"/> is <see langword="null"/>.</exception>
    public static async Task<BranchingResult> ShouldHaveSingleSkippedTransitionAsync(
        this Task<BranchingResult> task,
        string expectedTransitionId)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        var result = await task;
        result.SkippedTransitions.Should().HaveCount(1, "Expected exactly one skipped transition");
        result.SkippedTransitions[0].Id.Should().Be(expectedTransitionId, "Transition ID mismatch");
        return result;
    }

    /// <summary>
    /// Asserts that the result contains the specified number of selected transitions.
    /// </summary>
    /// <param name="task">The task returning the branching result to assert.</param>
    /// <param name="expectedCount">The expected count of selected transitions.</param>
    /// <returns>The original <see cref="BranchingResult"/> for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="task"/> is <see langword="null"/>.</exception>
    public static async Task<BranchingResult> ShouldHaveSelectedTransitionsCountAsync(
        this Task<BranchingResult> task,
        int expectedCount)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        var result = await task;
        result.SelectedTransitions.Should().HaveCount(expectedCount, $"Expected {expectedCount} selected transitions");
        return result;
    }

    /// <summary>
    /// Asserts that the result contains the specified number of skipped transitions.
    /// </summary>
    /// <param name="task">The task returning the branching result to assert.</param>
    /// <param name="expectedCount">The expected count of skipped transitions.</param>
    /// <returns>The original <see cref="BranchingResult"/> for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="task"/> is <see langword="null"/>.</exception>
    public static async Task<BranchingResult> ShouldHaveSkippedTransitionsCountAsync(
        this Task<BranchingResult> task,
        int expectedCount)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        var result = await task;
        result.SkippedTransitions.Should().HaveCount(expectedCount, $"Expected {expectedCount} skipped transitions");
        return result;
    }

    /// <summary>
    /// Asserts that the result has no evaluation errors.
    /// </summary>
    /// <param name="task">The task returning the branching result to assert.</param>
    /// <returns>The original <see cref="BranchingResult"/> for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="task"/> is <see langword="null"/>.</exception>
    public static async Task<BranchingResult> ShouldHaveNoErrorsAsync(
        this Task<BranchingResult> task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        var result = await task;
        result.EvaluationErrors.Should().BeEmpty("Expected no evaluation errors");
        return result;
    }

    /// <summary>
    /// Asserts that the result has the specified number of evaluation errors.
    /// </summary>
    /// <param name="task">The task returning the branching result to assert.</param>
    /// <param name="expectedCount">The expected count of evaluation errors.</param>
    /// <returns>The original <see cref="BranchingResult"/> for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="task"/> is <see langword="null"/>.</exception>
    public static async Task<BranchingResult> ShouldHaveErrorsCountAsync(
        this Task<BranchingResult> task,
        int expectedCount)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        var result = await task;
        result.EvaluationErrors.Should().HaveCount(expectedCount, $"Expected {expectedCount} evaluation errors");
        return result;
    }

    /// <summary>
    /// Asserts that the result indicates a default transition was used.
    /// </summary>
    /// <param name="task">The task returning the branching result to assert.</param>
    /// <returns>The original <see cref="BranchingResult"/> for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="task"/> is <see langword="null"/>.</exception>
    public static async Task<BranchingResult> ShouldHaveUsedDefaultTransitionAsync(
        this Task<BranchingResult> task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        var result = await task;
        result.UsedDefaultTransition.Should().BeTrue("Expected default transition to be used");
        return result;
    }

    /// <summary>
    /// Asserts that the result indicates no default transition was used.
    /// </summary>
    /// <param name="task">The task returning the branching result to assert.</param>
    /// <returns>The original <see cref="BranchingResult"/> for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="task"/> is <see langword="null"/>.</exception>
    public static async Task<BranchingResult> ShouldNotHaveUsedDefaultTransitionAsync(
        this Task<BranchingResult> task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        var result = await task;
        result.UsedDefaultTransition.Should().BeFalse("Expected no default transition to be used");
        return result;
    }

    /// <summary>
    /// Asserts that the result indicates at least one condition matched.
    /// </summary>
    /// <param name="task">The task returning the branching result to assert.</param>
    /// <returns>The original <see cref="BranchingResult"/> for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="task"/> is <see langword="null"/>.</exception>
    public static async Task<BranchingResult> ShouldHaveAnyConditionMatchedAsync(
        this Task<BranchingResult> task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        var result = await task;
        result.AnyConditionMatched.Should().BeTrue("Expected at least one condition to match");
        return result;
    }

    /// <summary>
    /// Asserts that the result indicates no conditions matched.
    /// </summary>
    /// <param name="task">The task returning the branching result to assert.</param>
    /// <returns>The original <see cref="BranchingResult"/> for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="task"/> is <see langword="null"/>.</exception>
    public static async Task<BranchingResult> ShouldNotHaveAnyConditionMatchedAsync(
        this Task<BranchingResult> task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        var result = await task;
        result.AnyConditionMatched.Should().BeFalse("Expected no conditions to match");
        return result;
    }

    /// <summary>
    /// Asserts that the result contains transitions with the specified IDs in the given order.
    /// </summary>
    /// <param name="task">The task returning the branching result to assert.</param>
    /// <param name="expectedTransitionIds">The expected transition IDs in order.</param>
    /// <returns>The original <see cref="BranchingResult"/> for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="task"/> is <see langword="null"/>.</exception>
    public static async Task<BranchingResult> ShouldHaveSelectedTransitionsInOrderAsync(
        this Task<BranchingResult> task,
        params string[] expectedTransitionIds)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

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
    /// <param name="task">The task returning the branching result to assert.</param>
    /// <param name="expectedTransitionIds">The expected skipped transition IDs.</param>
    /// <returns>The original <see cref="BranchingResult"/> for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="task"/> is <see langword="null"/>.</exception>
    public static async Task<BranchingResult> ShouldHaveSkippedTransitionsAsync(
        this Task<BranchingResult> task,
        params string[] expectedTransitionIds)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

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
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="activities">The activities to include in the workflow.</param>
    /// <returns>A new <see cref="Workflow"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="workflowId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="workflowId"/> is whitespace.</exception>
    public static Workflow CreateWorkflow(
        string workflowId,
        params (string id, string name)[] activities)
    {
        ArgumentNullException.ThrowIfNull(workflowId);
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowId, nameof(workflowId));

        var activityList = activities.Select(a => new Activity
        {
            Id = a.id,
            Name = a.name,
            TimeoutSeconds = 30,
            MaxRetries = 0
        }).ToList();

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
    /// <param name="variables">The variables to include in the context; defaults to empty dictionary if null.</param>
    /// <returns>A new <see cref="WorkflowExecutionContext"/> instance.</returns>
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
    /// Creates a <see cref="ConditionalBranchingService"/> for testing.
    /// </summary>
    /// <returns>A new <see cref="ConditionalBranchingService"/> instance.</returns>
    public static ConditionalBranchingService CreateService()
    {
        var loggerMock = new Mock<ILogger<ConditionalBranchingService>>();
        return new ConditionalBranchingService(loggerMock.Object);
    }

    /// <summary>
    /// Creates a workflow with the specified activities and transitions.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="transitions">The transitions to include in the workflow.</param>
    /// <returns>A new <see cref="Workflow"/> instance with activities and transitions.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="workflowId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="workflowId"/> is whitespace.</exception>
    public static Workflow CreateWorkflowWithTransitions(
        string workflowId,
        params (string id, string name, string toActivityId, string? conditionExpression, bool isDefault)[] transitions)
    {
        ArgumentNullException.ThrowIfNull(workflowId);
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowId, nameof(workflowId));

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
    /// <param name="workflow">The workflow to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="workflow"/> is <see langword="null"/>.</exception>
    public static void ShouldHaveValidTransitionExpressions(
        this Workflow workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        var service = CreateService();
        var errors = service.ValidateTransitionExpressions(workflow);
        errors.Should().BeEmpty("Expected all transition expressions to be valid");
    }

    /// <summary>
    /// Validates that the workflow has the specified number of invalid transition expressions.
    /// </summary>
    /// <param name="workflow">The workflow to validate.</param>
    /// <param name="expectedErrorCount">The expected count of invalid transition expressions.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="workflow"/> is <see langword="null"/>.</exception>
    public static void ShouldHaveInvalidTransitionExpressions(
        this Workflow workflow,
        int expectedErrorCount)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        var service = CreateService();
        var errors = service.ValidateTransitionExpressions(workflow);
        errors.Should().HaveCount(expectedErrorCount, $"Expected {expectedErrorCount} invalid transition expressions");
    }
}