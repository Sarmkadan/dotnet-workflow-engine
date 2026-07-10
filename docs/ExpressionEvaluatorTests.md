# ExpressionEvaluatorTests

Unit tests for the `ExpressionEvaluator` class in the `dotnet-workflow-engine` project. These tests verify the correct evaluation of workflow expressions, including literal values, variable references, comparisons, logical operators, and edge cases. The test suite ensures that expression evaluation behaves as expected across different data types and operator combinations.

## API

### `Evaluate_NullOrEmptyExpression_ReturnsTrue`
Verifies that evaluating a null or empty expression returns `true`. This test ensures the evaluator handles empty expressions gracefully without throwing exceptions.

### `Evaluate_LiteralTrue_ReturnsTrue`
Tests that a literal `true` expression evaluates to `true`. This confirms basic boolean literal handling.

### `Evaluate_LiteralFalse_ReturnsFalse`
Tests that a literal `false` expression evaluates to `false`. This confirms basic boolean literal handling.

### `Evaluate_VariableReference_ReturnsVariableValue`
Validates that referencing a workflow variable returns its stored value. The test ensures variable resolution works correctly during evaluation.

### `Evaluate_VariableReferenceNullOrFalse_ReturnsFalse`
Checks that referencing a null or false variable returns `false`. This test ensures proper handling of falsy variable values.

### `Evaluate_EqualityComparison_StringValues_EvaluatesCorrectly`
Tests string equality comparisons (e.g., `"a" == "a"`). Verifies that string comparisons are case-sensitive and return the correct boolean result.

### `Evaluate_InequalityComparison_StringValues_EvaluatesCorrectly`
Tests string inequality comparisons (e.g., `"a" != "b"`). Verifies that string comparisons are case-sensitive and return the correct boolean result.

### `Evaluate_NumericGreaterThan_EvaluatesCorrectly`
Tests numeric greater-than comparisons (e.g., `5 > 3`). Ensures numeric comparisons evaluate correctly for positive and negative numbers.

### `Evaluate_NumericLessThan_EvaluatesCorrectly`
Tests numeric less-than comparisons (e.g., `2 < 4`). Ensures numeric comparisons evaluate correctly for positive and negative numbers.

### `Evaluate_NumericGreaterThanOrEqual_EvaluatesCorrectly`
Tests numeric greater-than-or-equal comparisons (e.g., `5 >= 5`). Ensures numeric comparisons evaluate correctly for boundary values.

### `Evaluate_NumericLessThanOrEqual_EvaluatesCorrectly`
Tests numeric less-than-or-equal comparisons (e.g., `3 <= 3`). Ensures numeric comparisons evaluate correctly for boundary values.

### `Evaluate_StringContains_EvaluatesCorrectly`
Tests string containment checks (e.g., `"hello".Contains("ell")`). Verifies that substring matching works as expected.

### `Evaluate_LogicalAnd_AllTrue_ReturnsTrue`
Tests logical AND operations where all conditions are true (e.g., `true && true`). Ensures the operator short-circuits correctly and returns the expected result.

### `Evaluate_LogicalAnd_OneFalse_ReturnsFalse`
Tests logical AND operations where at least one condition is false (e.g., `true && false`). Ensures the operator short-circuits correctly and returns `false`.

### `Evaluate_LogicalAnd_MultipleConditions_EvaluatesCorrectly`
Tests logical AND operations with multiple conditions (e.g., `true && true && false`). Ensures correct evaluation order and short-circuiting behavior.

### `Evaluate_LogicalOr_OneTrue_ReturnsTrue`
Tests logical OR operations where at least one condition is true (e.g., `false || true`). Ensures the operator short-circuits correctly and returns `true`.

### `Evaluate_LogicalOr_AllFalse_ReturnsFalse`
Tests logical OR operations where all conditions are false (e.g., `false || false`). Ensures the operator evaluates all conditions and returns `false`.

### `Evaluate_LogicalOr_MultipleConditions_EvaluatesCorrectly`
Tests logical OR operations with multiple conditions (e.g., `false || false || true`). Ensures correct evaluation order and short-circuiting behavior.

### `Evaluate_LogicalNot_True_ReturnsFalse`
Tests logical NOT operations on a true value (e.g., `!true`). Ensures the operator negates the input correctly.

### `Evaluate_LogicalNot_False_ReturnsTrue`
Tests logical NOT operations on a false value (e.g., `!false`). Ensures the operator negates the input correctly.

## Usage
