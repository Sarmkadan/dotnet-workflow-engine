// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotNetWorkflowEngine.Monitoring;

/// <summary>
/// Metrics collection and reporting for workflow engine.
/// Tracks execution statistics, error rates, performance metrics,
/// and operational health indicators for monitoring dashboards.
/// </summary>
public interface IWorkflowMetrics
{
    void RecordWorkflowExecution(string workflowId, long durationMs, bool success);
    void RecordActivityExecution(string activityId, long durationMs, bool success);
    void RecordError(string errorType, string? details = null);
    Task<WorkflowMetricsSnapshot> GetMetricsAsync();
    void Reset();
}

/// <summary>
/// Snapshot of current metrics at a point in time.
/// </summary>
public class WorkflowMetricsSnapshot
{
    public int TotalWorkflowsExecuted { get; set; }
    public int SuccessfulWorkflows { get; set; }
    public int FailedWorkflows { get; set; }
    public double SuccessRate { get; set; }
    public long AverageWorkflowDurationMs { get; set; }
    public long MinWorkflowDurationMs { get; set; }
    public long MaxWorkflowDurationMs { get; set; }
    public int TotalActivitiesExecuted { get; set; }
    public int SuccessfulActivities { get; set; }
    public int FailedActivities { get; set; }
    public long AverageActivityDurationMs { get; set; }
    public Dictionary<string, int> ErrorCount { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public DateTime SnapshotTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Implementation of workflow metrics using in-memory storage.
/// Suitable for single-node deployments; use external metrics service for distributed scenarios.
/// </summary>
public class WorkflowMetrics : IWorkflowMetrics
{
    private readonly ILogger<WorkflowMetrics> _logger;
    private int _totalWorkflowsExecuted;
    private int _successfulWorkflows;
    private int _failedWorkflows;
    private long _totalWorkflowDurationMs;
    private long _minWorkflowDurationMs = long.MaxValue;
    private long _maxWorkflowDurationMs;

    private int _totalActivitiesExecuted;
    private int _successfulActivities;
    private int _failedActivities;
    private long _totalActivityDurationMs;
    private long _minActivityDurationMs = long.MaxValue;
    private long _maxActivityDurationMs;

    private readonly Dictionary<string, int> _errorCounts = new();
    private readonly object _lock = new();

    public WorkflowMetrics(ILogger<WorkflowMetrics> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Records workflow execution metrics.
    /// </summary>
    public void RecordWorkflowExecution(string workflowId, long durationMs, bool success)
    {
        lock (_lock)
        {
            _totalWorkflowsExecuted++;

            if (success)
                _successfulWorkflows++;
            else
                _failedWorkflows++;

            _totalWorkflowDurationMs += durationMs;
            _minWorkflowDurationMs = Math.Min(_minWorkflowDurationMs, durationMs);
            _maxWorkflowDurationMs = Math.Max(_maxWorkflowDurationMs, durationMs);

            _logger.LogDebug(
                "Workflow executed: {WorkflowId}, Duration: {DurationMs}ms, Success: {Success}",
                workflowId,
                durationMs,
                success);
        }
    }

    /// <summary>
    /// Records activity execution metrics.
    /// </summary>
    public void RecordActivityExecution(string activityId, long durationMs, bool success)
    {
        lock (_lock)
        {
            _totalActivitiesExecuted++;

            if (success)
                _successfulActivities++;
            else
                _failedActivities++;

            _totalActivityDurationMs += durationMs;
            _minActivityDurationMs = Math.Min(_minActivityDurationMs, durationMs);
            _maxActivityDurationMs = Math.Max(_maxActivityDurationMs, durationMs);

            _logger.LogDebug(
                "Activity executed: {ActivityId}, Duration: {DurationMs}ms, Success: {Success}",
                activityId,
                durationMs,
                success);
        }
    }

    /// <summary>
    /// Records an error occurrence.
    /// </summary>
    public void RecordError(string errorType, string? details = null)
    {
        lock (_lock)
        {
            if (!_errorCounts.ContainsKey(errorType))
                _errorCounts[errorType] = 0;

            _errorCounts[errorType]++;

            _logger.LogWarning(
                "Error recorded: {ErrorType}. Count: {Count}. Details: {Details}",
                errorType,
                _errorCounts[errorType],
                details ?? "none");
        }
    }

    /// <summary>
    /// Gets a snapshot of current metrics.
    /// </summary>
    public Task<WorkflowMetricsSnapshot> GetMetricsAsync()
    {
        lock (_lock)
        {
            var snapshot = new WorkflowMetricsSnapshot
            {
                TotalWorkflowsExecuted = _totalWorkflowsExecuted,
                SuccessfulWorkflows = _successfulWorkflows,
                FailedWorkflows = _failedWorkflows,
                SuccessRate = _totalWorkflowsExecuted > 0
                    ? Math.Round((double)_successfulWorkflows / _totalWorkflowsExecuted * 100, 2)
                    : 0,
                AverageWorkflowDurationMs = _totalWorkflowsExecuted > 0
                    ? _totalWorkflowDurationMs / _totalWorkflowsExecuted
                    : 0,
                MinWorkflowDurationMs = _minWorkflowDurationMs == long.MaxValue ? 0 : _minWorkflowDurationMs,
                MaxWorkflowDurationMs = _maxWorkflowDurationMs,

                TotalActivitiesExecuted = _totalActivitiesExecuted,
                SuccessfulActivities = _successfulActivities,
                FailedActivities = _failedActivities,
                AverageActivityDurationMs = _totalActivitiesExecuted > 0
                    ? _totalActivityDurationMs / _totalActivitiesExecuted
                    : 0,

                ErrorCount = new Dictionary<string, int>(_errorCounts),
                SnapshotTime = DateTime.UtcNow
            };

            return Task.FromResult(snapshot);
        }
    }

    /// <summary>
    /// Resets all metrics to zero. Useful for testing or periodic resets.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _totalWorkflowsExecuted = 0;
            _successfulWorkflows = 0;
            _failedWorkflows = 0;
            _totalWorkflowDurationMs = 0;
            _minWorkflowDurationMs = long.MaxValue;
            _maxWorkflowDurationMs = 0;

            _totalActivitiesExecuted = 0;
            _successfulActivities = 0;
            _failedActivities = 0;
            _totalActivityDurationMs = 0;
            _minActivityDurationMs = long.MaxValue;
            _maxActivityDurationMs = 0;

            _errorCounts.Clear();

            _logger.LogInformation("Metrics reset");
        }
    }
}

/// <summary>
/// Metrics endpoint controller for exposing metrics to monitoring systems.
/// </summary>
public class MetricsEndpoint
{
    private readonly IWorkflowMetrics _metrics;
    private readonly ILogger<MetricsEndpoint> _logger;

