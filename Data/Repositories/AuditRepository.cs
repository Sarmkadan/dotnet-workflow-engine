// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Exceptions;

namespace DotNetWorkflowEngine.Data.Repositories;

/// <summary>
/// Repository for audit log persistence.
/// </summary>
public class AuditRepository : IAuditRepository
{
    private readonly Dictionary<string, List<AuditLogEntry>> _auditLogs = new();
    private readonly List<AuditLogEntry> _allEntries = new();

    /// <summary>
    /// Gets an audit entry by ID.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when ID is invalid.</exception>
    public Task<AuditLogEntry?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID cannot be null or empty", nameof(id));

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
    /// <exception cref="ArgumentNullException">Thrown when entry is null.</exception>
    public Task AddAsync(AuditLogEntry entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (string.IsNullOrWhiteSpace(entity.WorkflowInstanceId))
            throw new ValidationException("Audit log entry must have a workflow instance ID", "INVALID_INSTANCE_ID");

        if (string.IsNullOrWhiteSpace(entity.EventType))
            throw new ValidationException("Audit log entry must have an event type", "INVALID_EVENT_TYPE");

        if (string.IsNullOrWhiteSpace(entity.Description))
            throw new ValidationException("Audit log entry must have a description", "INVALID_DESCRIPTION");

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
    /// <exception cref="ArgumentException">Thrown when ID is invalid.</exception>
    public Task DeleteAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID cannot be null or empty", nameof(id));

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
    /// <exception cref="ArgumentException">Thrown when ID is invalid.</exception>
    public Task<bool> ExistsAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID cannot be null or empty", nameof(id));

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
        if (pageNumber < 1)
            throw new ArgumentException("Page number must be positive", nameof(pageNumber));

        if (pageSize < 1)
            throw new ArgumentException("Page size must be positive", nameof(pageSize));

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
    /// <exception cref="ArgumentException">Thrown when instance ID is invalid.</exception>
    public Task<List<AuditLogEntry>> GetByInstanceIdAsync(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        _auditLogs.TryGetValue(instanceId, out var entries);
        return Task.FromResult(entries?.OrderBy(e => e.Timestamp).ToList() ?? new List<AuditLogEntry>());
    }

    /// <summary>
    /// Gets audit entries by event type.
    /// </summary>
    public Task<List<AuditLogEntry>> GetByEventTypeAsync(string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be null or empty", nameof(eventType));

        var entries = _allEntries.Where(e => e.EventType == eventType).ToList();
        return Task.FromResult(entries);
    }

    /// <summary>
    /// Gets audit entries by severity level.
    /// </summary>
    public Task<List<AuditLogEntry>> GetBySeverityAsync(string severity)
    {
        if (string.IsNullOrWhiteSpace(severity))
            throw new ArgumentException("Severity cannot be null or empty", nameof(severity));

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
        if (from > to)
            throw new ArgumentException("From date cannot be after to date", nameof(from));

        var entries = _allEntries.Where(e => e.Timestamp >= from && e.Timestamp <= to).ToList();
        return Task.FromResult(entries);
    }

    /// <summary>
    /// Gets recent audit entries for an instance.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when instance ID is invalid.</exception>
    public Task<List<AuditLogEntry>> GetRecentForInstanceAsync(string instanceId, int count = 10)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        if (count <= 0)
            throw new ArgumentException("Count must be positive", nameof(count));

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
    /// <exception cref="ArgumentException">Thrown when activity ID is invalid.</exception>
    public Task<List<AuditLogEntry>> GetByActivityIdAsync(string activityId)
    {
        if (string.IsNullOrWhiteSpace(activityId))
            throw new ArgumentException("Activity ID cannot be null or empty", nameof(activityId));

        var entries = _allEntries.Where(e => e.ActivityId == activityId).ToList();
        return Task.FromResult(entries);
    }

    /// <summary>
    /// Clears audit log for an instance.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when instance ID is invalid.</exception>
    public Task ClearInstanceAsync(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

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

    /// <summary>
    /// Gets filtered and paginated audit entries across all workflows/instances.
    /// </summary>
    public Task<(List<AuditLogEntry> Items, int Total)> GetFilteredAndPagedAsync(
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
        IQueryable<AuditLogEntry> query = _allEntries.AsQueryable();

        if (!string.IsNullOrWhiteSpace(workflowId))
            query = query.Where(e => e.WorkflowInstanceId.StartsWith(workflowId)); // Assuming instanceId can identify workflow

        if (!string.IsNullOrWhiteSpace(instanceId))
            query = query.Where(e => e.WorkflowInstanceId == instanceId);

        if (!string.IsNullOrWhiteSpace(activityId))
            query = query.Where(e => e.ActivityId == activityId);

        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(e => e.EventType == eventType);

        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(e => e.Severity == severity);

        if (fromDate.HasValue)
            query = query.Where(e => e.Timestamp >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(e => e.Timestamp <= toDate.Value);

        if (!string.IsNullOrWhiteSpace(actor))
            query = query.Where(e => e.Actor == actor);

        var total = query.Count();
        var items = query.OrderByDescending(e => e.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToList();

        return Task.FromResult((items, total));
    }
}