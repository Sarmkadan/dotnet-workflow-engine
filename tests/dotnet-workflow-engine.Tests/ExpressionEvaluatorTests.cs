// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Utilities;
using FluentAssertions;
using Xunit;

namespace DotNetWorkflowEngine.Tests;

public class ExpressionEvaluatorTests
{
    private ExecutionContext CreateContext(Dictionary<string, object?>? variables = null)
    {
        var context = new ExecutionContext
        {
            WorkflowInstanceId = "test-instance",
            Variables = variables ?? new Dictionary<string, object?>()
        };
        return context;
    }

    [Fact]
    public void Evaluate_NullOrEmptyExpression_ReturnsTrue()
    {
        var context = CreateContext();

        ExpressionEvaluator.Evaluate(null!, context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("   ", context).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_LiteralTrue_ReturnsTrue()
    {
        var context = CreateContext();

        ExpressionEvaluator.Evaluate("true", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("1", context).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_LiteralFalse_ReturnsFalse()
    {
        var context = CreateContext();

        ExpressionEvaluator.Evaluate("false", context).Should().BeFalse();
        ExpressionEvaluator.Evaluate("0", context).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_VariableReference_ReturnsVariableValue()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "isApproved", true },
            { "status", "active" }
        });

        ExpressionEvaluator.Evaluate("${isApproved}", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("${status}", context).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_VariableReferenceNullOrFalse_ReturnsFalse()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "emptyString", "" },
            { "nullValue", null },
            { "falseValue", false }
        });

