// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System.Collections.Concurrent;
using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Exceptions;
using DotNetWorkflowEngine.Models;
using ExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

namespace DotNetWorkflowEngine.Services;

/// <summary>
/// Core execution engine for workflows. Manages the full lifecycle of workflow instances -
/// creation, execution, suspension, resumption, completion, and failure handling.
/// </summary>
/// <remarks>
/// <para>
/// Workflow execution follows this sequence:
/// <list type="number">
/// <item>Create an instance via <see cref="CreateInstance"/> from a published workflow definition</item>
/// <item>Start execution with <see cref="StartAsync"/> which runs the start activity</item>
/// <item>Activities execute in sequence, following transitions defined in the workflow graph</item>
/// <item>Instance completes when no more transitions remain, or fails on unhandled exceptions</item>
/// </list>
/// </para>
/// <para>
/// All instances are stored in a thread-safe <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// and can be queried by workflow ID, correlation ID, or status.
/// </para>
/// </remarks>
public class WorkflowExecutionService
{
    private readonly ConcurrentDictionary<string, WorkflowInstance> _instances = new();
    private readonly WorkflowDefinitionService _definitionService;
    private readonly AuditService _auditService;
    private readonly ActivityService _activityService;

    /// <summary>
    /// Initializes the execution service with required dependencies.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
    public WorkflowExecutionService(
        WorkflowDefinitionService definitionService,
        AuditService auditService,
        ActivityService activityService)
    {
        _definitionService = definitionService ?? throw new ArgumentNullException(nameof(definitionService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
    }

    /// <summary>
    /// Creates a new workflow instance from a published workflow definition.
    /// The instance starts in an idle state and must be explicitly started via <see cref="StartAsync"/>.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow definition to instantiate.</param>
    /// <param name="correlationId">Optional business correlation ID for grouping related instances.</param>
    /// <param name="initiatedBy">Optional identifier of the user or system that triggered this instance.</param>
    /// <returns>The newly created <see cref="WorkflowInstance"/>.</returns>
    /// <exception cref="WorkflowException">Thrown when the workflow is not found or not in Active status.</exception>
    public WorkflowInstance CreateInstance(string workflowId, string? correlationId = null, string? initiatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            throw new ArgumentException("Workflow ID cannot be null or empty", nameof(workflowId));

        var workflow = _definitionService.GetWorkflow(workflowId);
        if (workflow == null)
            throw new WorkflowException($"Workflow '{workflowId}' not found", "WORKFLOW_NOT_FOUND");

        if (workflow.Status != WorkflowStatus.Active)
            throw new StateException("Workflow is not active", workflow.Status.ToString(), "Active", workflowId);

        var instance = new WorkflowInstance(workflowId, correlationId)
        {
            InitiatedBy = initiatedBy
        };

        _instances[instance.Id] = instance;
        _auditService.LogInstanceCreated(instance.Id, initiatedBy ?? "System").GetAwaiter().GetResult();

        return instance;
    }

    /// <summary>
    /// Begins executing a workflow instance by running its start activity and following
    /// transitions until completion or failure.
    /// </summary>
    /// <param name="instanceId">The ID of the instance to start.</param>
    /// <returns>The updated <see cref="WorkflowInstance"/> after the start activity completes.</returns>
    /// <exception cref="WorkflowException">Thrown when the instance is not found or has no start activity.</exception>
    /// <exception cref="StateException">Thrown when the instance is not in an active state.</exception>
    public async Task<WorkflowInstance> StartAsync(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        var instance = GetInstance(instanceId);
        if (instance == null)
            throw new WorkflowException($"Instance '{instanceId}' not found", "INSTANCE_NOT_FOUND");

        if (!instance.IsActive())
            throw new StateException("Instance is not in active state", instance.Status.ToString(), "Active");

        instance.Start();
        var workflow = _definitionService.GetWorkflow(instance.WorkflowId);
        if (workflow?.StartActivityId == null)
            throw new WorkflowException("Workflow has no start activity", "NO_START_ACTIVITY");

        await _auditService.LogInstanceStarted(instance.Id);

        // Execute start activity
        await ExecuteActivityAsync(instance, workflow.StartActivityId);

        return instance;
    }

    /// <summary>
    /// Executes a specific activity within an instance.
    /// </summary>
    /// <exception cref="WorkflowException">Thrown when workflow or activity not found.</exception>
    /// <exception cref="ActivityException">Thrown when activity execution fails.</exception>
    public async Task ExecuteActivityAsync(WorkflowInstance instance, string activityId)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        if (string.IsNullOrWhiteSpace(activityId))
            throw new ArgumentException("Activity ID cannot be null or empty", nameof(activityId));

        var workflow = _definitionService.GetWorkflow(instance.WorkflowId);
        if (workflow == null)
            throw new WorkflowException($"Workflow '{instance.WorkflowId}' not found", "WORKFLOW_NOT_FOUND");

        var activity = workflow.Activities.FirstOrDefault(a => a.Id == activityId);
        if (activity == null)
            throw new WorkflowException($"Activity '{activityId}' not found", "ACTIVITY_NOT_FOUND");

        instance.CurrentActivityId = activityId;
        lock (instance.ActiveActivities)
            instance.ActiveActivities.Add(activityId);

        // Handle MessageCatchEvent: suspend the workflow and wait for an external message.
        // The correlation key is read from the instance context (set by a prior activity).
        if (activity.Type == "MessageCatchEvent")
        {
            if (string.IsNullOrWhiteSpace(activity.MessageName))
                throw new ValidationException($"MessageCatchEvent '{activityId}' must specify a MessageName.", "MESSAGE_NAME_REQUIRED");
            if (string.IsNullOrWhiteSpace(activity.CorrelationProperty))
                throw new ValidationException($"MessageCatchEvent '{activityId}' must specify a CorrelationProperty.", "CORRELATION_PROPERTY_REQUIRED");

            var correlationKey = instance.GetContextVariable(activity.CorrelationProperty)?.ToString();
            if (string.IsNullOrWhiteSpace(correlationKey))
                throw new WorkflowException($"Correlation property '{activity.CorrelationProperty}' evaluated to null or empty for MessageCatchEvent '{activityId}'.", "CORRELATION_KEY_MISSING");

            instance.SetContextVariable("WaitingForMessageName", activity.MessageName);
            instance.SetContextVariable("WaitingForCorrelationKey", correlationKey);
            instance.SetContextVariable("WaitingActivityId", activityId);
            instance.Status = WorkflowStatus.WaitingForMessage;

            await _auditService.LogCustomEvent(instance.Id, "WorkflowSuspended",
                $"Workflow suspended at MessageCatchEvent '{activityId}', waiting for message '{activity.MessageName}' with correlation key '{correlationKey}'",
                "Info", activityId);

            lock (instance.ActiveActivities)
                instance.ActiveActivities.Remove(activityId);
            return;
        }

        try
        {
            var context = new ExecutionContext
            {
                WorkflowInstanceId = instance.Id,
                ActivityId = activityId,
                CorrelationId = instance.CorrelationId ?? instance.Id
            };

            // Seed the activity's execution context with the instance's current variables so
            // handlers and condition expressions see state produced by previously executed activities.
            foreach (var kvp in instance.Context)
                context.SetVariable(kvp.Key, kvp.Value);

            foreach (var param in activity.InputParameters)
            {
                context.SetActivityInput(param.Key, param.Value);
            }

            var result = await _activityService.ExecuteAsync(activity, context);

            // Persist any variables the handler set on the execution context back onto the
            // instance so subsequent activities and transition conditions can see them.
            foreach (var kvp in context.Variables)
                instance.SetContextVariable(kvp.Key, kvp.Value);

            foreach (var mapping in activity.OutputMapping)
            {
                if (result.Output.TryGetValue(mapping.Key, out var value))
                {
                    instance.SetContextVariable(mapping.Value, value);
                }
            }

            instance.RecordActivityExecution(activityId);
            await _auditService.LogActivityCompleted(instance.Id, activityId, result);

            var nextActivities = ResolveNextActivities(workflow, activityId, instance);

            // For a Fork (parallel split) gateway, execute all branches concurrently.
            // Every branch exception is captured so that a composite fault is surfaced
            // instead of letting the join barrier wait indefinitely.
            if (activity.ExecutionMode == ExecutionMode.Fork)
            {
                var branchExceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
                await Task.WhenAll(nextActivities.Select(async next =>
                {
                    try
                    {
                        await ExecuteActivityAsync(instance, next.Id);
                    }
                    catch (Exception ex)
                    {
                        branchExceptions.Add(ex);
                    }
                }));

                if (branchExceptions.Count > 0)
                {
                    var composite = new AggregateException(
                        $"Parallel gateway '{activityId}': {branchExceptions.Count} branch(es) failed.",
                        branchExceptions);
                    await _auditService.LogActivityFailed(instance.Id, activityId, composite.Message);
                    instance.Fail(composite.Message);
                    throw composite;
                }
            }
            else
            {
                foreach (var next in nextActivities)
                {
                    await ExecuteActivityAsync(instance, next.Id);
                }
            }
        }
        catch (AggregateException)
        {
            // Already logged and failed inside the Fork branch above; just propagate.
            throw;
        }
        catch (Exception ex)
        {
            await _auditService.LogActivityFailed(instance.Id, activityId, ex.Message);
            instance.Fail(ex.Message);
            throw;
        }
        finally
        {
            lock (instance.ActiveActivities)
                instance.ActiveActivities.Remove(activityId);
        }
    }

    /// <summary>
    /// Completes a workflow instance.
    /// </summary>
    /// <exception cref="WorkflowException">Thrown when instance not found.</exception>
    public void CompleteInstance(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        var instance = GetInstance(instanceId);
        if (instance == null)
            throw new WorkflowException($"Instance '{instanceId}' not found", "INSTANCE_NOT_FOUND");

        instance.Complete();
        _auditService.LogInstanceCompleted(instanceId).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Fails a workflow instance with error message.
    /// </summary>
    /// <exception cref="WorkflowException">Thrown when instance not found.</exception>
    public virtual void FailInstance(string instanceId, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message cannot be null or empty", nameof(errorMessage));

        var instance = GetInstance(instanceId);
        if (instance == null)
            throw new WorkflowException($"Instance '{instanceId}' not found", "INSTANCE_NOT_FOUND");

        instance.Fail(errorMessage);
        _auditService.LogInstanceFailed(instanceId, errorMessage).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets a workflow instance.
    /// </summary>
    public WorkflowInstance? GetInstance(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        _instances.TryGetValue(instanceId, out var instance);
        return instance;
    }

    /// <summary>
    /// Gets all instances for a workflow.
    /// </summary>
    public List<WorkflowInstance> GetInstancesByWorkflow(string workflowId)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            throw new ArgumentException("Workflow ID cannot be null or empty", nameof(workflowId));

        return _instances.Values.Where(i => i.WorkflowId == workflowId).ToList();
    }

    /// <summary>
    /// Gets instances by correlation ID.
    /// </summary>
    public virtual List<WorkflowInstance> GetInstancesByCorrelation(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("Correlation ID cannot be null or empty", nameof(correlationId));

        return _instances.Values.Where(i => i.CorrelationId == correlationId).ToList();
    }

    /// <summary>
    /// Gets all active instances.
    /// </summary>
    public List<WorkflowInstance> GetActiveInstances()
    {
        return _instances.Values.Where(i => i.IsActive()).ToList();
    }

    /// <summary>
    /// Resumes a suspended or waiting instance.
    /// </summary>
    /// <exception cref="WorkflowException">Thrown when instance not found or invalid.</exception>
    public async Task ResumeInstanceAsync(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        var instance = GetInstance(instanceId);
        if (instance == null)
            throw new WorkflowException($"Instance '{instanceId}' not found", "INSTANCE_NOT_FOUND");

        if (instance.CurrentActivityId == null)
            throw new WorkflowException("Instance has no current activity", "NO_CURRENT_ACTIVITY");

        await _auditService.LogInstanceResumed(instanceId);
        var workflow = _definitionService.GetWorkflow(instance.WorkflowId);
        if (workflow?.StartActivityId == null)
            throw new WorkflowException("Workflow has no start activity", "NO_START_ACTIVITY");

        // Continue with next activities
        var nextActivities = workflow.GetNextActivities(instance.CurrentActivityId);
        foreach (var activity in nextActivities)
        {
            await ExecuteActivityAsync(instance, activity.Id);
        }
    }

    /// <summary>
    /// Resumes a workflow instance that was suspended waiting for a message.
    /// </summary>
    /// <param name="instanceId">The ID of the instance to resume.</param>
    /// <param name="messageName">The name of the message that arrived.</param>
    /// <param name="correlationKey">The correlation key of the message.</param>
    /// <param name="messagePayload">The payload of the message.</param>
    /// <exception cref="WorkflowException">Thrown if the instance is not found, not waiting for a message, or the message details don't match.</exception>
    /// <exception cref="StateException">Thrown if the instance is in an unexpected state.</exception>
    public virtual async Task ResumeFromMessageAsync(string instanceId, string messageName, string correlationKey, Dictionary<string, object?> messagePayload)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));

