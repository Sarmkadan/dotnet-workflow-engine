# DateTimeExtensions

Provides a set of utility methods for common `DateTime` operations including ISO 8601 formatting, temporal comparisons, duration formatting, Unix timestamp conversion, and rounding to time intervals.

## API

### `public static string ToIso8601(DateTime date)`
Formats the given `DateTime` as an ISO 8601 string in UTC. The output uses the format `yyyy-MM-ddTHH:mm:ss.fffZ`.

- **Parameters**
  - `date`: The `DateTime` to format.
- **Returns**
  - A string representing the date in ISO 8601 format.
- **Throws**
  - `ArgumentNullException`: If `date` is `null`.

---

### `public static bool IsPast(DateTime date)`
Determines whether the given `DateTime` is in the past relative to `DateTime.UtcNow`.

- **Parameters**
  - `date`: The `DateTime` to evaluate.
- **Returns**
  - `true` if `date` is earlier than the current UTC time; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `date` is `null`.

---

### `public static bool IsFuture(DateTime date)`
Determines whether the given `DateTime` is in the future relative to `DateTime.UtcNow`.

- **Parameters**
  - `date`: The `DateTime` to evaluate.
- **Returns**
  - `true` if `date` is later than the current UTC time; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `date` is `null`.

---
### `public static bool IsBetween(DateTime date, DateTime start, DateTime end)`
Determines whether the given `DateTime` falls between two other `DateTime` values (inclusive).

- **Parameters**
  - `date`: The `DateTime` to check.
  - `start`: The start of the interval.
  - `end`: The end of the interval.
- **Returns**
  - `true` if `date` is between `start` and `end` (inclusive); otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If any argument is `null`.
  - `ArgumentException`: If `start` is later than `end`.

---
### `public static string DurationToString(TimeSpan duration)`
Converts a `TimeSpan` into a human-readable string (e.g., "2h 30m 15s").

- **Parameters**
  - `duration`: The `TimeSpan` to format.
- **Returns**
  - A string representing the duration in a concise format.
- **Throws**
  - `ArgumentNullException`: If `duration` is `null`.

---
### `public static string DurationToString(TimeSpan duration, bool includeMilliseconds)`
Overload. Includes milliseconds in the output when `includeMilliseconds` is `true`.

- **Parameters**
  - `duration`: The `TimeSpan` to format.
  - `includeMilliseconds`: Whether to include milliseconds in the output.
- **Returns**
  - A string representing the duration with optional millisecond precision.
- **Throws**
  - `ArgumentNullException`: If `duration` is `null`.

---
### `public static DateTime RoundToInterval(DateTime date, TimeSpan interval)`
Rounds the given `DateTime` to the nearest multiple of the specified time interval.

- **Parameters**
  - `date`: The `DateTime` to round.
  - `interval`: The rounding interval (e.g., `TimeSpan.FromHours(1)`).
- **Returns**
  - A `DateTime` rounded to the nearest interval.
- **Throws**
  - `ArgumentNullException`: If `date` is `null` or `interval` is `TimeSpan.Zero` or negative.

---
### `public static DateTime StartOfDay(DateTime date)`
Returns a `DateTime` representing the start of the day (00:00:00) for the given date.

- **Parameters**
  - `date`: The input `DateTime`.
- **Returns**
  - A `DateTime` at the start of the day in the same timezone as `date`.
- **Throws**
  - `ArgumentNullException`: If `date` is `null`.

---
### `public static DateTime EndOfDay(DateTime date)`
Returns a `DateTime` representing the end of the day (23:59:59.999) for the given date.

- **Parameters**
  - `date`: The input `DateTime`.
- **Returns**
  - A `DateTime` at the end of the day in the same timezone as `date`.
- **Throws**
  - `ArgumentNullException`: If `date` is `null`.

---
### `public static DateTime StartOfMonth(DateTime date)`
Returns a `DateTime` representing the first moment of the month for the given date.

- **Parameters**
  - `date`: The input `DateTime`.
- **Returns**
  - A `DateTime` at the start of the month in the same timezone as `date`.
- **Throws**
  - `ArgumentNullException`: If `date` is `null`.

---
### `public static DateTime EndOfMonth(DateTime date)`
Returns a `DateTime` representing the last moment of the month for the given date.

- **Parameters**
  - `date`: The input `DateTime`.
- **Returns**
  - A `DateTime` at the end of the month in the same timezone as `date`.
- **Throws**
  - `ArgumentNullException`: If `date` is `null`.

---
### `public static DateTime StartOfWeek(DateTime date, DayOfWeek firstDayOfWeek = DayOfWeek.Monday)`
Returns a `DateTime` representing the start of the week for the given date, using the specified first day of the week.

- **Parameters**
  - `date`: The input `DateTime`.
  - `firstDayOfWeek`: The first day of the week (default: `DayOfWeek.Monday`).
- **Returns**
  - A `DateTime` at the start of the week in the same timezone as `date`.
- **Throws**
  - `ArgumentNullException`: If `date` is `null`.
  - `ArgumentOutOfRangeException`: If `firstDayOfWeek` is not a valid `DayOfWeek` value.

---
### `public static DateTime EndOfWeek(DateTime date, DayOfWeek firstDayOfWeek = DayOfWeek.Monday)`
Returns a `DateTime` representing the end of the week for the given date, using the specified first day of the week.

- **Parameters**
  - `date`: The input `DateTime`.
  - `firstDayOfWeek`: The first day of the week (default: `DayOfWeek.Monday`).
- **Returns**
  - A `DateTime` at the end of the week in the same timezone as `date`.
- **Throws**
  - `ArgumentNullException`: If `date` is `null`.
  - `ArgumentOutOfRangeException`: If `firstDayOfWeek` is not a valid `DayOfWeek` value.

---
### `public static long ToUnixTimestamp(DateTime date)`
Converts the given `DateTime` to a Unix timestamp (seconds since 1970-01-01T00:00:00Z).

- **Parameters**
  - `date`: The `DateTime` to convert. Assumed to be in UTC.
- **Returns**
  - The Unix timestamp as a `long`.
- **Throws**
  - `ArgumentNullException`: If `date` is `null`.
  - `ArgumentException`: If `date` is earlier than `DateTime.UnixEpoch`.

---
### `public static DateTime FromUnixTimestamp(long timestamp)`
Converts a Unix timestamp (seconds since 1970-01-01T00:00:00Z) to a `DateTime` in UTC.

- **Parameters**
  - `timestamp`: The Unix timestamp to convert.
- **Returns**
  - A `DateTime` representing the timestamp in UTC.
- **Throws**
  - `ArgumentOutOfRangeException`: If `timestamp` is negative.

---
### `public static bool IsApproximately(DateTime date, DateTime other, TimeSpan tolerance)`
Determines whether two `DateTime` values are approximately equal within a specified tolerance.

- **Parameters**
  - `date`: The first `DateTime`.
  - `other`: The second `DateTime`.
  - `tolerance`: The maximum allowed difference between the two dates.
- **Returns**
  - `true` if the absolute difference between `date` and `other` is less than or equal to `tolerance`; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `date` or `other` is `null`.
  - `ArgumentNullException`: If `tolerance` is `null` or negative.

---
### `public static string ToRelativeTime(DateTime date)`
Converts the given `DateTime` to a human-readable relative time string (e.g., "2 minutes ago", "in 3 hours").

- **Parameters**
  - `date`: The `DateTime` to convert. Assumed to be in UTC.
- **Returns**
  - A string describing the relative time.
- **Throws**
  - `ArgumentNullException`: If `date` is `null`.

## Usage
