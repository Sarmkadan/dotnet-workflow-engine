// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using DotNetWorkflowEngine.Constants;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Data.Repositories;
using DotNetWorkflowEngine.Exceptions;

namespace DotNetWorkflowEngine.Services;

/// <summary>
/// Service for managing audit logs and tracking workflow events.
/// </summary>
public class AuditService : IAuditTrailQuery
{
    private readonly IAuditRepository _auditRepository;

    /// <summary>
    /// Initializes a new instance of the AuditService.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when audit repository is null.</exception>
    public AuditService(IAuditRepository auditRepository)
    {
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
    }

    /// <summary>
    /// Logs when a workflow instance is created.
    /// </summary>
    /// <param name="instanceId">The ID of the workflow instance.</param>
    /// <param name="createdBy">The user or system that created the instance.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when instance ID is invalid.</exception>
    public async Task LogInstanceCreated(string instanceId, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Created by cannot be null or empty", nameof(createdBy));

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
    /// <exception cref="ArgumentException">Thrown when instance ID is invalid.</exception>
    public async Task LogInstanceStarted(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        var entry = new AuditLogEntry(instanceId, "InstanceStarted", "Workflow instance started execution")
        {
            Severity = "Info"
        };

        await AddEntry(entry);
    }

    /// <summary>
    /// Logs when a workflow instance completes.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when instance ID is invalid.</exception>
    public async Task LogInstanceCompleted(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        var entry = new AuditLogEntry(instanceId, "InstanceCompleted", "Workflow instance completed successfully")
        {
            Severity = "Info"
        };

        await AddEntry(entry);
    }

    /// <summary>
    /// Logs when a workflow instance fails.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when instance ID or error message is invalid.</exception>
    public async Task LogInstanceFailed(string instanceId, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message cannot be null or empty", nameof(errorMessage));

        var entry = new AuditLogEntry(instanceId, "InstanceFailed", $"Workflow instance failed: {errorMessage}")
        {
            Severity = "Error"
        };

        await AddEntry(entry);
    }

    /// <summary>
    /// Logs when a workflow instance is resumed.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when instance ID is invalid.</exception>
    public async Task LogInstanceResumed(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        var entry = new AuditLogEntry(instanceId, "InstanceResumed", "Workflow instance resumed from suspension")
        {
            Severity = "Warning"
        };

        await AddEntry(entry);
    }

    /// <summary>
    /// Logs when a workflow instance is paused.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when instance ID is invalid.</exception>
    public async Task LogInstancePaused(string instanceId, string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        var entry = new AuditLogEntry(instanceId, "InstancePaused", string.IsNullOrWhiteSpace(reason)
            ? "Workflow instance paused"
            : $"Workflow instance paused. Reason: {reason}")
        {
            Severity = "Warning"
        };

        await AddEntry(entry);
    }

    /// <summary>
    /// Logs when an activity completes.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when instance ID or activity ID is invalid.</exception>
    public async Task LogActivityCompleted(string instanceId, string activityId, ActivityResult result)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        if (string.IsNullOrWhiteSpace(activityId))
            throw new ArgumentException("Activity ID cannot be null or empty", nameof(activityId));

        if (result == null)
            throw new ArgumentNullException(nameof(result));

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
    /// <exception cref="ArgumentException">Thrown when instance ID or activity ID is invalid.</exception>
    public async Task LogActivityFailed(string instanceId, string activityId, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        if (string.IsNullOrWhiteSpace(activityId))
            throw new ArgumentException("Activity ID cannot be null or empty", nameof(activityId));

        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message cannot be null or empty", nameof(errorMessage));

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
    /// <exception cref="ArgumentException">Thrown when instance ID or activity ID is invalid.</exception>
    public async Task LogActivityRetry(string instanceId, string activityId, int attemptNumber, string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        if (string.IsNullOrWhiteSpace(activityId))
            throw new ArgumentException("Activity ID cannot be null or empty", nameof(activityId));

        if (attemptNumber <= 0)
            throw new ArgumentException("Attempt number must be positive", nameof(attemptNumber));

        var entry = new AuditLogEntry(instanceId, "ActivityRetry", $"Activity '{activityId}' being retried (attempt {attemptNumber})");
        entry.ActivityId = activityId;
        entry.Severity = "Warning";
        entry.Details = new Dictionary<string, object?>
        {
            ["AttemptNumber"] = attemptNumber,
            ["Reason"] = reason ?? "Execution failed"
        };

        await AddEntry(entry);
    }

    /// <summary>
    /// Logs a custom event.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when instance ID is invalid.</exception>
    public virtual async Task LogCustomEvent(string instanceId, string eventType, string description, string severity = "Info", string? activityId = null)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be null or empty", nameof(eventType));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be null or empty", nameof(description));

        if (string.IsNullOrWhiteSpace(severity))
            throw new ArgumentException("Severity cannot be null or empty", nameof(severity));

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
    /// <exception cref="ArgumentException">Thrown when instance ID is invalid.</exception>
    public async Task<List<AuditLogEntry>> GetAuditLog(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        return await _auditRepository.GetByInstanceIdAsync(instanceId);
    }

    /// <summary>
    /// Gets audit log entries with filtering.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when instance ID is invalid.</exception>
    public async Task<List<AuditLogEntry>> GetAuditLog(string instanceId, DateTime? since = null, string? eventType = null)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

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
    /// <exception cref="ArgumentException">Thrown when instance ID is invalid.</exception>
    public async Task<List<AuditLogEntry>> GetRecentAuditLog(string instanceId, int count = 10)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        if (count <= 0)
            throw new ArgumentException("Count must be positive", nameof(count));

        return await _auditRepository.GetRecentForInstanceAsync(instanceId, count);
    }

