# CommandContext
The `CommandContext` type holds the parsed information for a command invocation in the workflow engine. It provides access to the command name, its arguments, options, execution metadata, and helper methods for querying options and validating the input.

## API
### CommandName
- **Purpose:** Gets or sets the name of the command being executed.  
- **Parameters:** None.  
- **Return value:** The command name as a `string`.  
- **Exceptions:** None.

### Arguments
- **Purpose:** Gets or sets the list of positional arguments supplied to the command.  
- **Parameters:** None.  
- **Return value:** A `List<string>` containing the arguments in the order they were provided.  
- **Exceptions:** None.

### Options
- **Purpose:** Gets or sets the dictionary of named options (flags) and their associated values.  
- **Parameters:** None.  
- **Return value:** A `Dictionary<string,string>` where the key is the option name and the value is the option value (or `null` for flags without a value).  
- **Exceptions:** None.

### OutputFormat
- **Purpose:** Gets or sets the desired output format for the command result (e.g., `"json"`, `"text"`).  
- **Parameters:** None.  
- **Return value:** An `OutputFormat` string.  
- **Exceptions:** None.

### IsVerbose
- **Purpose:** Gets or sets a flag indicating whether verbose logging should be enabled for this command execution.  
- **Parameters:** None.  
- **Return value:** `true` if verbose mode is on; otherwise `false`.  
- **Exceptions:** None.

### ExecutingUser
- **Purpose:** Gets or sets the identifier of the user who triggered the command execution. May be `null` if the user is unknown.  
- **Parameters:** None.  
- **Return value:** A nullable `string` representing the user.  
- **Exceptions:** None.

### GetOption(string key)
- **Purpose:** Retrieves the value associated with the specified option name.  
- **Parameters:**  
  - `key`: The name of the option to look up. Must not be `null`.  
- **Return value:** The option value as a `string`, or `null` if the option does not exist or has no value.  
- **Exceptions:**  
  - `ArgumentNullException` if `key` is `null`.  
  - No exception is thrown for missing options; the method returns `null`.

### HasFlag(string flag)
- **Purpose:** Determines whether a flag‑style option (an option without a value) is present.  
- **Parameters:**  
  - `flag`: The name of the flag to check. Must not be `null`.  
- **Return value:** `true` if the flag exists in `Options`; otherwise `false`.  
- **Exceptions:**  
  - `ArgumentNullException` if `flag` is `null`.

### ValidateArguments()
- **Purpose:** Performs basic validation of the command’s arguments according to the command’s definition.  
- **Parameters:** None.  
- **Return value:** `true` if all arguments satisfy the command’s constraints; otherwise `false`.  
- **Exceptions:**  
  - `InvalidOperationException` if the context is in an inconsistent state (e.g., `Arguments` is `null`).

## Usage
```csharp
// Example 1: Inspecting a parsed command
var ctx = engine.ParseCommand("build --target release --verbose");
Console.WriteLine($"Command: {ctx.CommandName}");
Console.WriteLine($"Arguments: {string.Join(", ", ctx.Arguments)}");
if (ctx.GetOption("target") is string target)
{
    Console.WriteLine($"Target: {target}");
}
Console.WriteLine($"Verbose: {ctx.IsVerbose}");
```

```csharp
// Example 2: Validating before execution
var ctx = engine.ParseCommand("deploy");
if (!ctx.ValidateArguments())
{
    throw new InvalidOperationException("Missing required arguments for deploy.");
}
if (ctx.HasFlag("dry-run"))
{
    logger.Info("Performing a dry‑run deployment.");
}
else
{
    engine.Execute(ctx);
}
```

## Notes
- The `Arguments` and `Options` collections are mutable; modifying them after parsing will affect subsequent command execution. External synchronization is required if the same `CommandContext` instance is accessed from multiple threads.  
- `ExecutingUser` may be `null` when the command is invoked by an automated process or service account.  
- `GetOption` returns `null` for both missing options and options that were supplied without a value (flags). Use `HasFlag` to distinguish the latter case.  
- `ValidateArguments` performs only the basic checks defined by the command schema; it does not guarantee that the command will succeed at runtime.  
- None of the members throw exceptions under normal usage except where explicitly noted for null argument validation.  
- The type does not implement any synchronization primitives; callers must ensure thread‑safety when sharing instances.
