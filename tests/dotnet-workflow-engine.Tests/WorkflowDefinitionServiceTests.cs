// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Exceptions;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using FluentAssertions;
using Xunit;

namespace DotNetWorkflowEngine.Tests;

public class WorkflowDefinitionServiceTests
{
    private WorkflowDefinitionService CreateService()
    {
        return new WorkflowDefinitionService();
    }

    private Activity CreateActivity(string id = "activity-1", string name = "Activity 1")
    {
        return new Activity
        {
            Id = id,
            Name = name,
            TimeoutSeconds = 30,
            MaxRetries = 0,
            RetryPolicy = RetryPolicy.NoRetry
        };
    }

    [Fact]
    public void CreateWorkflow_WithValidData_CreatesWorkflow()
    {
        var service = CreateService();

        var workflow = service.CreateWorkflow("wf-1", "Test Workflow", "A test workflow");

        workflow.Should().NotBeNull();
        workflow.Id.Should().Be("wf-1");
        workflow.Name.Should().Be("Test Workflow");
        workflow.Description.Should().Be("A test workflow");
    }

    [Fact]
    public void CreateWorkflow_WithNullOrEmptyId_ThrowsValidationException()
    {
        var service = CreateService();

        var act1 = () => service.CreateWorkflow(null!, "Test");
        var act2 = () => service.CreateWorkflow("", "Test");
        var act3 = () => service.CreateWorkflow("   ", "Test");

        act1.Should().Throw<ValidationException>();
        act2.Should().Throw<ValidationException>();
        act3.Should().Throw<ValidationException>();
    }

