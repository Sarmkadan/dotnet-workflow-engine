// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentValidation;

namespace DotNetWorkflowEngine.Configuration;

/// <summary>
/// Validates DotnetWorkflowEngineOptions using FluentValidation.
/// Provides comprehensive validation for all configuration properties.
/// </summary>
public class DotnetWorkflowEngineOptionsValidator : AbstractValidator<DotnetWorkflowEngineOptions>
{
    public DotnetWorkflowEngineOptionsValidator()
    {
        // Connection string validation
        RuleFor(x => x.ConnectionString)
            .NotEmpty().WithMessage("Connection string is required")
            .MinimumLength(10).WithMessage("Connection string must be at least 10 characters long");

        // Core engine validations
        RuleFor(x => x.MaxConcurrentWorkflows)
            .InclusiveBetween(1, 1000).WithMessage("MaxConcurrentWorkflows must be between 1 and 1000");

        RuleFor(x => x.DefaultActivityTimeoutSeconds)
            .InclusiveBetween(1, 86400).WithMessage("DefaultActivityTimeoutSeconds must be between 1 and 86400");

        RuleFor(x => x.MaxExpressionDepth)
            .InclusiveBetween(1, 100).WithMessage("MaxExpressionDepth must be between 1 and 100");

        RuleFor(x => x.MaxWorkflowVariables)
            .InclusiveBetween(1, 10000).WithMessage("MaxWorkflowVariables must be between 1 and 10000");

        RuleFor(x => x.MaxWorkflowDepth)
            .InclusiveBetween(1, 200).WithMessage("MaxWorkflowDepth must be between 1 and 200");

        RuleFor(x => x.MaxParallelActivities)
            .InclusiveBetween(1, 100).WithMessage("MaxParallelActivities must be between 1 and 100");

        RuleFor(x => x.AuditTrailRetentionDays)
            .InclusiveBetween(30, 3650).WithMessage("AuditTrailRetentionDays must be between 30 and 3650");

        RuleFor(x => x.HealthCheckIntervalSeconds)
            .InclusiveBetween(10, 3600).WithMessage("HealthCheckIntervalSeconds must be between 10 and 3600");

        RuleFor(x => x.MetricsPort)
            .InclusiveBetween(1024, 65535).WithMessage("MetricsPort must be between 1024 and 65535");

        // Rate limit validations
        RuleFor(x => x.RateLimit)
            .SetValidator(new RateLimitConfigValidator());

        // Circuit breaker validations
        RuleFor(x => x.CircuitBreaker)
            .SetValidator(new CircuitBreakerConfigValidator());

        // Cache provider validation
        RuleFor(x => x.CacheProvider)
            .Must(x => x.Equals("Memory", StringComparison.OrdinalIgnoreCase) ||
                   x.Equals("Redis", StringComparison.OrdinalIgnoreCase))
            .WithMessage("CacheProvider must be either 'Memory' or 'Redis'");

        // Execution mode validation
        RuleFor(x => x.ExecutionMode)
            .Must(x => x.Equals("Sequential", StringComparison.OrdinalIgnoreCase) ||
                   x.Equals("Parallel", StringComparison.OrdinalIgnoreCase))
            .WithMessage("ExecutionMode must be either 'Sequential' or 'Parallel'");

        // Redis connection string validation when distributed cache is enabled
        RuleFor(x => x)
            .Must(x => !x.UseDistributedCache || !string.IsNullOrEmpty(x.RedisConnectionString))
            .When(x => x.UseDistributedCache)
            .WithMessage("RedisConnectionString is required when UseDistributedCache is true");
    }
}

/// <summary>
/// Validates RateLimitConfig.
/// </summary>
public class RateLimitConfigValidator : AbstractValidator<RateLimitConfig>
{
    public RateLimitConfigValidator()
    {
        RuleFor(x => x.MaxRequests)
            .InclusiveBetween(1, 10000).WithMessage("MaxRequests must be between 1 and 10000");

        RuleFor(x => x.WindowSeconds)
            .InclusiveBetween(1, 3600).WithMessage("WindowSeconds must be between 1 and 3600");

        RuleFor(x => x.RetryAfterSeconds)
            .InclusiveBetween(1, 3600).WithMessage("RetryAfterSeconds must be between 1 and 3600");
    }
}

/// <summary>
/// Validates CircuitBreakerConfig.
/// </summary>
public class CircuitBreakerConfigValidator : AbstractValidator<CircuitBreakerConfig>
{
    public CircuitBreakerConfigValidator()
    {
        RuleFor(x => x.FailureThreshold)
            .InclusiveBetween(1, 100).WithMessage("FailureThreshold must be between 1 and 100");

        RuleFor(x => x.SamplingDurationSeconds)
            .InclusiveBetween(10, 600).WithMessage("SamplingDurationSeconds must be between 10 and 600");

        RuleFor(x => x.MinimumThroughput)
            .InclusiveBetween(1, 100).WithMessage("MinimumThroughput must be between 1 and 100");

        RuleFor(x => x.BreakDurationSeconds)
            .InclusiveBetween(1, 600).WithMessage("BreakDurationSeconds must be between 1 and 600");
    }
}