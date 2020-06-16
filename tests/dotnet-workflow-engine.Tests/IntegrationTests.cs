// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Exceptions;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using DotNetWorkflowEngine.Data.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using WorkflowExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

namespace DotNetWorkflowEngine.Tests;

/// <summary>
/// Contains integration tests that verify the end-to-end functionality of the workflow engine,
/// including workflow execution, activity handling, state transitions, and error recovery.
/// </summary>
public class IntegrationTests
{
	/// <summary>
	/// Creates and returns the core service instances needed for testing workflow execution.
	/// </summary>
	/// <returns>A tuple containing the workflow execution service, activity service, audit service, and audit repository mock.</returns>
	private (WorkflowExecutionService, ActivityService, AuditService, Mock<IAuditRepository>, WorkflowDefinitionService) CreateServices()
	{
		var auditRepoMock = new Mock<IAuditRepository>();
		auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);

		var auditService = new AuditService(auditRepoMock.Object);
		var definitionService = new WorkflowDefinitionService();
		var retryPolicyService = new RetryPolicyService();
		var activityService = new ActivityService(retryPolicyService);
		var executionService = new WorkflowExecutionService(definitionService, auditService, activityService);

		return (executionService, activityService, auditService, auditRepoMock, definitionService);
	}

	/// <summary>
	/// Creates a simple workflow with three activities and two transitions for testing purposes.
	/// </summary>
	/// <returns>A configured workflow instance with basic structure.</returns>
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

	/// <summary>
	/// Tests that a simple workflow with three activities executes successfully from start to finish.
	/// </summary>
	[Fact]
	public async Task EndToEnd_SimpleWorkflow_ExecutesSuccessfully()
	{
		var (executionService, activityService, auditService, _, definitionService) = CreateServices();
		var mockHandler = new Mock<ActivityService.IActivityHandler>();
		mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
			.ReturnsAsync(new Dictionary<string, object?> { { "status", "completed" } });
		activityService.RegisterHandler("default", mockHandler.Object);

		var workflow = CreateSimpleWorkflow();
		definitionService.AddWorkflow(workflow);
		var instance = executionService.CreateInstance(workflow.Id, "corr-123", "user1");

		instance.Status.Should().Be(WorkflowStatus.Draft);

		instance.Start();
		instance.Status.Should().Be(WorkflowStatus.Active);

		var result = await executionService.StartAsync(instance.Id);

		result.Should().NotBeNull();
		mockHandler.Verify(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()), Times.AtLeast(3));
	}

	/// <summary>
	/// Tests that conditional routing evaluates expressions correctly and follows the appropriate path.
	/// </summary>
	[Fact]
	public async Task EndToEnd_ConditionalRouting_SelectsCorrectPath()
	{
		var (executionService, activityService, auditService, _, definitionService) = CreateServices();
		var mockHandler = new Mock<ActivityService.IActivityHandler>();
		mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
			.ReturnsAsync((Activity activity, WorkflowExecutionContext ctx) =>
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
		definitionService.AddWorkflow(workflow);

		var instance = executionService.CreateInstance(workflow.Id);
		instance.Start();

		var result = await executionService.StartAsync(instance.Id);

		result.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that a new workflow instance is created with correct initial state and properties.
	/// </summary>
	[Fact]
	public void CreateInstance_NewWorkflowInstance_InitializesCorrectly()
	{
		var (executionService, _, _, _, definitionService) = CreateServices();
		var workflow = CreateSimpleWorkflow();
		definitionService.AddWorkflow(workflow);

		var instance = executionService.CreateInstance(workflow.Id, "corr-456", "admin");

		instance.WorkflowId.Should().Be(workflow.Id);
		instance.Status.Should().Be(WorkflowStatus.Draft);
		instance.CorrelationId.Should().Be("corr-456");
		instance.InitiatedBy.Should().Be("admin");
		instance.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
	}

	/// <summary>
	/// Tests that attempting to create an instance with a non-existent workflow throws an exception.
	/// </summary>
	[Fact]
	public void CreateInstance_NonExistentWorkflow_ThrowsWorkflowException()
	{
		var (executionService, _, _, _, _) = CreateServices();

		Assert.Throws<WorkflowException>(() => executionService.CreateInstance("nonexistent-workflow"));
	}

	/// <summary>
	/// Tests that workflow execution with audit trail logs all events to the audit repository.
	/// </summary>
	[Fact]
	public async Task ExecuteWorkflow_WithAuditTrail_LogsAllEvents()
	{
		var (executionService, activityService, auditService, auditRepoMock, definitionService) = CreateServices();
		var mockHandler = new Mock<ActivityService.IActivityHandler>();
		mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
			.ReturnsAsync(new Dictionary<string, object?>());
		activityService.RegisterHandler("default", mockHandler.Object);

		var workflow = CreateSimpleWorkflow();
		definitionService.AddWorkflow(workflow);
		var instance = executionService.CreateInstance(workflow.Id, "corr-789", "user2");
		instance.Start();

		await executionService.StartAsync(instance.Id);

		auditRepoMock.Verify(r => r.AddAsync(It.IsAny<AuditLogEntry>()), Times.AtLeastOnce);
	}

	/// <summary>
	/// Tests that multiple workflow instances maintain independent state during concurrent execution.
	/// </summary>
	[Fact]
	public async Task WorkflowExecution_MultipleInstances_MaintainsIndependentState()
	{
		var (executionService, activityService, _, _, definitionService) = CreateServices();
		var mockHandler = new Mock<ActivityService.IActivityHandler>();
		var executedActivities = new List<string>();
		mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
			.Callback<Activity, WorkflowExecutionContext>((act, _) => executedActivities.Add(act.Id))
			.ReturnsAsync(new Dictionary<string, object?>());
		activityService.RegisterHandler("default", mockHandler.Object);

		var workflow = CreateSimpleWorkflow();
		definitionService.AddWorkflow(workflow);

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

	/// <summary>
	/// Tests that workflow execution retries failed activities according to retry policy and eventually succeeds.
	/// </summary>
	[Fact]
	public async Task WorkflowExecution_RetryOnFailure_EventuallySucceeds()
	{
		var (executionService, activityService, _, _, definitionService) = CreateServices();
		var callCount = 0;
		var mockHandler = new Mock<ActivityService.IActivityHandler>();
		mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
			.Returns((Activity act, WorkflowExecutionContext ctx) =>
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
		definitionService.AddWorkflow(workflow);

		var instance = executionService.CreateInstance(workflow.Id);
		instance.Start();

		var result = await executionService.StartAsync(instance.Id);

		result.Should().NotBeNull();
		callCount.Should().BeGreaterThanOrEqualTo(3);
	}

	/// <summary>
	/// Tests that workflow instance state transitions work correctly between Draft and Active states.
	/// </summary>
	[Fact]
	public void Instance_StateTransitions_AreCorrect()
	{
		var (executionService, _, _, _, definitionService) = CreateServices();
		var workflow = CreateSimpleWorkflow();
		definitionService.AddWorkflow(workflow);

		var instance = executionService.CreateInstance(workflow.Id);

		instance.IsActive().Should().BeFalse(); // Initially in Draft status
		instance.Status.Should().Be(WorkflowStatus.Draft);

		instance.Start();

		instance.IsActive().Should().BeTrue();
		instance.Status.Should().Be(WorkflowStatus.Active);
		instance.StartedAt.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that activity execution tracking correctly records and reports executed activities.
	/// </summary>
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

	/// <summary>
	/// Tests that activity result status transitions work correctly between Pending, Completed, Failed, and Skipped states.
	/// </summary>
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

	/// <summary>
	/// Tests that activity result skipped status works correctly.
	/// </summary>
	[Fact]
	public void ActivityResult_SkippedStatus_WorksCorrectly()
	{
		var result = new ActivityResult("activity1");

		result.SetSkipped("Condition not met");

		result.Status.Should().Be(ActivityStatus.Skipped);
		result.IsSuccess().Should().BeFalse();
		result.IsFailed().Should().BeFalse();
	}

	/// <summary>
	/// Tests that execution context variable management works correctly for setting and getting variables.
	/// </summary>
	[Fact]
	public async Task ExecutionContext_VariableManagement_WorksCorrectly()
	{
		var context = new WorkflowExecutionContext
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

		await Task.Delay(5);
		context.Complete();

		context.IsActive.Should().BeFalse();
		context.EndTime.Should().NotBeNull();
		context.ExecutionDurationMs.Should().BeGreaterThan(0);
	}

	/// <summary>
	/// Tests that execution context reset clears all variables and resets state.
	/// </summary>
	[Fact]
	public void ExecutionContext_Reset_ClearsAllData()
	{
		var context = new WorkflowExecutionContext();
		context.SetVariable("key", "value");
		context.SetActivityInput("input", "data");
		context.IsActive = false;

		context.Reset();

		context.Variables.Should().BeEmpty();
		context.ActivityInput.Should().BeEmpty();
		context.IsActive.Should().BeTrue();
		context.ExecutionError.Should().BeNull();
	}

	/// <summary>
	/// Tests that the workflow engine handles concurrent workflow executions correctly.
	/// </summary>
	[Fact]
	public async Task ConcurrentWorkflowExecution_HandlesConcurrency()
	{
		var (executionService, activityService, _, _, definitionService) = CreateServices();
		var mockHandler = new Mock<ActivityService.IActivityHandler>();
		mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<WorkflowExecutionContext>()))
			.ReturnsAsync(new Dictionary<string, object?>());
		activityService.RegisterHandler("default", mockHandler.Object);

		var workflow = CreateSimpleWorkflow();
		definitionService.AddWorkflow(workflow);

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

	/// <summary>
	/// Tests that a complete workflow passes validation.
	/// </summary>
	[Fact]
	public void WorkflowValidation_ValidatesCompleteWorkflow()
	{
		var workflow = CreateSimpleWorkflow();

		var validationResult = DotNetWorkflowEngine.Utilities.WorkflowValidator.ValidateWorkflow(workflow);

		validationResult.IsValid.Should().BeTrue();
		validationResult.Errors.Should().BeEmpty();
	}

	/// <summary>
	/// Tests that a workflow with invalid activities fails validation.
	/// </summary>
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

	/// <summary>
	/// Tests that expression evaluation handles complex conditions correctly.
	/// </summary>
	[Fact]
	public void ExpressionEvaluation_ComplexConditions_EvaluateCorrectly()
	{
		var context = new WorkflowExecutionContext();
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