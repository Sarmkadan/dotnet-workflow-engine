# StringExtensions

Utility extension methods for `string` manipulation, validation, and transformation. This static class provides common operations such as case conversion, whitespace normalization, truncation, validation of email addresses and URLs, and safe substring extraction, reducing the need for repetitive boilerplate code across the `dotnet-workflow-engine` project.

## API

### ToPascalCase

```csharp
public static string ToPascalCase(this string input)
```

Converts the input string to PascalCase (upper camel case). Words are identified by splitting on non-alphanumeric characters, underscores, and case transitions. Each word is capitalized, and all separators are removed.

**Parameters:**
- `input` — The source string.

**Return value:** A new string in PascalCase format. Returns `string.Empty` if `input` is null or empty.

**Throws:** Does not throw.

---

### ToSnakeCase

```csharp
public static string ToSnakeCase(this string input)
```

Converts the input string to snake_case. Words are identified by splitting on non-alphanumeric characters, underscores, and case transitions. Words are lowercased and joined with underscores.

**Parameters:**
- `input` — The source string.

**Return value:** A new string in snake_case format. Returns `string.Empty` if `input` is null or empty.

**Throws:** Does not throw.

---

### ToKebabCase

```csharp
public static string ToKebabCase(this string input)
```

Converts the input string to kebab-case. Words are identified by splitting on non-alphanumeric characters, underscores, and case transitions. Words are lowercased and joined with hyphens.

**Parameters:**
- `input` — The source string.

**Return value:** A new string in kebab-case format. Returns `string.Empty` if `input` is null or empty.

**Throws:** Does not throw.

---

### Truncate

```csharp
public static string Truncate(this string input, int maxLength)
```

Truncates the string to the specified maximum length. If the string exceeds `maxLength`, it is cut to exactly `maxLength` characters. No ellipsis or suffix is appended unless the caller adds one separately.

**Parameters:**
- `input` — The source string.
- `maxLength` — The maximum number of characters to retain.

**Return value:** The truncated string, or the original string if it is shorter than or equal to `maxLength`. Returns `string.Empty` if `input` is null.

**Throws:**
- `ArgumentOutOfRangeException` — when `maxLength` is less than zero.

---

### IsValidEmail

```csharp
public static bool IsValidEmail(this string input)
```

Validates whether the string represents a well-formed email address according to a standard email regex pattern. This checks structural validity, not deliverability.

**Parameters:**
- `input` — The string to validate.

**Return value:** `true` if the string matches the email pattern; otherwise `false`. Returns `false` for null or empty input.

**Throws:** Does not throw.

---

### IsValidUrl

```csharp
public static bool IsValidUrl(this string input)
```

Validates whether the string represents a well-formed absolute URL using `Uri.TryCreate` with `UriKind.Absolute`. Both HTTP and non-HTTP schemes are accepted.

**Parameters:**
- `input` — The string to validate.

**Return value:** `true` if the string is a valid absolute URL; otherwise `false`. Returns `false` for null or empty input.

**Throws:** Does not throw.

---

### RemoveWhitespace

```csharp
public static string RemoveWhitespace(this string input)
```

Removes all whitespace characters from the string, including spaces, tabs, newlines, and other Unicode whitespace.

**Parameters:**
- `input` — The source string.

**Return value:** A new string with all whitespace characters removed. Returns `string.Empty` if `input` is null or empty.

**Throws:** Does not throw.

---

### NormalizeWhitespace

```csharp
public static string NormalizeWhitespace(this string input)
```

Collapses all sequences of whitespace characters into a single space and trims leading and trailing whitespace.

**Parameters:**
- `input` — The source string.

**Return value:** A new string with normalized whitespace. Returns `string.Empty` if `input` is null or empty.

**Throws:** Does not throw.

---

### Repeat

```csharp
public static string Repeat(this string input, int count)
```

Returns a new string consisting of the input string repeated the specified number of times.

**Parameters:**
- `input` — The string to repeat.
- `count` — The number of repetitions.

**Return value:** The concatenated result. Returns `string.Empty` if `input` is null or empty, or if `count` is zero.

**Throws:**
- `ArgumentOutOfRangeException` — when `count` is negative.

---

### SafeSubstring

```csharp
public static string SafeSubstring(this string input, int startIndex, int length)
```

Extracts a substring without throwing when the requested range exceeds the string bounds. If `startIndex` is beyond the string length, an empty string is returned. If `startIndex + length` exceeds the string length, the substring from `startIndex` to the end is returned.

