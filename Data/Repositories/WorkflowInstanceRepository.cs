// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Data.Repositories;

/// <summary>
/// Repository for workflow instance persistence.
/// </summary>
public class WorkflowInstanceRepository : IRepository<WorkflowInstance>
{
    private readonly Dictionary<string, WorkflowInstance> _instances = new();

    /// <summary>
    /// Gets an instance by ID.
    /// </summary>
    public Task<WorkflowInstance?> GetByIdAsync(string id)
    {
        _instances.TryGetValue(id, out var instance);
        return Task.FromResult(instance);
    }

    /// <summary>
    /// Gets all instances.
    /// </summary>
    public Task<List<WorkflowInstance>> GetAllAsync()
    {
        return Task.FromResult(_instances.Values.ToList());
    }

    /// <summary>
    /// Adds a new instance.
    /// </summary>
    public Task AddAsync(WorkflowInstance entity)
    {
        if (!_instances.ContainsKey(entity.Id))
        {
            entity.Version = 0;
            _instances[entity.Id] = entity;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates an instance.
    /// </summary>
    public Task UpdateAsync(WorkflowInstance entity)
    {
        if (_instances.TryGetValue(entity.Id, out var existing))
        {
            // Check for optimistic concurrency conflict
            if (existing.Version != entity.Version)
            {
                throw new InvalidOperationException(
                    $"Concurrent modification detected for workflow instance '{entity.Id}'. " +
                    $"The instance was modified by another process. Please refresh and try again.");
            }

            // Increment version for next update
            entity.Version++;
            _instances[entity.Id] = entity;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Deletes an instance.
    /// </summary>
    public Task DeleteAsync(string id)
    {
        _instances.Remove(id);
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
        var total = _instances.Count;
        var items = _instances.Values
            .OrderByDescending(i => i.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult((items, total));
    }

    /// <summary>
    /// Gets instances by workflow ID.
    /// </summary>
    public Task<List<WorkflowInstance>> GetByWorkflowIdAsync(string workflowId)
    {
        var instances = _instances.Values.Where(i => i.WorkflowId == workflowId).ToList();
        return Task.FromResult(instances);
    }

    /// <summary>
    /// Gets instances by status.
    /// </summary>
    public Task<List<WorkflowInstance>> GetByStatusAsync(WorkflowStatus status)
    {
        var instances = _instances.Values.Where(i => i.Status == status).ToList();
        return Task.FromResult(instances);
    }

    /// <summary>
    /// Gets active instances.
    /// </summary>
    public Task<List<WorkflowInstance>> GetActiveInstancesAsync()
    {
        var instances = _instances.Values.Where(i => i.IsActive()).ToList();
        return Task.FromResult(instances);
    }

    /// <summary>
    /// Gets instances by correlation ID.
    /// </summary>
    public Task<List<WorkflowInstance>> GetByCorrelationIdAsync(string correlationId)
    {
        var instances = _instances.Values.Where(i => i.CorrelationId == correlationId).ToList();
        return Task.FromResult(instances);
    }

    /// <summary>
    /// Gets instances created within a time range.
    /// </summary>
    public Task<List<WorkflowInstance>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var instances = _instances.Values
            .Where(i => i.CreatedAt >= from && i.CreatedAt <= to)
            .ToList();

        return Task.FromResult(instances);
    }

    /// <summary>
    /// Gets failed instances.
    /// </summary>
    public Task<List<WorkflowInstance>> GetFailedInstancesAsync()
    {
        var instances = _instances.Values.Where(i => !string.IsNullOrEmpty(i.ErrorMessage)).ToList();
        return Task.FromResult(instances);
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
        var query = _instances.Values.AsQueryable();

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
