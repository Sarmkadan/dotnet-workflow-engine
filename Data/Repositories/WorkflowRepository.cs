// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Data.Repositories;

/// <summary>
/// Repository for workflow definition persistence.
/// </summary>
public class WorkflowRepository : IRepository<Workflow>
{
    private readonly Dictionary<string, Workflow> _workflows = new();
    private readonly List<Workflow> _allWorkflows = new();

    /// <summary>
    /// Gets a workflow by ID.
    /// </summary>
    public Task<Workflow?> GetByIdAsync(string id)
    {
        _workflows.TryGetValue(id, out var workflow);
        return Task.FromResult(workflow);
    }

    /// <summary>
    /// Gets all workflows.
    /// </summary>
    public Task<List<Workflow>> GetAllAsync()
    {
        return Task.FromResult(_workflows.Values.ToList());
    }

    /// <summary>
    /// Adds a new workflow.
    /// </summary>
    public Task AddAsync(Workflow entity)
    {
        if (!_workflows.ContainsKey(entity.Id))
        {
            _workflows[entity.Id] = entity;
            _allWorkflows.Add(entity);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates a workflow.
    /// </summary>
    public Task UpdateAsync(Workflow entity)
    {
        if (_workflows.ContainsKey(entity.Id))
        {
            _workflows[entity.Id] = entity;
            entity.ModifiedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Deletes a workflow.
    /// </summary>
    public Task DeleteAsync(string id)
    {
        if (_workflows.TryGetValue(id, out var workflow))
        {
            _workflows.Remove(id);
            _allWorkflows.Remove(workflow);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks if a workflow exists.
    /// </summary>
    public Task<bool> ExistsAsync(string id)
    {
        return Task.FromResult(_workflows.ContainsKey(id));
    }

    /// <summary>
    /// Gets the count of workflows.
    /// </summary>
    public Task<int> CountAsync()
    {
        return Task.FromResult(_workflows.Count);
    }

    /// <summary>
    /// Gets workflows with pagination.
    /// </summary>
    public Task<(List<Workflow> Items, int Total)> GetPagedAsync(int pageNumber, int pageSize)
    {
        var total = _workflows.Count;
        var items = _workflows.Values
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult((items, total));
    }

    /// <summary>
    /// Gets workflows by status.
    /// </summary>
    public Task<List<Workflow>> GetByStatusAsync(WorkflowStatus status)
    {
        var workflows = _workflows.Values.Where(w => w.Status == status).ToList();
        return Task.FromResult(workflows);
    }

    /// <summary>
    /// Gets active workflows.
    /// </summary>
    public Task<List<Workflow>> GetActiveWorkflowsAsync()
    {
        return GetByStatusAsync(WorkflowStatus.Active);
    }

    /// <summary>
    /// Gets workflows by name pattern.
    /// </summary>
    public Task<List<Workflow>> SearchByNameAsync(string pattern)
    {
        var workflows = _workflows.Values
            .Where(w => w.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Task.FromResult(workflows);
    }

    /// <summary>
    /// Gets workflows created after a date.
    /// </summary>
    public Task<List<Workflow>> GetCreatedSinceAsync(DateTime date)
    {
        var workflows = _workflows.Values.Where(w => w.CreatedAt >= date).ToList();
        return Task.FromResult(workflows);
    }

    /// <summary>
    /// Gets workflows with activity count.
    /// </summary>
    public Task<List<(Workflow Workflow, int ActivityCount)>> GetWithActivityCountAsync()
    {
        var result = _workflows.Values
            .Select(w => (w, w.Activities.Count))
            .ToList();

        return Task.FromResult(result);
    }

    /// <summary>
    /// Clears all workflows.
    /// </summary>
    public Task ClearAsync()
    {
        _workflows.Clear();
        _allWorkflows.Clear();
        return Task.CompletedTask;
    }
}
