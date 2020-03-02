// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Data.Repositories;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using FluentAssertions;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetWorkflowEngine.Tests;

public class AuditServiceTests
{
    private readonly Mock<IAuditRepository> _mockAuditRepository;
    private readonly AuditService _auditService;

    public AuditServiceTests()
    {
        _mockAuditRepository = new Mock<IAuditRepository>();
        _auditService = new AuditService(_mockAuditRepository.Object);
    }

    private static List<AuditLogEntry> GetTestAuditLogs()
    {
        return new List<AuditLogEntry>
        {
            new AuditLogEntry("instance1", "InstanceCreated", "Workflow instance created")
            {
                Id = "audit1",
                WorkflowInstanceId = "workflowA_instance1",
                ActivityId = null,
                Severity = "Info",
                Timestamp = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                Actor = "userA"
            },
            new AuditLogEntry("instance1", "ActivityCompleted", "Activity 'activity1' completed")
            {
                Id = "audit2",
                WorkflowInstanceId = "workflowA_instance1",
                ActivityId = "activity1",
                Severity = "Info",
                Timestamp = new DateTime(2023, 1, 1, 10, 1, 0, DateTimeKind.Utc),
                Actor = "userA"
            },
            new AuditLogEntry("instance2", "InstanceCreated", "Workflow instance created")
            {
                Id = "audit3",
                WorkflowInstanceId = "workflowB_instance2",
                ActivityId = null,
                Severity = "Info",
                Timestamp = new DateTime(2023, 1, 2, 11, 0, 0, DateTimeKind.Utc),
                Actor = "userB"
            },
            new AuditLogEntry("instance2", "ActivityFailed", "Activity 'activity2' failed")
            {
                Id = "audit4",
                WorkflowInstanceId = "workflowB_instance2",
                ActivityId = "activity2",
                Severity = "Error",
                Timestamp = new DateTime(2023, 1, 2, 11, 5, 0, DateTimeKind.Utc),
                Actor = "userB"
            },
            new AuditLogEntry("instance1", "ActivityFailed", "Activity 'activity3' failed")
            {
                Id = "audit5",
                WorkflowInstanceId = "workflowA_instance1",
                ActivityId = "activity3",
                Severity = "Error",
                Timestamp = new DateTime(2023, 1, 1, 10, 10, 0, DateTimeKind.Utc),
                Actor = "userA"
            }
        };
    }

    [Fact]
    public async Task LogInstanceCreated_CallsRepositoryAddAsync()
    {
        // Arrange
        var instanceId = "testInstance";
        var createdBy = "testUser";
        _mockAuditRepository.Setup(r => r.AddAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);

        // Act
        await _auditService.LogInstanceCreated(instanceId, createdBy);

        // Assert
        _mockAuditRepository.Verify(
            r => r.AddAsync(It.Is<AuditLogEntry>(e =>
                e.WorkflowInstanceId == instanceId &&
                e.EventType == "InstanceCreated" &&
                e.Description == "Workflow instance created" &&
                e.Severity == "Info" &&
                e.Actor == createdBy)), Times.Once);
    }

