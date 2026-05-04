// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Utilities;

/// <summary>
/// Comprehensive workflow validation utility.
/// </summary>
public class WorkflowValidator
{
    /// <summary>
    /// Validates a complete workflow definition.
    /// </summary>
    public static ValidationResult ValidateWorkflow(Workflow workflow)
    {
        var result = new ValidationResult();

        // Basic validation
        if (string.IsNullOrWhiteSpace(workflow.Id))
            result.AddError("Workflow ID is required");

        if (string.IsNullOrWhiteSpace(workflow.Name))
            result.AddError("Workflow name is required");

        if (workflow.Activities.Count == 0)
            result.AddError("Workflow must have at least one activity");

        // Validate activities
        foreach (var activity in workflow.Activities)
        {
            var activityErrors = ValidateActivity(activity);
            if (!activityErrors.IsValid)
            {
                result.AddError($"Invalid activity '{activity.Id}': {string.Join(", ", activityErrors.Errors)}");
            }
        }

        // Validate transitions
        foreach (var transition in workflow.Transitions)
        {
            var transitionErrors = ValidateTransition(transition, workflow);
            if (!transitionErrors.IsValid)
            {
                result.AddError($"Invalid transition '{transition.Id}': {string.Join(", ", transitionErrors.Errors)}");
            }
        }

        // Validate start and end activities
        if (!string.IsNullOrEmpty(workflow.StartActivityId))
        {
            if (!workflow.Activities.Any(a => a.Id == workflow.StartActivityId))
                result.AddError($"Start activity '{workflow.StartActivityId}' not found");
        }
        else
        {
            result.AddWarning("No start activity defined");
        }

        if (!string.IsNullOrEmpty(workflow.EndActivityId))
        {
            if (!workflow.Activities.Any(a => a.Id == workflow.EndActivityId))
                result.AddError($"End activity '{workflow.EndActivityId}' not found");
        }

        // Validate connectivity
        ValidateConnectivity(workflow, result);

        return result;
    }

    /// <summary>
    /// Validates an activity definition.
    /// </summary>
    public static ValidationResult ValidateActivity(Activity activity)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(activity.Id))
            result.AddError("Activity ID is required");

        if (string.IsNullOrWhiteSpace(activity.Name))
            result.AddError("Activity name is required");

        if (activity.TimeoutSeconds <= 0)
            result.AddError("Timeout must be greater than zero");

        if (activity.MaxRetries < 0)
            result.AddError("MaxRetries cannot be negative");

        if (activity.MaxRetries > 0 && activity.RetryPolicy == RetryPolicy.NoRetry)
            result.AddWarning("MaxRetries is set but RetryPolicy is NoRetry");

        if (!activity.IsGateway() && activity.RequiresHandler())
        {
            if (string.IsNullOrEmpty(activity.HandlerType))
                result.AddWarning($"Activity '{activity.Id}' requires a handler but none is specified");
        }

        return result;
    }

    /// <summary>
    /// Validates a transition.
    /// </summary>
    public static ValidationResult ValidateTransition(Transition transition, Workflow workflow)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(transition.FromActivityId))
            result.AddError("From activity is required");

        if (string.IsNullOrWhiteSpace(transition.ToActivityId))
            result.AddError("To activity is required");

        if (transition.FromActivityId == transition.ToActivityId)
            result.AddWarning("Transition points to the same activity (self-loop)");

        if (!workflow.Activities.Any(a => a.Id == transition.FromActivityId))
            result.AddError($"From activity '{transition.FromActivityId}' not found in workflow");

        if (!workflow.Activities.Any(a => a.Id == transition.ToActivityId))
            result.AddError($"To activity '{transition.ToActivityId}' not found in workflow");

        return result;
    }

    /// <summary>
    /// Validates workflow connectivity and reachability.
    /// </summary>
    private static void ValidateConnectivity(Workflow workflow, ValidationResult result)
    {
        if (string.IsNullOrEmpty(workflow.StartActivityId))
            return;

        var reachable = GetReachableActivities(workflow, workflow.StartActivityId);
        var unreachable = workflow.Activities.Where(a => !reachable.Contains(a.Id)).ToList();

        if (unreachable.Count > 0)
        {
            var unreachableIds = string.Join(", ", unreachable.Select(a => a.Id));
            result.AddWarning($"Unreachable activities from start: {unreachableIds}");
        }

        // Check if all activities can reach the end
        if (!string.IsNullOrEmpty(workflow.EndActivityId))
        {
            foreach (var activity in workflow.Activities.Where(a => a.Id != workflow.EndActivityId))
            {
                var canReachEnd = CanReachActivity(workflow, activity.Id, workflow.EndActivityId);
                if (!canReachEnd && !activity.IsOptional)
                {
                    result.AddWarning($"Activity '{activity.Id}' cannot reach the end activity");
                }
            }
        }
    }

    /// <summary>
    /// Gets all activities reachable from a starting activity.
    /// </summary>
    private static HashSet<string> GetReachableActivities(Workflow workflow, string startId)
    {
        var reachable = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(startId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (reachable.Contains(current))
                continue;

            reachable.Add(current);

            var nextActivities = workflow.GetNextActivities(current);
            foreach (var next in nextActivities)
            {
                if (!reachable.Contains(next.Id))
                    queue.Enqueue(next.Id);
            }
        }

        return reachable;
    }

    /// <summary>
    /// Checks if a path exists between two activities.
    /// </summary>
    private static bool CanReachActivity(Workflow workflow, string fromId, string toId)
    {
        if (fromId == toId)
            return true;

        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(fromId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (visited.Contains(current))
                continue;

            visited.Add(current);

            if (current == toId)
                return true;

            var nextActivities = workflow.GetNextActivities(current);
            foreach (var next in nextActivities)
            {
                if (!visited.Contains(next.Id))
                    queue.Enqueue(next.Id);
            }
        }

        return false;
    }

    /// <summary>
    /// Result of a validation operation.
    /// </summary>
    public class ValidationResult
    {
        private readonly List<string> _errors = new();
        private readonly List<string> _warnings = new();

        /// <summary>Gets validation errors.</summary>
        public IReadOnlyList<string> Errors => _errors;

        /// <summary>Gets validation warnings.</summary>
        public IReadOnlyList<string> Warnings => _warnings;

        /// <summary>Gets whether validation passed.</summary>
        public bool IsValid => _errors.Count == 0;

        /// <summary>
        /// Adds an error to the result.
        /// </summary>
        public void AddError(string error)
        {
            _errors.Add(error);
        }

        /// <summary>
        /// Adds a warning to the result.
        /// </summary>
        public void AddWarning(string warning)
        {
            _warnings.Add(warning);
        }

        /// <summary>
        /// Gets a formatted report of the validation result.
        /// </summary>
        public string GetReport()
        {
            var report = new System.Text.StringBuilder();

            if (IsValid)
            {
                report.AppendLine("✓ Validation passed");
            }
            else
            {
                report.AppendLine("✗ Validation failed");
                foreach (var error in _errors)
                    report.AppendLine($"  ERROR: {error}");
            }

            if (_warnings.Count > 0)
            {
                foreach (var warning in _warnings)
                    report.AppendLine($"  WARNING: {warning}");
            }

            return report.ToString();
        }
    }
}
