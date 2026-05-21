// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Utilities;
using FluentAssertions;
using ExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

namespace DotNetWorkflowEngine.Tests;

public class ExpressionEvaluatorTests
{
    private static ExecutionContext CreateContext(Dictionary<string, object>? variables = null)
    {
        var ctx = new ExecutionContext();
        if (variables != null)
        {
            foreach (var kvp in variables)
                ctx.SetVariable(kvp.Key, kvp.Value);
        }
        return ctx;
    }

    [Fact]
    public void Evaluate_NullExpression_ReturnsTrue()
    {
        ExpressionEvaluator.Evaluate(null!, CreateContext()).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_EmptyExpression_ReturnsTrue()
    {
        ExpressionEvaluator.Evaluate("", CreateContext()).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_WhitespaceExpression_ReturnsTrue()
    {
        ExpressionEvaluator.Evaluate("   ", CreateContext()).Should().BeTrue();
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public void Evaluate_LiteralValues_ReturnsExpected(string expression, bool expected)
    {
        ExpressionEvaluator.Evaluate(expression, CreateContext()).Should().Be(expected);
    }

    [Fact]
    public void Evaluate_VariableReference_ReturnsTruthyValue()
    {
        var ctx = CreateContext(new() { { "isActive", true } });
        ExpressionEvaluator.Evaluate("${isActive}", ctx).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_EqualityComparison_ReturnsCorrectResult()
    {
        var ctx = CreateContext(new() { { "status", "active" } });
        ExpressionEvaluator.Evaluate("${status} == active", ctx).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_InequalityComparison_ReturnsCorrectResult()
    {
        var ctx = CreateContext(new() { { "status", "active" } });
        ExpressionEvaluator.Evaluate("${status} != inactive", ctx).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_NumericGreaterThan_ReturnsCorrectResult()
    {
        var ctx = CreateContext(new() { { "count", 10 } });
        ExpressionEvaluator.Evaluate("${count} > 5", ctx).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_NumericLessThan_ReturnsCorrectResult()
    {
        var ctx = CreateContext(new() { { "count", 3 } });
        ExpressionEvaluator.Evaluate("${count} < 5", ctx).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_LogicalAnd_BothTrue_ReturnsTrue()
    {
        var ctx = CreateContext(new() { { "a", true }, { "b", true } });
        ExpressionEvaluator.Evaluate("${a} && ${b}", ctx).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_LogicalAnd_OneFalse_ReturnsFalse()
    {
        var ctx = CreateContext(new() { { "a", true }, { "b", false } });
        ExpressionEvaluator.Evaluate("${a} && ${b}", ctx).Should().BeFalse();
    }
}
