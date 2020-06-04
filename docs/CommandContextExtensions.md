# CommandContextExtensions

Static extension methods that simplify working with a `CommandContext` instance when parsing command‑line input. The helpers provide convenient ways to fetch argument values, option values with fallbacks, and to validate that required arguments are present.

## API

### `public static string? GetArgument(this CommandContext context, string argumentName)`

**Purpose**  
Retrieves the value of the argument identified by `argumentName`. If the argument is not present, the method returns `null`.

**Parameters**  
- `context`: The `CommandContext` whose argument collection is inspected.  
- `argumentName`: The name of the argument to look up (case‑sensitive).

**Return value**  
The argument’s value as a string, or `null` when the argument is missing.

**Exceptions**  
- `ArgumentNullException` – if `context` or `argumentName` is `null`.

### `public static string GetArgumentsFrom(this CommandContext context, int startIndex)`

**Purpose**  
Returns a single string containing all arguments from the position `startIndex` to the end of the argument list, separated by a single space. This is useful when a command accepts a variable‑length tail of arguments.

**Parameters**  
- `context`: The `CommandContext` providing the argument list.  
- `startIndex`: Zero‑based index of the first argument to include.

**Return value**  
A concatenated string of the selected arguments. If `startIndex` equals the number of arguments, an empty string is returned.

**Exceptions**  
- `ArgumentNullException` – if `context` is `null`.  
- `ArgumentOutOfRangeException` – if `startIndex` is less than zero or greater than the number of arguments in `context`.

### `public static string GetOptionOrDefault(this CommandContext context, string optionName, string defaultValue)`

**Purpose**  
Looks for an option (e.g., `--option` or `-o`) named `optionName` and returns its associated value. If the option is not present, the supplied `defaultValue` is returned instead.

**Parameters**  
- `context`: The `CommandContext` to search.  
- `optionName`: The option name without the leading dash(es) (case‑sensitive).  
- `defaultValue`: The value to return when the option is absent.

**Return value**  
The option’s value if found; otherwise `defaultValue`.

**Exceptions**  
- `ArgumentNullException` – if `context` or `optionName` is `null`.

### `public static string? ValidateRequiredArguments(this CommandContext context, params string[] requiredArgumentNames)`

**Purpose**  
Verifies that each argument listed in `requiredArgumentNames` has a non‑null value in the context. Useful for early validation before executing command logic.

**Parameters**  
- `context`: The `CommandContext` to validate.  
- `requiredArgumentNames`: Variable list of argument names that must be present.

**Return value**  
`null` when all required arguments are present; otherwise a descriptive error message listing the missing arguments.

**Exceptions**  
- `ArgumentNullException` – if `context` or `requiredArgumentNames` is `null`.  
- `ArgumentException` – if any element in `requiredArgumentNames` is `null`, empty, or consists only of white‑space.

## Usage

### Example 1 – Retrieving an argument and an option with a fallback

```csharp
var ctx = new CommandContext(args); // args from Main(string[])

string inputFile = ctx.GetArgument("input") ?? throw new CommandException("Missing input file.");
string verbosity = ctx.GetOptionOrDefault("verbose", "info");

Console.WriteLine($"Processing {inputFile} with verbosity {verbosity}");
```

### Example 2 – Validating required arguments before proceeding

```csharp
var ctx = new CommandContext(args);

string? validationError = ctx.ValidateRequiredArguments("source", "destination");
if (validationError != null)
{
    Console.Error.WriteLine(validationError);
    return 1; // exit with error
}

// All required arguments are present; continue with the command logic.
CopyFiles(ctx.GetArgument("source"), ctx.GetArgument("destination"));
```

## Notes

- The extension methods treat argument and option names as case‑sensitive; callers must match the exact casing used when the `CommandContext` was populated.  
- `GetArgumentsFrom` does not modify the original argument list; it merely reads from it, making the method safe to call multiple times.  
- Because these methods are static and operate only on the supplied `CommandContext` instance, they are inherently thread‑safe as long as the `CommandContext` itself is not mutated concurrently by other threads.  
- If an option can appear multiple times, `GetOptionOrDefault` returns the value from the first occurrence; subsequent instances are ignored.  
- Validation stops at the first missing argument and returns a message that includes all missing names; it does not throw an exception, allowing callers to decide how to report the problem.
