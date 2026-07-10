// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System.Globalization;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Services;

/// <summary>
/// Extension methods for <see cref="AuditService"/> providing additional audit log functionality.
/// </summary>
public static class AuditServiceExtensions
{
    /// <summary>
    /// Gets audit log entries for a specific workflow across all its instances.
    /// </summary>
    /// <param name="auditService">The audit service instance.</param>
    /// <param name="workflowId">The workflow ID to filter by.</param>
    /// <param name="skip">Number of entries to skip for pagination.</param>
    /// <param name="take">Maximum number of entries to return.</param>
    /// <returns>A tuple containing the list of audit entries and the total count.</returns>
    /// <exception cref="ArgumentException">Thrown when workflow ID is invalid.</exception>
    public static async Task<(IReadOnlyList<AuditLogEntry> Items, int Total)> GetWorkflowAuditLogAsync(
        this AuditService auditService,
        string workflowId,
        int skip = 0,
        int take = 100)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowId);

        return await auditService.GetFilteredAuditLogsAsync(
            workflowId: workflowId,
            skip: skip,
            take: take);
    }

    /// <summary>
    /// Gets audit log entries filtered by severity level.
    /// </summary>
    /// <param name="auditService">The audit service instance.</param>
    /// <param name="severity">The severity level to filter by (Info, Warning, Error, etc.).</param>
    /// <param name="skip">Number of entries to skip for pagination.</param>
    /// <param name="take">Maximum number of entries to return.</param>
    /// <returns>A tuple containing the list of audit entries and the total count.</returns>
    /// <exception cref="ArgumentException">Thrown when severity is invalid.</exception>
    public static async Task<(IReadOnlyList<AuditLogEntry> Items, int Total)> GetBySeverityAsync(
        this AuditService auditService,
        string severity,
        int skip = 0,
        int take = 100)
    {
        ArgumentException.ThrowIfNullOrEmpty(severity);

        return await auditService.GetFilteredAuditLogsAsync(
            severity: severity,
            skip: skip,
            take: take);
    }

    /// <summary>
    /// Gets audit log entries filtered by date range.
    /// </summary>
    /// <param name="auditService">The audit service instance.</param>
    /// <param name="fromDate">Start date for filtering (inclusive).</param>
    /// <param name="toDate">End date for filtering (inclusive).</param>
    /// <param name="skip">Number of entries to skip for pagination.</param>
    /// <param name="take">Maximum number of entries to return.</param>
    /// <returns>A tuple containing the list of audit entries and the total count.</returns>
    public static async Task<(IReadOnlyList<AuditLogEntry> Items, int Total)> GetByDateRangeAsync(
        this AuditService auditService,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int skip = 0,
        int take = 100)
    {
        return await auditService.GetFilteredAuditLogsAsync(
            fromDate: fromDate,
            toDate: toDate,
            skip: skip,
            take: take);
    }

    /// <summary>
    /// Gets audit log entries for a specific activity ID.
    /// </summary>
    /// <param name="auditService">The audit service instance.</param>
    /// <param name="activityId">The activity ID to filter by.</param>
    /// <param name="skip">Number of entries to skip for pagination.</param>
    /// <param name="take">Maximum number of entries to return.</param>
    /// <returns>A tuple containing the list of audit entries and the total count.</returns>
    /// <exception cref="ArgumentException">Thrown when activity ID is invalid.</exception>
    public static async Task<(IReadOnlyList<AuditLogEntry> Items, int Total)> GetByActivityIdAsync(
        this AuditService auditService,
        string activityId,
        int skip = 0,
        int take = 100)
    {
        ArgumentException.ThrowIfNullOrEmpty(activityId);

        return await auditService.GetFilteredAuditLogsAsync(
            activityId: activityId,
            skip: skip,
            take: take);
    }

    /// <summary>
    /// Gets the most recent audit entries across all workflows.
    /// </summary>
    /// <param name="auditService">The audit service instance.</param>
    /// <param name="count">Maximum number of entries to return.</param>
    /// <returns>List of the most recent audit entries.</returns>
    /// <exception cref="ArgumentException">Thrown when count is not positive.</exception>
    public static async Task<IReadOnlyList<AuditLogEntry>> GetGlobalRecentAuditLogAsync(
        this AuditService auditService,
        int count = 50)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be positive", nameof(count));

        var (all, _) = await auditService.GetFilteredAuditLogsAsync(take: count);
        return all.OrderByDescending(e => e.Timestamp).ToList();
    }

    /// <summary>
    /// Gets audit log entries filtered by actor (user/system).
    /// </summary>
    /// <param name="auditService">The audit service instance.</param>
    /// <param name="actor">The actor name to filter by.</param>
    /// <param name="skip">Number of entries to skip for pagination.</param>
    /// <param name="take">Maximum number of entries to return.</param>
    /// <returns>A tuple containing the list of audit entries and the total count.</returns>
    /// <exception cref="ArgumentException">Thrown when actor is invalid.</exception>
    public static async Task<(IReadOnlyList<AuditLogEntry> Items, int Total)> GetByActorAsync(
        this AuditService auditService,
        string actor,
        int skip = 0,
        int take = 100)
    {
        ArgumentException.ThrowIfNullOrEmpty(actor);

        return await auditService.GetFilteredAuditLogsAsync(
            actor: actor,
            skip: skip,
            take: take);
    }

    /// <summary>
    /// Exports audit log entries as CSV with custom field selection.
    /// </summary>
    /// <param name="auditService">The audit service instance.</param>
    /// <param name="instanceId">The workflow instance ID.</param>
    /// <param name="fields">List of field names to include in CSV (Timestamp, EventType, ActivityId, Severity, Description, Actor, CorrelationId).</param>
    /// <returns>CSV formatted string with selected fields.</returns>
    /// <exception cref="ArgumentException">Thrown when instance ID is invalid.</exception>
    public static async Task<string> ExportAuditLogAsCsvAsync(
        this AuditService auditService,
        string instanceId,
        IReadOnlyList<string>? fields = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(instanceId);

        var log = await auditService.GetAuditLog(instanceId);
        if (log.Count == 0)
            return "No audit entries";

        // Default fields if none specified
        fields ??= new List<string> { "Timestamp", "EventType", "WorkflowInstanceId", "ActivityId", "Severity", "Description", "Actor", "CorrelationId" };

        var csv = new System.Text.StringBuilder();

        // Write header
        csv.AppendLine(string.Join(",", fields));

        // Write data rows
        foreach (var entry in log.OrderBy(e => e.Timestamp))
        {
            var row = new List<string>();

            foreach (var field in fields)
            {
                row.Add(GetCsvFieldValue(entry, field));
            }

            csv.AppendLine(string.Join(",", row));
        }

        return csv.ToString();
    }

    /// <summary>
    /// Gets a summary of workflow instance states from audit logs.
    /// </summary>
    /// <param name="auditService">The audit service instance.</param>
    /// <param name="instanceId">The workflow instance ID.</param>
    /// <returns>Dictionary mapping event types to their counts.</returns>
    /// <exception cref="ArgumentException">Thrown when instance ID is invalid.</exception>
    public static async Task<IReadOnlyDictionary<string, int>> GetInstanceStateSummaryAsync(
        this AuditService auditService,
        string instanceId)
    {
        ArgumentException.ThrowIfNullOrEmpty(instanceId);

        var log = await auditService.GetAuditLog(instanceId);
        return log.GroupBy(e => e.EventType)
                 .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Gets the duration statistics for a workflow instance.
    /// </summary>
    /// <param name="auditService">The audit service instance.</param>
    /// <param name="instanceId">The workflow instance ID.</param>
    /// <returns>Tuple containing start time, end time, and total duration in milliseconds.</returns>
    /// <exception cref="InvalidOperationException">Thrown when instance has no start/end events.</exception>
    public static async Task<(DateTime StartTime, DateTime? EndTime, long DurationMs)> GetWorkflowDurationAsync(
        this AuditService auditService,
        string instanceId)
    {
        ArgumentException.ThrowIfNullOrEmpty(instanceId);

        var log = await auditService.GetAuditLog(instanceId);

        var startEntry = log.FirstOrDefault(e => e.EventType == "InstanceStarted");
        var completedEntry = log.FirstOrDefault(e => e.EventType == "InstanceCompleted");
        var failedEntry = log.FirstOrDefault(e => e.EventType == "InstanceFailed");

        if (startEntry == null)
            throw new InvalidOperationException("Workflow instance has no start event recorded");

        DateTime? endTime = null;
        long durationMs = 0;

        if (completedEntry != null)
        {
            endTime = completedEntry.Timestamp;
            durationMs = (long)(endTime.Value - startEntry.Timestamp).TotalMilliseconds;
        }
        else if (failedEntry != null)
        {
            endTime = failedEntry.Timestamp;
            durationMs = (long)(endTime.Value - startEntry.Timestamp).TotalMilliseconds;
        }
        else
        {
            // If no completion or failure, use current time as end time
            endTime = DateTime.UtcNow;
            durationMs = (long)(endTime.Value - startEntry.Timestamp).TotalMilliseconds;
        }

        return (startEntry.Timestamp, endTime, durationMs);
    }

    /// <summary>
    /// Gets the failure rate for a specific activity type across all instances.
    /// </summary>
    /// <param name="auditService">The audit service instance.</param>
    /// <param name="activityType">The activity type to analyze.</param>
    /// <param name="timeWindowDays">Number of days to look back for statistics.</param>
    /// <returns>Tuple containing total attempts, failures, and failure rate percentage.</returns>
    public static async Task<(int TotalAttempts, int Failures, double FailureRate)> GetActivityFailureRateAsync(
        this AuditService auditService,
        string activityType,
        int timeWindowDays = 30)
    {
        ArgumentException.ThrowIfNullOrEmpty(activityType);

        if (timeWindowDays <= 0)
            throw new ArgumentException("Time window must be positive", nameof(timeWindowDays));

        var fromDate = DateTime.UtcNow.AddDays(-timeWindowDays);
        var (all, _) = await auditService.GetFilteredAuditLogsAsync(
            fromDate: fromDate,
            take: int.MaxValue);

        var activityAttempts = all.Where(e => e.ActivityId != null && e.ActivityId.Contains(activityType, StringComparison.OrdinalIgnoreCase));
        var failures = activityAttempts.Count(e => e.Severity == "Error" && e.EventType.Contains("Failed", StringComparison.OrdinalIgnoreCase));
        var total = activityAttempts.Count();

        var failureRate = total > 0 ? (double)failures / total * 100 : 0;

        return (total, failures, Math.Round(failureRate, 2));
    }

    /// <summary>
    /// Helper method to extract field values for CSV export.
    /// </summary>
    private static string GetCsvFieldValue(AuditLogEntry entry, string fieldName)
    {
        return fieldName switch
        {
            "Timestamp" => entry.GetFormattedTimestamp(),
            "EventType" => entry.EventType ?? string.Empty,
            "WorkflowInstanceId" => entry.WorkflowInstanceId ?? string.Empty,
            "ActivityId" => entry.ActivityId ?? string.Empty,
            "Severity" => entry.Severity ?? string.Empty,
            "Description" => EscapeCsvField(entry.Description ?? string.Empty),
            "Actor" => entry.Actor ?? string.Empty,
            "CorrelationId" => entry.CorrelationId ?? string.Empty,
            _ => string.Empty
        };
    }

    /// <summary>
    /// Escapes a field value for CSV format.
    /// </summary>
    private static string EscapeCsvField(string field)
    {
        if (field.Contains('"') || field.Contains(',') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}