// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Utilities;
using FluentAssertions;
using Xunit;
using WorkflowExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

namespace DotNetWorkflowEngine.Tests;

/// <summary>
/// Contains unit tests for the <see cref="ExpressionEvaluator"/> class, verifying expression evaluation logic.
/// </summary>
public class ExpressionEvaluatorTests
{
    private WorkflowExecutionContext CreateContext(Dictionary<string, object?>? variables = null)
    {
        var context = new WorkflowExecutionContext
        {
            WorkflowInstanceId = "test-instance",
            Variables = variables ?? new Dictionary<string, object?>()
        };
        return context;
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> returns true for null, empty, or whitespace-only expressions.
    /// </summary>
    [Fact]
    public void Evaluate_NullOrEmptyExpression_ReturnsTrue()
    {
        var context = CreateContext();

        ExpressionEvaluator.Evaluate(null!, context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("   ", context).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> returns true for the literal "true" or numeric "1".
    /// </summary>
    [Fact]
    public void Evaluate_LiteralTrue_ReturnsTrue()
    {
        var context = CreateContext();

        ExpressionEvaluator.Evaluate("true", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("1", context).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> returns false for the literal "false" or numeric "0".
    /// </summary>
    [Fact]
    public void Evaluate_LiteralFalse_ReturnsFalse()
    {
        var context = CreateContext();

        ExpressionEvaluator.Evaluate("false", context).Should().BeFalse();
        ExpressionEvaluator.Evaluate("0", context).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> resolves variable references correctly to their values.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> returns false for variable references that are null, empty, or false.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> correctly evaluates equality comparisons for string values.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> correctly evaluates inequality comparisons for string values.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> correctly evaluates numeric greater-than comparisons.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> correctly evaluates numeric less-than comparisons.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> correctly evaluates numeric greater-than-or-equal comparisons.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> correctly evaluates numeric less-than-or-equal comparisons.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> correctly evaluates string "contains" operations.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> returns true for a logical AND operation when all conditions are true.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> returns false for a logical AND operation when one condition is false.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> correctly evaluates logical AND operations with multiple conditions.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> returns true for a logical OR operation when one condition is true.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> returns false for a logical OR operation when all conditions are false.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> correctly evaluates logical OR operations with multiple conditions.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> returns false for a logical NOT operation on a true variable.
    /// </summary>
    [Fact]
    public void Evaluate_LogicalNot_True_ReturnsFalse()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "blocked", true }
        });

        ExpressionEvaluator.Evaluate("!${blocked}", context).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> returns true for a logical NOT operation on a false variable.
    /// </summary>
    [Fact]
    public void Evaluate_LogicalNot_False_ReturnsTrue()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "blocked", false }
        });

