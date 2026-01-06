// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotNetWorkflowEngine.Examples;

/// <summary>
/// Example: Monitoring, metrics, and health checks for workflow engine.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MonitoringExample : ControllerBase
{
    private readonly IWorkflowExecutionService _executionService;
    private readonly IAuditService _auditService;
    private readonly ILogger<MonitoringExample> _logger;

    public MonitoringExample(
        IWorkflowExecutionService executionService,
        IAuditService auditService,
        ILogger<MonitoringExample> logger)
    {
        _executionService = executionService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Get workflow engine metrics and statistics.
    /// </summary>
    [HttpGet("metrics")]
    public async Task<ActionResult> GetMetrics([FromQuery] string? period = "today")
    {
        try
        {
            var startDate = period switch
            {
                "week" => DateTime.UtcNow.AddDays(-7),
                "month" => DateTime.UtcNow.AddDays(-30),
                "quarter" => DateTime.UtcNow.AddDays(-90),
                "year" => DateTime.UtcNow.AddDays(-365),
                _ => DateTime.UtcNow.AddDays(-1)
            };

            var instances = await _executionService.GetInstancesByDateRangeAsync(
                startDate,
                DateTime.UtcNow
            );

            var metrics = new
            {
                period = period,
                startDate = startDate,
                endDate = DateTime.UtcNow,
                workflowMetrics = new
                {
                    totalInstances = instances.Count(),
                    completedInstances = instances.Count(x => x.Status == WorkflowStatus.Completed),
                    runningInstances = instances.Count(x => x.Status == WorkflowStatus.Running),
                    failedInstances = instances.Count(x => x.Status == WorkflowStatus.Failed),
                    completionRate = instances.Any()
                        ? (double)instances.Count(x => x.Status == WorkflowStatus.Completed) / instances.Count() * 100
                        : 0
                },
                performanceMetrics = new
                {
                    averageExecutionTimeMs = instances.Any()
                        ? instances
                            .Where(x => x.CompletedAt.HasValue)
                            .Average(x => (x.CompletedAt.Value - x.StartedAt).TotalMilliseconds)
                        : 0,
                    minExecutionTimeMs = instances.Any(x => x.CompletedAt.HasValue)
                        ? instances
                            .Where(x => x.CompletedAt.HasValue)
                            .Min(x => (x.CompletedAt.Value - x.StartedAt).TotalMilliseconds)
                        : 0,
                    maxExecutionTimeMs = instances.Any(x => x.CompletedAt.HasValue)
                        ? instances
                            .Where(x => x.CompletedAt.HasValue)
                            .Max(x => (x.CompletedAt.Value - x.StartedAt).TotalMilliseconds)
                        : 0
                },
                successRate = instances.Any()
                    ? (double)instances.Count(x => x.Status == WorkflowStatus.Completed) / instances.Count() * 100
                    : 0
            };

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating metrics");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get health status of all system components.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(200)]
    [ProducesResponseType(503)]
    public async Task<ActionResult> GetHealthStatus()
    {
        try
        {
            var health = new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                components = new
                {
                    database = await CheckDatabaseHealthAsync(),
                    cache = await CheckCacheHealthAsync(),
                    hangfire = await CheckHangfireHealthAsync(),
                    api = "Healthy"
                }
            };

            var isHealthy = health.components.GetType()
                .GetProperties()
                .All(p => p.GetValue(health.components)?.ToString().Contains("Healthy") ?? false);

            if (!isHealthy)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, health);
            }

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health");
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { status = "Unhealthy", error = ex.Message }
            );
        }
    }

    private async Task<string> CheckDatabaseHealthAsync()
    {
        try
        {
            await _executionService.GetInstanceAsync(Guid.Empty);
            return "Healthy";
        }
        catch
        {
            return "Unhealthy";
        }
    }

    private async Task<string> CheckCacheHealthAsync()
    {
        try
        {
            // Simulate cache check
            await Task.Delay(10);
            return "Healthy";
        }
        catch
        {
            return "Unhealthy";
        }
    }

    private async Task<string> CheckHangfireHealthAsync()
    {
        try
        {
            // Simulate Hangfire check
            await Task.Delay(10);
            return "Healthy";
        }
        catch
        {
            return "Unhealthy";
        }
    }

    /// <summary>
    /// Get audit trail statistics.
    /// </summary>
    [HttpGet("audit-stats")]
    public async Task<ActionResult> GetAuditStatistics([FromQuery] string? period = "today")
    {
        try
        {
            var startDate = period switch
            {
                "week" => DateTime.UtcNow.AddDays(-7),
                "month" => DateTime.UtcNow.AddDays(-30),
                _ => DateTime.UtcNow.AddDays(-1)
            };

            var auditLogs = await _auditService.GetAuditLogsAsync(
                startDate: startDate,
                endDate: DateTime.UtcNow
            );

            var stats = new
            {
                period = period,
                totalEntries = auditLogs.Count(),
                entriesByAction = auditLogs
                    .GroupBy(x => x.Action)
                    .Select(g => new { action = g.Key, count = g.Count() })
                    .ToList(),
                entriesByEntityType = auditLogs
                    .GroupBy(x => x.EntityType)
                    .Select(g => new { entityType = g.Key, count = g.Count() })
                    .ToList(),
                topUsers = auditLogs
                    .GroupBy(x => x.UserId)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => new { userId = g.Key, actions = g.Count() })
                    .ToList()
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit statistics");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get performance trends over time.
    /// </summary>
    [HttpGet("performance-trends")]
    public async Task<ActionResult> GetPerformanceTrends([FromQuery] int? days = 7)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-(days ?? 7));
            var instances = await _executionService.GetInstancesByDateRangeAsync(
                startDate,
                DateTime.UtcNow
            );

            var trends = instances
                .Where(x => x.CompletedAt.HasValue)
                .GroupBy(x => x.StartedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    date = g.Key,
                    instanceCount = g.Count(),
                    averageExecutionTimeMs = g.Average(
                        x => (x.CompletedAt.Value - x.StartedAt).TotalMilliseconds
                    ),
                    successRate = (double)g.Count(x => x.Status == WorkflowStatus.Completed)
                        / g.Count() * 100
                })
                .ToList();

            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance trends");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get slowest workflows.
    /// </summary>
    [HttpGet("slow-workflows")]
    public async Task<ActionResult> GetSlowestWorkflows([FromQuery] int? limit = 10)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-7);
            var instances = await _executionService.GetInstancesByDateRangeAsync(
                startDate,
                DateTime.UtcNow
            );

            var slowest = instances
                .Where(x => x.CompletedAt.HasValue)
                .OrderByDescending(x => (x.CompletedAt.Value - x.StartedAt).TotalSeconds)
                .Take(limit ?? 10)
                .Select(x => new
                {
                    instanceId = x.Id,
                    executionTimeSeconds = (x.CompletedAt.Value - x.StartedAt).TotalSeconds,
                    status = x.Status,
                    startedAt = x.StartedAt,
                    completedAt = x.CompletedAt
                })
                .ToList();

            return Ok(slowest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting slowest workflows");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get failed workflows summary.
    /// </summary>
    [HttpGet("failed-workflows")]
    public async Task<ActionResult> GetFailedWorkflows([FromQuery] int? days = 7)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-(days ?? 7));
            var instances = await _executionService.GetInstancesByDateRangeAsync(
                startDate,
                DateTime.UtcNow
            );

            var failed = instances
                .Where(x => x.Status == WorkflowStatus.Failed)
                .GroupBy(x => x.CurrentActivityId)
                .Select(g => new
                {
                    activityId = g.Key,
                    failureCount = g.Count(),
                    failureRate = (double)g.Count()
                        / instances.Count() * 100
                })
                .OrderByDescending(x => x.failureCount)
                .ToList();

            return Ok(new
            {
                period = $"{days} days",
                totalFailed = instances.Count(x => x.Status == WorkflowStatus.Failed),
                failuresByActivity = failed
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting failed workflows");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get system resource usage estimates.
    /// </summary>
    [HttpGet("resource-usage")]
    public ActionResult GetResourceUsage()
    {
        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();

            return Ok(new
            {
                timestamp = DateTime.UtcNow,
                memory = new
                {
                    workingSetMb = process.WorkingSet64 / 1024 / 1024,
                    privateMemoryMb = process.PrivateMemorySize64 / 1024 / 1024
                },
                cpu = new
                {
                    userProcessorTime = process.UserProcessorTime.TotalSeconds,
                    totalProcessorTime = process.TotalProcessorTime.TotalSeconds
                },
                handles = process.HandleCount,
                threads = process.Threads.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resource usage");
            return BadRequest(new { error = ex.Message });
        }
    }
}
