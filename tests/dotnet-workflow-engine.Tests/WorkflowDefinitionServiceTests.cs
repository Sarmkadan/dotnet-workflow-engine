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

/// <summary>
/// Test suite for the <see cref="WorkflowDefinitionService"/> class.
/// Contains unit tests that verify the behavior of workflow definition management operations
/// including creation, modification, validation, and deletion of workflows and their components.
/// </summary>
public class WorkflowDefinitionServiceTests
{
    /// <summary>
    /// Creates an instance of <see cref="WorkflowDefinitionService"/> for testing.
    /// </summary>
    /// <returns>A new instance of <see cref="WorkflowDefinitionService"/>.</returns>
    private WorkflowDefinitionService CreateService()
    {
        return new WorkflowDefinitionService();
    }

    /// <summary>
    /// Creates a test activity with default or specified parameters.
    /// </summary>
    /// <param name="id">The activity identifier. Defaults to "activity-1".</param>
    /// <param name="name">The activity name. Defaults to "Activity 1".</param>
    /// <returns>A new <see cref="Activity"/> instance configured for testing.</returns>
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

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.CreateWorkflow"/> successfully creates a workflow
    /// with valid data including id, name, and description.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.CreateWorkflow"/> throws a <see cref="WorkflowException"/>
    /// when attempting to create a workflow with an id that already exists.
    /// </summary>
    [Fact]
    public void CreateWorkflow_WithDuplicateId_ThrowsWorkflowException()
    {
        var service = CreateService();
        service.CreateWorkflow("wf-1", "First");

        var act = () => service.CreateWorkflow("wf-1", "Second");

        act.Should().Throw<WorkflowException>()
            .WithMessage("*already exists*");
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.GetWorkflow"/> returns the workflow
    /// when querying with an existing workflow id.
    /// </summary>
    [Fact]
    public void GetWorkflow_WithExistingId_ReturnsWorkflow()
    {
        var service = CreateService();
        service.CreateWorkflow("wf-1", "Test");

        var workflow = service.GetWorkflow("wf-1");

        workflow.Should().NotBeNull();
        workflow!.Id.Should().Be("wf-1");
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.GetWorkflow"/> returns null
    /// when querying with a non-existent workflow id.
    /// </summary>
    [Fact]
    public void GetWorkflow_WithNonExistentId_ReturnsNull()
    {
        var service = CreateService();

        var workflow = service.GetWorkflow("nonexistent");

        workflow.Should().BeNull();
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.GetAllWorkflows"/> returns all workflows
    /// that have been created in the service.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.GetAllWorkflows"/> returns an empty list
    /// when no workflows have been created.
    /// </summary>
    [Fact]
    public void GetAllWorkflows_WhenEmpty_ReturnsEmptyList()
    {
        var service = CreateService();

        var workflows = service.GetAllWorkflows();

        workflows.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.AddActivity"/> successfully adds an activity
    /// to an existing workflow.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.AddActivity"/> throws a <see cref="WorkflowException"/>
    /// when attempting to add an activity to a non-existent workflow.
    /// </summary>
    [Fact]
    public void AddActivity_ToNonExistentWorkflow_ThrowsWorkflowException()
    {
        var service = CreateService();
        var activity = CreateActivity();

        var act = () => service.AddActivity("nonexistent", activity);

        act.Should().Throw<WorkflowException>()
            .WithMessage("*not found*");
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.AddActivity"/> throws a <see cref="WorkflowException"/>
    /// when attempting to add an activity with an id that already exists in the workflow.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.AddActivity"/> throws a <see cref="ValidationException"/>
    /// when attempting to add an activity with invalid properties (null or empty id/name).
    /// </summary>
    [Fact]
    public void AddActivity_WithInvalidActivity_ThrowsValidationException()
    {
        var service = CreateService();
        service.CreateWorkflow("wf-1", "Test");
        var invalidActivity = new Activity { Id = "", Name = "", TimeoutSeconds = 0 };

        var act = () => service.AddActivity("wf-1", invalidActivity);

        act.Should().Throw<ValidationException>();
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.AddActivity"/> updates the workflow's ModifiedAt timestamp
    /// when an activity is added.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.AddTransition"/> successfully adds a transition
    /// between existing activities in a workflow.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.AddTransition"/> throws a <see cref="WorkflowException"/>
    /// when the transition's FromActivityId references a non-existent activity.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.AddTransition"/> throws a <see cref="WorkflowException"/>
    /// when the transition's ToActivityId references a non-existent activity.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.AddTransition"/> throws a <see cref="WorkflowException"/>
    /// when attempting to add a transition with an id that already exists in the workflow.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.SetStartActivity"/> successfully sets the start activity
    /// for a workflow.
    /// </summary>
    [Fact]
    public void SetStartActivity_WithExistingActivity_SetsStartActivity()
    {
        var service = CreateService();
        var workflow = service.CreateWorkflow("wf-1", "Test");
        service.AddActivity("wf-1", CreateActivity("start"));

        service.SetStartActivity("wf-1", "start");

        workflow.StartActivityId.Should().Be("start");
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.SetStartActivity"/> throws a <see cref="WorkflowException"/>
    /// when attempting to set the start activity for a non-existent workflow.
    /// </summary>
    [Fact]
    public void SetStartActivity_WithNonExistentWorkflow_ThrowsWorkflowException()
    {
        var service = CreateService();

        var act = () => service.SetStartActivity("nonexistent", "start");

        act.Should().Throw<WorkflowException>()
            .WithMessage("*not found*");
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.SetStartActivity"/> throws a <see cref="WorkflowException"/>
    /// when attempting to set a non-existent activity as the start activity.
    /// </summary>
    [Fact]
    public void SetStartActivity_WithNonExistentActivity_ThrowsWorkflowException()
    {
        var service = CreateService();
        service.CreateWorkflow("wf-1", "Test");

        var act = () => service.SetStartActivity("wf-1", "nonexistent");

        act.Should().Throw<WorkflowException>()
            .WithMessage("*not found*");
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.SetEndActivity"/> successfully sets the end activity
    /// for a workflow.
    /// </summary>
    [Fact]
    public void SetEndActivity_WithExistingActivity_SetsEndActivity()
    {
        var service = CreateService();
        var workflow = service.CreateWorkflow("wf-1", "Test");
        service.AddActivity("wf-1", CreateActivity("end"));

        service.SetEndActivity("wf-1", "end");

        workflow.EndActivityId.Should().Be("end");
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.PublishWorkflow"/> successfully publishes a workflow
    /// when the workflow is valid and properly configured.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.PublishWorkflow"/> throws a <see cref="WorkflowException"/>
    /// when attempting to publish a non-existent workflow.
    /// </summary>
    [Fact]
    public void PublishWorkflow_WithNonExistentWorkflow_ThrowsWorkflowException()
    {
        var service = CreateService();

        var act = () => service.PublishWorkflow("nonexistent");

        act.Should().Throw<WorkflowException>()
            .WithMessage("*not found*");
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.PublishWorkflow"/> throws a <see cref="ValidationException"/>
    /// when attempting to publish a workflow that is invalid (missing required properties).
    /// </summary>
    [Fact]
    public void PublishWorkflow_WithInvalidWorkflow_ThrowsValidationException()
    {
        var service = CreateService();
        service.CreateWorkflow("wf-1", "Test");

        var act = () => service.PublishWorkflow("wf-1");

        act.Should().Throw<ValidationException>();
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.ValidateWorkflow"/> returns true
    /// when validating a properly configured workflow.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.ValidateWorkflow"/> returns false
    /// when validating an invalid workflow (missing required properties).
    /// </summary>
    [Fact]
    public void ValidateWorkflow_WithInvalidWorkflow_ReturnsFalse()
    {
        var service = CreateService();
        service.CreateWorkflow("wf-1", "Test");

        var isValid = service.ValidateWorkflow("wf-1", out var errors);

        isValid.Should().BeFalse();
        errors.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.ValidateWorkflow"/> returns false
    /// when validating a non-existent workflow.
    /// </summary>
    [Fact]
    public void ValidateWorkflow_WithNonExistentWorkflow_ReturnsFalse()
    {
        var service = CreateService();

        var isValid = service.ValidateWorkflow("nonexistent", out var errors);

        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("not found"));
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.GetActivities"/> returns all activities
    /// that have been added to a specific workflow.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.GetActivities"/> throws a <see cref="WorkflowException"/>
    /// when attempting to get activities for a non-existent workflow.
    /// </summary>
    [Fact]
    public void GetActivities_WithNonExistentWorkflow_ThrowsWorkflowException()
    {
        var service = CreateService();

        var act = () => service.GetActivities("nonexistent");

        act.Should().Throw<WorkflowException>();
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.GetActivity"/> returns the activity
    /// when querying with existing workflow id and activity id.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.GetActivity"/> returns null
    /// when querying with a non-existent activity id.
    /// </summary>
    [Fact]
    public void GetActivity_WithNonExistentActivity_ReturnsNull()
    {
        var service = CreateService();
        service.CreateWorkflow("wf-1", "Test");

        var activity = service.GetActivity("wf-1", "nonexistent");

        activity.Should().BeNull();
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.GetActivity"/> returns null
    /// when querying with a non-existent workflow id.
    /// </summary>
    [Fact]
    public void GetActivity_WithNonExistentWorkflow_ReturnsNull()
    {
        var service = CreateService();

        var activity = service.GetActivity("nonexistent", "act-1");

        activity.Should().BeNull();
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.DeleteWorkflow"/> successfully deletes a workflow
    /// when the workflow exists.
    /// </summary>
    [Fact]
    public void DeleteWorkflow_WithExistingWorkflow_DeletesSuccessfully()
    {
        var service = CreateService();
        service.CreateWorkflow("wf-1", "Test");

        var deleted = service.DeleteWorkflow("wf-1");

        deleted.Should().BeTrue();
        service.GetWorkflow("wf-1").Should().BeNull();
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.DeleteWorkflow"/> returns false
    /// when attempting to delete a non-existent workflow.
    /// </summary>
    [Fact]
    public void DeleteWorkflow_WithNonExistentWorkflow_ReturnsFalse()
    {
        var service = CreateService();

        var deleted = service.DeleteWorkflow("nonexistent");

        deleted.Should().BeFalse();
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.CloneWorkflow"/> creates an exact copy of a workflow
    /// including all activities, transitions, and configuration.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.CloneWorkflow"/> throws a <see cref="WorkflowException"/>
    /// when attempting to clone a non-existent source workflow.
    /// </summary>
    [Fact]
    public void CloneWorkflow_WithNonExistentSource_ThrowsWorkflowException()
    {
        var service = CreateService();

        var act = () => service.CloneWorkflow("nonexistent", "new", "New");

        act.Should().Throw<WorkflowException>()
            .WithMessage("*not found*");
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.CloneWorkflow"/> creates an independent copy
    /// that does not affect the original workflow when modified.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.AddWorkflow"/> successfully adds an existing workflow definition.
    /// </summary>
    [Fact]
    public void AddWorkflow_WithValidWorkflow_AddsSuccessfully()
    {
        var service = CreateService();
        var workflow = new Workflow
        {
            Id = "wf-1",
            Name = "Test Workflow",
            Description = "Test Description"
        };

        service.AddWorkflow(workflow);

        var retrieved = service.GetWorkflow("wf-1");
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be("wf-1");
        retrieved.Name.Should().Be("Test Workflow");
        retrieved.Description.Should().Be("Test Description");
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.AddWorkflow"/> throws <see cref="ArgumentNullException"/> when workflow is null.
    /// </summary>
    [Fact]
    public void AddWorkflow_WithNullWorkflow_ThrowsArgumentNullException()
    {
        var service = CreateService();

        var act = () => service.AddWorkflow(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.AddWorkflow"/> throws <see cref="ValidationException"/> when workflow ID is invalid.
    /// </summary>
    [Fact]
    public void AddWorkflow_WithInvalidWorkflowId_ThrowsValidationException()
    {
        var service = CreateService();
        var workflow = new Workflow { Id = "", Name = "Test" };

        var act = () => service.AddWorkflow(workflow);

        act.Should().Throw<ValidationException>();
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.AddWorkflow"/> throws <see cref="ValidationException"/> when workflow name is invalid.
    /// </summary>
    [Fact]
    public void AddWorkflow_WithInvalidWorkflowName_ThrowsValidationException()
    {
        var service = CreateService();
        var workflow = new Workflow { Id = "wf-1", Name = "" };

        var act = () => service.AddWorkflow(workflow);

        act.Should().Throw<ValidationException>();
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.ExportWorkflowToJson"/> successfully exports a workflow to JSON.
    /// </summary>
    [Fact]
    public void ExportWorkflowToJson_WithValidWorkflow_ReturnsJson()
    {
        var service = CreateService();
        var workflow = service.CreateWorkflow("wf-1", "Test Workflow", "Test Description");
        service.AddActivity("wf-1", CreateActivity("act-1"));
        workflow.StartActivityId = "act-1";

        var json = service.ExportWorkflowToJson("wf-1");

        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("wf-1");
        json.Should().Contain("Test Workflow");
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.ExportWorkflowToJson"/> throws <see cref="WorkflowException"/> when workflow not found.
    /// </summary>
    [Fact]
    public void ExportWorkflowToJson_WithNonExistentWorkflow_ThrowsWorkflowException()
    {
        var service = CreateService();

        var act = () => service.ExportWorkflowToJson("nonexistent");

        act.Should().Throw<WorkflowException>()
            .WithMessage("*not found*");
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.ImportWorkflowFromJson"/> successfully imports a valid workflow from JSON.
    /// </summary>
    [Fact]
    public void ImportWorkflowFromJson_WithValidJson_ImportsSuccessfully()
    {
        var service = CreateService();
        var json = "{\r\n    \"Id\": \"wf-imported\",\r\n    \"Name\": \"Imported Workflow\",\r\n    \"Description\": \"Imported Description\",\r\n    \"Activities\": [\r\n        {\r\n            \"Id\": \"act-1\",\r\n            \"Name\": \"Activity 1\",\r\n            \"Type\": \"Task\",\r\n            \"ExecutionMode\": \"Sequential\"\r\n        }\r\n    ],\r\n    \"Transitions\": [],\r\n    \"StartActivityId\": \"act-1\",\r\n    \"CreatedAt\": \"2024-01-01T00:00:00Z\",\r\n    \"ModifiedAt\": \"2024-01-01T00:00:00Z\"\r\n}";

        var workflow = service.ImportWorkflowFromJson("wf-imported", "Imported Workflow", json);

        workflow.Should().NotBeNull();
        workflow.Id.Should().Be("wf-imported");
        workflow.Name.Should().Be("Imported Workflow");
        workflow.Description.Should().Be("Imported Description");
        service.GetWorkflow("wf-imported").Should().NotBeNull();
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.ImportWorkflowFromJson"/> throws <see cref="ValidationException"/> when JSON is invalid.
    /// </summary>
    [Fact]
    public void ImportWorkflowFromJson_WithInvalidJson_ThrowsValidationException()
    {
        var service = CreateService();

        var act = () => service.ImportWorkflowFromJson("wf-1", "Test", "invalid json");

        act.Should().Throw<ValidationException>()
            .WithMessage("*Invalid JSON format*");
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.ImportWorkflowFromJson"/> throws <see cref="ValidationException"/> when workflow validation fails.
    /// </summary>
    [Fact]
    public void ImportWorkflowFromJson_WithInvalidWorkflow_ThrowsValidationException()
    {
        var service = CreateService();
        var invalidJson = "{\r\n    \"Id\": \"wf-invalid\",\r\n    \"Name\": \"\",\r\n    \"Activities\": [],\r\n    \"Transitions\": []\r\n}";

        var act = () => service.ImportWorkflowFromJson("wf-invalid", "Invalid", invalidJson);

        act.Should().Throw<ValidationException>();
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.ImportWorkflowFromJson"/> throws <see cref="WorkflowException"/> when workflow already exists and overwrite is false.
    /// </summary>
    [Fact]
    public void ImportWorkflowFromJson_WithExistingWorkflowAndNoOverwrite_ThrowsWorkflowException()
    {
        var service = CreateService();
        service.CreateWorkflow("wf-1", "Existing");
        var json = "{\r\n    \"Id\": \"wf-1\",\r\n    \"Name\": \"New Name\",\r\n    \"Activities\": [],\r\n    \"Transitions\": [],\r\n    \"CreatedAt\": \"2024-01-01T00:00:00Z\",\r\n    \"ModifiedAt\": \"2024-01-01T00:00:00Z\"\r\n}";

        var act = () => service.ImportWorkflowFromJson("wf-1", "New Name", json, false);

        act.Should().Throw<WorkflowException>();
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.ImportWorkflowFromJson"/> overwrites existing workflow when overwrite is true.
    /// </summary>
    [Fact]
    public void ImportWorkflowFromJson_WithExistingWorkflowAndOverwrite_OverwritesSuccessfully()
    {
        var service = CreateService();
        var original = service.CreateWorkflow("wf-1", "Original", "Original Description");
        var json = "{\r\n    \"Id\": \"wf-1\",\r\n    \"Name\": \"New Name\",\r\n    \"Description\": \"New Description\",\r\n    \"Activities\": [\r\n        {\r\n            \"Id\": \"act-1\",\r\n            \"Name\": \"Activity 1\",\r\n            \"Type\": \"Task\",\r\n            \"ExecutionMode\": \"Sequential\"\r\n        }\r\n    ],\r\n    \"Transitions\": [],\r\n    \"StartActivityId\": \"act-1\",\r\n    \"CreatedAt\": \"2024-01-01T00:00:00Z\",\r\n    \"ModifiedAt\": \"2024-01-01T00:00:00Z\"\r\n}";

        var workflow = service.ImportWorkflowFromJson("wf-1", "New Name", json, true);

        workflow.Should().NotBeNull();
        workflow.Name.Should().Be("New Name");
        workflow.Description.Should().Be("New Description");
        service.GetWorkflow("wf-1")!.Name.Should().Be("New Name");
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.ValidateWorkflowJson"/> validates valid JSON successfully.
    /// </summary>
    [Fact]
    public void ValidateWorkflowJson_WithValidJson_ReturnsTrue()
    {
        var service = CreateService();
        var json = "{\r\n    \"Id\": \"wf-valid\",\r\n    \"Name\": \"Valid Workflow\",\r\n    \"Activities\": [\r\n        {\r\n            \"Id\": \"act-1\",\r\n            \"Name\": \"Activity 1\",\r\n            \"Type\": \"Task\",\r\n            \"ExecutionMode\": \"Sequential\"\r\n        }\r\n    ],\r\n    \"Transitions\": [],\r\n    \"StartActivityId\": \"act-1\",\r\n    \"CreatedAt\": \"2024-01-01T00:00:00Z\",\r\n    \"ModifiedAt\": \"2024-01-01T00:00:00Z\"\r\n}";

        var isValid = service.ValidateWorkflowJson(json, out var errors);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.ValidateWorkflowJson"/> returns false and errors for invalid JSON.
    /// </summary>
    [Fact]
    public void ValidateWorkflowJson_WithInvalidJson_ReturnsFalseWithErrors()
    {
        var service = CreateService();

        var isValid = service.ValidateWorkflowJson("invalid json", out var errors);

        isValid.Should().BeFalse();
        errors.Should().ContainSingle(e => e.Contains("Invalid JSON format"));
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.ValidateWorkflowJson"/> returns false and errors for workflow with missing required fields.
    /// </summary>
    [Fact]
    public void ValidateWorkflowJson_WithInvalidWorkflow_ReturnsFalseWithErrors()
    {
        var service = CreateService();
        var invalidJson = "{\r\n    \"Id\": \"\",\r\n    \"Name\": \"\",\r\n    \"Activities\": [],\r\n    \"Transitions\": []\r\n}";

        var isValid = service.ValidateWorkflowJson(invalidJson, out var errors);

        isValid.Should().BeFalse();
        errors.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.ValidateWorkflowJson"/> returns false for null JSON.
    /// </summary>
    [Fact]
    public void ValidateWorkflowJson_WithNullJson_ReturnsFalseWithErrors()
    {
        var service = CreateService();

        var isValid = service.ValidateWorkflowJson(null!, out var errors);

        isValid.Should().BeFalse();
        errors.Should().ContainSingle(e => e.Contains("cannot be null or empty"));
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.DeleteWorkflow"/> removes workflow from service.
    /// </summary>
    [Fact]
    public void DeleteWorkflow_RemovesWorkflowFromService()
    {
        var service = CreateService();
        service.CreateWorkflow("wf-1", "Test");
        service.DeleteWorkflow("wf-1");

        var workflow = service.GetWorkflow("wf-1");
        workflow.Should().BeNull();
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.CreateWorkflow"/> throws ValidationException for empty ID.
    /// </summary>
    [Fact]
    public void CreateWorkflow_WithEmptyId_ThrowsValidationException()
    {
        var service = CreateService();

        var act = () => service.CreateWorkflow("", "Test");

        act.Should().Throw<ValidationException>();
    }

    /// <summary>
    /// Tests that <see cref="WorkflowDefinitionService.CreateWorkflow"/> throws ValidationException for empty name.
    /// </summary>
    [Fact]
    public void CreateWorkflow_WithEmptyName_ThrowsValidationException()
    {
        var service = CreateService();

        var act = () => service.CreateWorkflow("wf-1", "");

        act.Should().Throw<ValidationException>();
    }
}
