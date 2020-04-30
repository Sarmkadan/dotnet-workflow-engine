# IHttpClientFactory
The `IHttpClientFactory` interface is designed to provide a flexible and configurable way to create instances of `HttpClient`. It allows for customization of the client's base URL, timeout, default headers, and retry policies, making it suitable for a wide range of HTTP-based applications. This interface is part of the `dotnet-workflow-engine` project, which aims to simplify the development of workflow-based systems.

## API
* `BaseUrl`: Gets or sets the base URL for the HTTP client.
* `TimeoutSeconds`: Gets or sets the timeout in seconds for the HTTP client.
* `DefaultHeaders`: Gets or sets a dictionary of default headers for the HTTP client.
* `MaxRetries`: Gets or sets the maximum number of retries for failed requests.
* `RetryDelayMs`: Gets or sets the delay in milliseconds between retries.
* `StandardHttpClientFactory`: Not described in provided information.
* `GetClient`: Not described in provided information, but presumably returns an instance of `HttpClient`.
* `RegisterClient`: Not described in provided information, but presumably registers a client with the factory.
* `GetWithRetryAsync`: Sends a GET request with retry logic. Returns an `HttpResponseMessage`. Throws if the maximum number of retries is exceeded or if an underlying exception occurs.
* `PostWithRetryAsync`: Sends a POST request with retry logic. Returns an `HttpResponseMessage`. Throws if the maximum number of retries is exceeded or if an underlying exception occurs.

## Usage
The following examples demonstrate how to use the `IHttpClientFactory` interface:
```csharp
// Example 1: Creating a client with custom settings
var factory = new MyHttpClientFactory();
factory.BaseUrl = "https://example.com";
factory.TimeoutSeconds = 30;
factory.DefaultHeaders.Add("Accept", "application/json");
var client = factory.GetClient();
var response = await client.GetAsync("api/data");
```

```csharp
// Example 2: Using the factory to send a POST request with retries
var factory = new MyHttpClientFactory();
factory.MaxRetries = 3;
factory.RetryDelayMs = 500;
var response = await IHttpClientFactory.PostWithRetryAsync("https://example.com/api/data", new StringContent("Hello World", Encoding.UTF8, "text/plain"));
```

## Notes
When using the `IHttpClientFactory` interface, consider the following edge cases and thread-safety remarks:
* The `BaseUrl` property should be set before creating a client instance to ensure the correct base URL is used.
* The `TimeoutSeconds` property should be set according to the specific requirements of the application to avoid timeouts or slow responses.
* The `DefaultHeaders` dictionary should be populated with the required headers for the application, such as authentication tokens or content type headers.
* The `MaxRetries` and `RetryDelayMs` properties should be configured according to the specific retry policy required by the application.
* The `GetWithRetryAsync` and `PostWithRetryAsync` methods will throw if the maximum number of retries is exceeded or if an underlying exception occurs, so error handling should be implemented accordingly.
* The `IHttpClientFactory` interface is designed to be thread-safe, but the underlying `HttpClient` instances created by the factory may not be. Therefore, it is recommended to use a new instance of `HttpClient` for each request or to implement a thread-safe caching mechanism.
