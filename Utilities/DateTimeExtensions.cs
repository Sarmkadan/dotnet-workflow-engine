// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

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
    public static string ToIso8601(this DateTime dateTime)
    {
        return dateTime.ToUniversalTime().ToString("O");
    }

    /// <summary>
    /// Checks if a DateTime is in the past.
    /// </summary>
    public static bool IsPast(this DateTime dateTime)
    {
        return dateTime < DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if a DateTime is in the future.
    /// </summary>
    public static bool IsFuture(this DateTime dateTime)
    {
        return dateTime > DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if a DateTime is within a range (inclusive).
    /// </summary>
    public static bool IsBetween(this DateTime dateTime, DateTime start, DateTime end)
    {
        return dateTime >= start && dateTime <= end;
    }

    /// <summary>
    /// Calculates the duration between two DateTime values.
    /// Returns a human-readable string (e.g., "2 hours, 30 minutes").
    /// </summary>
    public static string DurationToString(this DateTime start, DateTime end)
    {
        var duration = end - start;
        return DurationToString(duration);
    }

    /// <summary>
    /// Converts a TimeSpan duration to a human-readable string.
    /// Example: 1 day, 2 hours, 30 minutes
    /// </summary>
    public static string DurationToString(this TimeSpan duration)
    {
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
    public static DateTime RoundToInterval(this DateTime dateTime, TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
            throw new ArgumentException("Interval must be greater than zero");

        var ticks = dateTime.Ticks + (interval.Ticks / 2);
        return new DateTime((ticks / interval.Ticks) * interval.Ticks, dateTime.Kind);
    }

    /// <summary>
    /// Gets the beginning of the day (midnight).
    /// </summary>
    public static DateTime StartOfDay(this DateTime dateTime)
    {
        return dateTime.Date;
    }

    /// <summary>
    /// Gets the end of the day (23:59:59.999).
    /// </summary>
    public static DateTime EndOfDay(this DateTime dateTime)
    {
        return dateTime.Date.AddDays(1).AddTicks(-1);
    }

    /// <summary>
    /// Gets the first day of the month for a given DateTime.
    /// </summary>
    public static DateTime StartOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1);
    }

    /// <summary>
    /// Gets the last day of the month for a given DateTime.
    /// </summary>
    public static DateTime EndOfMonth(this DateTime dateTime)
    {
        return dateTime.StartOfMonth().AddMonths(1).AddDays(-1);
    }

    /// <summary>
    /// Gets the first day of the week (Monday by default) for a given DateTime.
    /// </summary>
    public static DateTime StartOfWeek(this DateTime dateTime, DayOfWeek startDayOfWeek = DayOfWeek.Monday)
    {
        var diff = (7 + (dateTime.DayOfWeek - startDayOfWeek)) % 7;
        return dateTime.AddDays(-diff);
    }

    /// <summary>
    /// Gets the last day of the week (Sunday by default) for a given DateTime.
    /// </summary>
    public static DateTime EndOfWeek(this DateTime dateTime, DayOfWeek startDayOfWeek = DayOfWeek.Monday)
    {
        return dateTime.StartOfWeek(startDayOfWeek).AddDays(6).EndOfDay();
    }

    /// <summary>
    /// Converts a DateTime to Unix timestamp (seconds since epoch).
    /// </summary>
    public static long ToUnixTimestamp(this DateTime dateTime)
    {
        return (long)(dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }

    /// <summary>
    /// Converts a Unix timestamp (seconds since epoch) to DateTime.
    /// </summary>
    public static DateTime FromUnixTimestamp(long timestamp)
    {
        return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp).ToLocalTime();
    }

    /// <summary>
    /// Checks if a DateTime is approximately equal to another (within a tolerance).
    /// Useful for comparing times without microsecond precision.
    /// </summary>
    public static bool IsApproximately(this DateTime dateTime, DateTime other, TimeSpan? tolerance = null)
    {
        tolerance ??= TimeSpan.FromSeconds(1);
        var diff = Math.Abs((dateTime - other).TotalSeconds);
        return diff <= tolerance.Value.TotalSeconds;
    }

    /// <summary>
    /// Gets a friendly relative time description (e.g., "2 hours ago", "in 3 days").
    /// </summary>
    public static string ToRelativeTime(this DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        var span = now - dateTime.ToUniversalTime();

        // Past
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

        return span.TotalSeconds < 60
            ? "just now"
            : span.TotalMinutes < 60
                ? $"{(int)span.TotalMinutes} minute{((int)span.TotalMinutes != 1 ? "s" : "")} ago"
                : span.TotalHours < 24
                    ? $"{(int)span.TotalHours} hour{((int)span.TotalHours != 1 ? "s" : "")} ago"
                    : $"{(int)span.TotalDays} day{((int)span.TotalDays != 1 ? "s" : "")} ago";
    }
}