        ExpressionEvaluator.Evaluate("!${blocked}", context).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> correctly evaluates logical NOT operations on literals.
    /// </summary>
    [Fact]
    public void Evaluate_LogicalNot_Literal_EvaluatesCorrectly()
    {
        var context = CreateContext();

        ExpressionEvaluator.Evaluate("!true", context).Should().BeFalse();
        ExpressionEvaluator.Evaluate("!false", context).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> correctly evaluates complex, combined logical expressions.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> correctly trims whitespace from expressions.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> handles both single and double quotes correctly.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> correctly handles numeric comparisons with numeric variables.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.ValidateExpression"/> returns true for a valid "true" literal.
    /// </summary>
    [Fact]
    public void ValidateExpression_ValidLiteralTrue_ReturnsTrue()
    {
        var result = ExpressionEvaluator.ValidateExpression("true", out var errors);

        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.ValidateExpression"/> returns true for a valid variable reference.
    /// </summary>
    [Fact]
    public void ValidateExpression_ValidVariableReference_ReturnsTrue()
    {
        var result = ExpressionEvaluator.ValidateExpression("${status}", out var errors);

        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.ValidateExpression"/> returns true for a valid comparison expression.
    /// </summary>
    [Fact]
    public void ValidateExpression_ValidComparison_ReturnsTrue()
    {
        var result = ExpressionEvaluator.ValidateExpression("${status} == \"active\"", out var errors);

        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.ValidateExpression"/> returns true for a valid logical AND expression.
    /// </summary>
    [Fact]
    public void ValidateExpression_ValidLogicalAnd_ReturnsTrue()
    {
        var result = ExpressionEvaluator.ValidateExpression("${approved} && ${funded}", out var errors);

        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.ValidateExpression"/> returns false for an expression with unbalanced braces.
    /// </summary>
    [Fact]
    public void ValidateExpression_UnbalancedBraces_ReturnsFalse()
    {
        var result = ExpressionEvaluator.ValidateExpression("${status", out var errors);

        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Unbalanced braces"));
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.ValidateExpression"/> returns true for an empty expression.
    /// </summary>
    [Fact]
    public void ValidateExpression_EmptyExpression_ReturnsTrue()
    {
        var result = ExpressionEvaluator.ValidateExpression("", out var errors);

        result.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.ValidateExpression"/> returns false for an invalid expression lacking a valid operator.
    /// </summary>
    [Fact]
    public void ValidateExpression_InvalidWithoutOperator_ReturnsFalse()
    {
        var result = ExpressionEvaluator.ValidateExpression("randomtext", out var errors);

        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("valid operator"));
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> returns false when performing a "contains" operation on a null string.
    /// </summary>
    [Fact]
    public void Evaluate_StringContainsNull_ReturnsFalse()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "text", null }
        });

        ExpressionEvaluator.Evaluate("${text} contains \"word\"", context).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> returns false when a numeric comparison fails due to an invalid number.
    /// </summary>
    [Fact]
    public void Evaluate_NumericComparisonWithInvalidNumber_ReturnsFalse()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "value", "not-a-number" }
        });

        ExpressionEvaluator.Evaluate("${value} > \"100\"", context).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> treats nonexistent variables as null, which evaluates to false.
    /// </summary>
    [Fact]
    public void Evaluate_VariableNotInContext_TreatsAsNull()
    {
        var context = CreateContext();

        ExpressionEvaluator.Evaluate("${nonexistent}", context).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> correctly converts string-encoded numeric variables for comparison.
    /// </summary>
    [Fact]
    public void Evaluate_NumericStringVariable_ConvertsForComparison()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "amount", "500" }
        });

        ExpressionEvaluator.Evaluate("${amount} > \"100\"", context).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> correctly interprets string-encoded boolean values.
    /// </summary>
    [Fact]
    public void Evaluate_BooleanStringVariable_ConvertsCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "active", "true" }
        });

        ExpressionEvaluator.Evaluate("${active}", context).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> treats the numeric value 0 as false.
    /// </summary>
    [Fact]
    public void Evaluate_ZeroAsBoolean_ReturnsFalse()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "count", 0 }
        });

        ExpressionEvaluator.Evaluate("${count}", context).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> treats non-zero numeric values as true.
    /// </summary>
    [Fact]
    public void Evaluate_NonZeroAsBoolean_ReturnsTrue()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "count", 42 }
        });

        ExpressionEvaluator.Evaluate("${count}", context).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_LenFunction_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "name", "hello" }
        });

        ExpressionEvaluator.Evaluate("len(${name})", context).Should().BeTrue();
        context.GetVariable("_len_result").Should().Be(5);
    }

    [Fact]
    public void Evaluate_UpperFunction_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "name", "hello" }
        });

        ExpressionEvaluator.Evaluate("upper(${name})", context).Should().BeTrue();
        context.GetVariable("_upper_result").Should().Be("HELLO");
    }

    [Fact]
    public void Evaluate_LowerFunction_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "name", "HELLO" }
        });

        ExpressionEvaluator.Evaluate("lower(${name})", context).Should().BeTrue();
        context.GetVariable("_lower_result").Should().Be("hello");
    }

    [Fact]
    public void Evaluate_NowFunction_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>());

        ExpressionEvaluator.Evaluate("now()", context).Should().BeTrue();
        context.GetVariable("_now_result").Should().NotBeNull();
        ((DateTime)context.GetVariable("_now_result")!).Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Evaluate_UnknownFunction_ReturnsFalse()
    {
        var context = CreateContext(new Dictionary<string, object?>());

        ExpressionEvaluator.Evaluate("unknown()", context).Should().BeFalse();
    }


    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> correctly evaluates len() function with comparison.
    /// </summary>
    [Fact]
    public void Evaluate_LenFunction_WithComparison_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "name", "hello" },
            { "short", "hi" }
        });

        // Use len() result in comparison
        ExpressionEvaluator.Evaluate("len(${name}) > 3", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("len(${name}) == 5", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("len(${short}) < 5", context).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> correctly evaluates coalesce() function.
    /// </summary>
    [Fact]
    public void Evaluate_CoalesceFunction_ReturnsFirstNonNull_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "a", "value1" },
            { "b", "value2" },
            { "nullValue", null }
        });

        // coalesce() should return true and store first non-null value
        ExpressionEvaluator.Evaluate("coalesce(${a}, ${b})", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("coalesce(${nullValue}, ${b})", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("coalesce(${nullValue}, ${nullValue})", context).Should().BeFalse();

        // Verify the result is stored
        context.GetVariable("_coalesce_result").Should().Be("value1");
        context.GetVariable("_coalesce_result").Should().Be("value2");
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> correctly evaluates coalesce() function with boolean values.
    /// </summary>
    [Fact]
    public void Evaluate_CoalesceFunction_BooleanValues_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "flag1", true },
            { "flag2", false },
            { "nullFlag", null }
        });

        // coalesce should return true for first non-null value
        ExpressionEvaluator.Evaluate("coalesce(${flag1}, ${flag2})", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("coalesce(${nullFlag}, ${flag2})", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("coalesce(${nullFlag}, ${nullFlag})", context).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> correctly evaluates contains() function.
    /// </summary>
    [Fact]
    public void Evaluate_ContainsFunction_StringSearch_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "text", "hello world" },
            { "empty", "" }
        });

        // contains() should return true/false based on search
        ExpressionEvaluator.Evaluate("contains(${text}, \"hello\")", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("contains(${text}, \"world\")", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("contains(${text}, \"foo\")", context).Should().BeFalse();
        ExpressionEvaluator.Evaluate("contains(${empty}, \"test\")", context).Should().BeFalse();

        // Verify the result is stored
        context.GetVariable("_contains_result").Should().Be(true);
        context.GetVariable("_contains_result").Should().Be(false);
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> correctly evaluates contains() function with variable.
    /// </summary>
    [Fact]
    public void Evaluate_ContainsFunction_WithVariable_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "text", "hello world" },
            { "search", "hello" }
        });

        ExpressionEvaluator.Evaluate("contains(${text}, ${search})", context).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionEvaluator.Evaluate"/> correctly evaluates complex expressions with functions.
    /// </summary>
    [Fact]
    public void Evaluate_ComplexExpression_WithFunctions_EvaluatesCorrectly()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            { "name", "john" },
            { "age", 25 },
            { "status", "active" },
            { "description", "This is a test description" }
        });

        // Complex expressions combining functions and comparisons
        ExpressionEvaluator.Evaluate("len(${name}) == 4 && ${age} > 20", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("contains(${description}, \"test\") && ${status} == \"active\"", context).Should().BeTrue();
        ExpressionEvaluator.Evaluate("coalesce(${missing}, \"default\") == \"default\"", context).Should().BeTrue();
    }
}
