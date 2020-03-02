// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
///   <item>Create an instance via <see cref="CreateInstance"/> from a published workflow definition</item>
///   <item>Start execution with <see cref="StartAsync"/> which runs the start activity</item>
///   <item>Activities execute in sequence, following transitions defined in the workflow graph</item>
///   <item>Instance completes when no more transitions remain, or fails on unhandled exceptions</item>
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
    public WorkflowExecutionService(
        WorkflowDefinitionService definitionService,
        AuditService auditService,
        ActivityService activityService)
    {
        _definitionService = definitionService;
        _auditService = auditService;
        _activityService = activityService;
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
        _auditService.LogInstanceCreated(instance.Id, initiatedBy ?? "System");

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
        var instance = GetInstance(instanceId);
        if (instance == null)
            throw new WorkflowException($"Instance '{instanceId}' not found", "INSTANCE_NOT_FOUND");

        if (!instance.IsActive())
            throw new StateException("Instance is not in active state", instance.Status.ToString(), "Active");

        instance.Start();
        var workflow = _definitionService.GetWorkflow(instance.WorkflowId);
        if (workflow?.StartActivityId == null)
            throw new WorkflowException("Workflow has no start activity", "NO_START_ACTIVITY");

        _auditService.LogInstanceStarted(instance.Id);

        // Execute start activity
        await ExecuteActivityAsync(instance, workflow.StartActivityId);

        return instance;
    }

    /// <summary>
    /// Executes a specific activity within an instance.
    /// </summary>
    public async Task ExecuteActivityAsync(WorkflowInstance instance, string activityId)
    {
        var workflow = _definitionService.GetWorkflow(instance.WorkflowId);
        if (workflow == null)
            throw new WorkflowException($"Workflow '{instance.WorkflowId}' not found", "WORKFLOW_NOT_FOUND");

        var activity = workflow.Activities.FirstOrDefault(a => a.Id == activityId);
        if (activity == null)
            throw new WorkflowException($"Activity '{activityId}' not found", "ACTIVITY_NOT_FOUND");

        instance.CurrentActivityId = activityId;
        instance.ActiveActivities.Add(activityId);

        try
        {
            var context = new ExecutionContext
            {
                WorkflowInstanceId = instance.Id,
                ActivityId = activityId,
                CorrelationId = instance.CorrelationId ?? instance.Id
            };

            // Set input parameters from workflow context
            foreach (var param in activity.InputParameters)
            {
                context.SetActivityInput(param.Key, param.Value);
            }

            var result = await _activityService.ExecuteAsync(activity, context);
                    if (result.Output.TryGetValue("ExecutionMode", out var executionModeValue) && executionModeValue.ToString() == "Parallel")
                    {
                        // Add proper synchronization
                        await Task.WhenAll(instance.ActiveActivities.Select(a => ExecuteActivityAsync(instance, a)));
                    }

            // Map outputs back to workflow context
            foreach (var mapping in activity.OutputMapping)
            {
                if (result.Output.TryGetValue(mapping.Key, out var value))
                {
                    instance.SetContextVariable(mapping.Value, value);
                }
            }

            instance.RecordActivityExecution(activityId);
            _auditService.LogActivityCompleted(instance.Id, activityId, result);

            // Get next activities
            var nextActivities = workflow.GetNextActivities(activityId);
            foreach (var next in nextActivities)
            {
                await ExecuteActivityAsync(instance, next.Id);
            }
        }
        catch (Exception ex)
        {
            _auditService.LogActivityFailed(instance.Id, activityId, ex.Message);
            instance.Fail(ex.Message);
            throw;
        }
        finally
        {
            instance.ActiveActivities.Remove(activityId);
        }
    }

    /// <summary>
    /// Completes a workflow instance.
    /// </summary>
    public void CompleteInstance(string instanceId)
    {
        var instance = GetInstance(instanceId);
        if (instance == null)
            throw new WorkflowException($"Instance '{instanceId}' not found", "INSTANCE_NOT_FOUND");

        instance.Complete();
        _auditService.LogInstanceCompleted(instanceId);
    }

    /// <summary>
    /// Fails a workflow instance with error message.
    /// </summary>
    public void FailInstance(string instanceId, string errorMessage)
    {
        var instance = GetInstance(instanceId);
        if (instance == null)
            throw new WorkflowException($"Instance '{instanceId}' not found", "INSTANCE_NOT_FOUND");

        instance.Fail(errorMessage);
        _auditService.LogInstanceFailed(instanceId, errorMessage);
    }

    /// <summary>
    /// Gets a workflow instance.
    /// </summary>
    public WorkflowInstance? GetInstance(string instanceId)
    {
        _instances.TryGetValue(instanceId, out var instance);
        return instance;
    }

    /// <summary>
    /// Gets all instances for a workflow.
    /// </summary>
    public List<WorkflowInstance> GetInstancesByWorkflow(string workflowId)
    {
        return _instances.Values.Where(i => i.WorkflowId == workflowId).ToList();
    }

    /// <summary>
    /// Gets instances by correlation ID.
    /// </summary>
    public List<WorkflowInstance> GetInstancesByCorrelation(string correlationId)
    {
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
    public async Task ResumeInstanceAsync(string instanceId)
    {
        var instance = GetInstance(instanceId);
        if (instance == null)
            throw new WorkflowException($"Instance '{instanceId}' not found", "INSTANCE_NOT_FOUND");

        if (instance.CurrentActivityId == null)
            throw new WorkflowException("Instance has no current activity", "NO_CURRENT_ACTIVITY");

        _auditService.LogInstanceResumed(instanceId);
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
}