    [Fact]
    public void CreateWorkflow_WithDuplicateId_ThrowsWorkflowException()
    {
        var service = CreateService();
        service.CreateWorkflow("wf-1", "First");

        var act = () => service.CreateWorkflow("wf-1", "Second");

        act.Should().Throw<WorkflowException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public void GetWorkflow_WithExistingId_ReturnsWorkflow()
    {
        var service = CreateService();
        service.CreateWorkflow("wf-1", "Test");

        var workflow = service.GetWorkflow("wf-1");

        workflow.Should().NotBeNull();
        workflow!.Id.Should().Be("wf-1");
    }

    [Fact]
    public void GetWorkflow_WithNonExistentId_ReturnsNull()
    {
        var service = CreateService();

        var workflow = service.GetWorkflow("nonexistent");

        workflow.Should().BeNull();
    }

    [Fact]
    public void GetAllWorkflows_ReturnsAllCreatedWorkflows()
    {
        var service = CreateService();
        service.CreateWorkflow("wf-1", "Workflow 1");
        service.CreateWorkflow("wf-2", "Workflow 2");
        service.CreateWorkflow("wf-3", "Workflow 3");

        var workflows = service.GetAllWorkflows();

        workflows.Should().HaveCount(3);
        workflows.Select(w => w.Id).Should().Contain(new[] { "wf-1", "wf-2", "wf-3" });
    }

    [Fact]
    public void GetAllWorkflows_WhenEmpty_ReturnsEmptyList()
    {
        var service = CreateService();

        var workflows = service.GetAllWorkflows();

        workflows.Should().BeEmpty();
    }

    [Fact]
    public void AddActivity_ToExistingWorkflow_AddsActivitySuccessfully()
    {
        var service = CreateService();
        var workflow = service.CreateWorkflow("wf-1", "Test");
        var activity = CreateActivity();

        service.AddActivity("wf-1", activity);

        workflow.Activities.Should().HaveCount(1);
        workflow.Activities[0].Id.Should().Be("activity-1");
    }

    [Fact]
    public void AddActivity_ToNonExistentWorkflow_ThrowsWorkflowException()
    {
        var service = CreateService();
        var activity = CreateActivity();

        var act = () => service.AddActivity("nonexistent", activity);

        act.Should().Throw<WorkflowException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public void AddActivity_WithDuplicateId_ThrowsWorkflowException()
    {
        var service = CreateService();
        var workflow = service.CreateWorkflow("wf-1", "Test");
        var activity1 = CreateActivity("act-1");
        var activity2 = CreateActivity("act-1");

        service.AddActivity("wf-1", activity1);
        var act = () => service.AddActivity("wf-1", activity2);

        act.Should().Throw<WorkflowException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public void AddActivity_WithInvalidActivity_ThrowsValidationException()
    {
        var service = CreateService();
        service.CreateWorkflow("wf-1", "Test");
        var invalidActivity = new Activity { Id = "", Name = "", TimeoutSeconds = 0 };

        var act = () => service.AddActivity("wf-1", invalidActivity);

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void AddActivity_UpdatesWorkflowModifiedAt()
    {
        var service = CreateService();
        var workflow = service.CreateWorkflow("wf-1", "Test");
        var originalModified = workflow.ModifiedAt;
        var activity = CreateActivity();

        System.Threading.Thread.Sleep(10);
        service.AddActivity("wf-1", activity);

        workflow.ModifiedAt.Should().BeAfter(originalModified);
    }

    [Fact]
    public void AddTransition_BetweenExistingActivities_AddsTransitionSuccessfully()
    {
        var service = CreateService();
        var workflow = service.CreateWorkflow("wf-1", "Test");
        service.AddActivity("wf-1", CreateActivity("act-1"));
        service.AddActivity("wf-1", CreateActivity("act-2"));

        var transition = new Transition
        {
            Id = "t-1",
            FromActivityId = "act-1",
            ToActivityId = "act-2"
        };
        service.AddTransition("wf-1", transition);

        workflow.Transitions.Should().HaveCount(1);
        workflow.Transitions[0].Id.Should().Be("t-1");
    }

    [Fact]
    public void AddTransition_WithNonExistentFromActivity_ThrowsWorkflowException()
    {
        var service = CreateService();
        var workflow = service.CreateWorkflow("wf-1", "Test");
        service.AddActivity("wf-1", CreateActivity("act-1"));

        var transition = new Transition
        {
            Id = "t-1",
            FromActivityId = "nonexistent",
            ToActivityId = "act-1"
        };
        var act = () => service.AddTransition("wf-1", transition);

        act.Should().Throw<WorkflowException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public void AddTransition_WithNonExistentToActivity_ThrowsWorkflowException()
    {
        var service = CreateService();
        var workflow = service.CreateWorkflow("wf-1", "Test");
        service.AddActivity("wf-1", CreateActivity("act-1"));

        var transition = new Transition
        {
            Id = "t-1",
            FromActivityId = "act-1",
            ToActivityId = "nonexistent"
        };
        var act = () => service.AddTransition("wf-1", transition);

        act.Should().Throw<WorkflowException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public void AddTransition_WithDuplicateId_ThrowsWorkflowException()
    {
        var service = CreateService();
        var workflow = service.CreateWorkflow("wf-1", "Test");
        service.AddActivity("wf-1", CreateActivity("act-1"));
        service.AddActivity("wf-1", CreateActivity("act-2"));

        var transition1 = new Transition { Id = "t-1", FromActivityId = "act-1", ToActivityId = "act-2" };
        var transition2 = new Transition { Id = "t-1", FromActivityId = "act-1", ToActivityId = "act-2" };
        service.AddTransition("wf-1", transition1);

        var act = () => service.AddTransition("wf-1", transition2);
        act.Should().Throw<WorkflowException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public void SetStartActivity_WithExistingActivity_SetsStartActivity()
    {
        var service = CreateService();
        var workflow = service.CreateWorkflow("wf-1", "Test");
        service.AddActivity("wf-1", CreateActivity("start"));

        service.SetStartActivity("wf-1", "start");

        workflow.StartActivityId.Should().Be("start");
    }

    [Fact]
    public void SetStartActivity_WithNonExistentWorkflow_ThrowsWorkflowException()
    {
        var service = CreateService();

        var act = () => service.SetStartActivity("nonexistent", "start");

        act.Should().Throw<WorkflowException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public void SetStartActivity_WithNonExistentActivity_ThrowsWorkflowException()
    {
        var service = CreateService();
        service.CreateWorkflow("wf-1", "Test");

        var act = () => service.SetStartActivity("wf-1", "nonexistent");

        act.Should().Throw<WorkflowException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public void SetEndActivity_WithExistingActivity_SetsEndActivity()
    {
        var service = CreateService();
        var workflow = service.CreateWorkflow("wf-1", "Test");
        service.AddActivity("wf-1", CreateActivity("end"));

        service.SetEndActivity("wf-1", "end");

        workflow.EndActivityId.Should().Be("end");
    }

    [Fact]
    public void PublishWorkflow_WithValidWorkflow_PublishesSuccessfully()
    {
        var service = CreateService();
        var workflow = service.CreateWorkflow("wf-1", "Test");
        service.AddActivity("wf-1", CreateActivity("start"));
        workflow.StartActivityId = "start";

        service.PublishWorkflow("wf-1");

        workflow.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void PublishWorkflow_WithNonExistentWorkflow_ThrowsWorkflowException()
    {
        var service = CreateService();

        var act = () => service.PublishWorkflow("nonexistent");

        act.Should().Throw<WorkflowException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public void PublishWorkflow_WithInvalidWorkflow_ThrowsValidationException()
    {
        var service = CreateService();
        service.CreateWorkflow("wf-1", "Test");

        var act = () => service.PublishWorkflow("wf-1");

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void ValidateWorkflow_WithValidWorkflow_ReturnsTrue()
    {
        var service = CreateService();
        var workflow = service.CreateWorkflow("wf-1", "Test");
        service.AddActivity("wf-1", CreateActivity("start"));
        workflow.StartActivityId = "start";

        var isValid = service.ValidateWorkflow("wf-1", out var errors);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateWorkflow_WithInvalidWorkflow_ReturnsFalse()
    {
        var service = CreateService();
        service.CreateWorkflow("wf-1", "Test");

        var isValid = service.ValidateWorkflow("wf-1", out var errors);

        isValid.Should().BeFalse();
        errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateWorkflow_WithNonExistentWorkflow_ReturnsFalse()
    {
        var service = CreateService();

        var isValid = service.ValidateWorkflow("nonexistent", out var errors);

        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("not found"));
    }

    [Fact]
    public void GetActivities_ReturnsAllActivitiesForWorkflow()
    {
        var service = CreateService();
        var workflow = service.CreateWorkflow("wf-1", "Test");
        service.AddActivity("wf-1", CreateActivity("act-1"));
        service.AddActivity("wf-1", CreateActivity("act-2"));

        var activities = service.GetActivities("wf-1");

        activities.Should().HaveCount(2);
        activities.Select(a => a.Id).Should().Contain(new[] { "act-1", "act-2" });
    }

    [Fact]
    public void GetActivities_WithNonExistentWorkflow_ThrowsWorkflowException()
    {
        var service = CreateService();

        var act = () => service.GetActivities("nonexistent");

        act.Should().Throw<WorkflowException>();
    }

    [Fact]
    public void GetActivity_WithExistingActivity_ReturnsActivity()
    {
        var service = CreateService();
        service.CreateWorkflow("wf-1", "Test");
        service.AddActivity("wf-1", CreateActivity("act-1"));

        var activity = service.GetActivity("wf-1", "act-1");

        activity.Should().NotBeNull();
        activity!.Id.Should().Be("act-1");
    }

    [Fact]
    public void GetActivity_WithNonExistentActivity_ReturnsNull()
    {
        var service = CreateService();
        service.CreateWorkflow("wf-1", "Test");

        var activity = service.GetActivity("wf-1", "nonexistent");

        activity.Should().BeNull();
    }

    [Fact]
    public void GetActivity_WithNonExistentWorkflow_ReturnsNull()
    {
        var service = CreateService();

        var activity = service.GetActivity("nonexistent", "act-1");

        activity.Should().BeNull();
    }

    [Fact]
    public void DeleteWorkflow_WithExistingWorkflow_DeletesSuccessfully()
    {
        var service = CreateService();
        service.CreateWorkflow("wf-1", "Test");

        var deleted = service.DeleteWorkflow("wf-1");

        deleted.Should().BeTrue();
        service.GetWorkflow("wf-1").Should().BeNull();
    }

    [Fact]
    public void DeleteWorkflow_WithNonExistentWorkflow_ReturnsFalse()
    {
        var service = CreateService();

        var deleted = service.DeleteWorkflow("nonexistent");

        deleted.Should().BeFalse();
    }

    [Fact]
    public void CloneWorkflow_CreatesExactCopy()
    {
        var service = CreateService();
        var original = service.CreateWorkflow("original", "Original Workflow", "Original Description");
        service.AddActivity("original", CreateActivity("act-1"));
        service.AddActivity("original", CreateActivity("act-2"));
        original.StartActivityId = "act-1";
        original.EndActivityId = "act-2";
        service.AddTransition("original", new Transition
        {
            Id = "t-1",
            FromActivityId = "act-1",
            ToActivityId = "act-2"
        });

        var cloned = service.CloneWorkflow("original", "cloned", "Cloned Workflow");

        cloned.Id.Should().Be("cloned");
        cloned.Name.Should().Be("Cloned Workflow");
        cloned.Description.Should().Be("Original Description");
        cloned.Activities.Should().HaveCount(2);
        cloned.Transitions.Should().HaveCount(1);
        cloned.StartActivityId.Should().Be("act-1");
        cloned.EndActivityId.Should().Be("act-2");
    }

    [Fact]
    public void CloneWorkflow_WithNonExistentSource_ThrowsWorkflowException()
    {
        var service = CreateService();

        var act = () => service.CloneWorkflow("nonexistent", "new", "New");

        act.Should().Throw<WorkflowException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public void CloneWorkflow_CreatesIndependentCopy()
    {
        var service = CreateService();
        var original = service.CreateWorkflow("original", "Original");
        service.AddActivity("original", CreateActivity("act-1"));

        var cloned = service.CloneWorkflow("original", "cloned", "Cloned");
        service.AddActivity("cloned", CreateActivity("act-2"));

        original.Activities.Should().HaveCount(1);
        cloned.Activities.Should().HaveCount(2);
    }
}
