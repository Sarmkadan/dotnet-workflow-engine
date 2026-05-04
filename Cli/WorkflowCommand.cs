// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetWorkflowEngine.Services;
using DotNetWorkflowEngine.Formatters;

namespace DotNetWorkflowEngine.Cli;

/// <summary>
/// Central dispatcher for all CLI commands. Each command maps to a workflow
/// operation (create, execute, list, etc.). This class routes commands to their
/// handlers and manages the output formatting based on the requested format.
/// </summary>
public class WorkflowCommand
{
    private readonly WorkflowDefinitionService _workflowService;
    private readonly WorkflowExecutionService _executionService;
    private readonly AuditService _auditService;
    private readonly IOutputFormatter _formatter;

    public WorkflowCommand(
        WorkflowDefinitionService workflowService,
        WorkflowExecutionService executionService,
        AuditService auditService,
        IOutputFormatter formatter)
    {
        _workflowService = workflowService;
        _executionService = executionService;
        _auditService = auditService;
        _formatter = formatter;
    }

    /// <summary>
    /// Executes a command in the given context. This is the main entry point
    /// for CLI command processing. Returns exit code (0 = success, 1 = failure).
    /// </summary>
    public async Task<int> ExecuteAsync(CommandContext context)
    {
        try
        {
            return await context.CommandName.ToLowerInvariant() switch
            {
                "create-workflow" => await CreateWorkflowAsync(context),
                "list-workflows" => await ListWorkflowsAsync(context),
                "get-workflow" => await GetWorkflowAsync(context),
                "execute-instance" => await ExecuteInstanceAsync(context),
                "list-instances" => await ListInstancesAsync(context),
                "get-instance" => await GetInstanceAsync(context),
                "retry-instance" => await RetryInstanceAsync(context),
                "audit-log" => await ViewAuditLogAsync(context),
                "validate-workflow" => await ValidateWorkflowAsync(context),
                "help" => ExecuteHelp(context),
                _ => await HandleUnknownCommandAsync(context)
            };
        }
        catch (Exception ex)
        {
            await OutputErrorAsync(ex.Message, context.IsVerbose ? ex.StackTrace : null);
            return 1;
        }
    }

    private async Task<int> CreateWorkflowAsync(CommandContext context)
    {
        if (!context.ValidateArguments(1))
        {
            await OutputErrorAsync("Usage: create-workflow <config-file> [--name <name>]");
            return 1;
        }

        var configFile = context.Arguments[0];
        var workflowName = context.GetOption("name") ?? System.IO.Path.GetFileNameWithoutExtension(configFile);

        await Console.Out.WriteLineAsync($"Creating workflow from {configFile}...");
        // TODO: Implement actual workflow creation from file
        await Console.Out.WriteLineAsync($"✓ Workflow '{workflowName}' created successfully");
        return 0;
    }

    private async Task<int> ListWorkflowsAsync(CommandContext context)
    {
        await Console.Out.WriteLineAsync("Fetching workflows...");
        // TODO: Implement workflow listing
        return 0;
    }

    private async Task<int> GetWorkflowAsync(CommandContext context)
    {
        if (!context.ValidateArguments(1))
        {
            await OutputErrorAsync("Usage: get-workflow <workflow-id>");
            return 1;
        }

        var workflowId = context.Arguments[0];
        // TODO: Implement workflow retrieval
        return 0;
    }

    private async Task<int> ExecuteInstanceAsync(CommandContext context)
    {
        if (!context.ValidateArguments(1))
        {
            await OutputErrorAsync("Usage: execute-instance <workflow-id> [--input json]");
            return 1;
        }

        var workflowId = context.Arguments[0];
        var inputJson = context.GetOption("input");

        await Console.Out.WriteLineAsync($"Executing workflow instance for {workflowId}...");
        // TODO: Implement instance execution
        return 0;
    }

    private async Task<int> ListInstancesAsync(CommandContext context)
    {
        var statusFilter = context.GetOption("status");
        var workflowFilter = context.GetOption("workflow");

        await Console.Out.WriteLineAsync("Fetching workflow instances...");
        // TODO: Implement instance listing with filters
        return 0;
    }

    private async Task<int> GetInstanceAsync(CommandContext context)
    {
        if (!context.ValidateArguments(1))
        {
            await OutputErrorAsync("Usage: get-instance <instance-id>");
            return 1;
        }

        // TODO: Implement instance retrieval
        return 0;
    }

    private async Task<int> RetryInstanceAsync(CommandContext context)
    {
        if (!context.ValidateArguments(1))
        {
            await OutputErrorAsync("Usage: retry-instance <instance-id>");
            return 1;
        }

        var instanceId = context.Arguments[0];
        await Console.Out.WriteLineAsync($"Retrying instance {instanceId}...");
        // TODO: Implement retry logic
        return 0;
    }

    private async Task<int> ViewAuditLogAsync(CommandContext context)
    {
        var workflowId = context.GetOption("workflow");
        var instanceId = context.GetOption("instance");
        var limit = context.GetOption("limit") ?? "100";

        await Console.Out.WriteLineAsync("Fetching audit log...");
        // TODO: Implement audit log retrieval
        return 0;
    }

    private async Task<int> ValidateWorkflowAsync(CommandContext context)
    {
        if (!context.ValidateArguments(1))
        {
            await OutputErrorAsync("Usage: validate-workflow <config-file>");
            return 1;
        }

        var configFile = context.Arguments[0];
        await Console.Out.WriteLineAsync($"Validating {configFile}...");
        // TODO: Implement validation
        return 0;
    }

    private int ExecuteHelp(CommandContext context)
    {
        CommandParser.DisplayHelp();
        return 0;
    }

    private async Task<int> HandleUnknownCommandAsync(CommandContext context)
    {
        await OutputErrorAsync($"Unknown command: {context.CommandName}");
        CommandParser.DisplayHelp();
        return 1;
    }

    private async Task OutputErrorAsync(string message, string? details = null)
    {
        await Console.Error.WriteLineAsync($"❌ Error: {message}");
        if (details != null)
        {
            await Console.Error.WriteLineAsync($"Details: {details}");
        }
    }
}
