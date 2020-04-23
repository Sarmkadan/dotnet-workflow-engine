// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System.Globalization;
using System.Text.RegularExpressions;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;

namespace DotNetWorkflowEngine.Services;

/// <summary>
/// Extension methods for <see cref="WorkflowDefinitionService"/> providing additional workflow management functionality.
/// </summary>
public static class WorkflowDefinitionServiceExtensions
{
    /// <summary>
    /// Creates a new workflow definition with a generated ID based on the workflow name.
    /// </summary>
    /// <param name="service">The workflow definition service.</param>
    /// <param name="name">The workflow name.</param>
    /// <param name="description">Optional workflow description.</param>
    /// <returns>The created workflow.</returns>
    /// <exception cref="ArgumentNullException">Thrown when name is null.</exception>
    /// <exception cref="ValidationException">Thrown when name is empty or invalid.</exception>
    public static Workflow CreateWorkflowFromName(this WorkflowDefinitionService service, string name, string? description = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        // Generate a normalized workflow ID from the name
        var normalizedName = name.Trim()
            .Replace(" ", "-")
            .Replace("_", "-")
            .ToLowerInvariant();

        // Remove any non-alphanumeric characters except hyphens
        normalizedName = new string(normalizedName
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .ToArray());

        // Ensure the ID starts with a letter
        if (normalizedName.Length == 0 || !char.IsLetter(normalizedName[0]))
        {
            normalizedName = "workflow-" + normalizedName;
        }

        // Ensure the ID is not too long
        if (normalizedName.Length > 100)
        {
            normalizedName = normalizedName[..100];
        }

        // Add timestamp suffix to ensure uniqueness
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var workflowId = $"{normalizedName}-{timestamp}";

        return service.CreateWorkflow(workflowId, name, description);
    }

    /// <summary>
    /// Finds a workflow by name (case-insensitive).
    /// </summary>
    /// <param name="service">The workflow definition service.</param>
    /// <param name="name">The workflow name to search for.</param>
    /// <returns>The found workflow or null if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when name is null.</exception>
    public static Workflow? GetWorkflowByName(this WorkflowDefinitionService service, string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var allWorkflows = service.GetAllWorkflows();
        return allWorkflows.FirstOrDefault(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a workflow with the given name exists (case-insensitive).
    /// </summary>
    /// <param name="service">The workflow definition service.</param>
    /// <param name="name">The workflow name to check.</param>
    /// <returns>True if workflow exists; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when name is null.</exception>
    public static bool WorkflowExists(this WorkflowDefinitionService service, string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return service.GetWorkflowByName(name) != null;
    }

    /// <summary>
    /// Gets all workflows filtered by a name pattern (case-insensitive, supports wildcards).
    /// </summary>
    /// <param name="service">The workflow definition service.</param>
    /// <param name="namePattern">The search pattern (e.g., "approval-*", "*process*", "order-fulfillment").</param>
    /// <returns>Filtered collection of workflows.</returns>
    /// <exception cref="ArgumentNullException">Thrown when namePattern is null.</exception>
    public static IReadOnlyList<Workflow> GetWorkflowsByPattern(this WorkflowDefinitionService service, string namePattern)
    {
        ArgumentNullException.ThrowIfNull(namePattern);

        var allWorkflows = service.GetAllWorkflows();

        if (string.IsNullOrWhiteSpace(namePattern) || namePattern == "*")
        {
            return allWorkflows.AsReadOnly();
        }

        // Convert simple wildcard pattern to regex
        var regexPattern = namePattern
            .Replace(".", "\\.")  // Escape dots
            .Replace("*", ".*")  // * becomes .*
            .Replace("?", ".?"); // ? becomes .?

        var regex = new System.Text.RegularExpressions.Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        var matches = allWorkflows.Where(w => regex.IsMatch(w.Name)).ToList();
        return matches.AsReadOnly();
    }
}