    public MetricsEndpoint(IWorkflowMetrics metrics, ILogger<MetricsEndpoint> logger)
    {
        _metrics = metrics;
        _logger = logger;
    }

    /// <summary>
    /// Exposes metrics in Prometheus format for scraping by monitoring systems.
    /// </summary>
    public async Task<string> GetPrometheusMetricsAsync()
    {
        var snapshot = await _metrics.GetMetricsAsync();
        var lines = new List<string>
        {
            "# HELP workflow_executions_total Total number of workflow executions",
            "# TYPE workflow_executions_total counter",
            $"workflow_executions_total{{{GetLabels()}}} {snapshot.TotalWorkflowsExecuted}",

            "# HELP workflow_successes_total Total successful workflow executions",
            "# TYPE workflow_successes_total counter",
            $"workflow_successes_total{{{GetLabels()}}} {snapshot.SuccessfulWorkflows}",

            "# HELP workflow_failures_total Total failed workflow executions",
            "# TYPE workflow_failures_total counter",
            $"workflow_failures_total{{{GetLabels()}}} {snapshot.FailedWorkflows}",

            "# HELP workflow_duration_ms_avg Average workflow execution duration",
            "# TYPE workflow_duration_ms_avg gauge",
            $"workflow_duration_ms_avg{{{GetLabels()}}} {snapshot.AverageWorkflowDurationMs}",

            "# HELP activity_executions_total Total number of activity executions",
            "# TYPE activity_executions_total counter",
            $"activity_executions_total{{{GetLabels()}}} {snapshot.TotalActivitiesExecuted}",

            "# HELP activity_duration_ms_avg Average activity execution duration",
            "# TYPE activity_duration_ms_avg gauge",
            $"activity_duration_ms_avg{{{GetLabels()}}} {snapshot.AverageActivityDurationMs}"
        };

        return string.Join("\n", lines);
    }

    private string GetLabels()
    {
        return "instance=\"dotnet-workflow-engine\"";
    }
}
