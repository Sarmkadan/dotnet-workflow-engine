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

/// <summary>
/// Contains unit tests for the <see cref="AuditService"/> class.
/// Tests the audit logging functionality including instance creation, activity tracking,
/// and audit log retrieval with various filtering options.
/// </summary>
public class AuditServiceTests
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
	/// Initializes a new instance of the <see cref="AuditServiceTests"/> class.
	/// Sets up mock repository and creates the service instance for testing.
	/// </summary>
	public AuditServiceTests()
	{
		_mockAuditRepository = new Mock<IAuditRepository>();
		_auditService = new AuditService(_mockAuditRepository.Object);
	}

	/// <summary>
	/// Creates a collection of test audit log entries for use in multiple test cases.
	/// </summary>
	/// <returns>List of <see cref="AuditLogEntry"/> objects with various event types and severities.</returns>
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

	/// <summary>
	/// Tests that LogInstanceCreated calls the repository's AddAsync method with correct parameters.
	/// Verifies the audit service properly logs workflow instance creation events.
	/// </summary>
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

	/// <summary>
	/// Tests GetFilteredAuditLogsAsync with no filters returns all audit logs.
	/// Verifies the service retrieves all available audit entries when no filtering criteria are specified.
	/// </summary>
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

	/// <summary>
	/// Tests GetFilteredAuditLogsAsync with instance ID filter returns only matching logs.
	/// Verifies filtering by workflow instance ID works correctly.
	/// </summary>
	/// <param name="instanceId">The workflow instance ID to filter by.</param>
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

	/// <summary>
	/// Tests GetFilteredAuditLogsAsync with activity ID filter returns only matching logs.
	/// Verifies filtering by activity ID works correctly.
	/// </summary>
	/// <param name="activityId">The activity ID to filter by.</param>
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

	/// <summary>
	/// Tests GetFilteredAuditLogsAsync with event type filter returns only matching logs.
	/// Verifies filtering by event type works correctly.
	/// </summary>
	/// <param name="eventType">The event type to filter by.</param>
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

	/// <summary>
	/// Tests GetFilteredAuditLogsAsync with severity filter returns only matching logs.
	/// Verifies filtering by severity level works correctly.
	/// </summary>
	/// <param name="severity">The severity level to filter by.</param>
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

	/// <summary>
	/// Tests GetFilteredAuditLogsAsync with date range filter returns only logs within the specified range.
	/// Verifies date-based filtering works correctly.
	/// </summary>
	/// <param name="fromDate">The start date of the range.</param>
	/// <param name="toDate">The end date of the range.</param>
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

	/// <summary>
	/// Tests GetFilteredAuditLogsAsync with actor filter returns only logs created by the specified actor.
	/// Verifies filtering by actor/user works correctly.
	/// </summary>
	/// <param name="actor">The actor/user name to filter by.</param>
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

	/// <summary>
	/// Tests GetFilteredAuditLogsAsync with pagination parameters returns only the requested page of logs.
	/// Verifies pagination works correctly with skip and take parameters.
	/// </summary>
	/// <param name="skip">Number of items to skip.</param>
	/// <param name="take">Number of items to return.</param>
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

	/// <summary>
	/// Tests ExportAuditLogAsCsv generates CSV output in the correct format.
	/// Verifies the CSV export functionality produces properly formatted CSV data.
	/// </summary>
	/// <param name="instanceId">The workflow instance ID to export logs for.</param>
	/// <returns>CSV formatted string containing audit log data.</returns>
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