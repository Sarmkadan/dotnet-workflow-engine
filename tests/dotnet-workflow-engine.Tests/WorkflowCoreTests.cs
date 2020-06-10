// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Events;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using DotNetWorkflowEngine.Data.Repositories;
using DotNetWorkflowEngine.Utilities;
using WorkflowExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

/// <summary>
/// Tests for the WorkflowCore class.
/// </summary>
namespace DotNetWorkflowEngine.Tests;

public class WorkflowCoreTests
{
    /// <summary>
    /// Verifies that a workflow instance transitions to an active status when started.
    /// </summary>
    [Fact]
    public void WorkflowInstance_Start_TransitionsToActiveStatus()
    {
        // Arrange
        var instance = new WorkflowInstance("order-workflow");
        instance.Status.Should().Be(WorkflowStatus.Draft);

        // Act
        instance.Start();

        // Assert
        instance.Status.Should().Be(WorkflowStatus.Active);
        instance.StartedAt.Should().NotBeNull();
        instance.IsActive().Should().BeTrue();
    }

    /// <summary>
    /// Verifies that recording an activity execution does not duplicate entries.
    /// </summary>
    [Fact]
    public void WorkflowInstance_RecordActivityExecution_DoesNotDuplicateEntries()
    {
        // Arrange
        var instance = new WorkflowInstance("order-workflow");
        const string activityId = "validate-order";

        // Act
        instance.RecordActivityExecution(activityId);
        instance.RecordActivityExecution(activityId); // duplicate call

        // Assert
        instance.ExecutedActivities.Should().HaveCount(1);
        instance.HasActivityBeenExecuted(activityId).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that setting an activity result to success marks it as completed and exposes the output.
    /// </summary>
    [Fact]
    public void ActivityResult_SetSuccess_MarksCompletedAndExposesOutput()
    {
        // Arrange
        var result = new ActivityResult("payment-activity");
        var output = new Dictionary<string, object?> { ["transactionId"] = "TX-9001" };

        // Act
        result.SetSuccess(output);

        // Assert
        result.IsSuccess().Should().BeTrue();
        result.IsFailed().Should().BeFalse();
        result.Status.Should().Be(ActivityStatus.Completed);
        result.GetOutput<string>("transactionId").Should().Be("TX-9001");
        result.EndTime.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the workflow validator reports an error when a workflow is missing an ID.
    /// </summary>
    [Fact]
    public void WorkflowValidator_ValidateWorkflow_MissingId_ReportsError()
    {
        // Arrange — workflow with no Id but otherwise complete
        var workflow = new Workflow
        {
            Name = "Order Processing",
            StartActivityId = "step-one",
            Activities =
            {
                new Activity { Id = "step-one", Name = "First Step" }
            }
        };

        // Act
        var validationResult = WorkflowValidator.ValidateWorkflow(workflow);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.Contains("Workflow ID is required"));
    }

    /// <summary>
    /// Verifies that publishing an event to the event bus invokes the subscribed handler.
    /// </summary>
    [Fact]
    public async Task EventBus_Publish_InvokesSubscribedHandler()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EventBus>>();
        var eventBus = new EventBus(loggerMock.Object);
        var handlerInvoked = false;

        Task Handler(WorkflowStartedEvent e)
        {
            handlerInvoked = true;
            return Task.CompletedTask;
        }

        eventBus.Subscribe<WorkflowStartedEvent>(Handler);

        // Act
        await eventBus.PublishAsync(new WorkflowStartedEvent
        {
            WorkflowId = "order-wf",
            InstanceId = "inst-001"
        });

        // Assert
        handlerInvoked.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that the workflow execution service cleans up active activities when an activity throws an exception.
    /// </summary>
    [Fact]
    public async Task WorkflowExecutionService_ActivityThrowsException_ActiveActivitiesCleanedUp()
    {
        // Arrange
        var workflowId = "test-workflow";
        var activityId = "failing-activity";

        // Mock dependencies
        var mockWorkflowDefinitionService = new Mock<WorkflowDefinitionService>();
        var mockAuditRepository = new Mock<IAuditRepository>();
        var mockAuditService = new Mock<AuditService>(mockAuditRepository.Object);
        var mockActivityService = new Mock<ActivityService>();

        // Setup mock WorkflowDefinitionService
        var workflow = new Workflow
        {
            Id = workflowId,
            Name = "Test Workflow",
            StartActivityId = activityId,
            Activities = { new Activity { Id = activityId, Name = "Failing Activity" } },
            Transitions = new List<Transition>()
        };
        workflow.Publish();
        mockWorkflowDefinitionService.Setup(s => s.GetWorkflow(workflowId)).Returns(workflow);

        // Setup mock ActivityService to throw an exception
        mockActivityService = new Mock<ActivityService>(new RetryPolicyService());
        mockActivityService
            .Setup(s => s.ExecuteAsync(It.Is<Activity>(a => a.Id == activityId), It.IsAny<WorkflowExecutionContext>()))
            .ThrowsAsync(new InvalidOperationException("Simulated activity failure"));

        // Instantiate the service under test
        var workflowExecutionService = new WorkflowExecutionService(
            mockWorkflowDefinitionService.Object,
            mockAuditService.Object,
            mockActivityService.Object
        );

        // Create an instance (it will be added to _instances internally)
        var instance = workflowExecutionService.CreateInstance(workflowId);
        var actualInstanceId = instance.Id; // Get the actual ID generated by CreateInstance

        instance.Start(); // Ensure the instance is in an active state

        // Act & Assert
        // Expect the StartAsync to throw an exception because the first activity fails
        await Assert.ThrowsAsync<InvalidOperationException>(() => workflowExecutionService.StartAsync(actualInstanceId));

        // Assert that the instance status reflects failure
        var updatedInstance = workflowExecutionService.GetInstance(actualInstanceId);
        updatedInstance.Should().NotBeNull();
        updatedInstance!.Status.Should().Be(WorkflowStatus.Suspended);
        updatedInstance.ErrorMessage.Should().Contain("Simulated activity failure");

        // CRITICAL ASSERTION: Ensure ActiveActivities is empty, meaning it was cleaned up
        updatedInstance.ActiveActivities.Should().BeEmpty();
    }
}
