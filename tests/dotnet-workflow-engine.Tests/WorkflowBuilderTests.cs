// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Exceptions;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using DotNetWorkflowEngine.Utilities;
using FluentAssertions;
using Xunit;

namespace DotNetWorkflowEngine.Tests;

/// <summary>
/// Contains unit tests for the <see cref="WorkflowBuilder"/> class, which provides a fluent API for constructing workflow definitions.
/// </summary>
/// <remarks>
/// These tests verify that workflows can be built with various combinations of activities, transitions, and configuration options,
/// and that the builder correctly handles validation, registration, and error cases.
/// </remarks>
public class WorkflowBuilderTests
{
	/// <summary>
	/// Creates a new instance of <see cref="WorkflowDefinitionService"/> for testing purposes.
	/// </summary>
	/// <returns>A new <see cref="WorkflowDefinitionService"/> instance.</returns>
	private WorkflowDefinitionService CreateService()
	{
		return new WorkflowDefinitionService();
	}

	/// <summary>
	/// Tests that a basic workflow with activities and transitions can be built successfully.
	/// </summary>
	[Fact]
	public void BuildBasicWorkflow_WithActivitiesAndTransitions_BuildsSuccessfully()
	{
		var service = CreateService();
		var builder = new WorkflowBuilder("wf-1", "Test Workflow", service);

		var workflow = builder
			.WithDescription("A test workflow")
			.AddTaskActivity("start", "Start Activity")
			.AddTaskActivity("end", "End Activity")
			.AddTransition("start", "end")
			.WithStartActivity("start")
			.WithEndActivity("end")
			.Build();

		workflow.Id.Should().Be("wf-1");
		workflow.Name.Should().Be("Test Workflow");
		workflow.Description.Should().Be("A test workflow");
		workflow.Activities.Should().HaveCount(2);
		workflow.Transitions.Should().HaveCount(1);
		workflow.StartActivityId.Should().Be("start");
		workflow.EndActivityId.Should().Be("end");
	}

	/// <summary>
	/// Tests that building a workflow without required configuration throws a <see cref="ValidationException"/>.
	/// </summary>
	[Fact]
	public void Build_WithInvalidWorkflow_ThrowsValidationException()
	{
		var service = CreateService();
		var builder = new WorkflowBuilder("wf-1", "Test", service);

		var act = () => builder.Build();

		act.Should().Throw<ValidationException>();
	}

	/// <summary>
	/// Tests that a workflow can be built and registered with the workflow service.
	/// </summary>
	[Fact]
	public void BuildAndRegister_RegistersWorkflowWithService()
	{
		var service = CreateService();
		var builder = new WorkflowBuilder("wf-1", "Test Workflow", service);

		var workflow = builder
			.AddTaskActivity("start", "Start")
			.WithStartActivity("start")
			.WithEndActivity("start")
			.BuildAndRegister();

		service.GetWorkflow("wf-1").Should().NotBeNull();
		service.GetActivities("wf-1").Should().HaveCount(1);
	}

	/// <summary>
	/// Tests that transitions are properly registered when building and registering a workflow.
	/// </summary>
	[Fact]
	public void BuildAndRegister_WithTransitions_RegistersTransitionsWithService()
	{
		var service = CreateService();
		var builder = new WorkflowBuilder("wf-1", "Test", service);

		builder
			.AddTaskActivity("start", "Start")
			.AddTaskActivity("end", "End")
			.AddTransition("start", "end")
			.WithStartActivity("start")
			.WithEndActivity("end")
			.BuildAndRegister();

		var workflow = service.GetWorkflow("wf-1");
		workflow!.Transitions.Should().HaveCount(1);
	}

