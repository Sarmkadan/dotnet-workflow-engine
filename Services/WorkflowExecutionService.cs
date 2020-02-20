// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Exceptions;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Services;

/// <summary>
/// Service for executing workflows and managing instances.
/// </summary>
public class WorkflowExecutionService
{
    private readonly Dictionary<string, WorkflowInstance> _instances = new();
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
    /// Creates a new workflow instance.
    /// </summary>
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
    /// Starts execution of a workflow instance.
    /// </summary>
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

            instance.ActiveActivities.Remove(activityId);
        }
        catch (Exception ex)
        {
            _auditService.LogActivityFailed(instance.Id, activityId, ex.Message);
            instance.Fail(ex.Message);
            throw;
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