        ExpressionEvaluator.Evaluate("${emptyString}", context).Should().BeFalse();
        ExpressionEvaluator.Evaluate("${nullValue}", context).Should().BeFalse();
        ExpressionEvaluator.Evaluate("${falseValue}", context).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_EqualityComparison_StringValues_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "status", "approved" }
        });

        ExpressionEvaluator.Evaluate("${status} == \"approved\"", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("${status} == \"rejected\"", context).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_InequalityComparison_StringValues_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "status", "pending" }
        });

        ExpressionEvaluator.Evaluate("${status} != \"approved\"", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("${status} != \"pending\"", context).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_NumericGreaterThan_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "amount", 100 }
        });

        ExpressionEvaluator.Evaluate("${amount} > \"50\"", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("${amount} > \"150\"", context).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_NumericLessThan_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "priority", 5 }
        });

        ExpressionEvaluator.Evaluate("${priority} < \"10\"", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("${priority} < \"3\"", context).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_NumericGreaterThanOrEqual_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "value", 100 }
        });

        ExpressionEvaluator.Evaluate("${value} >= \"100\"", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("${value} >= \"101\"", context).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_NumericLessThanOrEqual_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "cost", 50 }
        });

        ExpressionEvaluator.Evaluate("${cost} <= \"50\"", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("${cost} <= \"49\"", context).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_StringContains_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "description", "This is an important task" }
        });

        ExpressionEvaluator.Evaluate("${description} contains \"important\"", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("${description} contains \"urgent\"", context).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_LogicalAnd_AllTrue_ReturnsTrue()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "approved", true },
            { "funded", true }
        });

        ExpressionEvaluator.Evaluate("${approved} && ${funded}", context).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_LogicalAnd_OneFalse_ReturnsFalse()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "approved", true },
            { "funded", false }
        });

        ExpressionEvaluator.Evaluate("${approved} && ${funded}", context).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_LogicalAnd_MultipleConditions_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "approved", true },
            { "funded", true },
            { "reviewed", true }
        });

        ExpressionEvaluator.Evaluate("${approved} && ${funded} && ${reviewed}", context).Should().BeTrue();

        context.SetVariable("reviewed", false);
        ExpressionEvaluator.Evaluate("${approved} && ${funded} && ${reviewed}", context).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_LogicalOr_OneTrue_ReturnsTrue()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "approved", true },
            { "urgent", false }
        });

        ExpressionEvaluator.Evaluate("${approved} || ${urgent}", context).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_LogicalOr_AllFalse_ReturnsFalse()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "approved", false },
            { "urgent", false }
        });

        ExpressionEvaluator.Evaluate("${approved} || ${urgent}", context).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_LogicalOr_MultipleConditions_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "approved", false },
            { "urgent", false },
            { "escalated", true }
        });

        ExpressionEvaluator.Evaluate("${approved} || ${urgent} || ${escalated}", context).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_LogicalNot_True_ReturnsFalse()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "blocked", true }
        });

        ExpressionEvaluator.Evaluate("!${blocked}", context).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_LogicalNot_False_ReturnsTrue()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "blocked", false }
        });

        ExpressionEvaluator.Evaluate("!${blocked}", context).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_LogicalNot_Literal_EvaluatesCorrectly()
    {
        var context = CreateContext();

        ExpressionEvaluator.Evaluate("!true", context).Should().BeFalse();
        ExpressionEvaluator.Evaluate("!false", context).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_ComplexExpression_CombinedLogic_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "approved", true },
            { "amount", 500 },
            { "status", "active" }
        });

        ExpressionEvaluator.Evaluate("${approved} && ${amount} > \"100\"", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("${approved} && ${amount} > \"1000\"", context).Should().BeFalse();
        ExpressionEvaluator.Evaluate("${approved} || ${status} == \"inactive\"", context).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_WhitespaceHandling_TrimsExpression()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "flag", true }
        });

        ExpressionEvaluator.Evaluate("  ${flag}  ", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("  true  ", context).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_QuotedStrings_WithSingleAndDoubleQuotes_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "language", "csharp" }
        });

        ExpressionEvaluator.Evaluate("${language} == \"csharp\"", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("${language} == 'csharp'", context).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_NumericVariableWithNumericComparison_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "count", 42.5 }
        });

        ExpressionEvaluator.Evaluate("${count} >= \"40\"", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("${count} <= \"50\"", context).Should().BeTrue();
    }

    [Fact]
    public void ValidateExpression_ValidLiteralTrue_ReturnsTrue()
    {
        var result = ExpressionEvaluator.ValidateExpression("true", out var errors);

        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateExpression_ValidVariableReference_ReturnsTrue()
    {
        var result = ExpressionEvaluator.ValidateExpression("${status}", out var errors);

        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateExpression_ValidComparison_ReturnsTrue()
    {
        var result = ExpressionEvaluator.ValidateExpression("${status} == \"active\"", out var errors);

        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateExpression_ValidLogicalAnd_ReturnsTrue()
    {
        var result = ExpressionEvaluator.ValidateExpression("${approved} && ${funded}", out var errors);

        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateExpression_UnbalancedBraces_ReturnsFalse()
    {
        var result = ExpressionEvaluator.ValidateExpression("${status", out var errors);

        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Unbalanced braces"));
    }

    [Fact]
    public void ValidateExpression_EmptyExpression_ReturnsTrue()
    {
        var result = ExpressionEvaluator.ValidateExpression("", out var errors);

        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateExpression_InvalidWithoutOperator_ReturnsFalse()
    {
        var result = ExpressionEvaluator.ValidateExpression("randomtext", out var errors);

        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("valid operator"));
    }

    [Fact]
    public void Evaluate_StringContainsNull_ReturnsFalse()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "text", null }
        });

        ExpressionEvaluator.Evaluate("${text} contains \"word\"", context).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_NumericComparisonWithInvalidNumber_ReturnsFalse()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "value", "not-a-number" }
        });

        ExpressionEvaluator.Evaluate("${value} > \"100\"", context).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_VariableNotInContext_TreatsAsNull()
    {
        var context = CreateContext();

        ExpressionEvaluator.Evaluate("${nonexistent}", context).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_NumericStringVariable_ConvertsForComparison()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "amount", "500" }
        });

        ExpressionEvaluator.Evaluate("${amount} > \"100\"", context).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_BooleanStringVariable_ConvertsCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "active", "true" }
        });

        ExpressionEvaluator.Evaluate("${active}", context).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_ZeroAsBoolean_ReturnsFalse()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "count", 0 }
        });

        ExpressionEvaluator.Evaluate("${count}", context).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_NonZeroAsBoolean_ReturnsTrue()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "count", 42 }
        });

        ExpressionEvaluator.Evaluate("${count}", context).Should().BeTrue();
    }
}
