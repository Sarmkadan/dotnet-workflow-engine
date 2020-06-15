using DotNetWorkflowEngine.Utilities;
using FluentAssertions;
using Xunit;

namespace DotNetWorkflowEngine.Tests;

/// <summary>
/// Tests for string extension methods in DotNetWorkflowEngine.Utilities.
/// </summary>
public class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <c>ToPascalCase</c> correctly converts a kebab-case string to PascalCase.
    /// </summary>
    [Fact]
    public void ToPascalCase_KebabInput_ConvertsCorrectly()
    {
        // Arrange
        const string input = "hello-world";

        // Act
        var result = input.ToPascalCase();

        // Assert
        result.Should().Be("HelloWorld");
    }

    /// <summary>
    /// Verifies that <c>ToSnakeCase</c> correctly inserts underscores and lowercases a camelCase string.
    /// </summary>
    [Fact]
    public void ToSnakeCase_CamelCaseInput_InsertsUnderscoresAndLowers()
    {
        // Arrange
        const string input = "HelloWorld";

        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be("hello_world");
    }

    /// <summary>
    /// Verifies that <c>Truncate</c> truncates a long string and appends the specified suffix.
    /// </summary>
    [Fact]
    public void Truncate_LongStringWithSuffix_TruncatesAndAppendsSuffix()
    {
        // Arrange
        const string input = "Hello World";

        // Act
        var result = input.Truncate(7, "...");

        // Assert
        result.Should().Be("Hell...");
    }

    /// <summary>
    /// Verifies that <c>SmartSplit</c> does not split delimiters that appear inside quoted sections.
    /// </summary>
    [Fact]
    public void SmartSplit_QuotedSection_DoesNotSplitDelimiterInsideQuotes()
    {
        // Arrange
        const string input = "a,\"b,c\",d";

        // Act
        var parts = input.SmartSplit(",").ToList();

        // Assert
        parts.Should().HaveCount(3);
        parts[0].Should().Be("a");
        parts[1].Should().Be("\"b,c\"");
        parts[2].Should().Be("d");
    }
}
