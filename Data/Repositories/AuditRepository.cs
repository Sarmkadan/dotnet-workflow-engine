// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Data.Repositories;

/// <summary>
/// Repository for audit log persistence.
/// </summary>
public class AuditRepository : IRepository<AuditLogEntry>
{
    private readonly Dictionary<string, List<AuditLogEntry>> _auditLogs = new();
    private readonly List<AuditLogEntry> _allEntries = new();

    /// <summary>
    /// Gets an audit entry by ID.
    /// </summary>
    public Task<AuditLogEntry?> GetByIdAsync(string id)
    {
        var entry = _allEntries.FirstOrDefault(e => e.Id == id);
        return Task.FromResult(entry);
    }

    /// <summary>
    /// Gets all audit entries.
    /// </summary>
    public Task<List<AuditLogEntry>> GetAllAsync()
    {
        return Task.FromResult(new List<AuditLogEntry>(_allEntries));
    }

    /// <summary>
    /// Adds a new audit entry.
    /// </summary>
    public Task AddAsync(AuditLogEntry entity)
    {
        if (!_auditLogs.ContainsKey(entity.WorkflowInstanceId))
            _auditLogs[entity.WorkflowInstanceId] = new List<AuditLogEntry>();

        _auditLogs[entity.WorkflowInstanceId].Add(entity);
        _allEntries.Add(entity);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates an audit entry (not typically used for audit logs).
    /// </summary>
    public Task UpdateAsync(AuditLogEntry entity)
    {
        // Audit entries are immutable, but allow updates for metadata
        return Task.CompletedTask;
    }

    /// <summary>
    /// Deletes an audit entry.
    /// </summary>
    public Task DeleteAsync(string id)
    {
        var entry = _allEntries.FirstOrDefault(e => e.Id == id);
        if (entry != null)
        {
            _allEntries.Remove(entry);
            if (_auditLogs.TryGetValue(entry.WorkflowInstanceId, out var list))
            {
                list.Remove(entry);
            }
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks if an audit entry exists.
    /// </summary>
    public Task<bool> ExistsAsync(string id)
    {
        return Task.FromResult(_allEntries.Any(e => e.Id == id));
    }

    /// <summary>
    /// Gets the count of audit entries.
    /// </summary>
    public Task<int> CountAsync()
    {
        return Task.FromResult(_allEntries.Count);
    }

    /// <summary>
    /// Gets audit entries with pagination.
    /// </summary>
    public Task<(List<AuditLogEntry> Items, int Total)> GetPagedAsync(int pageNumber, int pageSize)
    {
        var total = _allEntries.Count;
        var items = _allEntries
            .OrderByDescending(e => e.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult((items, total));
    }

    /// <summary>
    /// Gets audit log for a specific workflow instance.
    /// </summary>
    public Task<List<AuditLogEntry>> GetByInstanceIdAsync(string instanceId)
    {
        _auditLogs.TryGetValue(instanceId, out var entries);
        return Task.FromResult(entries?.OrderBy(e => e.Timestamp).ToList() ?? new List<AuditLogEntry>());
    }

    /// <summary>
    /// Gets audit entries by event type.
    /// </summary>
    public Task<List<AuditLogEntry>> GetByEventTypeAsync(string eventType)
    {
        var entries = _allEntries.Where(e => e.EventType == eventType).ToList();
        return Task.FromResult(entries);
    }

    /// <summary>
    /// Gets audit entries by severity level.
    /// </summary>
    public Task<List<AuditLogEntry>> GetBySeverityAsync(string severity)
    {
        var entries = _allEntries.Where(e => e.Severity == severity).ToList();
        return Task.FromResult(entries);
    }

    /// <summary>
    /// Gets error audit entries.
    /// </summary>
    public Task<List<AuditLogEntry>> GetErrorsAsync()
    {
        return GetBySeverityAsync("Error");
    }

    /// <summary>
    /// Gets audit entries within a time range.
    /// </summary>
    public Task<List<AuditLogEntry>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var entries = _allEntries.Where(e => e.Timestamp >= from && e.Timestamp <= to).ToList();
        return Task.FromResult(entries);
    }

    /// <summary>
    /// Gets recent audit entries for an instance.
    /// </summary>
    public Task<List<AuditLogEntry>> GetRecentForInstanceAsync(string instanceId, int count = 10)
    {
        _auditLogs.TryGetValue(instanceId, out var entries);
        var result = entries?
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToList() ?? new List<AuditLogEntry>();

        return Task.FromResult(result);
    }

    /// <summary>
    /// Gets audit entries for an activity.
    /// </summary>
    public Task<List<AuditLogEntry>> GetByActivityIdAsync(string activityId)
    {
        var entries = _allEntries.Where(e => e.ActivityId == activityId).ToList();
        return Task.FromResult(entries);
    }

    /// <summary>
    /// Clears audit log for an instance.
    /// </summary>
    public Task ClearInstanceAsync(string instanceId)
    {
        if (_auditLogs.TryGetValue(instanceId, out var entries))
        {
            foreach (var entry in entries)
            {
                _allEntries.Remove(entry);
            }
            _auditLogs.Remove(instanceId);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all audit logs.
    /// </summary>
    public Task ClearAsync()
    {
        _auditLogs.Clear();
        _allEntries.Clear();
        return Task.CompletedTask;
    }
}
