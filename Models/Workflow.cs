// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;
using Newtonsoft.Json;

namespace DotNetWorkflowEngine.Models;

/// <summary>
/// Represents a workflow definition that can be executed.
/// </summary>
public class Workflow
{
    /// <summary>Gets or sets the unique identifier of the workflow.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the workflow.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the description of the workflow.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the version of the workflow.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Gets or sets the current status of the workflow.</summary>
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Draft;

    /// <summary>Gets or sets the list of activities in this workflow.</summary>
    [JsonProperty]
    public List<Activity> Activities { get; set; } = new();

    /// <summary>Gets or sets the list of transitions between activities.</summary>
    [JsonProperty]
    public List<Transition> Transitions { get; set; } = new();

    /// <summary>Gets or sets the ID of the starting activity.</summary>
    public string? StartActivityId { get; set; }

    /// <summary>Gets or sets the ID of the ending activity.</summary>
    public string? EndActivityId { get; set; }

    /// <summary>Gets or sets when the workflow was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets when the workflow was last modified.</summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the creator of the workflow.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Gets or sets the user who last modified the workflow.</summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Validates the workflow definition for required properties and valid transitions.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Id))
            errors.Add("Workflow ID is required");

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Workflow name is required");

        if (Activities.Count == 0)
            errors.Add("Workflow must have at least one activity");

        if (string.IsNullOrWhiteSpace(StartActivityId))
            errors.Add("Start activity is required");

        if (!string.IsNullOrWhiteSpace(StartActivityId) && !Activities.Any(a => a.Id == StartActivityId))
            errors.Add($"Start activity '{StartActivityId}' does not exist");

        if (!string.IsNullOrWhiteSpace(EndActivityId) && !Activities.Any(a => a.Id == EndActivityId))
            errors.Add($"End activity '{EndActivityId}' does not exist");

        foreach (var transition in Transitions)
        {
            if (!Activities.Any(a => a.Id == transition.FromActivityId))
                errors.Add($"Transition references non-existent activity: {transition.FromActivityId}");

            if (!Activities.Any(a => a.Id == transition.ToActivityId))
                errors.Add($"Transition references non-existent activity: {transition.ToActivityId}");
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Gets all activities that can be reached from a given activity.
    /// </summary>
    public List<Activity> GetNextActivities(string activityId)
    {
        var transitions = Transitions.Where(t => t.FromActivityId == activityId).ToList();
        return Activities.Where(a => transitions.Any(t => t.ToActivityId == a.Id)).ToList();
    }

    /// <summary>
    /// Gets all activities that can reach a given activity.
    /// </summary>
    public List<Activity> GetPreviousActivities(string activityId)
    {
        var transitions = Transitions.Where(t => t.ToActivityId == activityId).ToList();
        return Activities.Where(a => transitions.Any(t => t.FromActivityId == a.Id)).ToList();
    }

    /// <summary>
    /// Marks the workflow as ready for execution.
    /// </summary>
    public void Publish()
    {
        if (Validate(out var errors))
        {
            Status = WorkflowStatus.Active;
            ModifiedAt = DateTime.UtcNow;
        }
        else
        {
            throw new Exceptions.ValidationException("Cannot publish invalid workflow", errors, "Workflow");
        }
    }
}