        if (string.IsNullOrWhiteSpace(messageName))
            throw new ArgumentException("Message name cannot be null or empty", nameof(messageName));

        if (string.IsNullOrWhiteSpace(correlationKey))
            throw new ArgumentException("Correlation key cannot be null or empty", nameof(correlationKey));

        if (messagePayload == null)
            throw new ArgumentNullException(nameof(messagePayload));

        var instance = GetInstance(instanceId);
        if (instance == null)
            throw new WorkflowException($"Instance '{instanceId}' not found", "INSTANCE_NOT_FOUND");

        if (instance.Status != WorkflowStatus.WaitingForMessage)
            throw new StateException($"Instance '{instanceId}' is not waiting for a message.", instance.Status.ToString(), WorkflowStatus.WaitingForMessage.ToString());

        var expectedMessageName = instance.GetContextVariable("WaitingForMessageName")?.ToString();
        var expectedCorrelationKey = instance.GetContextVariable("WaitingForCorrelationKey")?.ToString();
        var waitingActivityId = instance.GetContextVariable("WaitingActivityId")?.ToString();

        if (expectedMessageName != messageName || expectedCorrelationKey != correlationKey || string.IsNullOrWhiteSpace(waitingActivityId))
        {
            await _auditService.LogCustomEvent(instance.Id, "MessageMismatch",
                $"Received message '{messageName}' with key '{correlationKey}' but instance was waiting for '{expectedMessageName}' with key '{expectedCorrelationKey}'.",
                "Error", waitingActivityId);
            throw new WorkflowException("Message correlation mismatch or waiting activity not found in instance context.", "MESSAGE_CORRELATION_MISMATCH");
        }

