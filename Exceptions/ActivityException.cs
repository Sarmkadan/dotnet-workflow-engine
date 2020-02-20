// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetWorkflowEngine.Exceptions;

/// <summary>
/// Exception thrown during activity execution errors.
/// </summary>
public class ActivityException : WorkflowException
{
    /// <summary>
    /// Gets the ID of the activity that caused the exception.
    /// </summary>
    public string ActivityId { get; }

    /// <summary>
    /// Gets the attempt number when the exception occurred.
    /// </summary>
    public int AttemptNumber { get; }

    /// <summary>
    /// Initializes a new instance of the ActivityException class.
    /// </summary>
    public ActivityException(string message, string activityId)
        : base(message, "ACTIVITY_ERROR")
    {
        ActivityId = activityId;
        AttemptNumber = 1;
    }

    /// <summary>
    /// Initializes a new instance with attempt information.
    /// </summary>
    public ActivityException(string message, string activityId, int attemptNumber, Exception? innerException = null)
        : base(message, "ACTIVITY_ERROR", null, innerException)
    {
        ActivityId = activityId;
        AttemptNumber = attemptNumber;
    }

    /// <summary>
    /// Initializes a new instance with full details.
    /// </summary>
    public ActivityException(string message, string activityId, int attemptNumber, string correlationId, Exception? innerException = null)
        : base(message, "ACTIVITY_ERROR", correlationId, innerException)
    {
        ActivityId = activityId;
        AttemptNumber = attemptNumber;
    }
}
