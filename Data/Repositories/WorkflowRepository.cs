// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Exceptions;

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
    /// <exception cref="ArgumentException">Thrown when ID is invalid.</exception>
    public Task<Workflow?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Workflow ID cannot be null or empty", nameof(id));

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
    /// <exception cref="ArgumentNullException">Thrown when workflow is null.</exception>
    /// <exception cref="ValidationException">Thrown when workflow ID is invalid.</exception>
    public Task AddAsync(Workflow entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (string.IsNullOrWhiteSpace(entity.Id))
            throw new ValidationException("Workflow ID cannot be empty", "INVALID_ID");

        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ValidationException("Workflow name cannot be empty", "INVALID_NAME");

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
    /// <exception cref="ArgumentNullException">Thrown when workflow is null.</exception>
    /// <exception cref="WorkflowException">Thrown when workflow not found.</exception>
    public Task UpdateAsync(Workflow entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (string.IsNullOrWhiteSpace(entity.Id))
            throw new ValidationException("Workflow ID cannot be empty", "INVALID_ID");

        if (!_workflows.ContainsKey(entity.Id))
            throw new WorkflowException($"Workflow '{entity.Id}' not found", "WORKFLOW_NOT_FOUND");

        _workflows[entity.Id] = entity;
        entity.ModifiedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Deletes a workflow.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when ID is invalid.</exception>
    public Task DeleteAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Workflow ID cannot be null or empty", nameof(id));

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
    /// <exception cref="ArgumentException">Thrown when ID is invalid.</exception>
    public Task<bool> ExistsAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Workflow ID cannot be null or empty", nameof(id));

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
    /// <exception cref="ArgumentException">Thrown when pagination parameters are invalid.</exception>
    public Task<(List<Workflow> Items, int Total)> GetPagedAsync(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            throw new ArgumentException("Page number must be positive", nameof(pageNumber));

        if (pageSize < 1)
            throw new ArgumentException("Page size must be positive", nameof(pageSize));

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
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));

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