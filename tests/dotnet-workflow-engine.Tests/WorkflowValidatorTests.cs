// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Utilities;
using FluentAssertions;
using Xunit;

namespace DotNetWorkflowEngine.Tests;

/// <summary>
/// Contains unit tests for validating workflows, activities, transitions, and validation results.
/// </summary>
public class WorkflowValidatorTests
{
    /// <summary>
    /// Creates a valid workflow for testing purposes.
    /// </summary>
    /// <param name="id">Optional workflow ID. If not provided, defaults to "workflow-1".</param>
    /// <returns>A valid workflow instance with start, middle, and end activities connected by transitions.</returns>
    private Workflow CreateValidWorkflow(string? id = null)
    {
        return new Workflow
        {
            Id = id ?? "workflow-1",
            Name = "Test Workflow",
            StartActivityId = "start",
            EndActivityId = "end",
            Activities = new List<Activity>
            {
                new Activity { Id = "start", Name = "Start", TimeoutSeconds = 30, MaxRetries = 0 },
                new Activity { Id = "middle", Name = "Middle Step", TimeoutSeconds = 30, MaxRetries = 0 },
                new Activity { Id = "end", Name = "End", TimeoutSeconds = 30, MaxRetries = 0 }
            },
            Transitions = new List<Transition>
            {
                new Transition { Id = "t1", FromActivityId = "start", ToActivityId = "middle" },
                new Transition { Id = "t2", FromActivityId = "middle", ToActivityId = "end" }
            }
        };
    }

    /// <summary>
    /// Tests that a valid workflow passes validation successfully.
    /// </summary>
    [Fact]
    public void ValidateWorkflow_ValidWorkflow_ReturnsValid()
    {
        var workflow = CreateValidWorkflow();

        var result = WorkflowValidator.ValidateWorkflow(workflow);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests validation fails when workflow ID is missing or empty.
    /// </summary>
    [Fact]
    public void ValidateWorkflow_MissingId_ReturnsError()
    {
        var workflow = CreateValidWorkflow();
        workflow.Id = string.Empty;

        var result = WorkflowValidator.ValidateWorkflow(workflow);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("ID is required"));
    }

