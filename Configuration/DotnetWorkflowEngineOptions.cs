// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using FluentValidation;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Configuration;

/// <summary>
/// Configuration options for the dotnet-workflow-engine.
/// Uses IOptions pattern for proper dependency injection and configuration management.
/// </summary>
public class DotnetWorkflowEngineOptions
{
    // Core engine configuration
    [Required]
    [MinLength(10)]
    public string ConnectionString { get; set; } = string.Empty;

    public RetryPolicyConfig? DefaultRetryPolicy { get; set; }

    [Required]
    public bool EnableAuditLogging { get; set; } = true;

    [Range(1, 1000)]
    public int MaxConcurrentWorkflows { get; set; } = 100;

    [Range(1, 86400)]
    public int DefaultActivityTimeoutSeconds { get; set; } = 300;

    public bool ValidateWorkflowsOnLoad { get; set; } = true;

    // Infrastructure configuration
    public bool EnableMetrics { get; set; } = true;
    public bool EnableBackgroundJobs { get; set; } = true;
    public bool EnableAuditTrail { get; set; } = true;

    // Caching configuration
    public bool CachingEnabled { get; set; } = true;
    public string CacheProvider { get; set; } = "Memory";
    public string? RedisConnectionString { get; set; }
    public TimeSpan DefaultCacheExpiration { get; set; } = TimeSpan.FromHours(1);
    public bool UseDistributedCache { get; set; } = false;

    // Middleware configuration
    public bool EnableRequestLogging { get; set; } = true;
    public bool LogRequestBody { get; set; } = false;
    public bool LogResponseBody { get; set; } = false;
    public bool EnableRateLimiting { get; set; } = true;
    public RateLimitConfig RateLimit { get; set; } = new();
    public bool EnableCors { get; set; } = true;

    // Security configuration
    public bool EnableWebhookValidation { get; set; } = true;
    public string? WebhookSecret { get; set; }
    public bool EnableActivityValidation { get; set; } = true;
    public bool EnableWorkflowValidation { get; set; } = true;

    // Expression evaluation
    public bool EnableExpressionEvaluation { get; set; } = true;
    [Range(1, 100)]
    public int MaxExpressionDepth { get; set; } = 20;
    [Range(1, 10000)]
    public int MaxWorkflowVariables { get; set; } = 1000;
    [Range(1, 200)]
    public int MaxWorkflowDepth { get; set; } = 50;

    // Execution configuration
    public string ExecutionMode { get; set; } = "Sequential";
    public bool EnableParallelExecution { get; set; } = true;
    [Range(1, 100)]
    public int MaxParallelActivities { get; set; } = 10;
    public bool EnableConditionalBranching { get; set; } = true;
    public bool EnableErrorRecovery { get; set; } = true;
    public bool EnableCircuitBreaker { get; set; } = true;
    public CircuitBreakerConfig CircuitBreaker { get; set; } = new();

    // Audit trail configuration
    public bool EnableImmutableAuditTrail { get; set; } = true;
    [Range(30, 3650)]
    public int AuditTrailRetentionDays { get; set; } = 365;

    // Health and monitoring
    public bool EnableHealthChecks { get; set; } = true;
    [Range(10, 3600)]
    public int HealthCheckIntervalSeconds { get; set; } = 30;
    public bool EnablePrometheusMetrics { get; set; } = false;
    [Range(1024, 65535)]
    public int MetricsPort { get; set; } = 9090;
}

/// <summary>
/// Rate limiting configuration.
/// </summary>
public class RateLimitConfig
{
    [Range(1, 10000)]
    public int MaxRequests { get; set; } = 100;

    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;

    [Range(1, 3600)]
    public int RetryAfterSeconds { get; set; } = 60;
}

/// <summary>
/// Circuit breaker configuration for resilience.
/// </summary>
public class CircuitBreakerConfig
{
    [Range(1, 100)]
    public int FailureThreshold { get; set; } = 5;

    [Range(10, 600)]
    public int SamplingDurationSeconds { get; set; } = 60;

    [Range(1, 100)]
    public int MinimumThroughput { get; set; } = 10;

    [Range(1, 600)]
    public int BreakDurationSeconds { get; set; } = 30;
}