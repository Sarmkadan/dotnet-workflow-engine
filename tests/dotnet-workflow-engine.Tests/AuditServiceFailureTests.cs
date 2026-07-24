// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using DotNetWorkflowEngine.Data.Repositories;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using FluentAssertions;
using Moq;
using Xunit;
using System;
using System.Threading.Tasks;

namespace DotNetWorkflowEngine.Tests;

/// <summary>
/// Tests for audit service failure isolation and non-blocking behavior.
/// </summary>
public class AuditServiceFailureTests : IDisposable
{
    /// <summary>
    /// Mock repository for testing audit operations without actual database dependencies.
    /// </summary>
    private readonly Mock<IAuditRepository> _mockAuditRepository;

    /// <summary>
    /// Instance of the service being tested with mocked dependencies.
    /// </summary>
    private readonly AuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditServiceFailureTests"/> class.
    /// Sets up mock repository and creates the service instance for testing.
    /// </summary>
    public AuditServiceFailureTests()
    {
        _mockAuditRepository = new Mock<IAuditRepository>();
        _auditService = new AuditService(_mockAuditRepository.Object);
    }

    /// <summary>
    /// Disposes the audit service to clean up background resources.
    /// </summary>
    public void Dispose()
    {
        _auditService.DisposeAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Tests that workflow execution continues even when audit repository throws an exception.
    /// This verifies that audit failures are isolated and don't block workflow progress.
    /// </summary>
    [Fact]
    public async Task LogInstanceCreated_RepositoryThrows_WorkflowContinues()
    {
        // Arrange
        var instanceId = "testInstance";
        var createdBy = "testUser";

        // Setup repository to throw an exception
        _mockAuditRepository.Setup(r => r.AddAsync(It.IsAny<AuditLogEntry>()))
            .ThrowsAsync(new InvalidOperationException("Repository is unavailable"));

        // Act - should not throw even though repository fails
        var act = () => _auditService.LogInstanceCreated(instanceId, createdBy);

        // Assert - the call should complete successfully without throwing
        await act.Should().NotThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Tests that activity logging continues even when audit repository throws an exception.
    /// </summary>
    [Fact]
    public async Task LogActivityCompleted_RepositoryThrows_WorkflowContinues()
    {
        // Arrange
        var instanceId = "testInstance";
        var activityId = "testActivity";
        var result = new ActivityResult
        {
            ExecutionDurationMs = 100,
            AttemptNumber = 1,
            Output = new System.Collections.Generic.Dictionary<string, object?>
            {
                ["key1"] = "value1"
            }
        };

        // Setup repository to throw an exception
        _mockAuditRepository.Setup(r => r.AddAsync(It.IsAny<AuditLogEntry>()))
            .ThrowsAsync(new InvalidOperationException("Repository is unavailable"));

        // Act - should not throw even though repository fails
        var act = () => _auditService.LogActivityCompleted(instanceId, activityId, result);

        // Assert - the call should complete successfully without throwing
        await act.Should().NotThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Tests that multiple audit calls succeed even when repository consistently fails.
    /// This verifies the buffered pipeline works correctly under failure conditions.
    /// </summary>
    [Fact]
    public async Task MultipleAuditCalls_RepositoryAlwaysFails_AllCallsSucceed()
    {
        // Arrange - setup repository to always throw
        _mockAuditRepository.Setup(r => r.AddAsync(It.IsAny<AuditLogEntry>()))
            .ThrowsAsync(new InvalidOperationException("Repository is unavailable"));

        // Act & Assert - all calls should succeed
        await _auditService.LogInstanceCreated("instance1", "user1");
        await _auditService.LogInstanceStarted("instance1");
        await _auditService.LogInstanceCompleted("instance1");
        await _auditService.LogInstanceFailed("instance2", "Something went wrong");
        await _auditService.LogActivityCompleted("instance1", "activity1", new ActivityResult
        {
            ExecutionDurationMs = 50,
            AttemptNumber = 1,
            Output = new System.Collections.Generic.Dictionary<string, object?>()
        });

        // Verify all calls completed without throwing
        _mockAuditRepository.Verify(r => r.AddAsync(It.IsAny<AuditLogEntry>()), Times.Exactly(5));
    }

    /// <summary>
    /// Tests that GetDroppedEntryCount returns zero when no overflow occurs.
    /// </summary>
    [Fact]
    public void GetDroppedEntryCount_NoOverflow_ReturnsZero()
    {
        // Arrange & Act
        var droppedCount = _auditService.GetDroppedEntryCount();

        // Assert
        droppedCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that the audit service can be disposed without errors.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_NoErrors()
    {
        // Arrange
        var service = new AuditService(_mockAuditRepository.Object);

        // Act & Assert - should not throw
        await service.DisposeAsync();
    }
}
