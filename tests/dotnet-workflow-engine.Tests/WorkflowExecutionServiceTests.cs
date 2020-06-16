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

/// <summary>
/// Unit tests for <see cref="WorkflowExecutionService"/>.
/// </summary>
public class WorkflowExecutionServiceTests
{
    private readonly WorkflowExecutionService _executionService;
    private readonly WorkflowDefinitionService _definitionService;
    private readonly Mock<IAuditRepository> _mockAuditRepository;
    private readonly AuditService _auditService;
    private readonly ActivityService _activityService;

    /// <summary>
    /// Initializes services and mocks for testing.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="WorkflowExecutionService.CreateInstance(string)"/> creates an instance when the workflow is active.
    /// </summary>
    /// <returns>void</returns>
    [Fact]
    public void CreateInstance_ShouldCreateInstance_WhenWorkflowIsActive()
    {
        // Arrange
        var workflowId = "workflow1";
        var workflow = new Workflow { Id = workflowId, Name = "Workflow", Status = WorkflowStatus.Active };
        _definitionService.AddWorkflow(workflow);

        // Act
        var instance = _executionService.CreateInstance(workflowId);

        // Assert
        instance.Should().NotBeNull();
        instance.WorkflowId.Should().Be(workflowId);
        instance.Status.Should().Be(WorkflowStatus.Draft);
    }

    /// <summary>
    /// Tests that <see cref="WorkflowExecutionService.CreateInstance(string)"/> throws a <see cref="WorkflowException"/> when the workflow is not found.
    /// </summary>
    /// <returns>void</returns>
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

    /// <summary>
    /// Tests that <see cref="WorkflowExecutionService.CreateInstance(string)"/> throws a <see cref="StateException"/> when the workflow is not active.
    /// </summary>
    /// <returns>void</returns>
    [Fact]
    public void CreateInstance_ShouldThrowStateException_WhenWorkflowIsNotActive()
    {
        // Arrange
        var workflowId = "inactiveWorkflow";
        var workflow = new Workflow { Id = workflowId, Name = "Workflow", Status = WorkflowStatus.Archived };
        _definitionService.AddWorkflow(workflow);

        // Act
        Action act = () => _executionService.CreateInstance(workflowId);

        // Assert
        act.Should().Throw<StateException>()
           .WithMessage("Workflow is not active");
    }

    /// <summary>
    /// Tests that <see cref="WorkflowExecutionService.StartAsync(string)"/> throws a <see cref="WorkflowException"/> when the instance is not found.
    /// </summary>
    /// <returns>void</returns>
    [Fact]
    public async Task StartAsync_ShouldThrowWorkflowException_WhenInstanceNotFound()
    {
        // Act
        Func<Task> act = async () => await _executionService.StartAsync("nonExistent");

        // Assert
        await act.Should().ThrowAsync<WorkflowException>()
           .WithMessage("Instance 'nonExistent' not found");
    }

    /// <summary>
    /// Tests that <see cref="WorkflowExecutionService.GetInstance(string)"/> returns the instance when it exists.
    /// </summary>
    /// <returns>void</returns>
    [Fact]
    public void GetInstance_ShouldReturnInstance_WhenExists()
    {
        // Arrange
        var workflowId = "workflow1";
        var workflow = new Workflow { Id = workflowId, Name = "Workflow", Status = WorkflowStatus.Active };
        _definitionService.AddWorkflow(workflow);
        var instance = _executionService.CreateInstance(workflowId);

        // Act
        var result = _executionService.GetInstance(instance.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(instance.Id);
    }
}
