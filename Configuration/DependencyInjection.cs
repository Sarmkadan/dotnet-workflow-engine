// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using DotNetWorkflowEngine.Caching;
using DotNetWorkflowEngine.Events;
using DotNetWorkflowEngine.Formatters;
using DotNetWorkflowEngine.Integration;
using DotNetWorkflowEngine.Middleware;
using DotNetWorkflowEngine.Monitoring;
using DotNetWorkflowEngine.BackgroundJobs;
using DotNetWorkflowEngine.Filters;

namespace DotNetWorkflowEngine.Configuration;

/// <summary>
/// Dependency injection and service registration extensions for the workflow engine.
/// Centralizes configuration of all services, middleware, and filters.
/// Call AddWorkflowEngine() during startup to register all services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all workflow engine services into the DI container.
    /// Call this method in Program.cs during application startup.
    /// </summary>
    public static IServiceCollection AddWorkflowEngine(
        this IServiceCollection services,
        WorkflowEngineOptions? options = null)
    {
        options ??= new WorkflowEngineOptions();

        // Register core services (assumes these are in ServiceCollection.cs)
        // services.AddScoped<WorkflowDefinitionService>();
        // services.AddScoped<WorkflowExecutionService>();
        // services.AddScoped<AuditService>();
        // services.AddScoped<ActivityService>();

        // Register formatters
        services.AddSingleton<JsonOutputFormatter>();
        services.AddSingleton<CsvOutputFormatter>();
        services.AddScoped<IOutputFormatter>(sp => sp.GetRequiredService<JsonOutputFormatter>());

        // Register caching based on configuration
        if (options.UseCaching)
        {
            if (options.UseDistributedCache && !string.IsNullOrEmpty(options.RedisConnectionString))
            {
                services.AddStackExchangeRedisCache(config =>
                    config.Configuration = options.RedisConnectionString);

                services.AddScoped<ICacheService>(sp =>
                    new DistributedCacheService(
                        sp.GetRequiredService<IDistributedCache>(),
                        sp.GetRequiredService<ILogger<DistributedCacheService>>(),
                        options.DefaultCacheExpiration));
            }
            else
            {
                services.AddSingleton<IMemoryCache, MemoryCache>();
                services.AddScoped<ICacheService>(sp =>
                    new MemoryCacheService(
                        sp.GetRequiredService<IMemoryCache>(),
                        sp.GetRequiredService<ILogger<MemoryCacheService>>(),
                        options.DefaultCacheExpiration));
            }
        }

        // Register event bus
        services.AddSingleton<IEventBus, EventBus>();

        // Register webhook handler
        services.AddScoped<IWebhookHandler, WebhookHandler>();
        services.AddHttpClient<WebhookHandler>();

        // Register HTTP client factory
        services.AddHttpClient();
        services.AddScoped<IHttpClientFactory, StandardHttpClientFactory>();

        // Register metrics
        services.AddSingleton<IWorkflowMetrics, WorkflowMetrics>();
        services.AddScoped<MetricsEndpoint>();

        // Register background job processor
        if (options.EnableBackgroundJobs)
        {
            services.AddHostedService<WorkflowJobProcessor>();
            services.AddScoped<IWorkflowJobProcessor>(sp =>
                sp.GetRequiredService<WorkflowJobProcessor>());
        }

        // Register filters
        services.AddScoped<ValidationFilter>();
        services.AddScoped<DataAnnotationValidationFilter>();

        return services;
    }

    /// <summary>
    /// Adds workflow engine middleware to the ASP.NET Core pipeline.
    /// Call this method in Program.cs after app.Build().
    /// </summary>
    public static WebApplication UseWorkflowEngine(
        this WebApplication app,
        WorkflowEngineMiddlewareOptions? options = null)
    {
        options ??= new WorkflowEngineMiddlewareOptions();

        // Add error handling middleware (should be early in pipeline)
        app.UseMiddleware<ErrorHandlingMiddleware>();

        // Add logging middleware if enabled
        if (options.EnableRequestLogging)
        {
            app.UseMiddleware<LoggingMiddleware>(
                options.LogRequestBody,
                options.LogResponseBody);
        }

        // Add rate limiting middleware if enabled
        if (options.EnableRateLimiting)
        {
            app.UseMiddleware<RateLimitingMiddleware>(
                new RateLimitConfig
                {
                    MaxRequests = options.RateLimit.MaxRequests,
                    WindowSeconds = options.RateLimit.WindowSeconds,
                    RetryAfterSeconds = options.RateLimit.RetryAfterSeconds
                });
        }

        // Add CORS if configured
        if (options.EnableCors)
        {
            app.UseCors("WorkflowEngineCors");
        }

        // Add HTTPS redirect if not in development
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        // Add authentication/authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Map controller routes
        app.MapControllers();

        // Health check endpoint
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
            .WithName("Health")
            .WithOpenApi()
            .AllowAnonymous();

        return app;
    }

    /// <summary>
    /// Adds CORS policy for workflow engine endpoints.
    /// </summary>
    public static IServiceCollection AddWorkflowEngineCors(
        this IServiceCollection services,
        string policyName = "WorkflowEngineCors")
    {
        services.AddCors(options =>
        {
            options.AddPolicy(policyName, builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        return services;
    }

    /// <summary>
    /// Adds authentication with JWT bearer tokens.
    /// </summary>
    public static IServiceCollection AddWorkflowEngineAuthentication(
        this IServiceCollection services,
        string jwtSecret)
    {
        var key = System.Text.Encoding.ASCII.GetBytes(jwtSecret);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "Bearer";
            options.DefaultChallengeScheme = "Bearer";
        })
        .AddJwtBearer("Bearer", options =>
        {
            options.TokenValidationParameters = new()
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false
            };
        });

        return services;
    }
}

/// <summary>
/// Configuration options for the workflow engine.
/// </summary>
public class WorkflowEngineOptions
{
    // Core engine options
    public string? ConnectionString { get; set; }
    public DotNetWorkflowEngine.Models.RetryPolicyConfig? DefaultRetryPolicy { get; set; }
    public bool EnableAuditLogging { get; set; } = true;
    public int MaxConcurrentWorkflows { get; set; } = 100;
    public int DefaultActivityTimeoutSeconds { get; set; } = 300;
    public bool ValidateWorkflowsOnLoad { get; set; } = true;

    // Infrastructure options
    public bool UseCaching { get; set; } = true;
    public bool UseDistributedCache { get; set; } = false;
    public string? RedisConnectionString { get; set; }
    public TimeSpan DefaultCacheExpiration { get; set; } = TimeSpan.FromHours(1);
    public bool EnableBackgroundJobs { get; set; } = true;
}

/// <summary>
/// Configuration options for middleware.
/// </summary>
public class WorkflowEngineMiddlewareOptions
{
    public bool EnableRequestLogging { get; set; } = true;
    public bool LogRequestBody { get; set; } = false;
    public bool LogResponseBody { get; set; } = false;
    public bool EnableRateLimiting { get; set; } = true;
    public RateLimitConfiguration RateLimit { get; set; } = new();
    public bool EnableCors { get; set; } = true;
}

/// <summary>
/// Rate limiting configuration.
/// </summary>
public class RateLimitConfiguration
{
    public int MaxRequests { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60;
    public int RetryAfterSeconds { get; set; } = 60;
}
