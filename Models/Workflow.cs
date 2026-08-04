// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using DotNetWorkflowEngine.Enums;
using System.Text.Json.Serialization;

namespace DotNetWorkflowEngine.Models;

/// <summary>
/// Represents a workflow definition - a directed graph of <see cref="Activity"/> nodes
/// connected by <see cref="Transition"/> edges. A workflow must be validated and published
/// (status = <see cref="WorkflowStatus.Active"/>) before instances can be created from it.
/// </summary>
/// <remarks>
/// <para>
/// A valid workflow requires:
/// <list type="bullet">
/// <item>A non-empty <see cref="Id"/> and <see cref="Name"/></item>
/// <item>At least one <see cref="Activity"/> in <see cref="Activities"/></item>
/// <item>A <see cref="StartActivityId"/> referencing an existing activity</item>
/// <item>All <see cref="Transition"/> endpoints referencing existing activities</item>
/// </list>
/// Use <see cref="Validate"/> to check these constraints before calling <see cref="Publish"/>.
/// </para>
/// <para>
/// Workflow versions are immutable. When a workflow is updated, a new version is created
/// instead of mutating the existing workflow. This ensures that in-flight instances continue
/// to execute against the version they were created with.
/// </para>
/// </remarks>
/// <summary>
        /// XML doc comments go here
        /// </summary>
        public class Workflow
{
    /// <summary>Gets or sets the unique identifier of the workflow.</summary>
    /// <summary>
        /// XML doc comments go here
        /// </summary>
        public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the workflow.</summary>
    /// <summary>
        /// XML doc comments go here
        /// </summary>
        public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the description of the workflow.</summary>
    /// <summary>
        /// XML doc comments go here
        /// </summary>
        public string? Description { get; set; }

    /// <summary>Gets the immutable version number of this workflow definition.</summary>
    /// <remarks>
    /// This version is assigned when the workflow is first created and never changes.
    /// When a workflow is updated, a new version is created instead of mutating the existing one.
    /// Existing workflow instances are pinned to this version and will execute against it.
    /// </remarks>
    /// <summary>
        /// XML doc comments go here
        /// </summary>
        public int Version { get; init; } = 1;

    /// <summary>Gets or sets the current status of the workflow.</summary>
    /// <summary>
        /// XML doc comments go here
        /// </summary>
        public WorkflowStatus Status { get; set; } = WorkflowStatus.Draft;

    /// <summary>Gets whether the workflow has been published (status is <see cref="WorkflowStatus.Active"/>).</summary>
    /// <summary>
        /// XML doc comments go here
        /// </summary>
        public bool IsPublished => Status == WorkflowStatus.Active;

    /// <summary>Gets or sets the list of activities in this workflow.</summary>
    /// <summary>
        /// XML doc comments go here
        /// </summary>
        public List<Activity> Activities { get; set; } = new();

    /// <summary>Gets or sets the list of transitions between activities.</summary>
    /// <summary>
        /// XML doc comments go here
        /// </summary>
        public List<Transition> Transitions { get; set; } = new();

    /// <summary>Gets or sets the ID of the starting activity.</summary>
    /// <summary>
        /// XML doc comments go here
        /// </summary>
        public string? StartActivityId { get; set; }

    /// <summary>Gets or sets the ID of the ending activity.</summary>
    /// <summary>
        /// XML doc comments go here
        /// </summary>
        public string? EndActivityId { get; set; }

    /// <summary>Gets or sets when the workflow was created.</summary>
    /// <summary>
        /// XML doc comments go here
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets when the workflow was last modified.</summary>
    /// <summary>
        /// XML doc comments go here
        /// </summary>
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the creator of the workflow.</summary>
    /// <summary>
        /// XML doc comments go here
        /// </summary>
        public string? CreatedBy { get; set; }

    /// <summary>Gets or sets the user who last modified the workflow.</summary>
    /// <summary>
        /// XML doc comments go here
        /// </summary>
        public string? ModifiedBy { get; set; }

    /// <summary>
    /// Validates the workflow definition by checking required properties and verifying
    /// that all transition endpoints reference existing activities.
    /// </summary>
    /// <param name="errors">
    /// When the method returns <c>false</c>, contains one or more human-readable error messages
    /// describing validation failures.
    /// </param>
    /// <returns><c>true</c> if the workflow definition is valid; otherwise <c>false</c>.</returns>
    /// <summary>
        /// XML doc comments go here
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
    /// <summary>
        /// XML doc comments go here
        /// </summary>
        public List<Activity> GetNextActivities(string activityId)
    {
        var transitions = Transitions.Where(t => t.FromActivityId == activityId).ToList();
        return Activities.Where(a => transitions.Any(t => t.ToActivityId == a.Id)).ToList();
    }

    /// <summary>
    /// Gets all activities that can reach a given activity.
    /// </summary>
    /// <summary>
        /// XML doc comments go here
        /// </summary>
        public List<Activity> GetPreviousActivities(string activityId)
    {
        var transitions = Transitions.Where(t => t.ToActivityId == activityId).ToList();
        return Activities.Where(a => transitions.Any(t => t.FromActivityId == a.Id)).ToList();
    }

    /// <summary>
    /// Marks the workflow as ready for execution.
    /// </summary>
    /// <summary>
        /// XML doc comments go here
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

    /// <summary>
    /// Creates a deep copy of this workflow with a new version number.
    /// Used when creating a new version from an existing workflow.
    /// </summary>
    /// <param name="newVersion">The version number for the new workflow.</param>
    /// <returns>A new workflow instance with copied properties and activities.</returns>
    /// <summary>
        /// XML doc comments go here
        /// </summary>
        public Workflow CloneWithVersion(int newVersion)
    {
        return new Workflow
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Version = newVersion,
            Status = Status,
            Activities = Activities.Select(a => new Activity
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description,
                Type = a.Type,
                ExecutionMode = a.ExecutionMode,
                HandlerType = a.HandlerType,
                InputParameters = new Dictionary<string, object?>((IDictionary<string, object?>)a.InputParameters),
                OutputMapping = new Dictionary<string, string>((IDictionary<string, string>)a.OutputMapping),
                RetryPolicy = a.RetryPolicy,
                MaxRetries = a.MaxRetries,
                TimeoutSeconds = a.TimeoutSeconds,
                IsOptional = a.IsOptional,
                ConditionExpression = a.ConditionExpression,
                Metadata = new Dictionary<string, object?>((IDictionary<string, object?>)a.Metadata)
            }).ToList(),
            Transitions = Transitions.Select(t => new Transition
            {
                Id = t.Id,
                FromActivityId = t.FromActivityId,
                ToActivityId = t.ToActivityId,
                ConditionExpression = t.ConditionExpression,
                Label = t.Label,
                IsDefault = t.IsDefault,
                Priority = t.Priority
            }).ToList(),
            StartActivityId = StartActivityId,
            EndActivityId = EndActivityId,
            CreatedAt = CreatedAt,
            ModifiedAt = DateTime.UtcNow,
            CreatedBy = CreatedBy,
            ModifiedBy = ModifiedBy
        };
    }
}