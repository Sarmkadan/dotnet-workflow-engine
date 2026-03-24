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
    public string? ConnectionString { get; set; }
    public DotNetWorkflowEngine.Models.RetryPolicyConfig? DefaultRetryPolicy { get; set; }
    public bool EnableAuditLogging { get; set; } = true;
    public int MaxConcurrentWorkflows { get; set; } = 100;
    public int DefaultActivityTimeoutSeconds { get; set; } = 300;
    public bool ValidateWorkflowsOnLoad { get; set; } = true;
    public bool UseCaching { get; set; } = true;
    public bool UseDistributedCache { get; set; } = false;
    public string? RedisConnectionString { get; set; }
    public TimeSpan DefaultCacheExpiration { get; set; } = TimeSpan.FromHours(1);
    public bool EnableBackgroundJobs { get; set; } = true;
}
