// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Data.Context;
using DotNetWorkflowEngine.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetWorkflowEngine.Configuration;

/// <summary>
/// Extension methods for configuring the workflow engine services.
/// </summary>
public static class ServiceCollection
{
    /// <summary>
    /// Adds the workflow engine services to the DI container.
    /// </summary>
    public static IServiceCollection AddWorkflowEngine(this IServiceCollection services, string? connectionString = null)
    {
        // Register database context
        services.AddSingleton(new DatabaseContext(connectionString));

        // Register services
        services.AddSingleton<RetryPolicyService>();
        services.AddSingleton<AuditService>();
        services.AddSingleton<ActivityService>();
        services.AddSingleton<WorkflowDefinitionService>();
        services.AddSingleton<WorkflowExecutionService>();

        return services;
    }

    /// <summary>
    /// Adds the workflow engine with custom configuration.
    /// </summary>
    public static IServiceCollection AddWorkflowEngine(
        this IServiceCollection services,
        Action<WorkflowEngineOptions> configureOptions)
    {
        var options = new WorkflowEngineOptions();
        configureOptions(options);

        // Register database context with connection string
        services.AddSingleton(new DatabaseContext(options.ConnectionString));

        // Register services
        services.AddSingleton<RetryPolicyService>();
        services.AddSingleton<AuditService>();
        services.AddSingleton<ActivityService>();
        services.AddSingleton<WorkflowDefinitionService>();
        services.AddSingleton<WorkflowExecutionService>();

        // Configure retry policies if provided
        if (options.DefaultRetryPolicy != null)
        {
            services.AddSingleton(sp =>
            {
                var retryService = sp.GetRequiredService<RetryPolicyService>();
                retryService.CreatePolicy("default", options.DefaultRetryPolicy);
                return retryService;
            });
        }

        return services;
    }

    /// <summary>
    /// Initializes the workflow engine (creates schema, etc.).
    /// </summary>
    public static async Task InitializeWorkflowEngineAsync(this IServiceProvider services)
    {
        var context = services.GetRequiredService<DatabaseContext>();
        await context.InitializeAsync();
    }
}

/// <summary>
/// Configuration options for the workflow engine.
/// </summary>
public class WorkflowEngineOptions
{
    /// <summary>Gets or sets the database connection string.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Gets or sets the default retry policy.</summary>
    public Models.RetryPolicyConfig? DefaultRetryPolicy { get; set; }

    /// <summary>Gets or sets whether to enable audit logging.</summary>
    public bool EnableAuditLogging { get; set; } = true;

    /// <summary>Gets or sets the maximum concurrent workflows.</summary>
    public int MaxConcurrentWorkflows { get; set; } = 100;

    /// <summary>Gets or sets the default activity timeout in seconds.</summary>
    public int DefaultActivityTimeoutSeconds { get; set; } = 300;

    /// <summary>Gets or sets whether to validate workflows on load.</summary>
    public bool ValidateWorkflowsOnLoad { get; set; } = true;
}
