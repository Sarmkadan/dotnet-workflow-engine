# RateLimitConfig
The `RateLimitConfig` type is used to configure rate limiting settings, allowing you to define the maximum number of requests allowed within a specified time window, as well as the retry delay after the limit is exceeded. This configuration is essential in preventing abuse and ensuring the stability of your application.

## API
* `public int MaxRequests`: Gets the maximum number of requests allowed within the time window.
* `public int WindowSeconds`: Gets the time window in seconds during which the maximum number of requests is enforced.
* `public int RetryAfterSeconds`: Gets the delay in seconds after which a client can retry a request that was previously rate limited.
* `public static string ToJson(RateLimitConfig config)`: Converts a `RateLimitConfig` instance to a JSON string representation. This method does not throw any exceptions.
* `public static RateLimitConfig? FromJson(string json)`: Attempts to parse a JSON string and create a corresponding `RateLimitConfig` instance. Returns `null` if the parsing fails.
* `public static bool TryFromJson(string json, out RateLimitConfig config)`: Attempts to parse a JSON string and create a corresponding `RateLimitConfig` instance. Returns `true` if the parsing is successful, and `false` otherwise. The parsed `RateLimitConfig` instance is stored in the `config` output parameter.

## Usage
The following examples demonstrate how to use the `RateLimitConfig` type:
```csharp
// Create a new RateLimitConfig instance
var config = new RateLimitConfig { MaxRequests = 10, WindowSeconds = 60, RetryAfterSeconds = 30 };

// Convert the config to a JSON string
var json = RateLimitConfig.ToJson(config);
Console.WriteLine(json);

// Parse a JSON string to create a RateLimitConfig instance
if (RateLimitConfig.TryFromJson(json, out var parsedConfig))
{
    Console.WriteLine($"MaxRequests: {parsedConfig.MaxRequests}, WindowSeconds: {parsedConfig.WindowSeconds}, RetryAfterSeconds: {parsedConfig.RetryAfterSeconds}");
}
```

## Notes
When using the `RateLimitConfig` type, consider the following edge cases:
* If `MaxRequests` is set to 0, it effectively disables rate limiting.
* If `WindowSeconds` is set to 0, it will result in an immediate retry, which may lead to abuse.
* The `ToJson` and `FromJson` methods do not perform any validation on the input data. Therefore, it is essential to ensure that the data is correct and consistent to avoid errors.
* The `RateLimitConfig` type is not thread-safe. If you need to access or modify instances of this type from multiple threads, you must implement proper synchronization mechanisms to avoid data corruption or other concurrency-related issues.
