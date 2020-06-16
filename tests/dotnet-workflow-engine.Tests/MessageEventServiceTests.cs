// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Events;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetWorkflowEngine.Tests;

public class MessageEventServiceTests
{
    private (MessageEventService, Mock<IEventBus>, Mock<WorkflowExecutionService>, Mock<AuditService>) CreateServices()
    {
        var eventBusMock = new Mock<IEventBus>();
        var auditRepoMock = new Mock<DotNetWorkflowEngine.Data.Repositories.IAuditRepository>();
        auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);
        var definitionService = new WorkflowDefinitionService();
        var activityService = new ActivityService(new RetryPolicyService());
        var workflowExecutionMock = new Mock<WorkflowExecutionService>(definitionService, new AuditService(auditRepoMock.Object), activityService);
        var auditMock = new Mock<AuditService>(auditRepoMock.Object);

        eventBusMock.Setup(eb => eb.PublishAsync(It.IsAny<IWorkflowEvent>()))
            .Returns(Task.CompletedTask);

        var service = new MessageEventService(
            eventBusMock.Object,
            workflowExecutionMock.Object,
            auditMock.Object
        );

        return (service, eventBusMock, workflowExecutionMock, auditMock);
    }

    private IWorkflowMessage CreateMessage(
        string correlationKey = "corr-1",
        string messageName = "OrderApproved",
        Dictionary<string, object?>? payload = null)
    {
        return new WorkflowMessage
        {
            CorrelationKey = correlationKey,
            MessageName = messageName,
            Payload = payload ?? new Dictionary<string, object?>()
        };
    }

    [Fact]
    public async Task PublishMessageAsync_WithValidMessage_PublishesEvent()
    {
        var (service, eventBusMock, executionMock, _) = CreateServices();
        executionMock.Setup(e => e.GetInstancesByCorrelation(It.IsAny<string>()))
            .Returns(new List<WorkflowInstance>());
        var message = CreateMessage();

        await service.PublishMessageAsync(message);

        eventBusMock.Verify(
            eb => eb.PublishAsync(It.Is<MessageReceivedEvent>(e =>
                e.CorrelationKey == "corr-1" &&
                e.MessageName == "OrderApproved")),
            Times.Once);
    }

    [Fact]
    public async Task PublishMessageAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        var (service, _, _, _) = CreateServices();

        var act = () => service.PublishMessageAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PublishMessageAsync_WithNoWaitingInstance_LogsWarningAndReturnsFalse()
    {
        var (service, _, executionMock, auditMock) = CreateServices();
        executionMock.Setup(e => e.GetInstancesByCorrelation(It.IsAny<string>()))
            .Returns(new List<WorkflowInstance>());

        var message = CreateMessage();
        var result = await service.PublishMessageAsync(message);

        result.Should().BeFalse();
        auditMock.Verify(
            a => a.LogCustomEvent(It.IsAny<string>(), "UncorrelatedMessage", It.IsAny<string>(), "Warning"),
            Times.Once);
    }

    [Fact]
    public async Task PublishMessageAsync_WithWaitingInstance_ResumesWorkflow()
    {
        var (service, _, executionMock, _) = CreateServices();
        var instance = new WorkflowInstance("wf-1");
        instance.Status = WorkflowStatus.WaitingForMessage;
        instance.SetContextVariable("WaitingForMessageName", "OrderApproved");

        executionMock.Setup(e => e.GetInstancesByCorrelation("corr-1"))
            .Returns(new List<WorkflowInstance> { instance });
        executionMock.Setup(e => e.ResumeFromMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object?>>()))
            .Returns(Task.CompletedTask);

        var message = CreateMessage();
        var result = await service.PublishMessageAsync(message);

        result.Should().BeTrue();
        executionMock.Verify(
            e => e.ResumeFromMessageAsync(instance.Id, "OrderApproved", "corr-1", It.IsAny<Dictionary<string, object?>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishMessageAsync_WithMultipleWaitingInstances_ResumesFirstMatch()
    {
        var (service, _, executionMock, _) = CreateServices();
        var instance1 = new WorkflowInstance("wf-1");
        instance1.Status = WorkflowStatus.WaitingForMessage;
        instance1.SetContextVariable("WaitingForMessageName", "OrderApproved");

        var instance2 = new WorkflowInstance("wf-2");
        instance2.Status = WorkflowStatus.WaitingForMessage;
        instance2.SetContextVariable("WaitingForMessageName", "PaymentProcessed");

        executionMock.Setup(e => e.GetInstancesByCorrelation("corr-1"))
            .Returns(new List<WorkflowInstance> { instance1, instance2 });
        executionMock.Setup(e => e.ResumeFromMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object?>>()))
            .Returns(Task.CompletedTask);

        var message = CreateMessage(messageName: "OrderApproved");
        var result = await service.PublishMessageAsync(message);

        result.Should().BeTrue();
        executionMock.Verify(
            e => e.ResumeFromMessageAsync(instance1.Id, "OrderApproved", "corr-1", It.IsAny<Dictionary<string, object?>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishMessageAsync_WhenResumeThrows_FailsInstanceAndRethrows()
    {
        var (service, _, executionMock, auditMock) = CreateServices();
        var instance = new WorkflowInstance("wf-1");
        instance.Status = WorkflowStatus.WaitingForMessage;
        instance.SetContextVariable("WaitingForMessageName", "OrderApproved");

        executionMock.Setup(e => e.GetInstancesByCorrelation("corr-1"))
            .Returns(new List<WorkflowInstance> { instance });
        executionMock.Setup(e => e.ResumeFromMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object?>>()))
            .ThrowsAsync(new InvalidOperationException("Resume failed"));
        executionMock.Setup(e => e.FailInstance(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();

        var message = CreateMessage();

        var act = () => service.PublishMessageAsync(message);
        await act.Should().ThrowAsync<InvalidOperationException>();

        executionMock.Verify(
            e => e.FailInstance(instance.Id, It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishMessageAsync_PreservesPayloadInEvent()
    {
        var (service, eventBusMock, executionMock, _) = CreateServices();
        var payload = new Dictionary<string, object?> { { "orderId", "123" }, { "amount", 500 } };
        executionMock.Setup(e => e.GetInstancesByCorrelation(It.IsAny<string>()))
            .Returns(new List<WorkflowInstance>());

        var message = CreateMessage(payload: payload);
        await service.PublishMessageAsync(message);

        eventBusMock.Verify(
            eb => eb.PublishAsync(It.Is<MessageReceivedEvent>(e =>
                e.Payload["orderId"]!.ToString() == "123" &&
                (int)e.Payload["amount"]! == 500)),
            Times.Once);
    }

    [Fact]
    public async Task PublishMessageAsync_WithMatchingInstanceButWrongMessageName_DoesNotResume()
    {
        var (service, _, executionMock, auditMock) = CreateServices();
        var instance = new WorkflowInstance("wf-1");
        instance.Status = WorkflowStatus.WaitingForMessage;
        instance.SetContextVariable("WaitingForMessageName", "OrderApproved");

        executionMock.Setup(e => e.GetInstancesByCorrelation("corr-1"))
            .Returns(new List<WorkflowInstance> { instance });

        var message = CreateMessage(messageName: "PaymentProcessed");
        var result = await service.PublishMessageAsync(message);

        result.Should().BeFalse();
        auditMock.Verify(
            a => a.LogCustomEvent(It.IsAny<string>(), "UncorrelatedMessage", It.IsAny<string>(), "Warning"),
            Times.Once);
    }

    [Fact]
    public async Task PublishMessageAsync_WithInstanceNotInWaitingState_IgnoresInstance()
    {
        var (service, _, executionMock, auditMock) = CreateServices();
        var instance = new WorkflowInstance("wf-1");
        instance.Status = WorkflowStatus.Active;
        instance.SetContextVariable("WaitingForMessageName", "OrderApproved");

        executionMock.Setup(e => e.GetInstancesByCorrelation("corr-1"))
            .Returns(new List<WorkflowInstance> { instance });

        var message = CreateMessage();
        var result = await service.PublishMessageAsync(message);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task PublishMessageAsync_MultipleMessages_CanBePublishedSequentially()
    {
        var (service, _, executionMock, _) = CreateServices();
        var instance1 = new WorkflowInstance("wf-1");
        instance1.Status = WorkflowStatus.WaitingForMessage;
        instance1.SetContextVariable("WaitingForMessageName", "Message1");

        var instance2 = new WorkflowInstance("wf-2");
        instance2.Status = WorkflowStatus.WaitingForMessage;
        instance2.SetContextVariable("WaitingForMessageName", "Message2");

        executionMock.Setup(e => e.GetInstancesByCorrelation("corr-1"))
            .Returns(new List<WorkflowInstance> { instance1 });
        executionMock.Setup(e => e.GetInstancesByCorrelation("corr-2"))
            .Returns(new List<WorkflowInstance> { instance2 });
        executionMock.Setup(e => e.ResumeFromMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object?>>()))
            .Returns(Task.CompletedTask);

        var message1 = CreateMessage("corr-1", "Message1");
        var message2 = CreateMessage("corr-2", "Message2");

        var result1 = await service.PublishMessageAsync(message1);
        var result2 = await service.PublishMessageAsync(message2);

        result1.Should().BeTrue();
        result2.Should().BeTrue();
    }
}
