// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Utilities;
using FluentAssertions;
using Xunit;

namespace DotNetWorkflowEngine.Tests;

public class StringExtensionsTests
{
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
