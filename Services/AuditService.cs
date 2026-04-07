// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Constants;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Data.Repositories; // Add this using directive

namespace DotNetWorkflowEngine.Services;

/// <summary>
/// Service for managing audit logs and tracking workflow events.
/// </summary>
public class AuditService
{
    private readonly IAuditRepository _auditRepository;

    public AuditService(IAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    /// <summary>
    /// Logs when a workflow instance is created.
    /// </summary>
    public async Task LogInstanceCreated(string instanceId, string createdBy)
    {
        var entry = new AuditLogEntry(instanceId, "InstanceCreated", "Workflow instance created")
        {
            Severity = "Info",
            Actor = createdBy
        };

        await AddEntry(entry);
    }

    /// <summary>
    /// Logs when a workflow instance starts executing.
    /// </summary>
    public async Task LogInstanceStarted(string instanceId)
    {
        var entry = new AuditLogEntry(instanceId, "InstanceStarted", "Workflow instance started execution")
        {
            Severity = "Info"
        };

        await AddEntry(entry);
    }

    /// <summary>
    /// Logs when a workflow instance completes.
    /// </summary>
    public async Task LogInstanceCompleted(string instanceId)
    {
        var entry = new AuditLogEntry(instanceId, "InstanceCompleted", "Workflow instance completed successfully")
        {
            Severity = "Info"
        };

        await AddEntry(entry);
    }

    /// <summary>
    /// Logs when a workflow instance fails.
    /// </summary>
    public async Task LogInstanceFailed(string instanceId, string errorMessage)
    {
        var entry = new AuditLogEntry(instanceId, "InstanceFailed", $"Workflow instance failed: {errorMessage}")
        {
            Severity = "Error"
        };

        await AddEntry(entry);
    }

    /// <summary>
    /// Logs when a workflow instance is resumed.
    /// </summary>
    public async Task LogInstanceResumed(string instanceId)
    {
        var entry = new AuditLogEntry(instanceId, "InstanceResumed", "Workflow instance resumed from suspension")
        {
            Severity = "Warning"
        };

        await AddEntry(entry);
    }

    /// <summary>
    /// Logs when an activity completes.
    /// </summary>
    public async Task LogActivityCompleted(string instanceId, string activityId, ActivityResult result)
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

        await AddEntry(entry);
    }

    /// <summary>
    /// Logs when an activity fails.
    /// </summary>
    public async Task LogActivityFailed(string instanceId, string activityId, string errorMessage)
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

        await AddEntry(entry);
    }

    /// <summary>
    /// Logs when an activity is retried.
    /// </summary>
    public async Task LogActivityRetry(string instanceId, string activityId, int attemptNumber, string? reason = null)
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

        await AddEntry(entry);
    }

    /// <summary>
    /// Logs a custom event.
    /// </summary>
    public async Task LogCustomEvent(string instanceId, string eventType, string description, string severity = "Info", string? activityId = null)
    {
        var entry = new AuditLogEntry(instanceId, eventType, description)
        {
            ActivityId = activityId,
            Severity = severity
        };

        await AddEntry(entry);
    }

    /// <summary>
    /// Gets audit log entries for a specific workflow instance.
    /// </summary>
    public async Task<List<AuditLogEntry>> GetAuditLog(string instanceId)
    {
        return await _auditRepository.GetByInstanceIdAsync(instanceId);
    }

    /// <summary>
    /// Gets audit log entries with filtering.
    /// </summary>
    public async Task<List<AuditLogEntry>> GetAuditLog(string instanceId, DateTime? since = null, string? eventType = null)
    {
        var (logs, total) = await _auditRepository.GetFilteredAndPagedAsync(
            instanceId: instanceId,
            eventType: eventType,
            fromDate: since
        );
        return logs;
    }

    /// <summary>
    /// Gets the most recent audit entries.
    /// </summary>
    public async Task<List<AuditLogEntry>> GetRecentAuditLog(string instanceId, int count = 10)
    {
        return await _auditRepository.GetRecentForInstanceAsync(instanceId, count);
    }

    /// <summary>
    /// Clears audit log for an instance.
    /// </summary>
    public async Task ClearAuditLog(string instanceId)
    {
        await _auditRepository.ClearInstanceAsync(instanceId);
    }

    /// <summary>
    /// Exports audit log as CSV string.
    /// </summary>
    public async Task<string> ExportAuditLogAsCsv(string instanceId)
    {
        var log = await GetAuditLog(instanceId);
        if (log.Count == 0)
            return "No audit entries";

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Timestamp,EventType,WorkflowInstanceId,ActivityId,Severity,Description,Actor,CorrelationId");

        foreach (var entry in log.OrderBy(e => e.Timestamp))
        {
            csv.AppendLine($"\"{entry.GetFormattedTimestamp()}\",\"{entry.EventType}\",\"{entry.WorkflowInstanceId}\",\"{entry.ActivityId}\",\"{entry.Severity}\",\"{entry.Description.Replace("\"", "\"\"")}\",\"{entry.Actor}\",\"{entry.CorrelationId}\"");
        }

        return csv.ToString();
    }

    /// <summary>
    /// Gets filtered and paginated audit entries across all workflows/instances.
    /// </summary>
    public async Task<(List<AuditLogEntry> Items, int Total)> GetFilteredAuditLogsAsync(
        string? workflowId = null,
        string? instanceId = null,
        string? activityId = null,
        string? eventType = null,
        string? severity = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? actor = null,
        int skip = 0,
        int take = 100)
    {
        return await _auditRepository.GetFilteredAndPagedAsync(
            workflowId,
            instanceId,
            activityId,
            eventType,
            severity,
            fromDate,
            toDate,
            actor,
            skip,
            take
        );
    }

    /// <summary>
    /// Adds an audit entry to the log.
    /// </summary>
    private async Task AddEntry(AuditLogEntry entry)
    {
        await _auditRepository.AddAsync(entry);
    }
}