**Parameters:**
- `input` — The source string.
- `startIndex` — The zero-based starting character position.
- `length` — The desired number of characters.

**Return value:** The substring within the safe bounds. Returns `string.Empty` if `input` is null.

**Throws:**
- `ArgumentOutOfRangeException` — when `startIndex` or `length` is negative.

---

### ExtractBetween

```csharp
public static string? ExtractBetween(this string input, string startDelimiter, string endDelimiter, StringComparison comparison = StringComparison.Ordinal)
```

Extracts the substring between the first occurrence of `startDelimiter` and the first subsequent occurrence of `endDelimiter`. The delimiters themselves are excluded from the result.

**Parameters:**
- `input` — The source string.
- `startDelimiter` — The delimiter marking the start of the extraction region.
- `endDelimiter` — The delimiter marking the end of the extraction region.
- `comparison` — The string comparison rule to use (default: `Ordinal`).

**Return value:** The extracted substring, or `null` if either delimiter is not found, if `startDelimiter` appears after `endDelimiter`, or if `input` is null.

**Throws:**
- `ArgumentNullException` — when `startDelimiter` or `endDelimiter` is null.
- `ArgumentException` — when `startDelimiter` or `endDelimiter` is empty.

---

### SmartSplit

```csharp
public static IEnumerable<string> SmartSplit(this string input, params char[] separators)
```

Splits the string by the specified separator characters, trimming each segment and removing empty entries. This combines `Split` with automatic trimming and filtering in a single pass.

**Parameters:**
- `input` — The source string.
- `separators` — The characters to split on. If none are provided, splits on whitespace characters.

**Return value:** An `IEnumerable<string>` of non-empty, trimmed segments. Returns an empty enumeration if `input` is null or empty.

**Throws:** Does not throw.

---

## Usage

### Example 1: Normalizing and validating user input from a workflow form

```csharp
using dotnet_workflow_engine.Extensions;

public void ProcessWorkflowForm(string rawEmail, string rawDisplayName, string rawNotes)
{
    // Validate email before proceeding
    if (!rawEmail.IsValidEmail())
    {
        throw new ArgumentException("Invalid email address provided.");
    }

    // Normalize display name: collapse whitespace, then convert to PascalCase for storage
    string cleanName = rawDisplayName.NormalizeWhitespace().ToPascalCase();

    // Truncate notes to fit database column constraint
    string notes = rawNotes.NormalizeWhitespace().Truncate(500);

    // Extract a workflow ID from a bracketed reference in the notes
    string? workflowId = notes.ExtractBetween("[WF:", "]", StringComparison.OrdinalIgnoreCase);

    SaveToDatabase(cleanName, rawEmail, notes, workflowId);
}
```

### Example 2: Generating identifiers and splitting delimited configuration values

```csharp
using dotnet_workflow_engine.Extensions;

public void ConfigureWorkflowSteps(string stepDescription, string allowedActionsConfig)
{
    // Convert a human-readable step description into a machine-friendly kebab-case key
    string stepKey = stepDescription.ToKebabCase();

    // Repeat a separator line for log formatting
    string separator = "-".Repeat(40);

    // Split a pipe-delimited configuration string into actionable items
    IEnumerable<string> actions = allowedActionsConfig.SmartSplit('|');

    foreach (string action in actions)
    {
        // Each action is already trimmed and non-empty
        RegisterAction(stepKey, action);
    }
}
```

---

## Notes

- **Null handling:** All methods treat null input gracefully, typically returning an empty string, `false`, `null`, or an empty enumeration as appropriate. No method throws `NullReferenceException` due to a null `input` argument.
- **Case conversion boundaries:** `ToPascalCase`, `ToSnakeCase`, and `ToKebabCase` rely on heuristics for word boundary detection (non-alphanumeric characters, underscores, and case transitions). Strings with unusual casing (e.g., consecutive uppercase letters such as "XMLParser") may produce results that require manual review.
- **`ExtractBetween` return value:** Returns `null` when delimiters are not found or are in the wrong order. Callers should perform a null check before using the result.
- **`SafeSubstring` bounds:** Unlike `string.Substring`, this method silently clamps out-of-range requests rather than throwing. Negative `startIndex` or `length` values still throw `ArgumentOutOfRangeException`.
- **`SmartSplit` deferred execution:** Returns `IEnumerable<string>` with deferred execution. If the input string is modified after the call but before enumeration, results may be unpredictable. Materialize with `.ToList()` if the source may change.
- **Thread safety:** All methods are static and operate on immutable string inputs without shared state. They are safe to call concurrently from multiple threads.
