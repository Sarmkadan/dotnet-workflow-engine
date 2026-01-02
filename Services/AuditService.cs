// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Constants;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Services;

/// <summary>
/// Service for managing audit logs and tracking workflow events.
/// </summary>
public class AuditService
{
    private readonly Dictionary<string, List<AuditLogEntry>> _auditLogs = new();

    /// <summary>
    /// Logs when a workflow instance is created.
    /// </summary>
    public void LogInstanceCreated(string instanceId, string createdBy)
    {
        var entry = new AuditLogEntry(instanceId, "InstanceCreated", "Workflow instance created")
        {
            Severity = "Info",
            Actor = createdBy
        };

        AddEntry(instanceId, entry);
    }

    /// <summary>
    /// Logs when a workflow instance starts executing.
    /// </summary>
    public void LogInstanceStarted(string instanceId)
    {
        var entry = new AuditLogEntry(instanceId, "InstanceStarted", "Workflow instance started execution")
        {
            Severity = "Info"
        };

        AddEntry(instanceId, entry);
    }

    /// <summary>
    /// Logs when a workflow instance completes.
    /// </summary>
    public void LogInstanceCompleted(string instanceId)
    {
        var entry = new AuditLogEntry(instanceId, "InstanceCompleted", "Workflow instance completed successfully")
        {
            Severity = "Info"
        };

        AddEntry(instanceId, entry);
    }

    /// <summary>
    /// Logs when a workflow instance fails.
    /// </summary>
    public void LogInstanceFailed(string instanceId, string errorMessage)
    {
        var entry = new AuditLogEntry(instanceId, "InstanceFailed", $"Workflow instance failed: {errorMessage}")
        {
            Severity = "Error"
        };

        AddEntry(instanceId, entry);
    }

    /// <summary>
    /// Logs when a workflow instance is resumed.
    /// </summary>
    public void LogInstanceResumed(string instanceId)
    {
        var entry = new AuditLogEntry(instanceId, "InstanceResumed", "Workflow instance resumed from suspension")
        {
            Severity = "Warning"
        };

        AddEntry(instanceId, entry);
    }

    /// <summary>
    /// Logs when an activity completes.
    /// </summary>
    public void LogActivityCompleted(string instanceId, string activityId, ActivityResult result)
    {
        var entry = new AuditLogEntry(instanceId, "ActivityCompleted", $"Activity '{activityId}' completed successfully")
        {
            ActivityId = activityId,
            Severity = "Info",
            Details = new Dictionary<string, object?>
            {
                ["ExecutionTime"] = result.ExecutionDurationMs,
                ["Attempts"] = result.AttemptNumber,
                ["OutputKeys"] = result.Output.Keys.ToList()
            }
        };

        AddEntry(instanceId, entry);
    }

    /// <summary>
    /// Logs when an activity fails.
    /// </summary>
    public void LogActivityFailed(string instanceId, string activityId, string errorMessage)
    {
        var entry = new AuditLogEntry(instanceId, "ActivityFailed", $"Activity '{activityId}' failed: {errorMessage}")
        {
            ActivityId = activityId,
            Severity = "Error",
            Details = new Dictionary<string, object?>
            {
                ["ErrorMessage"] = errorMessage,
                ["Timestamp"] = DateTime.UtcNow
            }
        };

        AddEntry(instanceId, entry);
    }

    /// <summary>
    /// Logs when an activity is retried.
    /// </summary>
    public void LogActivityRetry(string instanceId, string activityId, int attemptNumber, string? reason = null)
    {
        var entry = new AuditLogEntry(instanceId, "ActivityRetry", $"Activity '{activityId}' being retried (attempt {attemptNumber})")
        {
            ActivityId = activityId,
            Severity = "Warning",
            Details = new Dictionary<string, object?>
            {
                ["AttemptNumber"] = attemptNumber,
                ["Reason"] = reason ?? "Execution failed"
            }
        };

        AddEntry(instanceId, entry);
    }

    /// <summary>
    /// Logs a custom event.
    /// </summary>
    public void LogCustomEvent(string instanceId, string eventType, string description, string severity = "Info", string? activityId = null)
    {
        var entry = new AuditLogEntry(instanceId, eventType, description)
        {
            ActivityId = activityId,
            Severity = severity
        };

        AddEntry(instanceId, entry);
    }

    /// <summary>
    /// Gets audit log entries for an instance.
    /// </summary>
    public List<AuditLogEntry> GetAuditLog(string instanceId)
    {
        _auditLogs.TryGetValue(instanceId, out var entries);
        return entries ?? new List<AuditLogEntry>();
    }

    /// <summary>
    /// Gets audit log entries with filtering.
    /// </summary>
    public List<AuditLogEntry> GetAuditLog(string instanceId, DateTime? since = null, string? eventType = null)
    {
        var log = GetAuditLog(instanceId);

        if (since.HasValue)
            log = log.Where(e => e.Timestamp >= since.Value).ToList();

        if (!string.IsNullOrEmpty(eventType))
            log = log.Where(e => e.EventType == eventType).ToList();

        return log;
    }

    /// <summary>
    /// Gets the most recent audit entries.
    /// </summary>
    public List<AuditLogEntry> GetRecentAuditLog(string instanceId, int count = 10)
    {
        var log = GetAuditLog(instanceId);
        return log.OrderByDescending(e => e.Timestamp).Take(count).ToList();
    }

    /// <summary>
    /// Clears audit log for an instance.
    /// </summary>
    public void ClearAuditLog(string instanceId)
    {
        _auditLogs.Remove(instanceId);
    }

    /// <summary>
    /// Exports audit log as CSV string.
    /// </summary>
    public string ExportAuditLogAsCsv(string instanceId)
    {
        var log = GetAuditLog(instanceId);
        if (log.Count == 0)
            return "No audit entries";

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Timestamp,EventType,ActivityId,Severity,Description");

        foreach (var entry in log.OrderBy(e => e.Timestamp))
        {
            csv.AppendLine($"\"{entry.GetFormattedTimestamp()}\",\"{entry.EventType}\",\"{entry.ActivityId}\",\"{entry.Severity}\",\"{entry.Description}\"");
        }

        return csv.ToString();
    }

    /// <summary>
    /// Adds an audit entry to the log.
    /// </summary>
    private void AddEntry(string instanceId, AuditLogEntry entry)
    {
        if (!_auditLogs.ContainsKey(instanceId))
            _auditLogs[instanceId] = new List<AuditLogEntry>();

        var log = _auditLogs[instanceId];

        // Maintain maximum entries per instance
        if (log.Count >= WorkflowConstants.MaxAuditEntriesPerInstance)
            log.RemoveRange(0, log.Count - WorkflowConstants.MaxAuditEntriesPerInstance + 1);

        log.Add(entry);
    }
}
