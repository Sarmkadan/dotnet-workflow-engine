// Copyright (c) 2024
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Data.Context
{
    /// <summary>
    /// Extension methods that add convenient, higher‑level operations to <see cref="DatabaseContext"/>.
    /// </summary>
    public static class DatabaseContextExtensions
    {
        /// <summary>
        /// Retrieves all <see cref="Workflow"/> entities from the underlying repository.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <returns>A read‑only list containing every workflow stored in the context.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <c>null</c>.</exception>
        public static async Task<IReadOnlyList<Workflow>> GetAllWorkflowsAsync(this DatabaseContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            // The repository implements <see cref="IRepository{T}.GetPagedAsync"/> which returns a tuple.
            var (items, _) = await context.Workflows
                .GetPagedAsync(pageNumber: 1, pageSize: int.MaxValue)
                .ConfigureAwait(false);

            return items;
        }

        /// <summary>
        /// Retrieves audit log entries that match a specific event type.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="eventType">The event type to filter by.</param>
        /// <returns>A read‑only list of <see cref="AuditLogEntry"/> objects with the given event type.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> or <paramref name="eventType"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="eventType"/> is an empty string.</exception>
        public static async Task<IReadOnlyList<AuditLogEntry>> GetAuditLogsByEventTypeAsync(this DatabaseContext context, string eventType)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentException.ThrowIfNullOrEmpty(eventType);

            var logs = await context.AuditLogs
                .GetByEventTypeAsync(eventType)
                .ConfigureAwait(false);

            return logs;
        }

        /// <summary>
        /// Clears all data from the context and immediately persists the change.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <c>null</c>.</exception>
        public static async Task ClearAllDataAsync(this DatabaseContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            await context.ClearAllAsync().ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Returns a human‑readable report of the statistics returned by <see cref="DatabaseContext.GetStatisticsAsync"/>.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <returns>A multi‑line string where each line contains a statistic name and its integer value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <c>null</c>.</exception>
        public static async Task<string> GetStatisticsReportAsync(this DatabaseContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var stats = await context.GetStatisticsAsync().ConfigureAwait(false);

            // Use invariant culture for deterministic formatting.
            return string.Join(
                Environment.NewLine,
                stats.Select(kv => $"{kv.Key}: {kv.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
        }
    }
}
