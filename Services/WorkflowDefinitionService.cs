// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using DotNetWorkflowEngine.Exceptions;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Utilities;

namespace DotNetWorkflowEngine.Services;

/// <summary>
/// Service for managing workflow definitions with versioning support.
/// Workflow definitions are immutable once created. Updates create new versions
/// instead of mutating existing workflows, ensuring in-flight instances execute
/// against the version they were created with.
/// </summary>
public class WorkflowDefinitionService
{
    private readonly Dictionary<string, Workflow> _workflows = new();
    private readonly Dictionary<string, List<Workflow>> _workflowVersions = new();

    /// <summary>
    /// Creates a new workflow definition with version 1.
    /// </summary>
    /// <exception cref="ValidationException">Thrown when workflow ID is invalid.</exception>
    public Workflow CreateWorkflow(string id, string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (_workflows.ContainsKey(id))
            throw new WorkflowException($"Workflow with ID '{id}' already exists", "WORKFLOW_EXISTS");

        var workflow = new Workflow
        {
            Id = id,
            Name = name,
            Description = description,
            Version = 1
        };

        // Validate the workflow before adding it
        var validationResult = WorkflowValidator.ValidateWorkflow(workflow);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                "Workflow validation failed",
                validationResult.Errors,
                "Workflow"
            );
        }

        _workflows[id] = workflow;
        _workflowVersions[id] = new List<Workflow> { workflow };
        return workflow;
    }

    /// <summary>
    /// Registers an already-constructed workflow definition, overwriting any
    /// existing definition with the same ID.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when workflow is null.</exception>
    /// <exception cref="ValidationException">Thrown when workflow ID is invalid.</exception>
    public void AddWorkflow(Workflow workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        if (string.IsNullOrWhiteSpace(workflow.Id))
            throw new ValidationException("Workflow ID cannot be empty", "INVALID_ID");

        if (string.IsNullOrWhiteSpace(workflow.Name))
            throw new ValidationException("Workflow name cannot be empty", "INVALID_NAME");

        // Validate the workflow before adding it
        var validationResult = WorkflowValidator.ValidateWorkflow(workflow);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                "Workflow validation failed",
                validationResult.Errors,
                "Workflow"
            );
        }

        _workflows[workflow.Id] = workflow;

        // Track this version
        if (!_workflowVersions.TryGetValue(workflow.Id, out var versions))
        {
            versions = new List<Workflow>();
            _workflowVersions[workflow.Id] = versions;
        }
        versions.Add(workflow);
    }

    /// <summary>
    /// Gets a workflow definition by ID, returning the latest version.
    /// </summary>
    public virtual Workflow? GetWorkflow(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        _workflows.TryGetValue(id, out var workflow);
        return workflow;
    }

    /// <summary>
    /// Gets a specific version of a workflow definition by ID and version number.
    /// </summary>
    /// <param name="id">The workflow ID.</param>
    /// <param name="version">The specific version number to retrieve.</param>
    /// <returns>The workflow definition with the specified version, or null if not found.</returns>
    public virtual Workflow? GetWorkflowVersion(string id, int version)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        if (_workflowVersions.TryGetValue(id, out var versions))
        {
            return versions.FirstOrDefault(w => w.Version == version);
        }
        return null;
    }

    /// <summary>
    /// Gets all workflow definitions (latest versions only).
    /// </summary>
    public List<Workflow> GetAllWorkflows()
    {
        return _workflows.Values.ToList();
    }

    /// <summary>
    /// Gets all versions of a workflow definition.
    /// </summary>
    /// <param name="workflowId">The workflow ID.</param>
    /// <returns>List of all versions of the workflow, ordered by version number.</returns>
    public List<Workflow> GetWorkflowVersions(string workflowId)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowId);

        if (_workflowVersions.TryGetValue(workflowId, out var versions))
        {
            return versions.OrderBy(w => w.Version).ToList();
        }
        return new List<Workflow>();
    }

    /// <summary>
    /// Gets the latest version number for a workflow.
    /// </summary>
    /// <param name="workflowId">The workflow ID.</param>
    /// <returns>The latest version number, or 0 if workflow not found.</returns>
    public int GetLatestVersion(string workflowId)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowId);

        if (_workflowVersions.TryGetValue(workflowId, out var versions))
        {
            return versions.Max(w => w.Version);
        }
        return 0;
    }

    /// <summary>
    /// Creates a new version of an existing workflow by copying it and incrementing the version.
    /// This is the primary method for updating workflows - it creates an immutable new version
    /// instead of mutating the existing workflow.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow to update.</param>
    /// <param name="updateAction">Action that modifies the workflow definition.</param>
    /// <returns>The new version of the workflow.</returns>
    /// <exception cref="WorkflowException">Thrown when workflow not found.</exception>
    /// <exception cref="ValidationException">Thrown when workflow validation fails.</exception>
    public Workflow UpdateWorkflow(string workflowId, Action<Workflow> updateAction)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowId);
        ArgumentNullException.ThrowIfNull(updateAction);

        // Get the latest version
        if (!_workflows.TryGetValue(workflowId, out var latestWorkflow))
            throw new WorkflowException($"Workflow '{workflowId}' not found", "WORKFLOW_NOT_FOUND");

        // Create a new version by cloning the latest with incremented version
        var newVersionNumber = latestWorkflow.Version + 1;
        var newWorkflow = latestWorkflow.CloneWithVersion(newVersionNumber);

        // Apply the update
        updateAction(newWorkflow);

        // Validate the updated workflow
        var validationResult = WorkflowValidator.ValidateWorkflow(newWorkflow);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                "Updated workflow validation failed",
                validationResult.Errors,
                "Workflow"
            );
        }

        // Store the new version
        _workflows[workflowId] = newWorkflow;
        _workflowVersions[workflowId].Add(newWorkflow);

        return newWorkflow;
    }

    /// <summary>
    /// Adds an activity to the latest version of a workflow, creating a new version.
    /// </summary>
    /// <exception cref="WorkflowException">Thrown when workflow not found or activity already exists.</exception>
    /// <exception cref="ValidationException">Thrown when activity is invalid.</exception>
    public void AddActivity(string workflowId, Activity activity)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowId);
        ArgumentNullException.ThrowIfNull(activity);

        UpdateWorkflow(workflowId, workflow =>
        {
            if (workflow.Activities.Any(a => a.Id == activity.Id))
                throw new WorkflowException($"Activity '{activity.Id}' already exists", "ACTIVITY_EXISTS");

            if (!activity.Validate(out var errors))
                throw new ValidationException("Invalid activity", errors, "Activity");

            workflow.Activities.Add(activity);
        });
    }

    /// <summary>
    /// Adds a transition between activities in the latest version of a workflow, creating a new version.
    /// </summary>
    /// <exception cref="WorkflowException">Thrown when workflow not found or activities don't exist.</exception>
    /// <exception cref="ValidationException">Thrown when transition is invalid.</exception>
    public void AddTransition(string workflowId, Transition transition)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowId);
        ArgumentNullException.ThrowIfNull(transition);

        UpdateWorkflow(workflowId, workflow =>
        {
            if (!transition.Validate(out var errors))
                throw new ValidationException("Invalid transition", errors, "Transition");

            if (!workflow.Activities.Any(a => a.Id == transition.FromActivityId))
                throw new WorkflowException($"Activity '{transition.FromActivityId}' not found", "ACTIVITY_NOT_FOUND");

            if (!workflow.Activities.Any(a => a.Id == transition.ToActivityId))
                throw new WorkflowException($"Activity '{transition.ToActivityId}' not found", "ACTIVITY_NOT_FOUND");

            if (workflow.Transitions.Any(t => t.Id == transition.Id))
                throw new WorkflowException($"Transition '{transition.Id}' already exists", "TRANSITION_EXISTS");

            workflow.Transitions.Add(transition);
        });
    }

    /// <summary>
    /// Sets the start activity for the latest version of a workflow, creating a new version.
    /// </summary>
    /// <exception cref="WorkflowException">Thrown when workflow not found or activity doesn't exist.</exception>
    public void SetStartActivity(string workflowId, string activityId)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowId);
        ArgumentException.ThrowIfNullOrEmpty(activityId);

        UpdateWorkflow(workflowId, workflow =>
        {
            if (!workflow.Activities.Any(a => a.Id == activityId))
                throw new WorkflowException($"Activity '{activityId}' not found", "ACTIVITY_NOT_FOUND");

            workflow.StartActivityId = activityId;
        });
    }

    /// <summary>
    /// Sets the end activity for the latest version of a workflow, creating a new version.
    /// </summary>
    /// <exception cref="WorkflowException">Thrown when workflow not found or activity doesn't exist.</exception>
    public void SetEndActivity(string workflowId, string activityId)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowId);
        ArgumentException.ThrowIfNullOrEmpty(activityId);

        UpdateWorkflow(workflowId, workflow =>
        {
            if (!workflow.Activities.Any(a => a.Id == activityId))
                throw new WorkflowException($"Activity '{activityId}' not found", "ACTIVITY_NOT_FOUND");

            workflow.EndActivityId = activityId;
        });
    }

    /// <summary>
    /// Publishes the latest version of a workflow to make it active.
    /// </summary>
    /// <exception cref="WorkflowException">Thrown when workflow not found.</exception>
    public void PublishWorkflow(string workflowId)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowId);

        UpdateWorkflow(workflowId, workflow =>
        {
            workflow.Publish();
        });
    }

    /// <summary>
    /// Validates a workflow without publishing it.
    /// </summary>
    public bool ValidateWorkflow(string workflowId, out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(workflowId))
        {
            errors.Add("Workflow ID cannot be null or empty");
            return false;
        }

        var workflow = GetWorkflow(workflowId);
        if (workflow == null)
        {
            errors.Add($"Workflow '{workflowId}' not found");
            return false;
        }

        var validationResult = WorkflowValidator.ValidateWorkflow(workflow);
        errors.AddRange(validationResult.Errors);
        return validationResult.IsValid;
    }

    /// <summary>
    /// Gets all activities in the latest version of a workflow.
    /// </summary>
    /// <exception cref="WorkflowException">Thrown when workflow not found.</exception>
    public List<Activity> GetActivities(string workflowId)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowId);

        var workflow = GetWorkflow(workflowId);
        if (workflow == null)
            throw new WorkflowException($"Workflow '{workflowId}' not found", "WORKFLOW_NOT_FOUND");

        return workflow.Activities;
    }

    /// <summary>
    /// Gets a specific activity from the latest version of a workflow.
    /// </summary>
    public Activity? GetActivity(string workflowId, string activityId)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowId);
        ArgumentException.ThrowIfNullOrEmpty(activityId);

        var workflow = GetWorkflow(workflowId);
        if (workflow == null)
            return null;

        return workflow.Activities.FirstOrDefault(a => a.Id == activityId);
    }

    /// <summary>
    /// Deletes a workflow definition and all its versions.
    /// </summary>
    public bool DeleteWorkflow(string workflowId)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowId);

        var removed = _workflows.Remove(workflowId);
        _workflowVersions.Remove(workflowId);
        return removed;
    }

    /// <summary>
    /// Clones a workflow definition, creating a new workflow with a new ID and version 1.
    /// </summary>
    /// <exception cref="WorkflowException">Thrown when source workflow not found.</exception>
    public Workflow CloneWorkflow(string sourceWorkflowId, string newWorkflowId, string newName)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceWorkflowId);
        ArgumentException.ThrowIfNullOrEmpty(newWorkflowId);
        ArgumentException.ThrowIfNullOrEmpty(newName);

        var source = GetWorkflow(sourceWorkflowId);
        if (source == null)
            throw new WorkflowException($"Source workflow '{sourceWorkflowId}' not found", "WORKFLOW_NOT_FOUND");

        var clone = source.CloneWithVersion(1);
        clone.Id = newWorkflowId;
        clone.Name = newName;
        clone.CreatedAt = DateTime.UtcNow;
        clone.ModifiedAt = DateTime.UtcNow;

        // Validate the cloned workflow
        var validationResult = WorkflowValidator.ValidateWorkflow(clone);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                "Cloned workflow validation failed",
                validationResult.Errors,
                "Workflow"
            );
        }

        _workflows[newWorkflowId] = clone;
        _workflowVersions[newWorkflowId] = new List<Workflow> { clone };
        return clone;
    }

    /// <summary>
    /// Exports a workflow definition to JSON format.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow to export</param>
    /// <returns>JSON string representation of the workflow</returns>
    /// <exception cref="WorkflowException">Thrown when workflow not found</exception>
    public string ExportWorkflowToJson(string workflowId)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowId);

        var workflow = GetWorkflow(workflowId);
        if (workflow == null)
            throw new WorkflowException($"Workflow '{workflowId}' not found", "WORKFLOW_NOT_FOUND");

        return SerializationHelper.ToJsonPretty(workflow);
    }

    /// <summary>
    /// Imports a workflow definition from JSON format.
    /// </summary>
    /// <param name="workflowId">The ID to assign to the imported workflow</param>
    /// <param name="workflowName">The name to assign to the imported workflow</param>
    /// <param name="jsonDefinition">JSON string containing the workflow definition</param>
    /// <param name="overwriteExisting">Whether to overwrite existing workflow with same ID</param>
    /// <returns>The imported workflow</returns>
    /// <exception cref="ValidationException">Thrown when JSON is invalid or workflow validation fails</exception>
    public Workflow ImportWorkflowFromJson(string workflowId, string workflowName, string jsonDefinition, bool overwriteExisting = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowId);
        ArgumentException.ThrowIfNullOrEmpty(workflowName);
        ArgumentException.ThrowIfNullOrEmpty(jsonDefinition);

        if (!SerializationHelper.IsValidJson(jsonDefinition))
            throw new ValidationException("Invalid JSON format", "INVALID_JSON");

        var workflow = SerializationHelper.FromJson<Workflow>(jsonDefinition);
        if (workflow == null)
            throw new ValidationException("Failed to deserialize workflow from JSON", "DESERIALIZATION_FAILED");

        // Ensure the workflow has the correct ID and name
        workflow.Id = workflowId;
        workflow.Name = workflowName;
        workflow.CreatedAt = DateTime.UtcNow;
        workflow.ModifiedAt = DateTime.UtcNow;

        // Validate the imported workflow
        if (!workflow.Validate(out var errors))
            throw new ValidationException("Imported workflow validation failed", errors, "Workflow");

        // Validate using WorkflowValidator for comprehensive checks
        var validationResult = WorkflowValidator.ValidateWorkflow(workflow);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                "Imported workflow validation failed",
                validationResult.Errors,
                "Workflow"
            );
        }

        // Check if workflow already exists
        if (_workflows.ContainsKey(workflowId) && !overwriteExisting)
            throw new WorkflowException($"Workflow with ID '{workflowId}' already exists", "WORKFLOW_EXISTS");

        _workflows[workflowId] = workflow;

        // Track this version
        if (!_workflowVersions.TryGetValue(workflowId, out var versions))
        {
            versions = new List<Workflow>();
            _workflowVersions[workflowId] = versions;
        }
        versions.Add(workflow);

        return workflow;
    }

    /// <summary>
    /// Validates JSON workflow definition without importing it.
    /// </summary>
    /// <param name="jsonDefinition">JSON string containing the workflow definition</param>
    /// <returns>Validation result with errors if any</returns>
    public bool ValidateWorkflowJson(string jsonDefinition, out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(jsonDefinition))
        {
            errors.Add("JSON definition cannot be null or empty");
            return false;
        }

        if (!SerializationHelper.IsValidJson(jsonDefinition))
        {
            errors.Add("Invalid JSON format");
            return false;
        }

        try
        {
            var workflow = SerializationHelper.FromJson<Workflow>(jsonDefinition);
            if (workflow == null)
            {
                errors.Add("Failed to deserialize workflow from JSON");
                return false;
            }

            var validationResult = WorkflowValidator.ValidateWorkflow(workflow);
            errors.AddRange(validationResult.Errors);
            return validationResult.IsValid;
        }
        catch (Exception ex)
        {
            errors.Add($"Deserialization error: {ex.Message}");
            return false;
        }
    }
}