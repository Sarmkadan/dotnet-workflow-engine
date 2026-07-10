# StringExtensionsTests

Unit tests for the `StringExtensions` static class, verifying behavior of string manipulation utilities used throughout the workflow engine.

## API

### `ToPascalCase_KebabInput_ConvertsCorrectly()`
Verifies that kebab-case input strings are correctly converted to PascalCase. The test asserts that each segment separated by hyphens is capitalized and concatenated without separators.

Parameters:
- None

Return value:
- None (void)

Throws:
- Throws `Xunit.AssertException` if the conversion does not match the expected PascalCase output.

---

### `ToSnakeCase_CamelCaseInput_InsertsUnderscoresAndLowers()`
Ensures that camelCase input strings are transformed into snake_case by inserting underscores before uppercase letters and converting the entire string to lowercase.

Parameters:
- None

Return value:
- None (void)

Throws:
- Throws `Xunit.AssertException` if the output does not match the expected snake_case format.

---

### `Truncate_LongStringWithSuffix_TruncatesAndAppendsSuffix()`
Confirms that long strings are truncated to a specified maximum length and that a suffix is appended to the truncated result. The test checks both the length of the output and the presence of the suffix.

Parameters:
- None

Return value:
- None (void)

Throws:
- Throws `Xunit.AssertException` if the truncated string exceeds the expected length or lacks the suffix.

---

### `SmartSplit_QuotedSection_DoesNotSplitDelimiterInsideQuotes()`
Validates that the `SmartSplit` method correctly handles quoted sections by not splitting on delimiters that appear within quotes. The test uses a comma delimiter and verifies that commas inside quoted substrings are preserved.

Parameters:
- None

Return value:
- None (void)

Throws:
- Throws `Xunit.AssertException` if the split result does not match the expected grouping of quoted and unquoted segments.

## Usage
