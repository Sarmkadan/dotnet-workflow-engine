// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using FluentAssertions;
using Xunit;

namespace DotNetWorkflowEngine.Tests;

/// <summary>
/// Contains unit tests for the <see cref="AuditServiceJsonExtensions"/> class.
/// Tests JSON serialization and deserialization with proper enum and date handling.
/// </summary>
public class AuditServiceJsonExtensionsTests
{
    /// <summary>
    /// Tests that ToJson and FromJsonToAuditLogEntry perform a round-trip successfully.
    /// Verifies that the JSON serialization preserves all data including DateTime and string properties.
    /// </summary>
    [Fact]
    public void ToJson_FromJsonToAuditLogEntry_RoundTripPreservesData()
    {
        // Arrange
        var original = new AuditLogEntry("workflow-123", "ActivityCompleted", "Activity 'process-data' completed successfully")
        {
            Id = "audit-456",
            ActivityId = "activity-789",
            Severity = "Info",
            Timestamp = new DateTime(2024, 6, 15, 14, 30, 45, DateTimeKind.Utc),
            Actor = "test-user",
            CorrelationId = "corr-123"
        };

        original.PreviousState["oldValue"] = "previous";
        original.CurrentState["newValue"] = "current";
        original.Details["durationMs"] = 1500;

        // Act
        var json = original.ToJson();
        var deserialized = AuditServiceJsonExtensions.FromJsonToAuditLogEntry(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Id.Should().Be(original.Id);
        deserialized.WorkflowInstanceId.Should().Be(original.WorkflowInstanceId);
        deserialized.EventType.Should().Be(original.EventType);
        deserialized.Description.Should().Be(original.Description);
        deserialized.Timestamp.Should().Be(original.Timestamp);
        deserialized.Severity.Should().Be(original.Severity);
        deserialized.Actor.Should().Be(original.Actor);
        deserialized.CorrelationId.Should().Be(original.CorrelationId);
        deserialized.ActivityId.Should().Be(original.ActivityId);
        deserialized.PreviousState.Should().HaveCount(original.PreviousState.Count);
        deserialized.CurrentState.Should().HaveCount(original.CurrentState.Count);
        deserialized.Details.Should().HaveCount(original.Details.Count);
    }

    /// <summary>
    /// Tests that ToJson with indented parameter produces properly formatted JSON.
    /// Verifies that the indented option works correctly.
    /// </summary>
    [Fact]
    public void ToJson_Indented_ProducesFormattedJson()
    {
        // Arrange
        var entry = new AuditLogEntry("workflow-123", "TestEvent", "Test description");

        // Act
        var compactJson = entry.ToJson(indented: false);
        var indentedJson = entry.ToJson(indented: true);

        // Assert
        compactJson.Should().NotContain("\n");
        indentedJson.Should().Contain("\n");
        indentedJson.Should().Contain("  "); // Indentation
    }

    /// <summary>
    /// Tests that ToJson and FromJsonToAuditLogEntries perform a round-trip for collections.
    /// Verifies that the JSON serialization preserves all data for collections.
    /// </summary>
    [Fact]
    public void ToJson_FromJsonToAuditLogEntries_RoundTripPreservesCollectionData()
    {
        // Arrange
        var entries = new List<AuditLogEntry>
        {
            new AuditLogEntry("workflow-1", "Event1", "First event")
            {
                Id = "audit-1",
                Timestamp = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                Severity = "Info"
            },
            new AuditLogEntry("workflow-2", "Event2", "Second event")
            {
                Id = "audit-2",
                Timestamp = new DateTime(2024, 1, 2, 11, 0, 0, DateTimeKind.Utc),
                Severity = "Warning"
            }
        };

        // Act
        var json = entries.ToJson();
        var deserialized = AuditServiceJsonExtensions.FromJsonToAuditLogEntries(json);

        // Assert
        deserialized.Should().HaveCount(2);
        deserialized[0].Should().BeEquivalentTo(entries[0]);
        deserialized[1].Should().BeEquivalentTo(entries[1]);
    }

    /// <summary>
    /// Tests that TryFromJsonToAuditLogEntry handles invalid JSON gracefully.
    /// Verifies that the Try method returns false for invalid JSON.
    /// </summary>
    [Fact]
    public void TryFromJsonToAuditLogEntry_InvalidJson_ReturnsFalse()
    {
        // Arrange
        var invalidJson = "{ invalid json";

        // Act
        var result = AuditServiceJsonExtensions.TryFromJsonToAuditLogEntry(invalidJson, out var value);

        // Assert
        result.Should().BeFalse();
        value.Should().BeNull();
    }

    /// <summary>
    /// Tests that TryFromJsonToAuditLogEntry handles empty JSON gracefully.
    /// Verifies that the Try method returns false for empty JSON.
    /// </summary>
    [Fact]
    public void TryFromJsonToAuditLogEntry_EmptyJson_ReturnsFalse()
    {
        // Arrange
        var emptyJson = "";

        // Act
        var result = AuditServiceJsonExtensions.TryFromJsonToAuditLogEntry(emptyJson, out var value);

        // Assert
        result.Should().BeFalse();
        value.Should().BeNull();
    }

    /// <summary>
    /// Tests that TryFromJsonToAuditLogEntries handles invalid JSON gracefully.
    /// Verifies that the Try method returns false for invalid JSON.
    /// </summary>
    [Fact]
    public void TryFromJsonToAuditLogEntries_InvalidJson_ReturnsFalse()
    {
        // Arrange
        var invalidJson = "{ invalid json";

        // Act
        var result = AuditServiceJsonExtensions.TryFromJsonToAuditLogEntries(invalidJson, out var values);

        // Assert
        result.Should().BeFalse();
        values.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that TryFromJsonToAuditLogEntries handles empty array JSON gracefully.
    /// Verifies that the Try method returns true for empty array JSON.
    /// </summary>
    [Fact]
    public void TryFromJsonToAuditLogEntries_EmptyArrayJson_ReturnsTrueWithEmptyCollection()
    {
        // Arrange
        var emptyArrayJson = "[]";

        // Act
        var result = AuditServiceJsonExtensions.TryFromJsonToAuditLogEntries(emptyArrayJson, out var values);

        // Assert
        result.Should().BeTrue();
        values.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that FromJsonToAuditLogEntry handles null input.
    /// Verifies that the method throws ArgumentNullException for null input.
    /// </summary>
    [Fact]
    public void FromJsonToAuditLogEntry_NullJson_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => AuditServiceJsonExtensions.FromJsonToAuditLogEntry(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that ToJson handles null input.
    /// Verifies that the method throws ArgumentNullException for null input.
    /// </summary>
    [Fact]
    public void ToJson_NullEntry_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => ((AuditLogEntry)null!).ToJson();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that ToJson handles null collection input.
    /// Verifies that the method throws ArgumentNullException for null collection.
    /// </summary>
    [Fact]
    public void ToJson_NullCollection_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => ((List<AuditLogEntry>)null!).ToJson();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
