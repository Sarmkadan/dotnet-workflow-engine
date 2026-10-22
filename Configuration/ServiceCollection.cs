// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Data.Context;
using DotNetWorkflowEngine.Services;
using DotNetWorkflowEngine.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using FluentValidation;

namespace DotNetWorkflowEngine.Configuration;

/// <summary>
/// Extension methods for configuring the workflow engine services.
/// </summary>
public static class ServiceCollection
{
    /// <summary>
    /// Adds the workflow engine services to the DI container using IOptions pattern.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">Database connection string</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddWorkflowEngine(this IServiceCollection services, string? connectionString = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        // Register database context
        services.AddSingleton(new DatabaseContext(connectionString));

        // AuditService depends on IAuditRepository; resolve it from the same
        // DatabaseContext instance so the audit trail has a single backing store.
        services.AddSingleton<Data.Repositories.IAuditRepository>(
            sp => sp.GetRequiredService<DatabaseContext>().AuditLogs);

        // Register services
        services.AddSingleton<RetryPolicyService>();
        services.AddSingleton<AuditService>();
        services.AddSingleton<ActivityService>();
        services.AddSingleton<WorkflowDefinitionService>();
        services.AddSingleton<WorkflowExecutionService>();

        // Configure options with validation
        if (connectionString != null)
        {
            services.AddOptions<DotnetWorkflowEngineOptions>()
                .Configure(options =>
                {
                    options.ConnectionString = connectionString;
                })
                .ValidateDataAnnotations()
                // Custom validation: retry delay must be positive when retry count > 0
                .Validate(options => !(options.RetryCount > 0 && options.RetryDelaySeconds <= 0),
                    "RetryDelaySeconds must be positive when RetryCount is greater than 0")
                .ValidateOnStart();
        }

        // Register options validator
        services.AddSingleton<IValidator<DotnetWorkflowEngineOptions>, DotnetWorkflowEngineOptionsValidator>();

        return services;
    }

    /// <summary>
    /// Adds the workflow engine with custom configuration.
    /// </summary>
    public static IServiceCollection AddWorkflowEngine(
        this IServiceCollection services,
        Action<DotnetWorkflowEngineOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);
        // Register database context with connection string from options
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DotnetWorkflowEngineOptions>>().Value;
            return new DatabaseContext(options.ConnectionString);
        });

        // Same reasoning as above: AuditService needs IAuditRepository, and it
        // must be the one owned by the DatabaseContext.
        services.AddSingleton<Data.Repositories.IAuditRepository>(
            sp => sp.GetRequiredService<DatabaseContext>().AuditLogs);

        // Register services
        services.AddSingleton<RetryPolicyService>();
        services.AddSingleton<AuditService>();
        services.AddSingleton<ActivityService>();
        services.AddSingleton<WorkflowDefinitionService>();
        services.AddSingleton<WorkflowExecutionService>();

        // Register options validator
        services.AddSingleton<IValidator<DotnetWorkflowEngineOptions>, DotnetWorkflowEngineOptionsValidator>();

        return services;
    }

    /// <summary>
    /// Initializes the workflow engine (creates schema, etc.).
    /// </summary>
    public static async Task InitializeWorkflowEngineAsync(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        var context = services.GetRequiredService<DatabaseContext>();
        await context.InitializeAsync();

        // Validate all registered workflow definitions on startup
        var workflowDefinitionService = services.GetRequiredService<WorkflowDefinitionService>();
        var allWorkflows = workflowDefinitionService.GetAllWorkflows();

        foreach (var workflow in allWorkflows)
        {
            var validationResult = WorkflowValidator.ValidateWorkflow(workflow);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException($"Workflow '{workflow.Id}' validation failed: {string.Join("; ", validationResult.Errors)}");
            }
        }
    }
}
