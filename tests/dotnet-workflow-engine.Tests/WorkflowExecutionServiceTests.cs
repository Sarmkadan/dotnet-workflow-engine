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
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotNetWorkflowEngine.Tests;

public class WorkflowExecutionServiceTests
{
    private readonly WorkflowExecutionService _executionService;
    private readonly WorkflowDefinitionService _definitionService;
    private readonly Mock<IAuditRepository> _mockAuditRepository;
    private readonly AuditService _auditService;
    private readonly ActivityService _activityService;

    public WorkflowExecutionServiceTests()
    {
        _definitionService = new WorkflowDefinitionService();
        _mockAuditRepository = new Mock<IAuditRepository>();
        _auditService = new AuditService(_mockAuditRepository.Object);
        
        // ActivityService depends on RetryPolicyService, which depends on nothing.
        // Assuming ActivityService can be instantiated simply.
        _activityService = new ActivityService(new RetryPolicyService());

        _executionService = new WorkflowExecutionService(_definitionService, _auditService, _activityService);
    }

    [Fact]
    public void CreateInstance_ShouldCreateInstance_WhenWorkflowIsActive()
    {
        // Arrange
        var workflowId = "workflow1";
        var workflow = new Workflow { Id = workflowId, Status = WorkflowStatus.Active };
        _definitionService.AddWorkflow(workflow);

        // Act
        var instance = _executionService.CreateInstance(workflowId);

        // Assert
        instance.Should().NotBeNull();
        instance.WorkflowId.Should().Be(workflowId);
        instance.Status.Should().Be(WorkflowStatus.Draft);
    }

    [Fact]
    public void CreateInstance_ShouldThrowWorkflowException_WhenWorkflowNotFound()
    {
        // Arrange
        var workflowId = "nonExistent";

        // Act
        Action act = () => _executionService.CreateInstance(workflowId);

        // Assert
        act.Should().Throw<WorkflowException>()
           .WithMessage($"Workflow '{workflowId}' not found");
    }

    [Fact]
    public void CreateInstance_ShouldThrowStateException_WhenWorkflowIsNotActive()
    {
        // Arrange
        var workflowId = "inactiveWorkflow";
        var workflow = new Workflow { Id = workflowId, Status = WorkflowStatus.Archived };
        _definitionService.AddWorkflow(workflow);

        // Act
        Action act = () => _executionService.CreateInstance(workflowId);

        // Assert
        act.Should().Throw<StateException>()
           .WithMessage("Workflow is not active");
    }

    [Fact]
    public async Task StartAsync_ShouldThrowWorkflowException_WhenInstanceNotFound()
    {
        // Act
        Func<Task> act = async () => await _executionService.StartAsync("nonExistent");

        // Assert
        await act.Should().ThrowAsync<WorkflowException>()
           .WithMessage("Instance 'nonExistent' not found");
    }

    [Fact]
    public void GetInstance_ShouldReturnInstance_WhenExists()
    {
        // Arrange
        var workflowId = "workflow1";
        var workflow = new Workflow { Id = workflowId, Status = WorkflowStatus.Active };
        _definitionService.AddWorkflow(workflow);
        var instance = _executionService.CreateInstance(workflowId);

        // Act
        var result = _executionService.GetInstance(instance.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(instance.Id);
    }
}
