// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Exceptions;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Services;

/// <summary>
/// Service for managing workflow definitions.
/// </summary>
public class WorkflowDefinitionService
{
    private readonly Dictionary<string, Workflow> _workflows = new();

    /// <summary>
    /// Creates a new workflow definition.
    /// </summary>
    public Workflow CreateWorkflow(string id, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ValidationException("Workflow ID cannot be empty", "INVALID_ID");

        if (_workflows.ContainsKey(id))
            throw new WorkflowException($"Workflow with ID '{id}' already exists", "WORKFLOW_EXISTS");

        var workflow = new Workflow
        {
            Id = id,
            Name = name,
            Description = description
        };

        _workflows[id] = workflow;
        return workflow;
    }

    /// <summary>
    /// Gets a workflow definition by ID.
    /// </summary>
    public Workflow? GetWorkflow(string id)
    {
        _workflows.TryGetValue(id, out var workflow);
        return workflow;
    }

    /// <summary>
    /// Gets all workflow definitions.
    /// </summary>
    public List<Workflow> GetAllWorkflows()
    {
        return _workflows.Values.ToList();
    }

    /// <summary>
    /// Adds an activity to a workflow.
    /// </summary>
    public void AddActivity(string workflowId, Activity activity)
    {
        var workflow = GetWorkflow(workflowId);
        if (workflow == null)
            throw new WorkflowException($"Workflow '{workflowId}' not found", "WORKFLOW_NOT_FOUND");

        if (workflow.Activities.Any(a => a.Id == activity.Id))
            throw new WorkflowException($"Activity '{activity.Id}' already exists", "ACTIVITY_EXISTS");

        if (!activity.Validate(out var errors))
            throw new ValidationException("Invalid activity", errors, "Activity");

        workflow.Activities.Add(activity);
        workflow.ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a transition between activities.
    /// </summary>
    public void AddTransition(string workflowId, Transition transition)
    {
        var workflow = GetWorkflow(workflowId);
        if (workflow == null)
            throw new WorkflowException($"Workflow '{workflowId}' not found", "WORKFLOW_NOT_FOUND");

        if (!transition.Validate(out var errors))
            throw new ValidationException("Invalid transition", errors, "Transition");

        if (!workflow.Activities.Any(a => a.Id == transition.FromActivityId))
            throw new WorkflowException($"Activity '{transition.FromActivityId}' not found", "ACTIVITY_NOT_FOUND");

        if (!workflow.Activities.Any(a => a.Id == transition.ToActivityId))
            throw new WorkflowException($"Activity '{transition.ToActivityId}' not found", "ACTIVITY_NOT_FOUND");

        if (workflow.Transitions.Any(t => t.Id == transition.Id))
            throw new WorkflowException($"Transition '{transition.Id}' already exists", "TRANSITION_EXISTS");

        workflow.Transitions.Add(transition);
        workflow.ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the start activity for a workflow.
    /// </summary>
    public void SetStartActivity(string workflowId, string activityId)
    {
        var workflow = GetWorkflow(workflowId);
        if (workflow == null)
            throw new WorkflowException($"Workflow '{workflowId}' not found", "WORKFLOW_NOT_FOUND");

        if (!workflow.Activities.Any(a => a.Id == activityId))
            throw new WorkflowException($"Activity '{activityId}' not found", "ACTIVITY_NOT_FOUND");

        workflow.StartActivityId = activityId;
        workflow.ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the end activity for a workflow.
    /// </summary>
    public void SetEndActivity(string workflowId, string activityId)
    {
        var workflow = GetWorkflow(workflowId);
        if (workflow == null)
            throw new WorkflowException($"Workflow '{workflowId}' not found", "WORKFLOW_NOT_FOUND");

        if (!workflow.Activities.Any(a => a.Id == activityId))
            throw new WorkflowException($"Activity '{activityId}' not found", "ACTIVITY_NOT_FOUND");

        workflow.EndActivityId = activityId;
        workflow.ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Publishes a workflow to make it active.
    /// </summary>
    public void PublishWorkflow(string workflowId)
    {
        var workflow = GetWorkflow(workflowId);
        if (workflow == null)
            throw new WorkflowException($"Workflow '{workflowId}' not found", "WORKFLOW_NOT_FOUND");

        try
        {
            workflow.Publish();
        }
        catch (ValidationException)
        {
            throw;
        }

        workflow.ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates a workflow without publishing it.
    /// </summary>
    public bool ValidateWorkflow(string workflowId, out List<string> errors)
    {
        var workflow = GetWorkflow(workflowId);
        if (workflow == null)
        {
            errors = new List<string> { $"Workflow '{workflowId}' not found" };
            return false;
        }

        return workflow.Validate(out errors);
    }

    /// <summary>
    /// Gets all activities in a workflow.
    /// </summary>
    public List<Activity> GetActivities(string workflowId)
    {
        var workflow = GetWorkflow(workflowId);
        if (workflow == null)
            throw new WorkflowException($"Workflow '{workflowId}' not found", "WORKFLOW_NOT_FOUND");

        return workflow.Activities;
    }

    /// <summary>
    /// Gets a specific activity from a workflow.
    /// </summary>
    public Activity? GetActivity(string workflowId, string activityId)
    {
        var workflow = GetWorkflow(workflowId);
        if (workflow == null)
            return null;

        return workflow.Activities.FirstOrDefault(a => a.Id == activityId);
    }

    /// <summary>
    /// Deletes a workflow definition.
    /// </summary>
    public bool DeleteWorkflow(string workflowId)
    {
        return _workflows.Remove(workflowId);
    }

    /// <summary>
    /// Clones a workflow definition.
    /// </summary>
    public Workflow CloneWorkflow(string sourceWorkflowId, string newWorkflowId, string newName)
    {
        var source = GetWorkflow(sourceWorkflowId);
        if (source == null)
            throw new WorkflowException($"Source workflow '{sourceWorkflowId}' not found", "WORKFLOW_NOT_FOUND");

        var clone = new Workflow
        {
            Id = newWorkflowId,
            Name = newName,
            Description = source.Description,
            StartActivityId = source.StartActivityId,
            EndActivityId = source.EndActivityId
        };

        foreach (var activity in source.Activities)
        {
            clone.Activities.Add(new Activity
            {
                Id = activity.Id,
                Name = activity.Name,
                Description = activity.Description,
                Type = activity.Type,
                ExecutionMode = activity.ExecutionMode,
                HandlerType = activity.HandlerType,
                InputParameters = new Dictionary<string, object?>(activity.InputParameters),
                OutputMapping = new Dictionary<string, string>(activity.OutputMapping),
                RetryPolicy = activity.RetryPolicy,
                MaxRetries = activity.MaxRetries,
                TimeoutSeconds = activity.TimeoutSeconds,
                IsOptional = activity.IsOptional,
                ConditionExpression = activity.ConditionExpression,
                Metadata = new Dictionary<string, object?>(activity.Metadata)
            });
        }

        foreach (var transition in source.Transitions)
        {
            clone.Transitions.Add(new Transition
            {
                Id = transition.Id,
                FromActivityId = transition.FromActivityId,
                ToActivityId = transition.ToActivityId,
                ConditionExpression = transition.ConditionExpression,
                Label = transition.Label,
                IsDefault = transition.IsDefault,
                Priority = transition.Priority
            });
        }

        _workflows[newWorkflowId] = clone;
        return clone;
    }
}
