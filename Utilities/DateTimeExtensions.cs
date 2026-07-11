// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Globalization;

namespace DotNetWorkflowEngine.Utilities;

/// <summary>
/// Extension methods for DateTime operations commonly used in workflow processing.
/// Includes formatting, duration calculation, and temporal comparisons.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts a DateTime to ISO 8601 format string (UTC).
    /// Example: 2026-05-04T12:30:45.123Z
    /// </summary>
    /// <param name="dateTime">The DateTime to convert</param>
    /// <returns>ISO 8601 formatted string in UTC</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dateTime"/> is null</exception>
    public static string ToIso8601(this DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        return dateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Checks if a DateTime is in the past.
    /// </summary>
    /// <param name="dateTime">The DateTime to check</param>
    /// <returns>True if the DateTime is in the past</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dateTime"/> is null</exception>
    public static bool IsPast(this DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        return dateTime < DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if a DateTime is in the future.
    /// </summary>
    /// <param name="dateTime">The DateTime to check</param>
    /// <returns>True if the DateTime is in the future</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dateTime"/> is null</exception>
    public static bool IsFuture(this DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        return dateTime > DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if a DateTime is within a range (inclusive).
    /// </summary>
    /// <param name="dateTime">The DateTime to check</param>
    /// <param name="start">The start of the range</param>
    /// <param name="end">The end of the range</param>
    /// <returns>True if the DateTime is within the range</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dateTime"/> is null</exception>
    public static bool IsBetween(this DateTime dateTime, DateTime start, DateTime end)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        return dateTime >= start && dateTime <= end;
    }

    /// <summary>
    /// Calculates the duration between two DateTime values.
    /// Returns a human-readable string (e.g., "2 hours, 30 minutes").
    /// </summary>
    /// <param name="start">The start DateTime</param>
    /// <param name="end">The end DateTime</param>
    /// <returns>Human-readable duration string</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="start"/> or <paramref name="end"/> is null</exception>
    public static string DurationToString(this DateTime start, DateTime end)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(end);
        var duration = end - start;
        return DurationToString(duration);
    }

    /// <summary>
    /// Converts a TimeSpan duration to a human-readable string.
    /// Example: 1 day, 2 hours, 30 minutes
    /// </summary>
    /// <param name="duration">The TimeSpan to convert</param>
    /// <returns>Human-readable duration string</returns>
    public static string DurationToString(this TimeSpan duration)
    {
        if (duration == TimeSpan.Zero)
            return "0 seconds";

        var absoluteDuration = duration < TimeSpan.Zero ? -duration : duration;

        var parts = new System.Collections.Generic.List<string>();
        if (absoluteDuration.Days > 0)
            parts.Add($"{absoluteDuration.Days} day{(absoluteDuration.Days > 1 ? "s" : "")}");
        if (absoluteDuration.Hours > 0)
            parts.Add($"{absoluteDuration.Hours} hour{(absoluteDuration.Hours > 1 ? "s" : "")}");
        if (absoluteDuration.Minutes > 0)
            parts.Add($"{absoluteDuration.Minutes} minute{(absoluteDuration.Minutes > 1 ? "s" : "")}");
        if (absoluteDuration.Seconds > 0 || parts.Count == 0)
            parts.Add($"{absoluteDuration.Seconds} second{(absoluteDuration.Seconds > 1 ? "s" : "")}");

        var result = string.Join(", ", parts.Take(3)); // Limit to 3 parts for readability
        return duration < TimeSpan.Zero ? $"-{result}" : result;
    }

    /// <summary>
    /// Rounds a DateTime to the nearest specified interval.
    /// Example: 12:34:56 rounded to 5 minutes -> 12:35:00
    /// </summary>
    /// <param name="dateTime">The DateTime to round</param>
    /// <param name="interval">The rounding interval</param>
    /// <returns>The rounded DateTime</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dateTime"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="interval"/> is not positive</exception>
    public static DateTime RoundToInterval(this DateTime dateTime, TimeSpan interval)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        if (interval <= TimeSpan.Zero)
            throw new ArgumentException("Interval must be greater than zero", nameof(interval));

        var ticks = dateTime.Ticks + (interval.Ticks / 2);
        return new DateTime((ticks / interval.Ticks) * interval.Ticks, dateTime.Kind);
    }

    /// <summary>
    /// Gets the beginning of the day (midnight).
    /// </summary>
    /// <param name="dateTime">The DateTime to get start of day for</param>
    /// <returns>The start of the day</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dateTime"/> is null</exception>
    public static DateTime StartOfDay(this DateTime dateTime) => dateTime.Date;

    /// <summary>
    /// Gets the end of the day (23:59:59.999).
    /// </summary>
    /// <param name="dateTime">The DateTime to get end of day for</param>
    /// <returns>The end of the day</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dateTime"/> is null</exception>
    public static DateTime EndOfDay(this DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        return dateTime.Date.AddDays(1).AddTicks(-1);
    }

    /// <summary>
    /// Gets the first day of the month for a given DateTime.
    /// </summary>
    /// <param name="dateTime">The DateTime to get start of month for</param>
    /// <returns>The first day of the month</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dateTime"/> is null</exception>
    public static DateTime StartOfMonth(this DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        return new DateTime(dateTime.Year, dateTime.Month, 1);
    }

    /// <summary>
    /// Gets the last day of the month for a given DateTime.
    /// </summary>
    /// <param name="dateTime">The DateTime to get end of month for</param>
    /// <returns>The last day of the month</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dateTime"/> is null</exception>
    public static DateTime EndOfMonth(this DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        return dateTime.StartOfMonth().AddMonths(1).AddDays(-1);
    }

    /// <summary>
    /// Gets the first day of the week (Monday by default) for a given DateTime.
    /// </summary>
    /// <param name="dateTime">The DateTime to get start of week for</param>
    /// <param name="startDayOfWeek">The day of week to consider as start of week</param>
    /// <returns>The first day of the week</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dateTime"/> is null</exception>
    public static DateTime StartOfWeek(this DateTime dateTime, DayOfWeek startDayOfWeek = DayOfWeek.Monday)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        var diff = (7 + (dateTime.DayOfWeek - startDayOfWeek)) % 7;
        return dateTime.AddDays(-diff);
    }

    /// <summary>
    /// Gets the last day of the week (Sunday by default) for a given DateTime.
    /// </summary>
    /// <param name="dateTime">The DateTime to get end of week for</param>
    /// <param name="startDayOfWeek">The day of week to consider as start of week</param>
    /// <returns>The last day of the week</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dateTime"/> is null</exception>
    public static DateTime EndOfWeek(this DateTime dateTime, DayOfWeek startDayOfWeek = DayOfWeek.Monday)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        return dateTime.StartOfWeek(startDayOfWeek).AddDays(6).EndOfDay();
    }

    /// <summary>
    /// Converts a DateTime to Unix timestamp (seconds since epoch).
    /// </summary>
    /// <param name="dateTime">The DateTime to convert</param>
    /// <returns>Unix timestamp in seconds</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dateTime"/> is null</exception>
    public static long ToUnixTimestamp(this DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        return (long)(dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }

    /// <summary>
    /// Converts a Unix timestamp (seconds since epoch) to DateTime.
    /// </summary>
    /// <param name="timestamp">The Unix timestamp to convert</param>
    /// <returns>The converted DateTime</returns>
    public static DateTime FromUnixTimestamp(long timestamp)
    {
        return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp).ToLocalTime();
    }

    /// <summary>
    /// Checks if a DateTime is approximately equal to another (within a tolerance).
    /// Useful for comparing times without microsecond precision.
    /// </summary>
    /// <param name="dateTime">The first DateTime</param>
    /// <param name="other">The second DateTime to compare with</param>
    /// <param name="tolerance">Optional tolerance for comparison</param>
    /// <returns>True if the DateTimes are approximately equal</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dateTime"/> is null</exception>
    public static bool IsApproximately(this DateTime dateTime, DateTime other, TimeSpan? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        tolerance ??= TimeSpan.FromSeconds(1);
        var diff = Math.Abs((dateTime - other).TotalSeconds);
        return diff <= tolerance.Value.TotalSeconds;
    }

    /// <summary>
    /// Gets a friendly relative time description (e.g., "2 hours ago", "in 3 days").
    /// </summary>
    /// <param name="dateTime">The DateTime to get relative time for</param>
    /// <returns>Friendly relative time description</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dateTime"/> is null</exception>
    public static string ToRelativeTime(this DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        var now = DateTime.UtcNow;
        var span = now - dateTime.ToUniversalTime();

        // Future
        if (span.TotalSeconds < 0)
        {
            var futureSpan = dateTime.ToUniversalTime() - now;
            return futureSpan.TotalSeconds < 60
                ? "just now"
                : futureSpan.TotalMinutes < 60
                    ? $"in {(int)futureSpan.TotalMinutes} minute{((int)futureSpan.TotalMinutes != 1 ? "s" : "")}"
                    : futureSpan.TotalHours < 24
                        ? $"in {(int)futureSpan.TotalHours} hour{((int)futureSpan.TotalHours != 1 ? "s" : "")}"
                        : $"in {(int)futureSpan.TotalDays} day{((int)futureSpan.TotalDays != 1 ? "s" : "")}";
        }

        // Past
        return span.TotalSeconds < 60
            ? "just now"
            : span.TotalMinutes < 60
                ? $"{(int)span.TotalMinutes} minute{((int)span.TotalMinutes != 1 ? "s" : "")} ago"
                : span.TotalHours < 24
                    ? $"{(int)span.TotalHours} hour{((int)span.TotalHours != 1 ? "s" : "")} ago"
                    : $"{(int)span.TotalDays} day{((int)span.TotalDays != 1 ? "s" : "")} ago";
    }
}