    /// <summary>
    /// Tests validation fails when workflow name is missing or null.
    /// </summary>
    [Fact]
    public void ValidateWorkflow_MissingName_ReturnsError()
    {
        var workflow = CreateValidWorkflow();
        workflow.Name = null!;

        var result = WorkflowValidator.ValidateWorkflow(workflow);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("name is required"));
    }

    /// <summary>
    /// Tests validation fails when workflow has no activities.
    /// </summary>
    [Fact]
    public void ValidateWorkflow_NoActivities_ReturnsError()
    {
        var workflow = CreateValidWorkflow();
        workflow.Activities.Clear();

        var result = WorkflowValidator.ValidateWorkflow(workflow);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("at least one activity"));
    }

    /// <summary>
    /// Tests validation fails when workflow contains an invalid activity (missing ID).
    /// </summary>
    [Fact]
    public void ValidateWorkflow_InvalidActivity_ReturnsError()
    {
        var workflow = CreateValidWorkflow();
        workflow.Activities.Add(new Activity { Id = "", Name = "Invalid" }); // Missing ID

        var result = WorkflowValidator.ValidateWorkflow(workflow);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid activity"));
    }

    /// <summary>
    /// Tests validation fails when workflow start activity ID references a non-existent activity.
    /// </summary>
    [Fact]
    public void ValidateWorkflow_StartActivityNotFound_ReturnsError()
    {
        var workflow = CreateValidWorkflow();
        workflow.StartActivityId = "nonexistent";

        var result = WorkflowValidator.ValidateWorkflow(workflow);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Start activity") && e.Contains("not found"));
    }

    /// <summary>
    /// Tests validation fails when workflow end activity ID references a non-existent activity.
    /// </summary>
    [Fact]
    public void ValidateWorkflow_EndActivityNotFound_ReturnsError()
    {
        var workflow = CreateValidWorkflow();
        workflow.EndActivityId = "nonexistent";

        var result = WorkflowValidator.ValidateWorkflow(workflow);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("End activity") && e.Contains("not found"));
    }

    /// <summary>
    /// Tests that a warning is returned when workflow has no start activity configured.
    /// </summary>
    [Fact]
    public void ValidateWorkflow_NoStartActivity_ReturnsWarning()
    {
        var workflow = CreateValidWorkflow();
        workflow.StartActivityId = string.Empty;

        var result = WorkflowValidator.ValidateWorkflow(workflow);

        result.Warnings.Should().Contain(e => e.Contains("No start activity"));
    }

    /// <summary>
    /// Tests validation fails when workflow contains an invalid transition (missing from activity ID).
    /// </summary>
    [Fact]
    public void ValidateWorkflow_InvalidTransition_ReturnsError()
    {
        var workflow = CreateValidWorkflow();
        workflow.Transitions.Add(new Transition { Id = "invalid", FromActivityId = "", ToActivityId = "end" });

        var result = WorkflowValidator.ValidateWorkflow(workflow);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid transition"));
    }

    /// <summary>
    /// Tests that a valid activity passes validation successfully.
    /// </summary>
    [Fact]
    public void ValidateActivity_ValidActivity_ReturnsValid()
    {
        var activity = new Activity { Id = "test", Name = "Test", TimeoutSeconds = 30, MaxRetries = 0 };

        var result = WorkflowValidator.ValidateActivity(activity);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests validation fails when activity ID is missing or empty.
    /// </summary>
    [Fact]
    public void ValidateActivity_MissingId_ReturnsError()
    {
        var activity = new Activity { Id = "", Name = "Test", TimeoutSeconds = 30, MaxRetries = 0 };

        var result = WorkflowValidator.ValidateActivity(activity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("ID is required"));
    }

    /// <summary>
    /// Tests validation fails when activity name is missing or empty.
    /// </summary>
    [Fact]
    public void ValidateActivity_MissingName_ReturnsError()
    {
        var activity = new Activity { Id = "test", Name = "", TimeoutSeconds = 30, MaxRetries = 0 };

        var result = WorkflowValidator.ValidateActivity(activity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("name is required"));
    }

    /// <summary>
    /// Tests validation fails when activity timeout is zero or negative.
    /// </summary>
    [Fact]
    public void ValidateActivity_InvalidTimeout_ReturnsError()
    {
        var activity = new Activity { Id = "test", Name = "Test", TimeoutSeconds = 0, MaxRetries = 0 };

        var result = WorkflowValidator.ValidateActivity(activity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Timeout must be greater than zero"));
    }

    /// <summary>
    /// Tests validation fails when activity max retries is negative.
    /// </summary>
    [Fact]
    public void ValidateActivity_NegativeRetries_ReturnsError()
    {
        var activity = new Activity { Id = "test", Name = "Test", TimeoutSeconds = 30, MaxRetries = -1 };

        var result = WorkflowValidator.ValidateActivity(activity);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("cannot be negative"));
    }

    /// <summary>
    /// Tests that a warning is returned when activity has MaxRetries set but RetryPolicy is NoRetry.
    /// </summary>
    [Fact]
    public void ValidateActivity_RetriesWithoutPolicy_ReturnsWarning()
    {
        var activity = new Activity
        {
            Id = "test",
            Name = "Test",
            TimeoutSeconds = 30,
            MaxRetries = 3,
            RetryPolicy = RetryPolicy.NoRetry
        };

        var result = WorkflowValidator.ValidateActivity(activity);

        result.Warnings.Should().Contain(e => e.Contains("MaxRetries is set but RetryPolicy is NoRetry"));
    }

    /// <summary>
    /// Tests that a valid transition passes validation successfully.
    /// </summary>
    [Fact]
    public void ValidateTransition_ValidTransition_ReturnsValid()
    {
        var workflow = CreateValidWorkflow();
        var transition = new Transition { Id = "t1", FromActivityId = "start", ToActivityId = "end" };

        var result = WorkflowValidator.ValidateTransition(transition, workflow);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests validation fails when transition is missing a from activity ID.
    /// </summary>
    [Fact]
    public void ValidateTransition_MissingFromActivity_ReturnsError()
    {
        var workflow = CreateValidWorkflow();
        var transition = new Transition { Id = "t1", FromActivityId = "", ToActivityId = "end" };

        var result = WorkflowValidator.ValidateTransition(transition, workflow);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("From activity is required"));
    }

    /// <summary>
    /// Tests validation fails when transition is missing a to activity ID.
    /// </summary>
    [Fact]
    public void ValidateTransition_MissingToActivity_ReturnsError()
    {
        var workflow = CreateValidWorkflow();
        var transition = new Transition { Id = "t1", FromActivityId = "start", ToActivityId = "" };

        var result = WorkflowValidator.ValidateTransition(transition, workflow);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("To activity is required"));
    }

    /// <summary>
    /// Tests validation fails when transition references a non-existent from activity.
    /// </summary>
    [Fact]
    public void ValidateTransition_FromActivityNotFound_ReturnsError()
    {
        var workflow = CreateValidWorkflow();
        var transition = new Transition { Id = "t1", FromActivityId = "nonexistent", ToActivityId = "end" };

        var result = WorkflowValidator.ValidateTransition(transition, workflow);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("From activity") && e.Contains("not found"));
    }

    /// <summary>
    /// Tests validation fails when transition references a non-existent to activity.
    /// </summary>
    [Fact]
    public void ValidateTransition_ToActivityNotFound_ReturnsError()
    {
        var workflow = CreateValidWorkflow();
        var transition = new Transition { Id = "t1", FromActivityId = "start", ToActivityId = "nonexistent" };

        var result = WorkflowValidator.ValidateTransition(transition, workflow);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("To activity") && e.Contains("not found"));
    }

    /// <summary>
    /// Tests that a warning is returned when transition creates a self-loop (activity to itself).
    /// </summary>
    [Fact]
    public void ValidateTransition_SelfLoop_ReturnsWarning()
    {
        var workflow = CreateValidWorkflow();
        var transition = new Transition { Id = "t1", FromActivityId = "start", ToActivityId = "start" };

        var result = WorkflowValidator.ValidateTransition(transition, workflow);

        result.Warnings.Should().Contain(e => e.Contains("self-loop"));
    }

    /// <summary>
    /// Tests that a warning is returned when workflow contains unreachable activities.
    /// </summary>
    [Fact]
    public void ValidateWorkflow_UnreachableActivity_ReturnsWarning()
    {
        var workflow = CreateValidWorkflow();
        workflow.Activities.Add(new Activity { Id = "orphan", Name = "Orphan", TimeoutSeconds = 30, MaxRetries = 0 });

        var result = WorkflowValidator.ValidateWorkflow(workflow);

        result.Warnings.Should().Contain(e => e.Contains("Unreachable activities"));
    }

    /// <summary>
    /// Tests that multiple validation errors are all reported when present.
    /// </summary>
    [Fact]
    public void ValidateWorkflow_MultipleErrors_ReturnsAll()
    {
        var workflow = new Workflow
        {
            Id = "", // Missing
            Name = "", // Missing
            Activities = new List<Activity>() // Empty
        };

        var result = WorkflowValidator.ValidateWorkflow(workflow);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    /// <summary>
    /// Tests that validation result report formats errors and warnings correctly.
    /// </summary>
    [Fact]
    public void ValidationResult_GetReport_FormatsCorrectly()
    {
        var result = new WorkflowValidator.ValidationResult();
        result.AddError("Test error");
        result.AddWarning("Test warning");

        var report = result.GetReport();

        report.Should().Contain("✗ Validation failed");
        report.Should().Contain("ERROR: Test error");
        report.Should().Contain("WARNING: Test warning");
    }

    /// <summary>
    /// Tests that validation result report shows success message when no errors are present.
    /// </summary>
    [Fact]
    public void ValidationResult_GetReport_SuccessMessage()
    {
        var result = new WorkflowValidator.ValidationResult();

        var report = result.GetReport();

        report.Should().Contain("✓ Validation passed");
    }

    /// <summary>
    /// Tests that a complex valid workflow with multiple activities and transitions passes validation.
    /// </summary>
    [Fact]
    public void ValidateWorkflow_ComplexValidPath_ReturnsValid()
    {
        var workflow = new Workflow
        {
            Id = "complex",
            Name = "Complex Workflow",
            StartActivityId = "start",
            EndActivityId = "end",
            Activities = new List<Activity>
            {
                new Activity { Id = "start", Name = "Start", TimeoutSeconds = 30, MaxRetries = 0 },
                new Activity { Id = "step1", Name = "Step 1", TimeoutSeconds = 30, MaxRetries = 0 },
                new Activity { Id = "step2", Name = "Step 2", TimeoutSeconds = 30, MaxRetries = 0 },
                new Activity { Id = "step3", Name = "Step 3", TimeoutSeconds = 30, MaxRetries = 0 },
                new Activity { Id = "end", Name = "End", TimeoutSeconds = 30, MaxRetries = 0 }
            },
            Transitions = new List<Transition>
            {
                new Transition { Id = "t1", FromActivityId = "start", ToActivityId = "step1" },
                new Transition { Id = "t2", FromActivityId = "step1", ToActivityId = "step2" },
                new Transition { Id = "t3", FromActivityId = "step2", ToActivityId = "step3" },
                new Transition { Id = "t4", FromActivityId = "step3", ToActivityId = "end" }
            }
        };

        var result = WorkflowValidator.ValidateWorkflow(workflow);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a warning is returned when workflow graph contains disconnected components.
    /// </summary>
    [Fact]
    public void ValidateWorkflow_DisconnectedGraph_ReturnsWarning()
    {
        var workflow = new Workflow
        {
            Id = "disconnected",
            Name = "Disconnected Workflow",
            StartActivityId = "start",
            EndActivityId = "end",
            Activities = new List<Activity>
            {
                new Activity { Id = "start", Name = "Start", TimeoutSeconds = 30, MaxRetries = 0 },
                new Activity { Id = "end", Name = "End", TimeoutSeconds = 30, MaxRetries = 0 },
                new Activity { Id = "isolated", Name = "Isolated", TimeoutSeconds = 30, MaxRetries = 0 }
            },
            Transitions = new List<Transition>
            {
                new Transition { Id = "t1", FromActivityId = "start", ToActivityId = "end" }
            }
        };

        var result = WorkflowValidator.ValidateWorkflow(workflow);

        result.Warnings.Should().Contain(e => e.Contains("Unreachable activities"));
    }

    /// <summary>
    /// Tests that a warning is returned when an activity cannot reach the end activity.
    /// </summary>
    [Fact]
    public void ValidateWorkflow_ActivityCannotReachEnd_ReturnsWarning()
    {
        var workflow = new Workflow
        {
            Id = "unreachable-end",
            Name = "Unreachable End Workflow",
            StartActivityId = "start",
            EndActivityId = "end",
            Activities = new List<Activity>
            {
                new Activity { Id = "start", Name = "Start", TimeoutSeconds = 30, MaxRetries = 0 },
                new Activity { Id = "middle", Name = "Middle", TimeoutSeconds = 30, MaxRetries = 0 },
                new Activity { Id = "end", Name = "End", TimeoutSeconds = 30, MaxRetries = 0 }
            },
            Transitions = new List<Transition>
            {
                new Transition { Id = "t1", FromActivityId = "start", ToActivityId = "middle" }
                // No transition from middle to end
            }
        };

        var result = WorkflowValidator.ValidateWorkflow(workflow);

        result.Warnings.Should().Contain(e => e.Contains("cannot reach the end activity"));
    }

    /// <summary>
    /// Tests that no warning is returned when an optional activity cannot reach the end activity.
    /// </summary>
    [Fact]
    public void ValidateWorkflow_OptionalActivityCannotReachEnd_ReturnsNoWarning()
    {
        var workflow = new Workflow
        {
            Id = "optional-end",
            Name = "Optional End Workflow",
            StartActivityId = "start",
            EndActivityId = "end",
            Activities = new List<Activity>
            {
                new Activity { Id = "start", Name = "Start", TimeoutSeconds = 30, MaxRetries = 0 },
                new Activity { Id = "optional", Name = "Optional", TimeoutSeconds = 30, MaxRetries = 0, IsOptional = true },
                new Activity { Id = "end", Name = "End", TimeoutSeconds = 30, MaxRetries = 0 }
            },
            Transitions = new List<Transition>
            {
                new Transition { Id = "t1", FromActivityId = "start", ToActivityId = "end" }
            }
        };

        var result = WorkflowValidator.ValidateWorkflow(workflow);

        result.Warnings.Should().NotContain(e => e.Contains("optional") && e.Contains("cannot reach"));
    }

/// <summary>
/// Tests that duplicate activity IDs are not explicitly validated (current behavior allows them).
/// This test documents the current behavior - duplicate IDs are not detected as an error.
/// </summary>
[Fact]
public void ValidateWorkflow_DuplicateActivityIds_NotDetectedAsError()
{
    var workflow = new Workflow
    {
        Id = "duplicate-ids",
        Name = "Workflow with Duplicate IDs",
        StartActivityId = "activity-1",
        EndActivityId = "activity-3",
        Activities = new List<Activity>
        {
            new Activity { Id = "activity-1", Name = "Start Activity", TimeoutSeconds = 30, MaxRetries = 0 },
            new Activity { Id = "activity-2", Name = "Middle Activity", TimeoutSeconds = 30, MaxRetries = 0 },
            new Activity { Id = "activity-2", Name = "Duplicate ID Activity", TimeoutSeconds = 30, MaxRetries = 0 },
            new Activity { Id = "activity-3", Name = "End Activity", TimeoutSeconds = 30, MaxRetries = 0 }
        },
        Transitions = new List<Transition>
        {
            new Transition { Id = "t1", FromActivityId = "activity-1", ToActivityId = "activity-2" },
            new Transition { Id = "t2", FromActivityId = "activity-2", ToActivityId = "activity-3" }
        }
    };

    var result = WorkflowValidator.ValidateWorkflow(workflow);

    // Current implementation does not detect duplicate IDs as an error
    result.IsValid.Should().BeTrue();
    result.Errors.Should().NotContain(e => e.Contains("duplicate") || e.Contains("Duplicate"));
}
}
