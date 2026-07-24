// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System.Text;
using System.Text.Json;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using FluentAssertions;
using Xunit;

namespace DotNetWorkflowEngine.Tests;

/// <summary>
/// Contains security tests for the AuditServiceJsonExtensions class.
/// Tests protection against JSON-based denial-of-service attacks and deep nesting.
/// </summary>
public class AuditServiceSecurityTests
{
    /// <summary>
    /// Tests that FromJsonToAuditLogEntry rejects JSON payloads exceeding the maximum size limit.
    /// Verifies protection against memory exhaustion attacks through large payloads.
    /// </summary>
    [Fact]
    public void FromJsonToAuditLogEntry_LargeJsonPayload_ThrowsArgumentException()
    {
        // Arrange - create a JSON payload larger than the default limit (1 MB)
        var largePayload = new StringBuilder();
        largePayload.Append('{');
        largePayload.Append('"');
        largePayload.Append('I');
        largePayload.Append('d');
        largePayload.Append('"');
        largePayload.Append(':');
        largePayload.Append('"');

        // Add enough data to exceed 1 MB
        largePayload.Append('x', 1024 * 1024 + 100); // 1 MB + 100 bytes
        largePayload.Append('"');
        largePayload.Append('}');

        var json = largePayload.ToString();

        // Act & Assert - should throw ArgumentException for size violation
        Action act = () => AuditServiceJsonExtensions.FromJsonToAuditLogEntry(json);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*exceeds maximum allowed size*");
    }

    /// <summary>
    /// Tests that TryFromJsonToAuditLogEntry gracefully handles large JSON payloads.
    /// Verifies that the try method doesn't throw exceptions for size violations.
    /// </summary>
    [Fact]
    public void TryFromJsonToAuditLogEntry_LargeJsonPayload_ReturnsFalse()
    {
        // Arrange - create a JSON payload larger than the default limit (1 MB)
        var largePayload = new StringBuilder();
        largePayload.Append('{');
        largePayload.Append('"');
        largePayload.Append('I');
        largePayload.Append('d');
        largePayload.Append('"');
        largePayload.Append(':');
        largePayload.Append('"');

        // Add enough data to exceed 1 MB
        largePayload.Append('x', 1024 * 1024 + 100); // 1 MB + 100 bytes
        largePayload.Append('"');
        largePayload.Append('}');

        var json = largePayload.ToString();

        // Act
        var result = AuditServiceJsonExtensions.TryFromJsonToAuditLogEntry(json, out var value);

        // Assert
        result.Should().BeFalse();
        value.Should().BeNull();
    }

    /// <summary>
    /// Tests that FromJsonToAuditLogEntries rejects JSON payloads exceeding the maximum size limit.
    /// Verifies protection against memory exhaustion attacks for collection deserialization.
    /// </summary>
    [Fact]
    public void FromJsonToAuditLogEntries_LargeJsonPayload_ThrowsArgumentException()
    {
        // Arrange - create a JSON array payload larger than the default limit (1 MB)
        var largePayload = new StringBuilder();
        largePayload.Append('[');
        largePayload.Append('{');
        largePayload.Append('"');
        largePayload.Append('I');
        largePayload.Append('d');
        largePayload.Append('"');
        largePayload.Append(':');
        largePayload.Append('"');

        // Add enough data to exceed 1 MB
        largePayload.Append('x', 1024 * 1024 + 100); // 1 MB + 100 bytes
        largePayload.Append('"');
        largePayload.Append('}');
        largePayload.Append(']');

        var json = largePayload.ToString();

        // Act & Assert - should throw ArgumentException for size violation
        Action act = () => AuditServiceJsonExtensions.FromJsonToAuditLogEntries(json);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*exceeds maximum allowed size*");
    }

