// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Events;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services; // Add this using directive
using DotNetWorkflowEngine.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetWorkflowEngine.Tests;

public class WorkflowCoreTests
{
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

    [Fact]
    public async Task WorkflowExecutionService_ActivityThrowsException_ActiveActivitiesCleanedUp()
    {
        // Arrange
        var workflowId = "test-workflow";
        var activityId = "failing-activity";

        // Mock dependencies
        var mockWorkflowDefinitionService = new Mock<WorkflowDefinitionService>();
        var mockAuditService = new Mock<AuditService>();
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
            .Setup(s => s.ExecuteAsync(It.Is<Activity>(a => a.Id == activityId), It.IsAny<DotNetWorkflowEngine.Models.ExecutionContext>()))
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
