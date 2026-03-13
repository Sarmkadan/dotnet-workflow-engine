// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Events;
using DotNetWorkflowEngine.Models;
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
}
