// =============================================================================
// Extensions for WorkflowException
// =============================================================================

using System;
using System.Collections.Generic;

namespace DotNetWorkflowEngine.Exceptions
{
    /// <summary>
    /// Provides useful extension methods for <see cref="WorkflowException"/>.
    /// </summary>
    public static class WorkflowExceptionExtensions
    {
        /// <summary>
        /// Creates a new <see cref="WorkflowException"/> that copies the original
        /// exception's message, error code and inner exception, but replaces the
        /// correlation identifier.
        /// </summary>
        /// <param name="exception">The source exception.</param>
        /// <param name="correlationId">The correlation identifier to set.</param>
        /// <returns>A new <see cref="WorkflowException"/> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/></exception>
        public static WorkflowException WithCorrelationId(this WorkflowException exception, string correlationId)
        {
            ArgumentNullException.ThrowIfNull(exception);

            // Use the most complete constructor to preserve all data.
            return new WorkflowException(
                message: exception.Message,
                errorCode: exception.ErrorCode ?? string.Empty,
                correlationId: correlationId,
                innerException: exception.InnerException);
        }

        /// <summary>
        /// Returns a dictionary representation of the exception useful for logging
        /// or telemetry systems.
        /// </summary>
        /// <param name="exception">The exception to convert.</param>
        /// <returns>A dictionary containing key details of the exception.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/></exception>
        public static IDictionary<string, string> ToDictionary(this WorkflowException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Message"] = exception.Message,
                ["ErrorCode"] = exception.ErrorCode ?? string.Empty,
                ["CorrelationId"] = exception.CorrelationId ?? string.Empty,
                ["StackTrace"] = exception.StackTrace ?? string.Empty
            };

            if (exception.InnerException is { } innerException)
            {
                dict["InnerExceptionMessage"] = innerException.Message;
                dict["InnerExceptionStackTrace"] = innerException.StackTrace ?? string.Empty;
            }

            return dict;
        }

        /// <summary>
        /// Determines whether the exception is considered critical based on its
        /// error code. By convention, error codes that start with "CRIT" are treated
        /// as critical.
        /// </summary>
        /// <param name="exception">The exception to evaluate.</param>
        /// <returns><c>true</c> if the error code indicates a critical error; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/></exception>
        public static bool IsCritical(this WorkflowException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return !string.IsNullOrEmpty(exception.ErrorCode) &&
                   exception.ErrorCode.StartsWith("CRIT", StringComparison.OrdinalIgnoreCase);
        }
    }
}
