// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetWorkflowEngine.Exceptions;

/// <summary>
/// Exception thrown when an optimistic concurrency check on a persisted entity's
/// <c>Version</c> fails - i.e. the caller's expected version no longer matches the
/// version currently stored, indicating the record was modified by another process
/// in between the caller's read and write.
/// </summary>
public class WorkflowConcurrencyException : WorkflowException
{
    /// <summary>
    /// Gets the identifier of the entity that failed the concurrency check.
    /// </summary>
    public string EntityId { get; }

    /// <summary>
    /// Gets the version the caller expected to overwrite.
    /// </summary>
    public int ExpectedVersion { get; }

    /// <summary>
    /// Gets the version actually stored at the time of the check.
    /// </summary>
    public int ActualVersion { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowConcurrencyException"/> class.
    /// </summary>
    /// <param name="entityId">The identifier of the entity that failed the concurrency check.</param>
    /// <param name="expectedVersion">The version the caller expected to overwrite.</param>
    /// <param name="actualVersion">The version actually stored at the time of the check.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="entityId"/> is null or empty.</exception>
    public WorkflowConcurrencyException(string entityId, int expectedVersion, int actualVersion)
        : base(
            $"Concurrent modification detected for entity '{entityId}'. " +
            $"Expected version {expectedVersion} but the stored version is {actualVersion}. " +
            "The entity was modified by another process; reload and retry.",
            "CONCURRENCY_CONFLICT")
    {
        ArgumentException.ThrowIfNullOrEmpty(entityId);

        EntityId = entityId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}
