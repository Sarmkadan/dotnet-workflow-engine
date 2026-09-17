# CLI

Documentation for the command-line interface implemented in
`Cli/CommandParser.cs` and `Cli/WorkflowCommand.cs`. This is the lightweight,
dependency-free CLI layer of dotnet-workflow-engine.

> Note: this documents the actual `Cli/` implementation. It is distinct from
> the aspirational command surface described in `cli-reference.md`, which
> covers a broader `workflow`/`instance`/`audit`/`db` command set that is not
> implemented by these classes.

## Overview

The CLI is split into two responsibilities:

- **`CommandParser`** — parses raw `string[]` arguments into a `CommandContext`.
- **`WorkflowCommand`** — dispatches a parsed `CommandContext` to the matching
  command handler and formats output.

Both live in the `DotNetWorkflowEngine.Cli` namespace.

## CommandParser

`CommandParser` turns the raw arguments passed to the process into a
`CommandContext`. It intentionally avoids external dependencies (such as
`System.CommandLine`) to keep the CLI self-contained.

### Parse

```csharp
public CommandContext Parse(string[] args)
```

Parses the argument array into a `CommandContext`.

**Syntax:**

```
command [arg1 arg2] [--option value] [--flag] [--verbose] [--output json|csv|text]
```

**Parsing rules:**

- `args[0]` becomes `CommandContext.CommandName`.
- Arguments starting with `--` are treated as options. The option name is
  lower-cased and stored in `CommandContext.Options`.
- `--verbose` is a special flag that sets `CommandContext.IsVerbose = true` and
  consumes no value.
- An option consumes the next argument as its value **only if** that next
  argument does not itself start with `--`. Otherwise the option is stored with
  an empty value (acting as a flag).
- Arguments not starting with `-` are added to `CommandContext.Arguments`
  (positional arguments).

**Example:**

```csharp
var parser = new CommandParser();
var context = parser.Parse(new[]
{
    "create-workflow", "config.json", "--name", "MyWorkflow", "--verbose"
});

context.CommandName; // "create-workflow"
context.Arguments;   // ["config.json"]
context.Options;     // { "name" = "MyWorkflow" }
context.IsVerbose;   // true
```

**Throws:** `ArgumentException` if `args` is empty ("No command provided").

### DisplayHelp

```csharp
public static void DisplayHelp()
```

Writes the full help text to standard output. It lists the available commands,
global options, and examples. Called by the `help` command and when an unknown
command is encountered.

### IsValidCommand

```csharp
public bool IsValidCommand(string commandName)
```

Returns `true` if the given command name (matched case-insensitively) is one of
the recognized commands:

```
create-workflow, list-workflows, get-workflow, execute-instance,
list-instances, get-instance, retry-instance, audit-log,
validate-workflow, export-workflow, import-workflow, help
```

## CommandContext

`CommandContext` (in `Cli/CommandContext.cs`) is the parsed representation of a
single command invocation. It is produced by `CommandParser.Parse` and consumed
by `WorkflowCommand.ExecuteAsync`.

**Properties:**

| Property | Type | Description |
| --- | --- | --- |
| `CommandName` | `string` | The command being executed. |
| `Arguments` | `List<string>` | Positional arguments. |
| `Options` | `Dictionary<string, string>` | Named options, keyed by lower-cased name. |
| `OutputFormat` | `string` | Desired output format (default `"text"`). |
| `IsVerbose` | `bool` | Whether verbose output is enabled. |
| `ExecutingUser` | `string?` | The user executing the command, if known. |

**Methods:**

- `GetOption(string key)` — returns the value of an option (case-insensitive),
  or `null` if not present.
- `HasFlag(string flagName)` — returns `true` if the option exists and its value
  is `"true"`, `"1"`, `""`, or `"yes"` (case-insensitive).
- `ValidateArguments(int expectedCount)` — returns `true` if the argument count
  meets or exceeds `expectedCount`.
- `ToString()` — debug-friendly string of all properties.

### CommandContextExtensions

`Cli/CommandContextExtensions.cs` adds convenience helpers:

- `GetArgument(this CommandContext, int index)` — positional argument at
  `index`, or `null` if out of range.
- `GetArgumentsFrom(this CommandContext, int startIndex)` — remaining arguments
  joined by spaces.
