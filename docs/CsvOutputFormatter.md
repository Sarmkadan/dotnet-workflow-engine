# CsvOutputFormatter

A formatter that converts collections of objects into comma-separated values (CSV) formatted strings. It is designed to serialize data for output in workflow engine contexts where CSV is a supported export format.

## API

### `public CsvOutputFormatter`

Initializes a new instance of the `CsvOutputFormatter` class.

### `public async Task<string> FormatAsync<T>(IEnumerable<T> items)`

Serializes an enumerable sequence of objects of type `T` into a CSV formatted string.

- **Parameters**
  - `items`: The collection of objects to serialize. Must not be `null`.
- **Return value**
  - A `Task<string>` that resolves to the CSV formatted string.
- **Exceptions**
  - Throws `ArgumentNullException` if `items` is `null`.

### `public Task<string> FormatAsync<T>(IEnumerable<T> items, CancellationToken cancellationToken)`

Serializes an enumerable sequence of objects of type `T` into a CSV formatted string with support for cooperative cancellation.

- **Parameters**
  - `items`: The collection of objects to serialize. Must not be `null`.
  - `cancellationToken`: A token to monitor for cancellation requests.
- **Return value**
  - A `Task<string>` that resolves to the CSV formatted string.
- **Exceptions**
  - Throws `ArgumentNullException` if `items` is `null`.
  - Throws `OperationCanceledException` if cancellation is requested via `cancellationToken`.

## Usage
