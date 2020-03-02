// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Data.Repositories;

/// <summary>
/// Interface for a repository to manage audit log entries, providing
/// methods for querying, filtering, and pagination.
/// </summary>
public interface IAuditRepository : IRepository<AuditLogEntry>
{
    /// <summary>
    /// Gets audit entries for a specific workflow instance.
    /// </summary>
    Task<List<AuditLogEntry>> GetByInstanceIdAsync(string instanceId);

    /// <summary>
    /// Gets audit entries by event type.
    /// </summary>
    Task<List<AuditLogEntry>> GetByEventTypeAsync(string eventType);

    /// <summary>
    /// Gets audit entries by severity level.
    /// </summary>
    Task<List<AuditLogEntry>> GetBySeverityAsync(string severity);

    /// <summary>
    /// Gets audit entries within a time range.
    /// </summary>
    Task<List<AuditLogEntry>> GetByDateRangeAsync(DateTime from, DateTime to);

    /// <summary>
    /// Gets recent audit entries for an instance.
    /// </summary>
    Task<List<AuditLogEntry>> GetRecentForInstanceAsync(string instanceId, int count = 10);

    /// <summary>
    /// Gets audit entries for a specific activity.
    /// </summary>
    Task<List<AuditLogEntry>> GetByActivityIdAsync(string activityId);

    /// <summary>
    /// Clears audit log for a specific workflow instance.
    /// </summary>
    Task ClearInstanceAsync(string instanceId);

    /// <summary>
    /// Clears all audit logs.
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// Gets filtered and paginated audit entries across all workflows/instances.
    /// </summary>
    Task<(List<AuditLogEntry> Items, int Total)> GetFilteredAndPagedAsync(
        string? workflowId = null,
        string? instanceId = null,
        string? activityId = null,
        string? eventType = null,
        string? severity = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? actor = null,
        int skip = 0,
        int take = 100);
}
