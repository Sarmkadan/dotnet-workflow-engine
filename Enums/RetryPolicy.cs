// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetWorkflowEngine.Enums;

/// <summary>
/// Defines the retry strategy for failed activities.
/// </summary>
public enum RetryPolicy
{
    /// <summary>No retry, activity fails immediately</summary>
    NoRetry = 0,

    /// <summary>Retry with fixed delay between attempts</summary>
    FixedDelay = 1,

    /// <summary>Retry with exponential backoff strategy</summary>
    ExponentialBackoff = 2,

    /// <summary>Retry with linear backoff strategy</summary>
    LinearBackoff = 3,

    /// <summary>Custom retry logic defined by implementation</summary>
    Custom = 4
}
