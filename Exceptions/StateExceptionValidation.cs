using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotNetWorkflowEngine.Exceptions
{
    /// <summary>
    /// Provides validation helpers for <see cref="StateException"/> instances.
    /// </summary>
    public static class StateExceptionValidation
    {
        /// <summary>
        /// Validates the specified <see cref="StateException"/> instance.
        /// </summary>
        /// <param name="value">The exception to validate.</param>
        /// <returns>An enumerable of human-readable validation problems; empty if valid.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
        public static IReadOnlyList<string> Validate(this StateException value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            if (string.IsNullOrWhiteSpace(value.CurrentState))
            {
                problems.Add($"CurrentState must be a non-empty string, but was: '{value.CurrentState}'");
            }

            if (string.IsNullOrWhiteSpace(value.RequestedState))
            {
                problems.Add($"RequestedState must be a non-empty string, but was: '{value.RequestedState}'");
            }

            if (value.EntityId is not null && string.IsNullOrWhiteSpace(value.EntityId))
            {
                problems.Add("EntityId must be null or a non-empty string, but was an empty string");
            }

            return problems.AsReadOnly();
        }

        /// <summary>
        /// Determines whether the specified <see cref="StateException"/> instance is valid.
        /// </summary>
        /// <param name="value">The exception to check.</param>
        /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
        public static bool IsValid(this StateException value)
        {
            return value.Validate().Count == 0;
        }

        /// <summary>
        /// Ensures that the specified <see cref="StateException"/> instance is valid, throwing an <see cref="ArgumentException"/> if it is not.
        /// </summary>
        /// <param name="value">The exception to validate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is invalid; the message lists all validation problems.</exception>
        public static void EnsureValid(this StateException value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = value.Validate();
            if (problems.Count == 0)
            {
                return;
            }

            throw new ArgumentException(
                $"StateException is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
        }
    }
}