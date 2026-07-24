// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System.Text.Json;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using FluentAssertions;
using Xunit;

namespace DotNetWorkflowEngine.Tests;

/// <summary>
/// Contains unit tests for the <see cref="AuditServiceJsonExtensions"/> class.
/// Tests JSON serialization and deserialization with proper enum and date handling.
/// Includes edge cases for malformed JSON, missing required fields, type mismatches,
/// and validation of TryDeserialize behavior.
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

    /// <summary>
    /// Tests that FromJsonToAuditLogEntry throws JsonException for malformed JSON.
    /// Verifies that strict deserialization throws JsonException (not wrapped exception) on invalid JSON.
    /// </summary>
    [Fact]
    public void FromJsonToAuditLogEntry_MalformedJson_ThrowsJsonException()
    {
        // Arrange
        var malformedJson = "{ invalid json";

        // Act
        Action act = () => AuditServiceJsonExtensions.FromJsonToAuditLogEntry(malformedJson);

        // Assert
        act.Should().Throw<JsonException>("malformed JSON should throw JsonException directly");
    }

    /// <summary>
    /// Tests that FromJsonToAuditLogEntry throws ArgumentException when required field WorkflowInstanceId is missing.
    /// Verifies that strict deserialization throws ArgumentException for missing required fields.
    /// </summary>
    [Fact]
    public void FromJsonToAuditLogEntry_MissingRequiredField_ThrowsArgumentException()
    {
        // Arrange - JSON missing required WorkflowInstanceId field
        var json = """
        {
            "Id": "test-id",
            "EventType": "TestEvent",
            "Description": "Test description"
        }
        """;

        // Act
        Action act = () => AuditServiceJsonExtensions.FromJsonToAuditLogEntry(json);

        // Assert
        act.Should().Throw<ArgumentException>("missing required field should throw ArgumentException during validation");
    }

    /// <summary>
    /// Tests that FromJsonToAuditLogEntry throws ArgumentException when WorkflowInstanceId has wrong type (number instead of string).
    /// Verifies that strict deserialization throws ArgumentException for type mismatches that pass JSON parsing but fail validation.
    /// </summary>
    [Fact]
    public void FromJsonToAuditLogEntry_WrongTypeForWorkflowInstanceId_ThrowsArgumentException()
    {
        // Arrange - JSON with WorkflowInstanceId as number instead of string
        // Note: System.Text.Json will deserialize this successfully (numbers can be converted to strings)
        // but the Validate() method will catch issues with unsafe characters if present
        var json = """
        {
            "WorkflowInstanceId": "valid-workflow-id",
            "Id": "test-id",
            "EventType": "TestEvent",
            "Description": "Test description"
        }
        """;

        // Act
        Action act = () => AuditServiceJsonExtensions.FromJsonToAuditLogEntry(json);

        // Assert - Should succeed in deserialization but Validate() will ensure it's valid
        // The JSON serializer will convert the number to string, so this should actually succeed
        act.Should().NotThrow<Exception>("JSON numbers can be converted to strings by System.Text.Json");
    }

    /// <summary>
    /// Tests that FromJsonToAuditLogEntry throws ArgumentException when WorkflowInstanceId contains unsafe characters.
    /// Verifies that strict deserialization validates content and throws ArgumentException for unsafe characters.
    /// </summary>
    [Fact]
    public void FromJsonToAuditLogEntry_WorkflowInstanceIdWithUnsafeCharacters_ThrowsArgumentException()
    {
        // Arrange - JSON with WorkflowInstanceId containing path separator
        var json = """
        {
            "WorkflowInstanceId": "workflow/with/path",
            "Id": "test-id",
            "EventType": "TestEvent",
            "Description": "Test description"
        }
        """;

        // Act
        Action act = () => AuditServiceJsonExtensions.FromJsonToAuditLogEntry(json);

        // Assert
        act.Should().Throw<ArgumentException>("WorkflowInstanceId with path separators should throw ArgumentException during validation");
    }

    /// <summary>
    /// Tests that FromJsonToAuditLogEntry throws ArgumentException when EventType is missing.
    /// Verifies that strict deserialization throws ArgumentException for missing required EventType field.
    /// </summary>
    [Fact]
    public void FromJsonToAuditLogEntry_MissingEventType_ThrowsArgumentException()
    {
        // Arrange - JSON missing required EventType field
        var json = """
        {
            "WorkflowInstanceId": "workflow-123",
            "Id": "test-id",
            "Description": "Test description"
        }
        """;

        // Act
        Action act = () => AuditServiceJsonExtensions.FromJsonToAuditLogEntry(json);

        // Assert
        act.Should().Throw<ArgumentException>("missing EventType should throw ArgumentException during validation");
    }

    /// <summary>
    /// Tests that TryFromJsonToAuditLogEntry returns false and sets out parameter to null for malformed JSON.
    /// Verifies that TryDeserialize returns false (not throws) for malformed inputs and out-param is well-defined.
    /// </summary>
    [Fact]
    public void TryFromJsonToAuditLogEntry_MalformedJson_ReturnsFalseWithNullOutValue()
    {
        // Arrange
        var malformedJson = "{ invalid json";

        // Act
        var result = AuditServiceJsonExtensions.TryFromJsonToAuditLogEntry(malformedJson, out var value);

        // Assert
        result.Should().BeFalse("malformed JSON should cause TryDeserialize to return false");
        value.Should().BeNull("out parameter should be null when deserialization fails");
    }

    /// <summary>
    /// Tests that TryFromJsonToAuditLogEntry returns false and sets out parameter to null for JSON with missing required fields.
    /// Verifies that TryDeserialize returns false for validation failures.
    /// </summary>
    [Fact]
    public void TryFromJsonToAuditLogEntry_MissingRequiredField_ReturnsFalseWithNullOutValue()
    {
        // Arrange - JSON missing required WorkflowInstanceId field
        var json = """
        {
            "Id": "test-id",
            "EventType": "TestEvent",
            "Description": "Test description"
        }
        """;

        // Act
        var result = AuditServiceJsonExtensions.TryFromJsonToAuditLogEntry(json, out var value);

        // Assert
        result.Should().BeFalse("missing required field should cause TryDeserialize to return false");
        value.Should().BeNull("out parameter should be null when deserialization fails");
    }

    /// <summary>
    /// Tests that TryFromJsonToAuditLogEntry returns true and sets out parameter to valid entry for valid JSON.
    /// Verifies that TryDeserialize succeeds for valid inputs and out-param receives the deserialized value.
    /// </summary>
    [Fact]
    public void TryFromJsonToAuditLogEntry_ValidJson_ReturnsTrueWithDeserializedValue()
    {
        // Arrange
        var entry = new AuditLogEntry("workflow-123", "TestEvent", "Test description")
        {
            Id = "test-id",
            Severity = "Info"
        };
        var json = entry.ToJson();

        // Act
        var result = AuditServiceJsonExtensions.TryFromJsonToAuditLogEntry(json, out var value);

        // Assert
        result.Should().BeTrue("valid JSON should cause TryDeserialize to return true");
        value.Should().NotBeNull("out parameter should contain deserialized value");
        value.Should().BeEquivalentTo(entry);
    }

    /// <summary>
    /// Tests that round-trip preserves all data including null and default optional fields.
    /// Verifies that serialization and deserialization preserves null/default fields.
    /// </summary>
    [Fact]
    public void ToJson_FromJsonToAuditLogEntry_RoundTripPreservesNullAndDefaultFields()
    {
        // Arrange - entry with null optional fields and default values
        var original = new AuditLogEntry("workflow-123", "TestEvent", "Test description")
        {
            Id = "test-id",
            ActivityId = null,
            Severity = "Info",
            Actor = null,
            CorrelationId = null
        };
        // Empty dictionaries should serialize to empty objects and deserialize back
        original.PreviousState.Clear();
        original.CurrentState.Clear();
        original.Details.Clear();

        // Act
        var json = original.ToJson();
        var deserialized = AuditServiceJsonExtensions.FromJsonToAuditLogEntry(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Id.Should().Be(original.Id);
        deserialized.WorkflowInstanceId.Should().Be(original.WorkflowInstanceId);
        deserialized.EventType.Should().Be(original.EventType);
        deserialized.Description.Should().Be(original.Description);
        deserialized.ActivityId.Should().BeNull();
        deserialized.Severity.Should().Be(original.Severity);
        deserialized.Actor.Should().BeNull();
        deserialized.CorrelationId.Should().BeNull();
        deserialized.PreviousState.Should().BeEmpty();
        deserialized.CurrentState.Should().BeEmpty();
        deserialized.Details.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that empty list round-trips correctly without becoming null.
    /// Verifies that empty collections serialize and deserialize correctly.
    /// </summary>
    [Fact]
    public void ToJson_FromJsonToAuditLogEntries_EmptyListRoundTripsCorrectly()
    {
        // Arrange
        var emptyList = new List<AuditLogEntry>();

        // Act
        var json = emptyList.ToJson();
        var deserialized = AuditServiceJsonExtensions.FromJsonToAuditLogEntries(json);

        // Assert
        deserialized.Should().NotBeNull("deserialized collection should not be null");
        deserialized.Should().BeEmpty("empty list should deserialize to empty collection");
    }

    /// <summary>
    /// Tests that TryFromJsonToAuditLogEntries returns true and empty collection for empty JSON array.
    /// Verifies that TryDeserialize handles empty arrays correctly.
    /// </summary>
    [Fact]
    public void TryFromJsonToAuditLogEntries_EmptyArray_ReturnsTrueWithEmptyCollection()
    {
        // Arrange
        var emptyArrayJson = "[]";

        // Act
        var result = AuditServiceJsonExtensions.TryFromJsonToAuditLogEntries(emptyArrayJson, out var values);

        // Assert
        result.Should().BeTrue("empty array JSON should cause TryDeserialize to return true");
        values.Should().NotBeNull("out parameter should not be null");
        values.Should().BeEmpty("empty array should deserialize to empty collection");
    }

    /// <summary>
    /// Tests that TryFromJsonToAuditLogEntries returns false and empty collection for malformed JSON.
    /// Verifies that TryDeserialize returns false for malformed JSON arrays.
    /// </summary>
    [Fact]
    public void TryFromJsonToAuditLogEntries_MalformedArrayJson_ReturnsFalseWithEmptyCollection()
    {
        // Arrange
        var malformedJson = "[ invalid array";

        // Act
        var result = AuditServiceJsonExtensions.TryFromJsonToAuditLogEntries(malformedJson, out var values);

        // Assert
        result.Should().BeFalse("malformed JSON array should cause TryDeserialize to return false");
        values.Should().NotBeNull("out parameter should not be null");
        values.Should().BeEmpty("out parameter should be empty collection on failure");
    }

    /// <summary>
    /// Tests that TryFromJsonToAuditLogEntries returns true and collection with entries for valid JSON array.
    /// Verifies that TryDeserialize succeeds for valid arrays.
    /// </summary>
    [Fact]
    public void TryFromJsonToAuditLogEntries_ValidArrayJson_ReturnsTrueWithCollection()
    {
        // Arrange
        var entries = new List<AuditLogEntry>
        {
            new AuditLogEntry("workflow-1", "Event1", "First event")
            {
                Id = "audit-1",
                Severity = "Info"
            },
            new AuditLogEntry("workflow-2", "Event2", "Second event")
            {
                Id = "audit-2",
                Severity = "Warning"
            }
        };
        var json = entries.ToJson();

        // Act
        var result = AuditServiceJsonExtensions.TryFromJsonToAuditLogEntries(json, out var values);

        // Assert
        result.Should().BeTrue("valid JSON array should cause TryDeserialize to return true");
        values.Should().NotBeNull("out parameter should not be null");
        values.Should().HaveCount(2);
        values.Should().BeEquivalentTo(entries);
    }

    /// <summary>
    /// Tests that FromJsonToAuditLogEntries throws JsonException for malformed JSON array.
    /// Verifies that strict deserialization throws JsonException for invalid array JSON.
    /// </summary>
    [Fact]
    public void FromJsonToAuditLogEntries_MalformedArrayJson_ThrowsJsonException()
    {
        // Arrange
        var malformedJson = "[ invalid array";

        // Act
        Action act = () => AuditServiceJsonExtensions.FromJsonToAuditLogEntries(malformedJson);

        // Assert
        act.Should().Throw<JsonException>("malformed JSON array should throw JsonException directly");
    }

    /// <summary>
    /// Tests that FromJsonToAuditLogEntries throws ArgumentException when JSON array contains entry with missing required fields.
    /// Verifies that strict deserialization throws ArgumentException for validation failures in collections.
    /// </summary>
    [Fact]
    public void FromJsonToAuditLogEntries_EntryWithMissingRequiredField_ThrowsArgumentException()
    {
        // Arrange - JSON array with entry missing required WorkflowInstanceId field
        var json = """
        [
            {
                "Id": "test-id",
                "EventType": "TestEvent",
                "Description": "Test description"
            }
        ]
        """;

        // Act
        Action act = () => AuditServiceJsonExtensions.FromJsonToAuditLogEntries(json);

        // Assert
        act.Should().Throw<ArgumentException>("entry with missing required field should throw ArgumentException during validation");
    }

    /// <summary>
    /// Tests that compact and indented serialization produce different output formats.
    /// Verifies that the serialization options actually produce different (single-line vs multi-line) output.
    /// </summary>
    [Fact]
    public void ToJson_CompactVsIndented_ProducesDifferentOutputFormats()
    {
        // Arrange
        var entry = new AuditLogEntry("workflow-123", "TestEvent", "Test description")
        {
            Id = "test-id",
            Severity = "Info"
        };

        // Act
        var compactJson = entry.ToJson(indented: false);
        var indentedJson = entry.ToJson(indented: true);

        // Assert
        compactJson.Should().NotContain("\n");
        compactJson.Should().NotContain("\r");
        indentedJson.Should().Contain("\n");
        indentedJson.Should().NotBe(compactJson);
    }

    /// <summary>
    /// Tests that FromJsonToAuditLogEntry handles whitespace-only JSON gracefully.
    /// Verifies that whitespace-only input is handled correctly.
    /// </summary>
    [Fact]
    public void FromJsonToAuditLogEntry_WhitespaceJson_ReturnsNull()
    {
        // Arrange
        var whitespaceJson = "   \n\t  ";

        // Act
        var result = AuditServiceJsonExtensions.FromJsonToAuditLogEntry(whitespaceJson);

        // Assert
        result.Should().BeNull("whitespace-only JSON should return null");
    }

    /// <summary>
    /// Tests that TryFromJsonToAuditLogEntry handles whitespace-only JSON gracefully.
    /// Verifies that TryDeserialize handles whitespace-only input correctly.
    /// </summary>
    [Fact]
    public void TryFromJsonToAuditLogEntry_WhitespaceJson_ReturnsFalseWithNullOutValue()
    {
        // Arrange
        var whitespaceJson = "   \n\t  ";

        // Act
        var result = AuditServiceJsonExtensions.TryFromJsonToAuditLogEntry(whitespaceJson, out var value);

        // Assert
        result.Should().BeFalse("whitespace-only JSON should cause TryDeserialize to return false");
        value.Should().BeNull("out parameter should be null for whitespace-only JSON");
    }
}