    [Fact]
    public async Task GetFilteredAuditLogsAsync_NoFilters_ReturnsAllLogs()
    {
        // Arrange
        var testLogs = GetTestAuditLogs();
        _mockAuditRepository.Setup(r => r.GetFilteredAndPagedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((testLogs, testLogs.Count));

        // Act
        var (logs, total) = await _auditService.GetFilteredAuditLogsAsync();

        // Assert
        logs.Should().BeEquivalentTo(testLogs);
        total.Should().Be(testLogs.Count);
    }

    [Fact]
    public async Task GetFilteredAuditLogsAsync_FilterByInstanceId_ReturnsFilteredLogs()
    {
        // Arrange
        var testLogs = GetTestAuditLogs();
        var expectedLogs = testLogs.Where(e => e.WorkflowInstanceId.Contains("instance1")).ToList();
        _mockAuditRepository.Setup(r => r.GetFilteredAndPagedAsync(
                It.IsAny<string>(), "instance1", It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((expectedLogs, expectedLogs.Count));

        // Act
        var (logs, total) = await _auditService.GetFilteredAuditLogsAsync(instanceId: "instance1");

        // Assert
        logs.Should().BeEquivalentTo(expectedLogs);
        total.Should().Be(expectedLogs.Count);
    }

    [Fact]
    public async Task GetFilteredAuditLogsAsync_FilterByActivityId_ReturnsFilteredLogs()
    {
        // Arrange
        var testLogs = GetTestAuditLogs();
        var expectedLogs = testLogs.Where(e => e.ActivityId == "activity1").ToList();
        _mockAuditRepository.Setup(r => r.GetFilteredAndPagedAsync(
                It.IsAny<string>(), It.IsAny<string>(), "activity1", It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((expectedLogs, expectedLogs.Count));

        // Act
        var (logs, total) = await _auditService.GetFilteredAuditLogsAsync(activityId: "activity1");

        // Assert
        logs.Should().BeEquivalentTo(expectedLogs);
        total.Should().Be(expectedLogs.Count);
    }

    [Fact]
    public async Task GetFilteredAuditLogsAsync_FilterByEventType_ReturnsFilteredLogs()
    {
        // Arrange
        var testLogs = GetTestAuditLogs();
        var expectedLogs = testLogs.Where(e => e.EventType == "ActivityFailed").ToList();
        _mockAuditRepository.Setup(r => r.GetFilteredAndPagedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "ActivityFailed",
                It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((expectedLogs, expectedLogs.Count));

        // Act
        var (logs, total) = await _auditService.GetFilteredAuditLogsAsync(eventType: "ActivityFailed");

        // Assert
        logs.Should().BeEquivalentTo(expectedLogs);
        total.Should().Be(expectedLogs.Count);
    }

    [Fact]
    public async Task GetFilteredAuditLogsAsync_FilterBySeverity_ReturnsFilteredLogs()
    {
        // Arrange
        var testLogs = GetTestAuditLogs();
        var expectedLogs = testLogs.Where(e => e.Severity == "Error").ToList();
        _mockAuditRepository.Setup(r => r.GetFilteredAndPagedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                "Error", It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((expectedLogs, expectedLogs.Count));

        // Act
        var (logs, total) = await _auditService.GetFilteredAuditLogsAsync(severity: "Error");

        // Assert
        logs.Should().BeEquivalentTo(expectedLogs);
        total.Should().Be(expectedLogs.Count);
    }

    [Fact]
    public async Task GetFilteredAuditLogsAsync_FilterByDateRange_ReturnsFilteredLogs()
    {
        // Arrange
        var testLogs = GetTestAuditLogs();
        var fromDate = new DateTime(2023, 1, 1, 10, 3, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2023, 1, 1, 10, 15, 0, DateTimeKind.Utc);
        var expectedLogs = testLogs.Where(e => e.Timestamp >= fromDate && e.Timestamp <= toDate).ToList();
        _mockAuditRepository.Setup(r => r.GetFilteredAndPagedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), fromDate, toDate, It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((expectedLogs, expectedLogs.Count));

        // Act
        var (logs, total) = await _auditService.GetFilteredAuditLogsAsync(fromDate: fromDate, toDate: toDate);

        // Assert
        logs.Should().BeEquivalentTo(expectedLogs);
        total.Should().Be(expectedLogs.Count);
    }

    [Fact]
    public async Task GetFilteredAuditLogsAsync_FilterByActor_ReturnsFilteredLogs()
    {
        // Arrange
        var testLogs = GetTestAuditLogs();
        var expectedLogs = testLogs.Where(e => e.Actor == "userB").ToList();
        _mockAuditRepository.Setup(r => r.GetFilteredAndPagedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), "userB",
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((expectedLogs, expectedLogs.Count));

        // Act
        var (logs, total) = await _auditService.GetFilteredAuditLogsAsync(actor: "userB");

        // Assert
        logs.Should().BeEquivalentTo(expectedLogs);
        total.Should().Be(expectedLogs.Count);
    }

    [Fact]
    public async Task GetFilteredAuditLogsAsync_WithPagination_ReturnsPagedLogs()
    {
        // Arrange
        var testLogs = GetTestAuditLogs().OrderByDescending(e => e.Timestamp).ToList(); // Ensure consistent order
        var skip = 1;
        var take = 2;
        var expectedLogs = testLogs.Skip(skip).Take(take).ToList();
        _mockAuditRepository.Setup(r => r.GetFilteredAndPagedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(),
                skip, take))
            .ReturnsAsync((expectedLogs, testLogs.Count));

        // Act
        var (logs, total) = await _auditService.GetFilteredAuditLogsAsync(skip: skip, take: take);

        // Assert
        logs.Should().BeEquivalentTo(expectedLogs);
        total.Should().Be(testLogs.Count);
    }

    [Fact]
    public async Task ExportAuditLogAsCsv_ReturnsCorrectCsvFormat()
    {
        // Arrange
        var instanceId = "workflowA_instance1";
        var testLogs = GetTestAuditLogs().Where(e => e.WorkflowInstanceId == instanceId)
                                         .OrderBy(e => e.Timestamp) // CSV export orders by timestamp ascending
                                         .ToList();
        _mockAuditRepository.Setup(r => r.GetByInstanceIdAsync(instanceId))
            .ReturnsAsync(testLogs);

        var expectedCsv = new System.Text.StringBuilder();
        expectedCsv.AppendLine("Timestamp,EventType,WorkflowInstanceId,ActivityId,Severity,Description,Actor,CorrelationId");
        foreach (var entry in testLogs)
        {
            expectedCsv.AppendLine($"\"{entry.GetFormattedTimestamp()}\",\"{entry.EventType}\",\"{entry.WorkflowInstanceId}\",\"{entry.ActivityId}\",\"{entry.Severity}\",\"{entry.Description.Replace("\"", "\"\"")}\",\"{entry.Actor}\",\"{entry.CorrelationId}\"");
        }

        // Act
        var csv = await _auditService.ExportAuditLogAsCsv(instanceId);

        // Assert
        csv.Should().Be(expectedCsv.ToString());
    }

    // Add more tests for other logging methods (LogInstanceStarted, LogActivityFailed, etc.)
}