	/// <summary>
	/// Tests that event activities (MessageCatchEvent) can be added to a workflow.
	/// </summary>
	[Fact]
	public void AddEventActivity_CreatesEventActivity()
	{
		var service = CreateService();
		var builder = new WorkflowBuilder("wf-1", "Event Workflow", service);

		var eventActivity = new Activity
		{
			Id = "wait-for-event",
			Name = "Wait for Event",
			Type = "MessageCatchEvent",
			ExecutionMode = ExecutionMode.Sequential
		};

		var workflow = builder
			.AddTaskActivity("start", "Start")
			.AddActivity(eventActivity)
			.AddTransition("start", "wait-for-event")
			.WithStartActivity("start")
			.WithEndActivity("wait-for-event")
			.Build();

		workflow.Activities.Should().HaveCount(2);
		var messageCatchActivity = workflow.Activities.FirstOrDefault(a => a.Type == "MessageCatchEvent");
		messageCatchActivity.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that conditional transitions with expressions can be created in a workflow.
	/// </summary>
	[Fact]
	public void AddConditionalTransition_CreatesConditionalTransition()
	{
		var service = CreateService();
		var builder = new WorkflowBuilder("wf-1", "Conditional Workflow", service);

		var workflow = builder
			.AddTaskActivity("decision", "Decision")
			.AddTaskActivity("approved", "Approved Path")
			.AddTaskActivity("rejected", "Rejected Path")
			.AddTransition("decision", "approved", "${approved}")
			.AddTransition("decision", "rejected", "!${approved}")
			.WithStartActivity("decision")
			.Build();

		workflow.Transitions.Should().HaveCount(2);
		workflow.Transitions[0].ConditionExpression.Should().Be("${approved}");
		workflow.Transitions[1].ConditionExpression.Should().Be("!${approved}");
	}

	/// <summary>
	/// Tests that all builder methods return the builder instance for method chaining.
	/// </summary>
	[Fact]
	public void Fluent_AllMethodsReturnBuilder_ForChaining()
	{
		var service = CreateService();
		var builder = new WorkflowBuilder("wf-1", "Test", service);

		var result1 = builder.WithDescription("Test");
		var result2 = builder.AddTaskActivity("act", "Activity");
		var result3 = builder.AddTransition("act", "act");
		var result4 = builder.WithStartActivity("act");
		var result5 = builder.WithEndActivity("act");

		result1.Should().Be(builder);
		result2.Should().Be(builder);
		result3.Should().Be(builder);
		result4.Should().Be(builder);
		result5.Should().Be(builder);
	}

	/// <summary>
	/// Tests that custom activity types can be added to a workflow.
	/// </summary>
	[Fact]
	public void AddCustomActivity_AllowsArbitraryActivities()
	{
		var service = CreateService();
		var customActivity = new Activity
		{
			Id = "custom",
			Name = "Custom Activity",
			Type = "CustomType",
			HandlerType = "custom-handler"
		};

		var workflow = new WorkflowBuilder("wf-1", "Test", service)
			.AddActivity(customActivity)
			.WithStartActivity("custom")
			.WithEndActivity("custom")
			.Build();

		workflow.Activities.Should().HaveCount(1);
		workflow.Activities[0].Type.Should().Be("CustomType");
	}

	/// <summary>
	/// Tests that a sequential workflow can be created using the static <see cref="WorkflowBuilder.CreateSerial"/> method.
	/// </summary>
	/// <param name="activityNames">The names of activities to create in sequence.</param>
	[Fact]
	public void CreateSerial_WithActivityNames_CreatesSequentialWorkflow()
	{
		var service = CreateService();

		var workflow = WorkflowBuilder
			.CreateSerial("wf-1", "Sequential", service, "First", "Second", "Third")
			.Build();

		workflow.Activities.Should().HaveCount(3);
		workflow.Transitions.Should().HaveCount(2);
		workflow.StartActivityId.Should().Be("first");
		workflow.EndActivityId.Should().Be("third");
	}

	/// <summary>
	/// Tests that an empty sequential workflow can be created.
	/// </summary>
	[Fact]
	public void CreateSerial_WithNoActivities_CreatesEmptyWorkflow()
	{
		var service = CreateService();

		var builder = WorkflowBuilder.CreateSerial("wf-1", "Empty", service);

		builder.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that a sequential workflow with a single activity can be created.
	/// </summary>
	[Fact]
	public void CreateSerial_WithOneActivity_CreatesValidWorkflow()
	{
		var service = CreateService();

		var workflow = WorkflowBuilder
			.CreateSerial("wf-1", "Single", service, "Activity")
			.Build();

		workflow.Activities.Should().HaveCount(1);
		workflow.Transitions.Should().BeEmpty();
		workflow.StartActivityId.Should().Be("activity");
		workflow.EndActivityId.Should().Be("activity");
	}

	/// <summary>
	/// Tests that adding a task activity with a handler sets the handler type correctly.
	/// </summary>
	[Fact]
	public void AddTaskActivity_WithHandler_SetsHandlerType()
	{
		var service = CreateService();

		var workflow = new WorkflowBuilder("wf-1", "Test", service)
			.AddTaskActivity("task", "Task", "http-handler")
			.WithStartActivity("task")
			.WithEndActivity("task")
			.Build();

		var task = workflow.Activities.First();
		task.HandlerType.Should().Be("http-handler");
		task.Type.Should().Be("Task");
	}

	/// <summary>
	/// Tests that adding a task activity without a handler results in a null handler type.
	/// </summary>
	[Fact]
	public void AddTaskActivity_WithoutHandler_HasNullHandler()
	{
		var service = CreateService();

		var workflow = new WorkflowBuilder("wf-1", "Test", service)
			.AddTaskActivity("task", "Task")
			.WithStartActivity("task")
			.WithEndActivity("task")
			.Build();

		var task = workflow.Activities.First();
		task.HandlerType.Should().BeNull();
	}

	/// <summary>
	/// Tests that attempting to register a workflow with a duplicate ID throws a <see cref="WorkflowException"/>.
	/// </summary>
	[Fact]
	public void BuildAndRegister_WithDuplicateWorkflowId_ThrowsWorkflowException()
	{
		var service = CreateService();
		service.CreateWorkflow("wf-1", "Existing");

		var builder = new WorkflowBuilder("wf-1", "New", service);
		builder.AddTaskActivity("start", "Start")
			.WithStartActivity("start")
			.WithEndActivity("start");

		var act = () => builder.BuildAndRegister();

		act.Should().Throw<WorkflowException>();
	}

	/// <summary>
	/// Tests that multiple transitions from the same source activity are all created.
	/// </summary>
	[Fact]
	public void MultipleTransitions_FromSameActivity_AllCreated()
	{
		var service = CreateService();

		var workflow = new WorkflowBuilder("wf-1", "Multi-Transition", service)
			.AddTaskActivity("source", "Source")
			.AddTaskActivity("target1", "Target 1")
			.AddTaskActivity("target2", "Target 2")
			.AddTransition("source", "target1", "${path} == \"A\"")
			.AddTransition("source", "target2", "${path} == \"B\"")
			.WithStartActivity("source")
			.Build();

		workflow.Transitions.Should().HaveCount(2);
		workflow.Transitions.Should().AllSatisfy(t => t.FromActivityId.Should().Be("source"));
	}

	/// <summary>
	/// Tests that a complex workflow with many activities and transitions can be built successfully.
	/// </summary>
	[Fact]
	public void ComplexWorkflow_WithManyActivities_BuildsSuccessfully()
	{
		var service = CreateService();

		var workflow = new WorkflowBuilder("wf-1", "Complex Workflow", service)
			.AddTaskActivity("step1", "Step 1")
			.AddTaskActivity("step2", "Step 2")
			.AddTaskActivity("decision", "Decision")
			.AddTaskActivity("path-a", "Path A")
			.AddTaskActivity("path-b", "Path B")
			.AddTaskActivity("merge", "Merge")
			.AddTransition("step1", "step2")
			.AddTransition("step2", "decision")
			.AddTransition("decision", "path-a", "${choice} == \"A\"")
			.AddTransition("decision", "path-b", "${choice} == \"B\"")
			.AddTransition("path-a", "merge")
			.AddTransition("path-b", "merge")
			.WithStartActivity("step1")
			.WithEndActivity("merge")
			.Build();

		workflow.Activities.Should().HaveCount(6);
		workflow.Transitions.Should().HaveCount(6);
	}
}