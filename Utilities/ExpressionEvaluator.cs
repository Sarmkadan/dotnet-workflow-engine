// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Models;
using System.Text.RegularExpressions;
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


        // Function calls: len(), coalesce(), contains()
        if (expression.Contains('(') && expression.EndsWith(")"))
        {
            if (EvaluateFunctionCall(expression, context))
                return true;
        }

        // Variable reference: ${variable_name}
        // Must match the whole expression exactly (no other operators/braces present),
        // otherwise a compound expression like "${a} && ${b}" would be misread as a
        // single variable reference because it also starts with "${" and ends with "}".
        if (IsSingleVariableReference(expression))
        {
            var varName = expression.Substring(2, expression.Length - 3);
            var value = context.GetVariable(varName);
            return ConvertToBoolean(value);
        }

        // Logical AND / OR must be evaluated before individual comparisons: a compound
        // expression like "${amount} > \"500\" && ${approved}" also contains ">" and would
        // otherwise be misparsed as a single numeric comparison across the whole string.

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

        if (double.TryParse(left?.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var leftNum) &&
            double.TryParse(right, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var rightNum))
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
    /// Determines whether the expression is exactly one variable reference (e.g. "${name}")
    /// with no additional braces or operators outside of it.
    /// </summary>
    private static bool IsSingleVariableReference(string expression)
    {
        if (!expression.StartsWith("${") || !expression.EndsWith("}"))
            return false;

        var inner = expression.Substring(2, expression.Length - 3);
        return !inner.Contains('{') && !inner.Contains('}');
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
            return !string.IsNullOrEmpty(s) && !s.Equals("false", StringComparison.OrdinalIgnoreCase) && s != "0";

        return true;
    }

    /// <summary>
    /// Evaluates a function call expression.
    /// </summary>
    private static bool EvaluateFunctionCall(string expression, ExecutionContext context)
    {
        // Match function calls like: len(${var}), coalesce(${a}, ${b}), contains("text", "needle")
        var match = Regex.Match(expression, @"^(\w+)\((.*)\)$", RegexOptions.IgnoreCase);

        if (!match.Success)
            return true; // Not a function call, let other logic handle it

        var functionName = match.Groups[1].Value.ToLowerInvariant();
        var arguments = match.Groups[2].Value;

        try
        {
            switch (functionName)
            {
                case "len":
                    return EvaluateLen(arguments, context);
                case "upper":
                    return EvaluateUpper(arguments, context);
                case "lower":
                    return EvaluateLower(arguments, context);
                case "now":
                    return EvaluateNow(arguments, context);
                case "coalesce":
                    return EvaluateCoalesce(arguments, context);
                case "contains":
                    return EvaluateContainsFunction(arguments, context);
                default:
                    // Unknown function, treat as non-matching
                    return false;
            }
        }
        catch
        {
            // If function evaluation fails, treat as non-matching
            return false;
        }
    }

    /// <summary>
    /// Evaluates len() function - returns length of string or collection.
    /// </summary>
    private static bool EvaluateLen(string arguments, ExecutionContext context)
    {
        var argList = ParseArguments(arguments);
        if (argList.Count != 1)
            return false;

        var value = ExtractValue(argList[0].Trim(), context);
        var str = value?.ToString();

        if (str == null)
            return false;

        context.SetVariable("_len_result", str.Length);
        return true;
    }

    /// <summary>
    /// Evaluates upper() function - returns uppercase string.
    /// </summary>
    private static bool EvaluateUpper(string arguments, ExecutionContext context)
    {
        var argList = ParseArguments(arguments);
        if (argList.Count != 1)
            return false;

        var value = ExtractValue(argList[0].Trim(), context);
        var str = value?.ToString();

        if (str == null)
            return false;

        context.SetVariable("_upper_result", str.ToUpperInvariant());
        return true;
    }

    /// <summary>
    /// Evaluates lower() function - returns lowercase string.
    /// </summary>
    private static bool EvaluateLower(string arguments, ExecutionContext context)
    {
        var argList = ParseArguments(arguments);
        if (argList.Count != 1)
            return false;

        var value = ExtractValue(argList[0].Trim(), context);
        var str = value?.ToString();

        if (str == null)
            return false;

        context.SetVariable("_lower_result", str.ToLowerInvariant());
        return true;
    }

    /// <summary>
    /// Evaluates now() function - returns current UTC date time.
    /// </summary>
    private static bool EvaluateNow(string arguments, ExecutionContext context)
    {
        context.SetVariable("_now_result", DateTime.UtcNow);
        return true;
    }

    /// <summary>
    /// Evaluates coalesce() function - returns first non-null value.
    /// </summary>
    private static bool EvaluateCoalesce(string arguments, ExecutionContext context)
    {
        var argList = ParseArguments(arguments);
        if (argList.Count < 2)
            return false;

        foreach (var arg in argList)
        {
            var value = ExtractValue(arg.Trim(), context);
            if (value != null)
            {
                // Return true if the first non-null value is truthy
                return ConvertToBoolean(value);
            }
        }

        return false; // All arguments are null
    }

    /// <summary>
    /// Evaluates contains() function - checks if haystack contains needle.
    /// </summary>
    private static bool EvaluateContainsFunction(string arguments, ExecutionContext context)
    {
        var argList = ParseArguments(arguments);
        if (argList.Count != 2)
            return false;

        var haystack = ExtractValue(argList[0].Trim(), context)?.ToString();
        var needle = argList[1].Trim().Trim('"', '\'');

        if (haystack == null)
            return false;

        return haystack.Contains(needle);
    }

    /// <summary>
    /// Parses comma-separated arguments, handling quoted strings and variable references.
    /// </summary>
    private static List<string> ParseArguments(string arguments)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;
        char quoteChar = '\0';

        for (int i = 0; i < arguments.Length; i++)
        {
            var c = arguments[i];

            if (c == '"' || c == '\'')
            {
                if (inQuotes && c == quoteChar)
                {
                    inQuotes = false;
                    quoteChar = '\0';
                }
                else if (!inQuotes)
                {
                    inQuotes = true;
                    quoteChar = c;
                }
                current.Append(c);
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0 || arguments.EndsWith(","))
            result.Add(current.ToString().Trim());

        return result;
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
        {
            // Check if it's a function call
            var match = Regex.Match(expression, @"^(\w+)\(.*\)$", RegexOptions.IgnoreCase);
            if (!match.Success)
                errors.Add("Expression must contain a valid operator or variable reference");
        }

        return errors.Count == 0;
    }
}
