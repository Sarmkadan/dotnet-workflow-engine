// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;

namespace DotNetWorkflowEngine.Models;

/// <summary>
/// Extension methods for the <see cref="ActivityResult"/> class.
/// </summary>
public static class ActivityResultExtensions
{
    /// <summary>
    /// Checks if the activity execution was successful.
    /// </summary>
    public static bool Succeeded(this ActivityResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Status == ActivityStatus.Completed;
    }

    /// <summary>
    /// Returns the error message if the activity execution failed.
    /// </summary>
    public static string? FailureReason(this ActivityResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.ErrorMessage;
    }

    /// <summary>
    /// Executes the next action if the activity succeeded, returning the result of that action.
    /// </summary>
    public static ActivityResult Then(this ActivityResult result, Func<ActivityResult, ActivityResult> next)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        if (result.Succeeded())
        {
            return next(result);
        }
        return result;
    }
}
