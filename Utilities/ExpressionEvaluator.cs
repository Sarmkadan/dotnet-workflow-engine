// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Models;
using ExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

namespace DotNetWorkflowEngine.Utilities;

/// <summary>
/// Evaluates conditional expressions in workflows.
/// </summary>
public class ExpressionEvaluator
{
    /// <summary>
    /// Evaluates a boolean expression against a context.
    /// </summary>
    public static bool Evaluate(string expression, ExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return true;

        // Remove whitespace
        expression = expression.Trim();

        // Literal values
        if (expression == "true" || expression == "1")
            return true;

        if (expression == "false" || expression == "0")
            return false;

        // Variable reference: ${variable_name}
        if (expression.StartsWith("${") && expression.EndsWith("}"))
        {
            var varName = expression.Substring(2, expression.Length - 3);
            var value = context.GetVariable(varName);
            return ConvertToBoolean(value);
        }

        // Comparison expressions: ${variable} == value
        if (expression.Contains("=="))
        {
            return EvaluateComparison(expression, context, "==", (a, b) => a == b);
        }

        if (expression.Contains("!="))
        {
            return EvaluateComparison(expression, context, "!=", (a, b) => a != b);
        }

        if (expression.Contains(">="))
        {
            return EvaluateNumericComparison(expression, context, ">=", (a, b) => a >= b);
        }

        if (expression.Contains("<="))
        {
            return EvaluateNumericComparison(expression, context, "<=", (a, b) => a <= b);
        }

        if (expression.Contains(">"))
        {
            return EvaluateNumericComparison(expression, context, ">", (a, b) => a > b);
        }

        if (expression.Contains("<"))
        {
            return EvaluateNumericComparison(expression, context, "<", (a, b) => a < b);
        }

        // Logical AND: expression1 && expression2
        if (expression.Contains("&&"))
        {
            var parts = expression.Split("&&");
            return parts.All(p => Evaluate(p.Trim(), context));
        }

        // Logical OR: expression1 || expression2
        if (expression.Contains("||"))
        {
            var parts = expression.Split("||");
            return parts.Any(p => Evaluate(p.Trim(), context));
        }

        // Logical NOT: !expression
        if (expression.StartsWith("!"))
        {
            var innerExpression = expression.Substring(1).Trim();
            return !Evaluate(innerExpression, context);
        }

        // String contains: ${var} contains "text"
        if (expression.Contains(" contains "))
        {
            return EvaluateContains(expression, context);
        }

        return true;
    }

    /// <summary>
    /// Evaluates equality/inequality comparison.
    /// </summary>
    private static bool EvaluateComparison(string expression, ExecutionContext context, string op, Func<string, string, bool> comparer)
    {
        var parts = expression.Split(new[] { op }, StringSplitOptions.None);
        if (parts.Length != 2)
            return false;

        var left = ExtractValue(parts[0].Trim(), context);
        var right = parts[1].Trim().Trim('"', '\'');

        return comparer(left?.ToString() ?? "", right);
    }

    /// <summary>
    /// Evaluates numeric comparisons.
    /// </summary>
    private static bool EvaluateNumericComparison(string expression, ExecutionContext context, string op, Func<double, double, bool> comparer)
    {
        var parts = expression.Split(new[] { op }, StringSplitOptions.None);
        if (parts.Length != 2)
            return false;

        var left = ExtractValue(parts[0].Trim(), context);
        var right = parts[1].Trim().Trim('"', '\'');

        if (double.TryParse(left?.ToString(), out var leftNum) && double.TryParse(right, out var rightNum))
        {
            return comparer(leftNum, rightNum);
        }

        return false;
    }

    /// <summary>
    /// Evaluates string contains operation.
    /// </summary>
    private static bool EvaluateContains(string expression, ExecutionContext context)
    {
        var parts = expression.Split(" contains ", StringSplitOptions.None);
        if (parts.Length != 2)
            return false;

        var left = ExtractValue(parts[0].Trim(), context);
        var right = parts[1].Trim().Trim('"', '\'');

        return left?.ToString()?.Contains(right) ?? false;
    }

    /// <summary>
    /// Extracts a value from a variable reference or literal.
    /// </summary>
    private static object? ExtractValue(string expression, ExecutionContext context)
    {
        // Variable reference
        if (expression.StartsWith("${") && expression.EndsWith("}"))
        {
            var varName = expression.Substring(2, expression.Length - 3);
            return context.GetVariable(varName);
        }

        // Literal value
        return expression.Trim('"', '\'');
    }

    /// <summary>
    /// Converts a value to boolean.
    /// </summary>
    private static bool ConvertToBoolean(object? value)
    {
        if (value == null)
            return false;

        if (value is bool b)
            return b;

        if (value is int i)
            return i != 0;

        if (value is long l)
            return l != 0;

        if (value is double d)
            return d != 0;

        if (value is string s)
            return !string.IsNullOrEmpty(s) && s.ToLower() != "false" && s != "0";

        return true;
    }

    /// <summary>
    /// Validates an expression for syntax errors.
    /// </summary>
    public static bool ValidateExpression(string expression, out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(expression))
            return true;

        // Check for balanced braces
        var openBraces = expression.Count(c => c == '{');
        var closeBraces = expression.Count(c => c == '}');
        if (openBraces != closeBraces)
            errors.Add("Unbalanced braces in expression");

        // Check for valid operators
        var validOps = new[] { "==", "!=", ">=", "<=", ">", "<", "&&", "||", "contains" };
        var hasOperator = validOps.Any(op => expression.Contains(op)) || expression.StartsWith("!");

        if (!hasOperator && !expression.StartsWith("${") && expression != "true" && expression != "false")
            errors.Add("Expression must contain a valid operator or variable reference");

        return errors.Count == 0;
    }
}
