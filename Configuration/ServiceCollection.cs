// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Data.Context;
using DotNetWorkflowEngine.Services;
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
            services.Configure<DotnetWorkflowEngineOptions>(options =>
            {
                options.ConnectionString = connectionString;
            });
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
        var context = services.GetRequiredService<DatabaseContext>();
        await context.InitializeAsync();
    }
}