        // Clear waiting message metadata
        instance.Context.Remove("WaitingForMessageName");
        instance.Context.Remove("WaitingForCorrelationKey");
        instance.Context.Remove("WaitingActivityId");

        instance.Status = WorkflowStatus.Active;
        await _auditService.LogCustomEvent(instance.Id, "MessageReceived",
            $"Workflow instance resumed by message '{messageName}' with key '{correlationKey}'.",
            "Info", waitingActivityId);

        var workflow = _definitionService.GetWorkflow(instance.WorkflowId);
        if (workflow == null)
            throw new WorkflowException($"Workflow '{instance.WorkflowId}' not found", "WORKFLOW_NOT_FOUND");

        var waitingActivity = workflow.Activities.FirstOrDefault(a => a.Id == waitingActivityId);
        if (waitingActivity == null)
            throw new WorkflowException($"Waiting activity '{waitingActivityId}' not found in workflow definition.", "ACTIVITY_NOT_FOUND");

        // Inject message payload into the execution context for the resuming activity
        var context = new ExecutionContext
        {
            WorkflowInstanceId = instance.Id,
            ActivityId = waitingActivityId,
            CorrelationId = instance.CorrelationId ?? instance.Id
        };
        foreach (var entry in messagePayload)
        {
            context.SetActivityInput($"MessagePayload.{entry.Key}", entry.Value);
        }

