// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Services;

/// <summary>
/// Structured query interface for the audit trail. Supports server-side filtering
/// by step name, activity type, date range, outcome, and other predicates so that
/// consumers avoid deserialising entire instance histories in memory.
/// </summary>
public interface IAuditTrailQuery
{
    /// <summary>
    /// Queries audit log entries with fine-grained server-side filtering and pagination.
    /// All parameters are optional; omitting a parameter removes that constraint.
    /// </summary>
    /// <param name="workflowId">Filter to entries whose instance ID starts with this workflow ID prefix.</param>
    /// <param name="instanceId">Filter to entries for this exact workflow instance.</param>
    /// <param name="stepName">
    /// Filter by the activity (step) name. Matches against <see cref="AuditLogEntry.ActivityId"/>.
    /// </param>
    /// <param name="activityType">
    /// Filter by the event type (e.g. "ActivityCompleted", "ActivityFailed").
    /// Matches against <see cref="AuditLogEntry.EventType"/>.
    /// </param>
    /// <param name="outcome">
    /// Filter by severity / outcome level (e.g. "Info", "Warning", "Error").
    /// Matches against <see cref="AuditLogEntry.Severity"/>.
    /// </param>
    /// <param name="actor">Filter to entries produced by this actor.</param>
    /// <param name="fromDate">Lower bound (inclusive) of the event timestamp range.</param>
    /// <param name="toDate">Upper bound (inclusive) of the event timestamp range.</param>
    /// <param name="skip">Number of results to skip for pagination.</param>
    /// <param name="take">Maximum number of results to return (capped at 1 000).</param>
    /// <returns>
    /// A tuple of the matching <see cref="AuditLogEntry"/> items and the total count of
    /// matching records (before pagination) so callers can compute page counts.
    /// </returns>
    Task<(List<AuditLogEntry> Items, int Total)> QueryAsync(
        string? workflowId = null,
        string? instanceId = null,
        string? stepName = null,
        string? activityType = null,
        string? outcome = null,
        string? actor = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int skip = 0,
        int take = 100);

    /// <summary>
    /// Returns all distinct event types present in the audit log, useful for
    /// populating filter drop-downs on operational dashboards.
    /// </summary>
    Task<IReadOnlyList<string>> GetEventTypesAsync();

    /// <summary>
    /// Returns a summary grouped by event type over the given date window.
    /// </summary>
    Task<IReadOnlyDictionary<string, int>> GetOutcomeSummaryAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null);
}
