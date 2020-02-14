// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetWorkflowEngine.Models;

/// <summary>
/// Represents a transition/edge between two activities in a workflow.
/// </summary>
public class Transition
{
    /// <summary>Gets or sets the unique identifier of the transition.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the ID of the source activity.</summary>
    public string FromActivityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the ID of the target activity.</summary>
    public string ToActivityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the condition expression for conditional transitions.</summary>
    public string? ConditionExpression { get; set; }

    /// <summary>Gets or sets the label or description of the transition.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets whether this is the default transition when no other conditions match.</summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>Gets or sets the priority for this transition (higher priority evaluated first).</summary>
    public int Priority { get; set; } = 0;

    /// <summary>Gets or sets when the transition was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Validates the transition configuration.
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Id))
            errors.Add("Transition ID is required");

        if (string.IsNullOrWhiteSpace(FromActivityId))
            errors.Add("From activity is required");

        if (string.IsNullOrWhiteSpace(ToActivityId))
            errors.Add("To activity is required");

        if (FromActivityId == ToActivityId)
            errors.Add("Transition cannot point to the same activity");

        return errors.Count == 0;
    }

    /// <summary>
    /// Creates a default transition between two activities.
    /// </summary>
    public static Transition CreateDefault(string fromId, string toId)
    {
        return new Transition
        {
            Id = $"{fromId}_to_{toId}_default",
            FromActivityId = fromId,
            ToActivityId = toId,
            IsDefault = true
        };
    }

    /// <summary>
    /// Creates a conditional transition with an expression.
    /// </summary>
    public static Transition CreateConditional(string fromId, string toId, string conditionExpression)
    {
        return new Transition
        {
            Id = $"{fromId}_to_{toId}_conditional",
            FromActivityId = fromId,
            ToActivityId = toId,
            ConditionExpression = conditionExpression
        };
    }
}
