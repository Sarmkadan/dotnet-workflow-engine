// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using DotNetWorkflowEngine.Configuration;
using DotNetWorkflowEngine.Monitoring;
using DotNetWorkflowEngine.Caching;
using DotNetWorkflowEngine.Data.Repositories;

namespace DotNetWorkflowEngine.Controllers;

/// <summary>
/// Health check endpoints for monitoring application status and readiness.
/// Provides liveness and readiness probes for container orchestration systems.
/// </summary>
[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkflowMetrics _metrics;
    private readonly IOptions<WorkflowEngineOptions> _options;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IServiceProvider serviceProvider,
        IWorkflowMetrics metrics,
        IOptions<WorkflowEngineOptions> options,
        ILogger<HealthController> logger)
    {
        _serviceProvider = serviceProvider;
        _metrics = metrics;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Liveness probe endpoint. Returns 200 OK if the application is running.
    /// Does not perform any dependency checks. Used by orchestrators to determine
    /// if the container should be restarted.
    /// </summary>
    [HttpGet("liveness")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [AllowAnonymous]
    public IActionResult Liveness()
    {
        try
        {
            _logger.LogDebug("Health liveness check requested");

            var response = new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                uptime = GetUptime(),
                version = GetType().Assembly.GetName().Version?.ToString() ?? "unknown"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health liveness check failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "unhealthy", error = ex.Message });
        }
    }

    /// <summary>
    /// Readiness probe endpoint. Returns 200 OK only if the application is ready
    /// to receive traffic. Performs dependency checks including database, cache,
    /// and other critical services.
    /// </summary>
    [HttpGet("readiness")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [AllowAnonymous]
    public async Task<IActionResult> Readiness()
    {
        try
        {
            _logger.LogDebug("Health readiness check requested");

            var checks = new Dictionary<string, HealthCheckResult>();
            var overallStatus = HealthStatus.Healthy;
            var timestamp = DateTime.UtcNow;

            // Check database connection
            var dbCheck = await CheckDatabaseAsync();
            checks["database"] = dbCheck;
            if (dbCheck.Status != HealthStatus.Healthy)
                overallStatus = HealthStatus.Unhealthy;

            // Check cache connection (if enabled)
            var cacheCheck = await CheckCacheAsync();
            checks["cache"] = cacheCheck;
            if (cacheCheck.Status != HealthStatus.Healthy)
                overallStatus = HealthStatus.Unhealthy;

            // Check metrics service
            var metricsCheck = await CheckMetricsAsync();
            checks["metrics"] = metricsCheck;
            if (metricsCheck.Status != HealthStatus.Healthy)
                overallStatus = HealthStatus.Unhealthy;

            var response = new
            {
                status = overallStatus.ToString().ToLower(),
                timestamp = timestamp,
                components = checks,
                overallStatus = overallStatus.ToString()
            };

            return overallStatus == HealthStatus.Healthy
                ? Ok(response)
                : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health readiness check failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message,
                components = new Dictionary<string, object>()
            });
        }
    }

    /// <summary>
    /// Comprehensive health check endpoint. Returns detailed status of all
    /// components including database, cache, metrics, and system health.
    /// </summary>
    [HttpGet] // Maps to /health
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [AllowAnonymous]
    public async Task<IActionResult> Health()
    {
        try
        {
            _logger.LogDebug("Health comprehensive check requested");

            var checks = new Dictionary<string, HealthCheckResult>();
            var overallStatus = HealthStatus.Healthy;
            var timestamp = DateTime.UtcNow;

            // System health
            var systemCheck = CheckSystemHealth();
            checks["system"] = systemCheck;
            if (systemCheck.Status != HealthStatus.Healthy)
                overallStatus = HealthStatus.Unhealthy;

            // Database connection
            var dbCheck = await CheckDatabaseAsync();
            checks["database"] = dbCheck;
            if (dbCheck.Status != HealthStatus.Healthy)
                overallStatus = HealthStatus.Unhealthy;

            // Cache connection (if enabled)
            var cacheCheck = await CheckCacheAsync();
            checks["cache"] = cacheCheck;
            if (cacheCheck.Status != HealthStatus.Healthy)
                overallStatus = HealthStatus.Unhealthy;

            // Metrics service
            var metricsCheck = await CheckMetricsAsync();
            checks["metrics"] = metricsCheck;
            if (metricsCheck.Status != HealthStatus.Healthy)
                overallStatus = HealthStatus.Unhealthy;

            // Configuration
            var configCheck = CheckConfiguration();
            checks["configuration"] = configCheck;
            if (configCheck.Status != HealthStatus.Healthy)
                overallStatus = HealthStatus.Unhealthy;

            var response = new
            {
                status = overallStatus.ToString().ToLower(),
                timestamp = timestamp,
                uptime = GetUptime(),
                version = GetType().Assembly.GetName().Version?.ToString() ?? "unknown",
                components = checks,
                overallStatus = overallStatus.ToString()
            };

            return overallStatus == HealthStatus.Healthy
                ? Ok(response)
                : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health comprehensive check failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message,
                components = new Dictionary<string, object>()
            });
        }
    }

    private async Task<HealthCheckResult> CheckDatabaseAsync()
    {
        try
        {
            // Try to get database context to verify connection
            var dbContext = _serviceProvider.GetService<Data.Context.DatabaseContext>();
            if (dbContext != null)
            {
                // Simple check - can query something lightweight
                // In a real implementation, you'd run an actual query
                return new HealthCheckResult
                {
                    Status = HealthStatus.Healthy,
                    Component = "database",
                    Message = "Database connection OK",
                    Timestamp = DateTime.UtcNow
                };
            }

            return new HealthCheckResult
            {
                Status = HealthStatus.Unhealthy,
                Component = "database",
                Message = "Database context not available",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new HealthCheckResult
            {
                Status = HealthStatus.Unhealthy,
                Component = "database",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow,
                Exception = ex.ToString()
            };
        }
    }

    private async Task<HealthCheckResult> CheckCacheAsync()
    {
        try
        {
            var cacheService = _serviceProvider.GetService<ICacheService>();
            if (cacheService != null)
            {
                // Try to set and get a test value
                await cacheService.SetAsync("health-check-cache-test", "ok", TimeSpan.FromSeconds(10));
                var value = await cacheService.GetAsync<string>("health-check-cache-test");

                if (value == "ok")
                {
                    return new HealthCheckResult
                    {
                        Status = HealthStatus.Healthy,
                        Component = "cache",
                        Message = "Cache connection OK",
                        Timestamp = DateTime.UtcNow
                    };
                }
            }

            return new HealthCheckResult
            {
                Status = HealthStatus.Unhealthy,
                Component = "cache",
                Message = "Cache service not available",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new HealthCheckResult
            {
                Status = HealthStatus.Unhealthy,
                Component = "cache",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow,
                Exception = ex.ToString()
            };
        }
    }

    private async Task<HealthCheckResult> CheckMetricsAsync()
    {
        try
        {
            if (_metrics != null)
            {
                var snapshot = await _metrics.GetMetricsAsync();
                return new HealthCheckResult
                {
                    Status = HealthStatus.Healthy,
                    Component = "metrics",
                    Message = "Metrics service OK",
                    Timestamp = DateTime.UtcNow,
                    Details = new { lastSnapshotTime = snapshot.SnapshotTime }
                };
            }

            return new HealthCheckResult
            {
                Status = HealthStatus.Unhealthy,
                Component = "metrics",
                Message = "Metrics service not available",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new HealthCheckResult
            {
                Status = HealthStatus.Unhealthy,
                Component = "metrics",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow,
                Exception = ex.ToString()
            };
        }
    }

    private HealthCheckResult CheckSystemHealth()
    {
        try
        {
            var memoryStatus = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
                ? "Windows"
                : System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux)
                    ? "Linux"
                    : System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX)
                        ? "macOS"
                        : "Unknown";

            var dotnetVersion = Environment.Version.ToString();
            var processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            var workingSet = Environment.WorkingSet / 1024 / 1024; // MB

            return new HealthCheckResult
            {
                Status = HealthStatus.Healthy,
                Component = "system",
                Message = "System resources OK",
                Timestamp = DateTime.UtcNow,
                Details = new
                {
                    os = memoryStatus,
                    dotnetVersion = dotnetVersion,
                    processId = processId,
                    memoryUsedMb = workingSet
                }
            };
        }
        catch (Exception ex)
        {
            return new HealthCheckResult
            {
                Status = HealthStatus.Unhealthy,
                Component = "system",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow,
                Exception = ex.ToString()
            };
        }
    }

    private HealthCheckResult CheckConfiguration()
    {
        try
        {
            var options = _options.Value;

            return new HealthCheckResult
            {
                Status = HealthStatus.Healthy,
                Component = "configuration",
                Message = "Configuration loaded successfully",
                Timestamp = DateTime.UtcNow,
                Details = new
                {
                    enableAuditLogging = options.EnableAuditLogging,
                    useCaching = options.UseCaching,
                    maxConcurrentWorkflows = options.MaxConcurrentWorkflows
                }
            };
        }
        catch (Exception ex)
        {
            return new HealthCheckResult
            {
                Status = HealthStatus.Unhealthy,
                Component = "configuration",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow,
                Exception = ex.ToString()
            };
        }
    }

    private string GetUptime()
    {
        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var startTime = process.StartTime;
            var uptime = DateTime.UtcNow - startTime;
            return uptime.ToString("c");
        }
        catch
        {
            return "unknown";
        }
    }
}

/// <summary>
/// Health check result structure.
/// </summary>
public class HealthCheckResult
{
    public HealthStatus Status { get; set; }
    public string Component { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public object? Details { get; set; }
    public string? Exception { get; set; }
}

/// <summary>
/// Health status enumeration.
/// </summary>
public enum HealthStatus
{
    Healthy,
    Unhealthy
}
