// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
    public long TotalWorkflowsExecuted { get; set; }
    public long SuccessfulWorkflows { get; set; }
    public long FailedWorkflows { get; set; }
    public double SuccessRate { get; set; }
    public long AverageWorkflowDurationMs { get; set; }
    public long MinWorkflowDurationMs { get; set; }
    public long MaxWorkflowDurationMs { get; set; }
    public long TotalActivitiesExecuted { get; set; }
    public long SuccessfulActivities { get; set; }
    public long FailedActivities { get; set; }
    public long AverageActivityDurationMs { get; set; }
    public Dictionary<string, long> ErrorCount { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public DateTime SnapshotTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Implementation of workflow metrics using in-memory storage.
/// Suitable for single-node deployments; use external metrics service for distributed scenarios.
/// </summary>
public class WorkflowMetrics : IWorkflowMetrics
{
    private class AtomicCounter
    {
        public long Value;
        public long Increment() => Interlocked.Increment(ref Value);
        public void Reset() => Interlocked.Exchange(ref Value, 0);
    }

    private readonly ILogger<WorkflowMetrics> _logger;
    private long _totalWorkflowsExecuted;
    private long _successfulWorkflows;
    private long _failedWorkflows;
    private long _totalWorkflowDurationMs;
    private long _minWorkflowDurationMs = long.MaxValue;
    private long _maxWorkflowDurationMs;

    private long _totalActivitiesExecuted;
    private long _successfulActivities;
    private long _failedActivities;
    private long _totalActivityDurationMs;
    private long _minActivityDurationMs = long.MaxValue;
    private long _maxActivityDurationMs;

    private readonly ConcurrentDictionary<string, AtomicCounter> _errorCounts = new();
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
        Interlocked.Increment(ref _totalWorkflowsExecuted);

        if (success)
            Interlocked.Increment(ref _successfulWorkflows);
        else
            Interlocked.Increment(ref _failedWorkflows);

        Interlocked.Add(ref _totalWorkflowDurationMs, durationMs);
        
        UpdateMin(ref _minWorkflowDurationMs, durationMs);
        UpdateMax(ref _maxWorkflowDurationMs, durationMs);

        _logger.LogDebug(
            "Workflow executed: {WorkflowId}, Duration: {DurationMs}ms, Success: {Success}",
            workflowId,
            durationMs,
            success);
    }

    /// <summary>
    /// Records activity execution metrics.
    /// </summary>
    public void RecordActivityExecution(string activityId, long durationMs, bool success)
    {
        Interlocked.Increment(ref _totalActivitiesExecuted);

        if (success)
            Interlocked.Increment(ref _successfulActivities);
        else
            Interlocked.Increment(ref _failedActivities);

        Interlocked.Add(ref _totalActivityDurationMs, durationMs);
        
        UpdateMin(ref _minActivityDurationMs, durationMs);
        UpdateMax(ref _maxActivityDurationMs, durationMs);

        _logger.LogDebug(
            "Activity executed: {ActivityId}, Duration: {DurationMs}ms, Success: {Success}",
            activityId,
            durationMs,
            success);
    }

    /// <summary>
    /// Records an error occurrence.
    /// </summary>
    public void RecordError(string errorType, string? details = null)
    {
        var counter = _errorCounts.GetOrAdd(errorType, _ => new AtomicCounter());
        long newCount = counter.Increment();

        _logger.LogWarning(
            "Error recorded: {ErrorType}. Count: {Count}. Details: {Details}",
            errorType,
            newCount,
            details ?? "none");
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
                TotalWorkflowsExecuted = Interlocked.Read(ref _totalWorkflowsExecuted),
                SuccessfulWorkflows = Interlocked.Read(ref _successfulWorkflows),
                FailedWorkflows = Interlocked.Read(ref _failedWorkflows),
                SuccessRate = _totalWorkflowsExecuted > 0
                    ? Math.Round((double)_successfulWorkflows / _totalWorkflowsExecuted * 100, 2)
                    : 0,
                AverageWorkflowDurationMs = _totalWorkflowsExecuted > 0
                    ? _totalWorkflowDurationMs / _totalWorkflowsExecuted
                    : 0,
                MinWorkflowDurationMs = _minWorkflowDurationMs == long.MaxValue ? 0 : _minWorkflowDurationMs,
                MaxWorkflowDurationMs = _maxWorkflowDurationMs,

                TotalActivitiesExecuted = Interlocked.Read(ref _totalActivitiesExecuted),
                SuccessfulActivities = Interlocked.Read(ref _successfulActivities),
                FailedActivities = Interlocked.Read(ref _failedActivities),
                AverageActivityDurationMs = _totalActivitiesExecuted > 0
                    ? _totalActivityDurationMs / _totalActivitiesExecuted
                    : 0,

                ErrorCount = _errorCounts.ToDictionary(kvp => kvp.Key, kvp => Interlocked.Read(ref kvp.Value.Value)),
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
            Interlocked.Exchange(ref _totalWorkflowsExecuted, 0);
            Interlocked.Exchange(ref _successfulWorkflows, 0);
            Interlocked.Exchange(ref _failedWorkflows, 0);
            Interlocked.Exchange(ref _totalWorkflowDurationMs, 0);
            Interlocked.Exchange(ref _minWorkflowDurationMs, long.MaxValue);
            Interlocked.Exchange(ref _maxWorkflowDurationMs, 0);

            Interlocked.Exchange(ref _totalActivitiesExecuted, 0);
            Interlocked.Exchange(ref _successfulActivities, 0);
            Interlocked.Exchange(ref _failedActivities, 0);
            Interlocked.Exchange(ref _totalActivityDurationMs, 0);
            Interlocked.Exchange(ref _minActivityDurationMs, long.MaxValue);
            Interlocked.Exchange(ref _maxActivityDurationMs, 0);

            _errorCounts.Clear();

            _logger.LogInformation("Metrics reset");
        }
    }

    private static void UpdateMin(ref long location, long value)
    {
        long initialValue = Volatile.Read(ref location);
        while (value < initialValue)
        {
            long originalValue = Interlocked.CompareExchange(ref location, value, initialValue);
            if (originalValue == initialValue)
                break;
            initialValue = originalValue;
        }
    }

    private static void UpdateMax(ref long location, long value)
    {
        long initialValue = Volatile.Read(ref location);
        while (value > initialValue)
        {
            long originalValue = Interlocked.CompareExchange(ref location, value, initialValue);
            if (originalValue == initialValue)
                break;
            initialValue = originalValue;
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