    /// <summary>
    /// Tests that TryFromJsonToAuditLogEntries gracefully handles large JSON payloads.
    /// Verifies that the try method doesn't throw exceptions for size violations.
    /// </summary>
    [Fact]
    public void TryFromJsonToAuditLogEntries_LargeJsonPayload_ReturnsFalse()
    {
        // Arrange - create a JSON array payload larger than the default limit (1 MB)
        var largePayload = new StringBuilder();
        largePayload.Append('[');
        largePayload.Append('{');
        largePayload.Append('"');
        largePayload.Append('I');
        largePayload.Append('d');
        largePayload.Append('"');
        largePayload.Append(':');
        largePayload.Append('"');

        // Add enough data to exceed 1 MB
        largePayload.Append('x', 1024 * 1024 + 100); // 1 MB + 100 bytes
        largePayload.Append('"');
        largePayload.Append('}');
        largePayload.Append(']');

        var json = largePayload.ToString();

        // Act
        var result = AuditServiceJsonExtensions.TryFromJsonToAuditLogEntries(json, out var values);

        // Assert
        result.Should().BeFalse();
        values.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that deeply nested JSON (10,000 levels) is rejected by MaxDepth configuration.
    /// Verifies protection against stack overflow attacks through deep nesting.
    /// </summary>
    [Fact]
    public void FromJsonToAuditLogEntry_DeeplyNestedJson_ThrowsJsonException()
    {
        // Arrange - create JSON with 10,000 levels of nesting
        var deeplyNestedJson = CreateDeeplyNestedJson(10000);

        // Act & Assert - should throw JsonException due to exceeding MaxDepth
        Action act = () => AuditServiceJsonExtensions.FromJsonToAuditLogEntry(deeplyNestedJson);

        act.Should().Throw<JsonException>();
    }

    /// <summary>
    /// Tests that TryFromJsonToAuditLogEntry gracefully handles deeply nested JSON.
    /// Verifies that the try method doesn't throw exceptions for deep nesting.
    /// </summary>
    [Fact]
    public void TryFromJsonToAuditLogEntry_DeeplyNestedJson_ReturnsFalse()
    {
        // Arrange - create JSON with 10,000 levels of nesting
        var deeplyNestedJson = CreateDeeplyNestedJson(10000);

        // Act
        var result = AuditServiceJsonExtensions.TryFromJsonToAuditLogEntry(deeplyNestedJson, out var value);

        // Assert
        result.Should().BeFalse();
        value.Should().BeNull();
    }

    /// <summary>
    /// Tests that deeply nested JSON arrays are rejected by MaxDepth configuration.
    /// Verifies protection against stack overflow attacks for collection deserialization.
    /// </summary>
    [Fact]
    public void FromJsonToAuditLogEntries_DeeplyNestedJsonArray_ThrowsJsonException()
    {
        // Arrange - create JSON array with 10,000 levels of nesting
        var deeplyNestedJson = CreateDeeplyNestedArrayJson(10000);

        // Act & Assert - should throw JsonException due to exceeding MaxDepth
        Action act = () => AuditServiceJsonExtensions.FromJsonToAuditLogEntries(deeplyNestedJson);

        act.Should().Throw<JsonException>();
    }

    /// <summary>
    /// Tests that TryFromJsonToAuditLogEntries gracefully handles deeply nested JSON arrays.
    /// Verifies that the try method doesn't throw exceptions for deep nesting.
    /// </summary>
    [Fact]
    public void TryFromJsonToAuditLogEntries_DeeplyNestedJsonArray_ReturnsFalse()
    {
        // Arrange - create JSON array with 10,000 levels of nesting
        var deeplyNestedJson = CreateDeeplyNestedArrayJson(10000);

        // Act
        var result = AuditServiceJsonExtensions.TryFromJsonToAuditLogEntries(deeplyNestedJson, out var values);

        // Assert
        result.Should().BeFalse();
        values.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that MaxJsonInputSizeBytes can be configured to allow larger payloads.
    /// Verifies that the configuration is adjustable for legitimate large payloads.
    /// </summary>
    [Fact]
    public void MaxJsonInputSizeBytes_CanBeConfigured()
    {
        // Arrange
        var originalLimit = AuditServiceJsonExtensions.MaxJsonInputSizeBytes;
        var largePayload = new StringBuilder();
        largePayload.Append('{');
        largePayload.Append('"');
        largePayload.Append('I');
        largePayload.Append('d');
        largePayload.Append('"');
        largePayload.Append(':');
        largePayload.Append('"');
        largePayload.Append('x', 2 * 1024 * 1024); // 2 MB
        largePayload.Append('"');
        largePayload.Append('}');
        var json = largePayload.ToString();

        try
        {
            // Act - increase the limit to allow 2 MB (2097152 bytes = 2 * 1024 * 1024)
            AuditServiceJsonExtensions.MaxJsonInputSizeBytes = 2 * 1024 * 1024;

            // Should now succeed - create a payload that's exactly 2 MB of data (not including JSON structure)
            var payloadBuilder = new StringBuilder();
            payloadBuilder.Append('{');
            payloadBuilder.Append('"');
            payloadBuilder.Append('I');
            payloadBuilder.Append('d');
            payloadBuilder.Append('"');
            payloadBuilder.Append(':');
            payloadBuilder.Append('"');
            payloadBuilder.Append('x', 2 * 1024 * 1024 - 20); // 2 MB - structure overhead
            payloadBuilder.Append('"');
            payloadBuilder.Append('}');
            var jsonForConfigTest = payloadBuilder.ToString();

            Action act = () => AuditServiceJsonExtensions.FromJsonToAuditLogEntry(jsonForConfigTest);
            act.Should().NotThrow();
        }
        finally
        {
            // Restore original limit
            AuditServiceJsonExtensions.MaxJsonInputSizeBytes = originalLimit;
        }
    }

    /// <summary>
    /// Tests that the MaxDepth is properly set in the JsonSerializerOptions.
    /// Verifies that the security configuration is correctly applied.
    /// </summary>
    [Fact]
    public void JsonSerializerOptions_HasMaxDepthSet()
    {
        // Act
        var compactOptions = typeof(AuditServiceJsonExtensions)
            .GetField("_jsonOptionsCompact", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .GetValue(null) as JsonSerializerOptions;

        var indentedOptions = typeof(AuditServiceJsonExtensions)
            .GetField("_jsonOptionsIndented", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .GetValue(null) as JsonSerializerOptions;

        // Assert
        compactOptions.Should().NotBeNull();
        indentedOptions.Should().NotBeNull();
        compactOptions.MaxDepth.Should().Be(128);
        indentedOptions.MaxDepth.Should().Be(128);
    }

    /// <summary>
    /// Creates JSON with specified nesting depth: {"key":{"key":{"key":...}}}
    /// </summary>
    private static string CreateDeeplyNestedJson(int depth)
    {
        if (depth <= 0)
            return "{\"key\":\"value\"}";

        var sb = new StringBuilder();
        sb.Append('{');
        sb.Append('"');
        sb.Append('k');
        sb.Append('e');
        sb.Append('y');
        sb.Append('"');
        sb.Append(':');
        sb.Append(CreateDeeplyNestedJson(depth - 1));
        sb.Append('}');
        return sb.ToString();
    }

    /// <summary>
    /// Creates JSON array with specified nesting depth: [[[...["value"]...]]]
    /// </summary>
    private static string CreateDeeplyNestedArrayJson(int depth)
    {
        if (depth <= 0)
            return "[\"value\"]";

        var sb = new StringBuilder();
        sb.Append('[');
        sb.Append(CreateDeeplyNestedArrayJson(depth - 1));
        sb.Append(']');
        return sb.ToString();
    }
}