- `GetOptionOrDefault(this CommandContext, string key, string defaultValue = "")`
  — option value with a fallback default.
- `ValidateRequiredArguments(this CommandContext, int expectedCount, params string[] argumentNames)`
  — returns an error message string when required arguments are missing,
  otherwise `null`.

## WorkflowCommand

`WorkflowCommand` is the central dispatcher. It is constructed with the workflow
services and an output formatter:

```csharp
public WorkflowCommand(
    WorkflowDefinitionService workflowService,
    WorkflowExecutionService executionService,
    AuditService auditService,
    IOutputFormatter formatter)
```

The injected services are held for future command implementations; the current
handlers are scaffolding stubs (see below).

### ExecuteAsync

```csharp
public async Task<int> ExecuteAsync(CommandContext context)
```

The main entry point for command processing. It switches on
`context.CommandName` (lower-cased) and routes to the matching handler. Returns
an exit code: `0` on success, `1` on failure.

Any exception thrown by a handler is caught, written to standard error (with the
stack trace when `context.IsVerbose` is set), and converted to exit code `1`.

**Command routing:**

| Command | Handler | Exit code |
| --- | --- | --- |
| `create-workflow` | `CreateWorkflowAsync` | `0` / `1` |
| `list-workflows` | `ListWorkflowsAsync` | `0` |
| `get-workflow` | `GetWorkflowAsync` | `0` / `1` |
| `execute-instance` | `ExecuteInstanceAsync` | `0` / `1` |
| `list-instances` | `ListInstancesAsync` | `0` |
| `get-instance` | `GetInstanceAsync` | `0` / `1` |
| `retry-instance` | `RetryInstanceAsync` | `0` / `1` |
| `audit-log` | `ViewAuditLogAsync` | `0` |
| `validate-workflow` | `ValidateWorkflowAsync` | `0` / `1` |
| `help` | `ExecuteHelp` | `0` |
| *(anything else)* | `HandleUnknownCommandAsync` | `1` |

### Command handlers

Handlers that require a positional argument call `context.ValidateArguments(1)`
first and print a usage message to standard error (returning `1`) when the
argument is missing.

| Command | Required argument | Options read |
| --- | --- | --- |
| `create-workflow` | `<config-file>` | `--name` (defaults to the config file name without extension) |
| `get-workflow` | `<workflow-id>` | — |
| `execute-instance` | `<workflow-id>` | `--input` (JSON) |
| `list-instances` | — | `--status`, `--workflow` |
| `get-instance` | `<instance-id>` | — |
| `retry-instance` | `<instance-id>` | — |
| `audit-log` | — | `--workflow`, `--instance`, `--limit` (default `"100"`) |
| `validate-workflow` | `<config-file>` | — |

**Current behavior:** most handlers are scaffolding stubs. They validate
arguments, print a progress line to standard output, and return `0` without yet
performing the underlying operation (marked with `// TODO` in the source). The
injected services and formatter are wired for the future implementations.

**Error output:** failures are written to standard error as
`❌ Error: <message>`, with a `Details:` line when verbose output is enabled.
Unknown commands print the error plus the full help text.

## Usage

The CLI is invoked by parsing process arguments and dispatching them:

```csharp
var parser = new CommandParser();
var context = parser.Parse(args);

var command = new WorkflowCommand(workflowService, executionService, auditService, formatter);
return await command.ExecuteAsync(context);
```

**Examples:**

```bash
# Create a workflow from a config file, with an explicit name
dotnet run create-workflow config.json --name MyWorkflow --verbose

# Execute a workflow instance with JSON input
dotnet run execute-instance wf-123 --input '{"orderId":"ORD-1"}'

# List instances filtered by status
dotnet run list-instances --status active --output json

# View the audit log for a workflow
dotnet run audit-log --workflow wf-123 --limit 50

# Show help
dotnet run help
```

## Exit codes

- `0` — success.
- `1` — failure (missing required argument, unknown command, or an exception
  thrown during handling).

## See also

- `Cli/CommandContext.cs` — the parsed command context.
- `Cli/CommandContextExtensions.cs` — argument/option helper extensions.
- `Formatters/IOutputFormatter.cs` — the output formatter abstraction injected
  into `WorkflowCommand`.
- `docs/cli-reference.md` — the broader (aspirational) CLI command reference.