        // Re-execute the waiting activity to process the message and continue the workflow
        // The MessageCatchEvent activity will now act as a no-op and transition to the next
        await ExecuteActivityAsync(instance, waitingActivityId);
    }

    /// <summary>
    /// Gets instance statistics.
    /// </summary>
    public (int Total, int Active, int Completed, int Failed) GetStatistics()
    {
        var total = _instances.Count;
        var active = _instances.Values.Count(i => i.IsActive());
        var completed = _instances.Values.Count(i => i.Status == WorkflowStatus.Archived && i.ErrorMessage == null);
        var failed = _instances.Values.Count(i => i.ErrorMessage != null);

        return (total, active, completed, failed);
    }

    /// <summary>
    /// Determines which activities to execute next after <paramref name="activityId"/> completes,
    /// honouring each outgoing transition's <c>ConditionExpression</c> against the instance's
    /// context variables. Conditional transitions are only followed when their expression
    /// evaluates to true; unconditional transitions are always followed; a default transition
    /// (<see cref="Transition.IsDefault"/>) is only used when nothing else matched.
    /// </summary>
    private static List<Activity> ResolveNextActivities(Workflow workflow, string activityId, WorkflowInstance instance)
    {
        var outgoing = workflow.Transitions.Where(t => t.FromActivityId == activityId).ToList();
        if (outgoing.Count == 0)
            return new List<Activity>();

        var evalContext = new ExecutionContext { WorkflowInstanceId = instance.Id };
        foreach (var kvp in instance.Context)
            evalContext.SetVariable(kvp.Key, kvp.Value);

        var conditionals = outgoing.Where(t => !t.IsDefault && t.ConditionExpression != null).ToList();
        var unconditionals = outgoing.Where(t => !t.IsDefault && t.ConditionExpression == null).ToList();
        var defaults = outgoing.Where(t => t.IsDefault).ToList();

        var selected = conditionals
            .Where(t => Utilities.ExpressionEvaluator.Evaluate(t.ConditionExpression!, evalContext))
            .ToList();

        selected.AddRange(unconditionals);

        if (selected.Count == 0 && defaults.Count > 0)
        {
            selected.Add(defaults.OrderByDescending(t => t.Priority).First());
        }

        var targetIds = selected.Select(t => t.ToActivityId).ToHashSet(StringComparer.Ordinal);
        return workflow.Activities.Where(a => targetIds.Contains(a.Id)).ToList();
    }
}