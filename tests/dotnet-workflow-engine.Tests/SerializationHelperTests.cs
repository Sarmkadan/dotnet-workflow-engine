// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Utilities;
using FluentAssertions;
using Xunit;

namespace DotNetWorkflowEngine.Tests;

/// <summary>
/// Provides unit tests for the <see cref="SerializationHelper"/> class.
/// Tests various serialization and deserialization methods including JSON conversion,
/// deep cloning, merging, and validation for workflow engine activities and other objects.
/// </summary>
public class SerializationHelperTests
{
    /// <summary>
    /// Creates a test activity for use in serialization tests.
    /// </summary>
    /// <param name="id">The activity identifier. Defaults to "activity-1".</param>
    /// <returns>A configured <see cref="Activity"/> instance for testing.</returns>
    private Activity CreateTestActivity(string id = "activity-1")
    {
        return new Activity
        {
            Id = id,
            Name = "Test Activity",
            TimeoutSeconds = 30,
            MaxRetries = 2
        };
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.ToJson"/> correctly serializes an object to JSON string.
    /// </summary>
    [Fact]
    public void ToJson_SerializesObjectToJson()
    {
        var activity = CreateTestActivity();

        var json = SerializationHelper.ToJson(activity);

        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("activity-1");
        json.Should().Contain("Test Activity");
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.ToJson"/> returns the string "null" when serializing a null object.
    /// </summary>
    [Fact]
    public void ToJson_WithNull_ReturnsNullJson()
    {
        var json = SerializationHelper.ToJson<Activity>(null);

        json.Should().Be("null");
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.ToJsonPretty"/> serializes an object with formatting and indentation.
    /// </summary>
    [Fact]
    public void ToJsonPretty_SerializesWithFormatting()
    {
        var activity = CreateTestActivity();

        var json = SerializationHelper.ToJsonPretty(activity);

        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\n"); // Should have line breaks for pretty printing
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.FromJson"/> correctly deserializes a JSON string back to an object.
    /// </summary>
    [Fact]
    public void FromJson_DeserializesJsonToObject()
    {
        var activity = CreateTestActivity();
        var json = SerializationHelper.ToJson(activity);

        var deserialized = SerializationHelper.FromJson<Activity>(json);

        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be("activity-1");
        deserialized.Name.Should().Be("Test Activity");
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.FromJson"/> returns null when deserializing null, empty, or whitespace strings.
    /// </summary>
    [Fact]
    public void FromJson_WithNullOrEmpty_ReturnsNull()
    {
        SerializationHelper.FromJson<Activity>(null).Should().BeNull();
        SerializationHelper.FromJson<Activity>("").Should().BeNull();
        SerializationHelper.FromJson<Activity>(" ").Should().BeNull();
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.FromJson"/> throws a SerializationException when given invalid JSON.
    /// </summary>
    [Fact]
    public void FromJson_WithInvalidJson_ThrowsSerializationException()
    {
        var act = () => SerializationHelper.FromJson<Activity>("{invalid json");

        act.Should().Throw<SerializationException>();
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.FromJsonToDict"/> correctly deserializes JSON to a dictionary.
    /// </summary>
    [Fact]
    public void FromJsonToDict_DeserializesToDictionary()
    {
        var json = "{\"key1\": \"value1\", \"key2\": \"value2\"}";

        var dict = SerializationHelper.FromJsonToDict(json);

        dict.Should().NotBeNull();
        dict!["key1"].Should().Be("value1");
        dict["key2"].Should().Be("value2");
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.FromJsonToDict"/> returns null when deserializing null, empty, or whitespace strings.
    /// </summary>
    [Fact]
    public void FromJsonToDict_WithNullOrEmpty_ReturnsNull()
    {
        SerializationHelper.FromJsonToDict(null).Should().BeNull();
        SerializationHelper.FromJsonToDict("").Should().BeNull();
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.FromJsonToDict"/> throws a SerializationException when given invalid JSON.
    /// </summary>
    [Fact]
    public void FromJsonToDict_WithInvalidJson_ThrowsSerializationException()
    {
        var act = () => SerializationHelper.FromJsonToDict("{invalid");

        act.Should().Throw<SerializationException>();
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.TryFromJson"/> successfully deserializes valid JSON to an object.
    /// </summary>
    [Fact]
    public void TryFromJson_WithValidJson_ReturnsObject()
    {
        var activity = CreateTestActivity();
        var json = SerializationHelper.ToJson(activity);

        var result = SerializationHelper.TryFromJson<Activity>(json);

        result.Should().NotBeNull();
        result!.Id.Should().Be("activity-1");
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.TryFromJson"/> returns null when given invalid JSON.
    /// </summary>
    [Fact]
    public void TryFromJson_WithInvalidJson_ReturnsNull()
    {
        var result = SerializationHelper.TryFromJson<Activity>("{invalid json}");

        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.TryFromJson"/> returns null when given null or empty strings.
    /// </summary>
    [Fact]
    public void TryFromJson_WithNullOrEmpty_ReturnsNull()
    {
        SerializationHelper.TryFromJson<Activity>(null).Should().BeNull();
        SerializationHelper.TryFromJson<Activity>("").Should().BeNull();
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.DeepClone"/> creates a complete independent clone of an object.
    /// </summary>
    [Fact]
    public void DeepClone_CreatesCompleteClone()
    {
        var activity = CreateTestActivity();
        activity.Metadata["key"] = "value";

        var clone = SerializationHelper.DeepClone(activity);

        clone.Should().NotBeNull();
        clone!.Id.Should().Be(activity.Id);
        clone.Name.Should().Be(activity.Name);
        clone.Metadata["key"].Should().Be("value");
        // Verify it's a true clone, not a reference
        clone.Should().NotBeSameAs(activity);
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.DeepClone"/> returns null when given a null object.
    /// </summary>
    [Fact]
    public void DeepClone_WithNull_ReturnsNull()
    {
        var result = SerializationHelper.DeepClone<Activity>(null);

        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that modifying a clone does not affect the original object.
    /// </summary>
    [Fact]
    public void DeepClone_ModifyingClone_DoesNotAffectOriginal()
    {
        var activity = CreateTestActivity();
        var clone = SerializationHelper.DeepClone(activity);

        clone!.Name = "Modified";
        clone.TimeoutSeconds = 999;

        activity.Name.Should().Be("Test Activity");
        activity.TimeoutSeconds.Should().Be(30);
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.Merge"/> combines two objects with the second object's values taking precedence.
    /// </summary>
    [Fact]
    public void Merge_CombinesTwoObjects()
    {
        var activity1 = new Activity { Id = "act-1", Name = "Original Name", TimeoutSeconds = 30 };
        var activity2 = new Activity { Id = "act-2", Name = "New Name", MaxRetries = 5 };

        var merged = SerializationHelper.Merge(activity1, activity2);

        merged.Should().NotBeNull();
        merged!.Name.Should().Be("New Name"); // activity2 overrides
        merged.MaxRetries.Should().Be(5);
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.Merge"/> returns the second object when the first is null.
    /// </summary>
    [Fact]
    public void Merge_WithFirstNull_ReturnsSecond()
    {
        var activity2 = CreateTestActivity("act-2");

        var result = SerializationHelper.Merge<Activity>(null, activity2);

        result.Should().NotBeNull();
        result!.Id.Should().Be("act-2");
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.Merge"/> returns the first object when the second is null.
    /// </summary>
    [Fact]
    public void Merge_WithSecondNull_ReturnsFirst()
    {
        var activity1 = CreateTestActivity("act-1");

        var result = SerializationHelper.Merge(activity1, null);

        result.Should().NotBeNull();
        result!.Id.Should().Be("act-1");
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.Merge"/> returns null when both objects are null.
    /// </summary>
    [Fact]
    public void Merge_WithBothNull_ReturnsNull()
    {
        var result = SerializationHelper.Merge<Activity>(null, null);

        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.FromJsonElement"/> correctly deserializes a JsonElement to an object.
    /// </summary>
    [Fact]
    public void FromJsonElement_DeserializesJsonElement()
    {
        var activity = CreateTestActivity();
        var json = SerializationHelper.ToJson(activity);
        using (var doc = System.Text.Json.JsonDocument.Parse(json))
        {
            var element = doc.RootElement;

            var deserialized = SerializationHelper.FromJsonElement<Activity>(element);

            deserialized.Should().NotBeNull();
            deserialized!.Id.Should().Be("activity-1");
        }
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.ToJsonElement"/> converts an object to a JsonElement.
    /// </summary>
    [Fact]
    public void ToJsonElement_ConvertsObjectToJsonElement()
    {
        var activity = CreateTestActivity();

        var element = SerializationHelper.ToJsonElement(activity);

        element.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Object);
        element.GetProperty("id").GetString().Should().Be("activity-1");
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.IsValidJson"/> returns true for valid JSON strings.
    /// </summary>
    [Fact]
    public void IsValidJson_WithValidJson_ReturnsTrue()
    {
        var validJson = SerializationHelper.ToJson(CreateTestActivity());

        var result = SerializationHelper.IsValidJson(validJson);

        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.IsValidJson"/> returns false for invalid JSON strings.
    /// </summary>
    [Fact]
    public void IsValidJson_WithInvalidJson_ReturnsFalse()
    {
        var result = SerializationHelper.IsValidJson("{invalid json}");

        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.IsValidJson"/> returns false for null, empty, or whitespace strings.
    /// </summary>
    [Fact]
    public void IsValidJson_WithNullOrEmpty_ReturnsFalse()
    {
        SerializationHelper.IsValidJson(null).Should().BeFalse();
        SerializationHelper.IsValidJson("").Should().BeFalse();
        SerializationHelper.IsValidJson(" ").Should().BeFalse();
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.PrettyPrintJson"/> formats JSON with proper indentation.
    /// </summary>
    [Fact]
    public void PrettyPrintJson_FormatsJsonWithIndentation()
    {
        var compactJson = SerializationHelper.ToJson(CreateTestActivity());
        var originalLineCount = compactJson.Split('\n').Length;

        var prettyJson = SerializationHelper.PrettyPrintJson(compactJson);
        var prettyLineCount = prettyJson.Split('\n').Length;

        prettyLineCount.Should().BeGreaterThan(originalLineCount);
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.PrettyPrintJson"/> returns the original string when given invalid JSON.
    /// </summary>
    [Fact]
    public void PrettyPrintJson_WithInvalidJson_ReturnsOriginal()
    {
        var invalidJson = "{invalid";

        var result = SerializationHelper.PrettyPrintJson(invalidJson);

        result.Should().Be(invalidJson);
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.PrettyPrintJson"/> returns the input for null or empty strings.
    /// </summary>
    [Fact]
    public void PrettyPrintJson_WithNullOrEmpty_ReturnsInput()
    {
        SerializationHelper.PrettyPrintJson(null).Should().BeNull();
        SerializationHelper.PrettyPrintJson("").Should().Be("");
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.MinifyJson"/> removes all whitespace from JSON.
    /// </summary>
    [Fact]
    public void MinifyJson_RemovesWhitespace()
    {
        var prettyJson = SerializationHelper.ToJsonPretty(CreateTestActivity());

        var minified = SerializationHelper.MinifyJson(prettyJson);

        minified.Should().NotContain("\n");
        minified.Should().NotContain(" ");
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.MinifyJson"/> returns the original string when given invalid JSON.
    /// </summary>
    [Fact]
    public void MinifyJson_WithInvalidJson_ReturnsOriginal()
    {
        var invalidJson = "{invalid";

        var result = SerializationHelper.MinifyJson(invalidJson);

        result.Should().Be(invalidJson);
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.MinifyJson"/> returns the input for null or empty strings.
    /// </summary>
    [Fact]
    public void MinifyJson_WithNullOrEmpty_ReturnsInput()
    {
        SerializationHelper.MinifyJson(null).Should().BeNull();
        SerializationHelper.MinifyJson("").Should().Be("");
    }

    /// <summary>
    /// Tests that serialization and deserialization preserves all data through a round trip.
    /// </summary>
    [Fact]
    public void SerializationRoundTrip_PreservesData()
    {
        var original = new Activity
        {
            Id = "test-activity",
            Name = "Test Activity",
            Description = "A test activity",
            TimeoutSeconds = 60,
            MaxRetries = 3,
            Type = "Task"
        };
        original.InputParameters["key"] = "value";

        var json = SerializationHelper.ToJson(original);
        var deserialized = SerializationHelper.FromJson<Activity>(json);

        deserialized.Should().BeEquivalentTo(original);
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.ToJson"/> uses camelCase naming for property names.
    /// </summary>
    [Fact]
    public void CamelCaseNaming_PropertyNameConversion()
    {
        var activity = CreateTestActivity();

        var json = SerializationHelper.ToJson(activity);

        json.Should().Contain("timeoutSeconds");
        json.Should().Contain("maxRetries");
        json.Should().NotContain("TimeoutSeconds");
    }

    /// <summary>
    /// Tests that <see cref="SerializationHelper.ToJson"/> ignores null property values during serialization.
    /// </summary>
    [Fact]
    public void NullPropertyHandling_IgnoresNullValues()
    {
        var activity = new Activity { Id = "act-1", Name = "Activity", Description = null };

        var json = SerializationHelper.ToJson(activity);

        json.Should().NotContain("\"description\"");
    }
}