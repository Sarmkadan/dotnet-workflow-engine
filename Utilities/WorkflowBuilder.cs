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
    /// Initializes a new instance of the <see cref="WorkflowBuilder"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the workflow.</param>
    /// <param name="name">The display name of the workflow.</param>
    /// <param name="service">The service used to register the completed workflow.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="id"/>, <paramref name="name"/>, or <paramref name="service"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> or <paramref name="name"/> is empty.</exception>
    public WorkflowBuilder(string id, string name, WorkflowDefinitionService service)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(service);
        _workflow = new Workflow { Id = id, Name = name };
        _service = service;
    }

    /// <summary>
    /// Sets the description of the workflow.
    /// </summary>
    /// <param name="description">The workflow description.</param>
    /// <returns>This builder instance, so additional operations can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="description"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="description"/> is empty.</exception>
    public WorkflowBuilder WithDescription(string description)
    {
        ArgumentException.ThrowIfNullOrEmpty(description);
        _workflow.Description = description;
        return this;
    }

    /// <summary>
    /// Adds an activity to the workflow.
    /// </summary>
    /// <param name="activity">The activity to add.</param>
    /// <returns>This builder instance, so additional operations can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="activity"/> is <see langword="null"/>.</exception>
    public WorkflowBuilder AddActivity(Activity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        _workflow.Activities.Add(activity);
        return this;
    }

    /// <summary>
    /// Adds a BPMN 2.0 intermediate message catch event that suspends execution until
    /// an external message matching <paramref name="messageName"/> arrives with a
    /// correlation key derived from <paramref name="correlationProperty"/> on the
    /// workflow instance context.
    /// </summary>
    /// <param name="id">Unique activity identifier.</param>
    /// <param name="name">Human-readable name for the event node.</param>
    /// <param name="messageName">The message name to subscribe to (e.g. "PaymentConfirmed").</param>
    /// <param name="correlationProperty">
    /// The workflow context variable whose value is used as the correlation key when
    /// matching the incoming message to this instance.
    /// </param>
    /// <returns>This builder instance, so additional operations can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when any parameter is empty.</exception>
    public WorkflowBuilder AddMessageCatchEvent(string id, string name, string messageName, string correlationProperty)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(messageName);
        ArgumentException.ThrowIfNullOrEmpty(correlationProperty);
        var activity = new Activity
        {
            Id = id,
            Name = name,
            Type = "MessageCatchEvent",
            MessageName = messageName,
            CorrelationProperty = correlationProperty,
            ExecutionMode = ExecutionMode.Sequential
        };

        _workflow.Activities.Add(activity);
        return this;
    }

    /// <summary>
    /// Adds a simple task activity.
    /// </summary>
    /// <param name="id">The unique identifier for the activity.</param>
    /// <param name="name">The display name of the activity.</param>
    /// <param name="handlerType">The optional type name of the activity handler.</param>
    /// <returns>This builder instance, so additional operations can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="id"/> or <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> or <paramref name="name"/> is empty.</exception>
    public WorkflowBuilder AddTaskActivity(string id, string name, string? handlerType = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(name);
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
    /// <param name="fromId">The identifier of the source activity.</param>
    /// <param name="toId">The identifier of the destination activity.</param>
    /// <param name="condition">The optional condition that must be satisfied for the transition to be followed.</param>
    /// <returns>This builder instance, so additional operations can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="fromId"/> or <paramref name="toId"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fromId"/> or <paramref name="toId"/> is empty.</exception>
    public WorkflowBuilder AddTransition(string fromId, string toId, string? condition = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(fromId);
        ArgumentException.ThrowIfNullOrEmpty(toId);
        var transition = string.IsNullOrEmpty(condition)
            ? Transition.CreateDefault(fromId, toId)
            : Transition.CreateConditional(fromId, toId, condition);

        _workflow.Transitions.Add(transition);
        return this;
    }

    /// <summary>
    /// Sets the start activity.
    /// </summary>
    /// <param name="activityId">The identifier of the activity at which workflow execution starts.</param>
    /// <returns>This builder instance, so additional operations can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="activityId"/> is <see langword="null"/>.</exception>
    public WorkflowBuilder WithStartActivity(string activityId)
    {
        ArgumentNullException.ThrowIfNull(activityId);
        _workflow.StartActivityId = activityId;
        return this;
    }

    /// <summary>
    /// Sets the end activity.
    /// </summary>
    /// <param name="activityId">The identifier of the activity at which workflow execution ends.</param>
    /// <returns>This builder instance, so additional operations can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="activityId"/> is <see langword="null"/>.</exception>
    public WorkflowBuilder WithEndActivity(string activityId)
    {
        ArgumentNullException.ThrowIfNull(activityId);
        _workflow.EndActivityId = activityId;
        return this;
    }

    /// <summary>
    /// Builds and validates the workflow.
    /// </summary>
    /// <returns>The validated workflow.</returns>
    /// <exception cref="Exceptions.ValidationException">Thrown when the workflow definition is invalid.</exception>
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
    /// <returns>The validated workflow that was registered.</returns>
    /// <exception cref="Exceptions.ValidationException">Thrown when the workflow definition is invalid.</exception>
    /// <exception cref="Exceptions.WorkflowException">
    /// Thrown when a workflow with the same identifier is already registered or registration otherwise conflicts with
    /// an existing definition.
    /// </exception>
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
    /// <param name="id">The unique identifier for the workflow.</param>
    /// <param name="name">The display name of the workflow.</param>
    /// <param name="service">The service used to register the completed workflow.</param>
    /// <param name="activityNames">The activity names to add in execution order.</param>
    /// <returns>A builder containing task activities connected in the specified order.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="id"/>, <paramref name="name"/>, or <paramref name="service"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/>, <paramref name="name"/>, or an activity name is empty.
    /// </exception>
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
