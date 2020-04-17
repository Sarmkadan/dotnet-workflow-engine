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
using Microsoft.Extensions.Options;
using FluentValidation;

namespace DotNetWorkflowEngine.Configuration;

/// <summary>
/// Dependency injection and service registration extensions for the workflow engine.
/// Centralizes configuration of all services, middleware, and filters.
/// Call AddWorkflowEngine() during startup to register all services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all workflow engine services into the DI container with IOptions pattern.
    /// Call this method in Program.cs during application startup.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional configuration action</param>
    /// <returns>The configured service collection</returns>
    public static IServiceCollection AddWorkflowEngine(
        this IServiceCollection services,
        Action<DotnetWorkflowEngineOptions>? configureOptions = null)
    {
        // Configure options with validation
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.AddOptions<DotnetWorkflowEngineOptions>()
                .ValidateOnStart();
        }

        // Register formatters
        services.AddSingleton<JsonOutputFormatter>();
        services.AddSingleton<CsvOutputFormatter>();
        services.AddScoped<IOutputFormatter>(sp => sp.GetRequiredService<JsonOutputFormatter>());

        // Register caching based on configuration
        services.AddScoped<ICacheService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DotnetWorkflowEngineOptions>>().Value;

            if (options.CachingEnabled)
            {
                if (options.UseDistributedCache && !string.IsNullOrEmpty(options.RedisConnectionString))
                {
                    services.AddStackExchangeRedisCache(config =>
                        config.Configuration = options.RedisConnectionString);

                    return new DistributedCacheService(
                        sp.GetRequiredService<IDistributedCache>(),
                        sp.GetRequiredService<ILogger<DistributedCacheService>>(),
                        options.DefaultCacheExpiration);
                }
                else
                {
                    services.AddSingleton<IMemoryCache, MemoryCache>();
                    return new MemoryCacheService(
                        sp.GetRequiredService<IMemoryCache>(),
                        sp.GetRequiredService<ILogger<MemoryCacheService>>(),
                        options.DefaultCacheExpiration);
                }
            }

            // Return a no-op cache service if caching is disabled
            return new NoOpCacheService();
        });

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
        services.AddHostedService<WorkflowJobProcessor>();
        services.AddScoped<IWorkflowJobProcessor>(sp => sp.GetRequiredService<WorkflowJobProcessor>());

        // Register filters
        services.AddScoped<ValidationFilter>();
        services.AddScoped<DataAnnotationValidationFilter>();

        // Register options validator
        services.AddSingleton<IValidator<DotnetWorkflowEngineOptions>, DotnetWorkflowEngineOptionsValidator>();

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