    /// <summary>
    /// Clears audit log for an instance.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when instance ID is invalid.</exception>
    public async Task ClearAuditLog(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        await _auditRepository.ClearInstanceAsync(instanceId);
    }

    /// <summary>
    /// Exports audit log as CSV string.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when instance ID is invalid.</exception>
    public async Task<string> ExportAuditLogAsCsv(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        var log = await GetAuditLog(instanceId);
        if (log.Count == 0)
            return "No audit entries";

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Timestamp,EventType,WorkflowInstanceId,ActivityId,Severity,Description,Actor,CorrelationId");

        foreach (var entry in log.OrderBy(e => e.Timestamp))
        {
            var escapedDescription = entry.Description.Replace("\"", "\"\"");
            csv.AppendLine($"\"{entry.GetFormattedTimestamp()}\",\"{entry.EventType}\",\"{entry.WorkflowInstanceId}\",\"{entry.ActivityId}\",\"{entry.Severity}\",\"{escapedDescription}\",\"{entry.Actor}\",\"{entry.CorrelationId}\"");
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
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        await _auditRepository.AddAsync(entry);
    }

    // -------------------------------------------------------------------------
    // IAuditTrailQuery implementation
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<(List<AuditLogEntry> Items, int Total)> QueryAsync(
        string? workflowId = null,
        string? instanceId = null,
        string? stepName = null,
        string? activityType = null,
        string? outcome = null,
        string? actor = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int skip = 0,
        int take = 100)
    {
        return await _auditRepository.GetFilteredAndPagedAsync(
            workflowId: workflowId,
            instanceId: instanceId,
            activityId: stepName,
            eventType: activityType,
            severity: outcome,
            fromDate: fromDate,
            toDate: toDate,
            actor: actor,
            skip: skip,
            take: take);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetEventTypesAsync()
    {
        var (all, _) = await _auditRepository.GetFilteredAndPagedAsync(take: int.MaxValue);
        return all.Select(e => e.EventType).Distinct().OrderBy(t => t).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, int>> GetOutcomeSummaryAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var (all, _) = await _auditRepository.GetFilteredAndPagedAsync(
            fromDate: fromDate,
            toDate: toDate,
            take: int.MaxValue);
        return all.GroupBy(e => e.EventType)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}