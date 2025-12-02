// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;

namespace DotNetWorkflowEngine.Utilities;

/// <summary>
/// Fluent builder for constructing workflows programmatically.
/// </summary>
public class WorkflowBuilder
{
    private readonly Workflow _workflow;
    private readonly WorkflowDefinitionService _service;

    /// <summary>
    /// Initializes a new workflow builder.
    /// </summary>
    public WorkflowBuilder(string id, string name, WorkflowDefinitionService service)
    {
        _workflow = new Workflow { Id = id, Name = name };
        _service = service;
    }

    /// <summary>
    /// Sets the description of the workflow.
    /// </summary>
    public WorkflowBuilder WithDescription(string description)
    {
        _workflow.Description = description;
        return this;
    }

    /// <summary>
    /// Adds an activity to the workflow.
    /// </summary>
    public WorkflowBuilder AddActivity(Activity activity)
    {
        _workflow.Activities.Add(activity);
        return this;
    }

    /// <summary>
    /// Adds a simple task activity.
    /// </summary>
    public WorkflowBuilder AddTaskActivity(string id, string name, string? handlerType = null)
    {
        var activity = new Activity
        {
            Id = id,
            Name = name,
            Type = "Task",
            HandlerType = handlerType,
            ExecutionMode = ExecutionMode.Sequential
        };

        _workflow.Activities.Add(activity);
        return this;
    }

    /// <summary>
    /// Adds a transition between activities.
    /// </summary>
    public WorkflowBuilder AddTransition(string fromId, string toId, string? condition = null)
    {
        var transition = string.IsNullOrEmpty(condition)
            ? Transition.CreateDefault(fromId, toId)
            : Transition.CreateConditional(fromId, toId, condition);

        _workflow.Transitions.Add(transition);
        return this;
    }

    /// <summary>
    /// Sets the start activity.
    /// </summary>
    public WorkflowBuilder WithStartActivity(string activityId)
    {
        _workflow.StartActivityId = activityId;
        return this;
    }

    /// <summary>
    /// Sets the end activity.
    /// </summary>
    public WorkflowBuilder WithEndActivity(string activityId)
    {
        _workflow.EndActivityId = activityId;
        return this;
    }

    /// <summary>
    /// Builds and validates the workflow.
    /// </summary>
    public Workflow Build()
    {
        if (!_workflow.Validate(out var errors))
        {
            throw new Exceptions.ValidationException("Workflow validation failed", errors, "Workflow");
        }

        return _workflow;
    }

    /// <summary>
    /// Builds and registers the workflow with the service.
    /// </summary>
    public Workflow BuildAndRegister()
    {
        var workflow = Build();
        _service.CreateWorkflow(workflow.Id, workflow.Name, workflow.Description);

        foreach (var activity in workflow.Activities)
        {
            _service.AddActivity(workflow.Id, activity);
        }

        foreach (var transition in workflow.Transitions)
        {
            _service.AddTransition(workflow.Id, transition);
        }

        if (!string.IsNullOrEmpty(workflow.StartActivityId))
            _service.SetStartActivity(workflow.Id, workflow.StartActivityId);

        if (!string.IsNullOrEmpty(workflow.EndActivityId))
            _service.SetEndActivity(workflow.Id, workflow.EndActivityId);

        return workflow;
    }

    /// <summary>
    /// Creates a new builder for a serial workflow (activities connected in sequence).
    /// </summary>
    public static WorkflowBuilder CreateSerial(string id, string name, WorkflowDefinitionService service, params string[] activityNames)
    {
        var builder = new WorkflowBuilder(id, name, service);

        string? previousId = null;
        foreach (var actName in activityNames)
        {
            var actId = actName.ToLowerInvariant();
            builder.AddTaskActivity(actId, actName);

            if (previousId != null)
                builder.AddTransition(previousId, actId);
            else
                builder.WithStartActivity(actId);

            previousId = actId;
        }

        if (previousId != null)
            builder.WithEndActivity(previousId);

        return builder;
    }
}
