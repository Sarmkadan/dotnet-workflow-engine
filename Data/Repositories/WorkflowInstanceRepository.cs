// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Exceptions;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Data.Repositories;

/// <summary>
/// Repository for workflow instance persistence.
/// </summary>
public class WorkflowInstanceRepository : IRepository<WorkflowInstance>
{
    private readonly Dictionary<string, WorkflowInstance> _instances = new();
    private readonly object _syncRoot = new();

    /// <summary>
    /// Gets an instance by ID. The returned instance is an independent clone: mutating it has no
    /// effect on persisted state until it is passed back through <see cref="UpdateAsync"/>, which
    /// re-validates its <see cref="WorkflowInstance.Version"/> against what is currently stored.
    /// </summary>
    /// <param name="id">The identifier of the instance to load.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is null or empty.</exception>
    public Task<WorkflowInstance?> GetByIdAsync(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        lock (_syncRoot)
        {
            return Task.FromResult(_instances.TryGetValue(id, out var instance) ? instance.Clone() : null);
        }
    }

    /// <summary>
    /// Gets all instances as independent clones of the persisted state.
    /// </summary>
    public Task<List<WorkflowInstance>> GetAllAsync()
    {
        lock (_syncRoot)
        {
            return Task.FromResult(_instances.Values.Select(i => i.Clone()).ToList());
        }
    }

