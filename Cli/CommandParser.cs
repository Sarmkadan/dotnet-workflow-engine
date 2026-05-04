// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetWorkflowEngine.Cli;

/// <summary>
/// Parses command-line arguments into a CommandContext. Handles both positional
/// arguments and named options (--key value, --flag). This parser is intentionally
/// simple to avoid external dependencies; for complex scenarios, consider integrating
/// a library like System.CommandLine in future versions.
/// </summary>
public class CommandParser
{
    /// <summary>
    /// Parses raw CLI arguments into a CommandContext.
    ///
    /// Syntax:
    ///   command [arg1 arg2] [--option value] [--flag] [--verbose] [--output json|csv|text]
    ///
    /// Example: "create-workflow config.json --name MyWorkflow --verbose --output json"
    /// </summary>
    public CommandContext Parse(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("No command provided");

        var context = new CommandContext { CommandName = args[0] };

        for (int i = 1; i < args.Length; i++)
        {
            var arg = args[i];

            // Handle options starting with --
            if (arg.StartsWith("--"))
            {
                var optionName = arg[2..].ToLowerInvariant();

                // Check for special flags that don't require values
                if (optionName == "verbose")
                {
                    context.IsVerbose = true;
                    continue;
                }

                // Extract value for the option
                string optionValue = string.Empty;
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    optionValue = args[++i];
                }

                context.Options[optionName] = optionValue;
            }
            // Positional arguments don't start with --
            else if (!arg.StartsWith("-"))
            {
                context.Arguments.Add(arg);
            }
        }

        return context;
    }

    /// <summary>
    /// Displays help text for all available commands. This method should be called
    /// when --help is requested or when an invalid command is provided.
    /// </summary>
    public static void DisplayHelp()
    {
        var helpText = @"
Workflow Engine CLI - Manage workflows, instances, and audit trails

USAGE:
  dotnet run <command> [arguments] [options]

COMMANDS:
  create-workflow   <config-file>      Create a new workflow definition
  list-workflows                        List all defined workflows
  get-workflow      <workflow-id>       Get workflow details
  execute-instance  <workflow-id>       Execute a workflow instance
  list-instances    [--status status]   List workflow instances
  get-instance      <instance-id>       Get instance details
  retry-instance    <instance-id>       Retry a failed workflow instance
  audit-log         [--workflow id]     View audit log entries
  validate-workflow <config-file>       Validate workflow configuration
  export-workflow   <workflow-id> <fmt> Export workflow (json|yaml|bpmn)
  import-workflow   <file>             Import workflow from file

GLOBAL OPTIONS:
  --verbose                  Enable verbose output
  --output <format>          Output format (json, csv, text) - default: text
  --user <username>          Specify executing user for audit logging
  --help                     Show this help text

EXAMPLES:
  dotnet run create-workflow ./workflows/order-processing.json --verbose
  dotnet run list-instances --status active --output json
  dotnet run audit-log --workflow wf-123 --output csv

For more information, visit: https://github.com/sarmkadan/dotnet-workflow-engine
";
        Console.WriteLine(helpText);
    }

    /// <summary>
    /// Validates that a command string is recognized and well-formed.
    /// </summary>
    public bool IsValidCommand(string commandName)
    {
        var validCommands = new[]
        {
            "create-workflow", "list-workflows", "get-workflow",
            "execute-instance", "list-instances", "get-instance",
            "retry-instance", "audit-log", "validate-workflow",
            "export-workflow", "import-workflow", "help"
        };

        return validCommands.Contains(commandName.ToLowerInvariant());
    }
}