    /// <summary>
    /// Adds a new instance, seeding its optimistic concurrency version at zero.
    /// </summary>
    /// <param name="entity">The workflow instance to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is null.</exception>
    /// <exception cref="WorkflowConcurrencyException">Thrown when an instance with the same ID already exists.</exception>
    public Task AddAsync(WorkflowInstance entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        lock (_syncRoot)
        {
            if (_instances.ContainsKey(entity.Id))
                throw new WorkflowConcurrencyException(entity.Id, entity.Version, _instances[entity.Id].Version);

            entity.Version = 0;
            _instances[entity.Id] = entity.Clone();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates an instance using an atomic compare-and-swap on <see cref="WorkflowInstance.Version"/>.
    /// The update only succeeds when <paramref name="entity"/>'s version matches the version currently
    /// stored; on success the stored version is incremented, guaranteeing no write silently overwrites
    /// a concurrent change (last-write-wins is not possible).
    /// </summary>
    /// <param name="entity">The workflow instance to persist, carrying the version it was loaded with.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is null.</exception>
    /// <exception cref="WorkflowException">Thrown when no instance with the given ID exists.</exception>
    /// <exception cref="WorkflowConcurrencyException">
    /// Thrown when <paramref name="entity"/>'s <see cref="WorkflowInstance.Version"/> does not match the
    /// version currently stored, indicating the instance was modified by another process in between.
    /// </exception>
    public Task UpdateAsync(WorkflowInstance entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        lock (_syncRoot)
        {
            if (!_instances.TryGetValue(entity.Id, out var existing))
                throw new WorkflowException($"Instance '{entity.Id}' not found", "INSTANCE_NOT_FOUND");

            if (existing.Version != entity.Version)
                throw new WorkflowConcurrencyException(entity.Id, entity.Version, existing.Version);

            // Compare succeeded: increment version and store an independent clone of the new
            // state so the caller's own reference can no longer mutate persisted data directly.
            entity.Version++;
            _instances[entity.Id] = entity.Clone();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Deletes an instance, atomically verifying the caller's expected version first so a delete
    /// cannot silently discard a concurrent update.
    /// </summary>
    /// <param name="id">The identifier of the instance to delete.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is null or empty.</exception>
    public Task DeleteAsync(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        lock (_syncRoot)
        {
            _instances.Remove(id);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Deletes an instance using an atomic compare-and-swap on <see cref="WorkflowInstance.Version"/>,
    /// ensuring the caller only deletes the version of the instance it actually observed.
    /// </summary>
    /// <param name="id">The identifier of the instance to delete.</param>
    /// <param name="expectedVersion">The version the caller last observed for this instance.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is null or empty.</exception>
    /// <exception cref="WorkflowConcurrencyException">
    /// Thrown when <paramref name="expectedVersion"/> does not match the version currently stored.
    /// </exception>
    public Task DeleteWithConcurrencyCheckAsync(string id, int expectedVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        lock (_syncRoot)
        {
            if (_instances.TryGetValue(id, out var existing) && existing.Version != expectedVersion)
                throw new WorkflowConcurrencyException(id, expectedVersion, existing.Version);

            _instances.Remove(id);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks if an instance exists.
    /// </summary>
    public Task<bool> ExistsAsync(string id)
    {
        return Task.FromResult(_instances.ContainsKey(id));
    }

    /// <summary>
    /// Gets the count of instances.
    /// </summary>
    public Task<int> CountAsync()
    {
        return Task.FromResult(_instances.Count);
    }

    /// <summary>
    /// Gets instances with pagination.
    /// </summary>
    public Task<(List<WorkflowInstance> Items, int Total)> GetPagedAsync(int pageNumber, int pageSize)
    {
        lock (_syncRoot)
        {
            var total = _instances.Count;
            var items = _instances.Values
                .OrderByDescending(i => i.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(i => i.Clone())
                .ToList();

            return Task.FromResult((items, total));
        }
    }

    /// <summary>
    /// Gets instances by workflow ID.
    /// </summary>
    public Task<List<WorkflowInstance>> GetByWorkflowIdAsync(string workflowId)
    {
        lock (_syncRoot)
        {
            var instances = _instances.Values.Where(i => i.WorkflowId == workflowId).Select(i => i.Clone()).ToList();
            return Task.FromResult(instances);
        }
    }

    /// <summary>
    /// Gets instances by status.
    /// </summary>
    public Task<List<WorkflowInstance>> GetByStatusAsync(WorkflowStatus status)
    {
        lock (_syncRoot)
        {
            var instances = _instances.Values.Where(i => i.Status == status).Select(i => i.Clone()).ToList();
            return Task.FromResult(instances);
        }
    }

    /// <summary>
    /// Gets active instances.
    /// </summary>
    public Task<List<WorkflowInstance>> GetActiveInstancesAsync()
    {
        lock (_syncRoot)
        {
            var instances = _instances.Values.Where(i => i.IsActive()).Select(i => i.Clone()).ToList();
            return Task.FromResult(instances);
        }
    }

    /// <summary>
    /// Gets instances by correlation ID.
    /// </summary>
    public Task<List<WorkflowInstance>> GetByCorrelationIdAsync(string correlationId)
    {
        lock (_syncRoot)
        {
            var instances = _instances.Values.Where(i => i.CorrelationId == correlationId).Select(i => i.Clone()).ToList();
            return Task.FromResult(instances);
        }
    }

    /// <summary>
    /// Gets instances created within a time range.
    /// </summary>
    public Task<List<WorkflowInstance>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        lock (_syncRoot)
        {
            var instances = _instances.Values
                .Where(i => i.CreatedAt >= from && i.CreatedAt <= to)
                .Select(i => i.Clone())
                .ToList();

            return Task.FromResult(instances);
        }
    }

    /// <summary>
    /// Gets failed instances.
    /// </summary>
    public Task<List<WorkflowInstance>> GetFailedInstancesAsync()
    {
        lock (_syncRoot)
        {
            var instances = _instances.Values.Where(i => !string.IsNullOrEmpty(i.ErrorMessage)).Select(i => i.Clone()).ToList();
            return Task.FromResult(instances);
        }
    }

    /// <summary>
    /// Gets statistics on instances.
    /// </summary>
    public Task<(int Total, int Active, int Completed, int Failed)> GetStatisticsAsync()
    {
        var total = _instances.Count;
        var active = _instances.Values.Count(i => i.IsActive());
        var completed = _instances.Values.Count(i => i.Status == WorkflowStatus.Archived && string.IsNullOrEmpty(i.ErrorMessage));
        var failed = _instances.Values.Count(i => !string.IsNullOrEmpty(i.ErrorMessage));

        return Task.FromResult((total, active, completed, failed));
    }

    /// <summary>
    /// Gets instances with advanced filtering and pagination.
    /// </summary>
    /// <param name="workflowId">Filter by workflow ID</param>
    /// <param name="status">Filter by status (string representation)</param>
    /// <param name="createdFrom">Filter instances created on or after this date</param>
    /// <param name="createdTo">Filter instances created on or before this date</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    public Task<(List<WorkflowInstance> Items, int Total)> GetFilteredPagedAsync(
        string? workflowId = null,
        string? status = null,
        DateTime? createdFrom = null,
        DateTime? createdTo = null,
        int pageNumber = 1,
        int pageSize = 50)
    {
        List<WorkflowInstance> snapshot;
        lock (_syncRoot)
        {
            snapshot = _instances.Values.Select(i => i.Clone()).ToList();
        }

        var query = snapshot.AsQueryable();

        // Apply workflow ID filter
        if (!string.IsNullOrWhiteSpace(workflowId))
        {
            query = query.Where(i => i.WorkflowId.Equals(workflowId, StringComparison.OrdinalIgnoreCase));
        }

        // Apply status filter
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<WorkflowStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(i => i.Status == parsedStatus);
            }
        }

        // Apply created date range filter
        if (createdFrom.HasValue)
        {
            query = query.Where(i => i.CreatedAt >= createdFrom.Value);
        }

        if (createdTo.HasValue)
        {
            query = query.Where(i => i.CreatedAt <= createdTo.Value);
        }

        // Calculate total count before pagination
        var total = query.Count();

        // Apply pagination
        var items = query
            .OrderByDescending(i => i.CreatedAt)
            .ThenBy(i => i.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult((items, total));
    }

    /// <summary>
    /// Clears all instances.
    /// </summary>
    public Task ClearAsync()
    {
        _instances.Clear();
        return Task.CompletedTask;
    }
}
