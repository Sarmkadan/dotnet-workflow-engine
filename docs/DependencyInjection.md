# DependencyInjection

The `DependencyInjection` type provides extension methods for configuring workflow engine services in ASP.NET Core applications. It enables registration of core workflow services, CORS policies, authentication schemes, and rate limiting directly in the application's service configuration and middleware pipeline.

## API

### `public static IServiceCollection AddWorkflowEngine(this IServiceCollection services, Action<WorkflowEngineOptions>? configure = null)`

Registers the core workflow engine services with the dependency injection container. The optional `configure` delegate allows customization of workflow engine behavior through `WorkflowEngineOptions`.

- **Parameters**
  - `services`: The `IServiceCollection` instance.
  - `configure`: Optional delegate to configure workflow engine options.
- **Return Value**: The same `IServiceCollection` instance for method chaining.
- **Throws**: `ArgumentNullException` if `services` is `null`.

---

### `public static WebApplication UseWorkflowEngine(this WebApplication app)`

Adds the workflow engine middleware to the ASP.NET Core pipeline. This middleware processes incoming workflow requests and manages engine lifecycle.

- **Parameters**
  - `app`: The `WebApplication` instance.
- **Return Value**: The same `WebApplication` instance for method chaining.
- **Throws**: `ArgumentNullException` if `app` is `null`.

---

### `public static IServiceCollection AddWorkflowEngineCors(this IServiceCollection services, Action<CorsPolicyBuilder>? configurePolicy = null)`

Registers CORS services and configures a default CORS policy for the workflow engine. The optional `configurePolicy` delegate allows further customization of the CORS policy.

- **Parameters**
  - `services`: The `IServiceCollection` instance.
  - `configurePolicy`: Optional delegate to configure the CORS policy.
- **Return Value**: The same `IServiceCollection` instance for method chaining.
- **Throws**: `ArgumentNullException` if `services` is `null`.

---

### `public static IServiceCollection AddWorkflowEngineAuthentication(this IServiceCollection services, Action<AuthenticationOptions>? configure = null)`

Registers authentication services and configures default authentication schemes for the workflow engine. The optional `configure` delegate allows customization of authentication requirements.

- **Parameters**
  - `services`: The `IServiceCollection` instance.
  - `configure`: Optional delegate to configure authentication options.
- **Return Value**: The same `IServiceCollection` instance for method chaining.
- **Throws**: `ArgumentNullException` if `services` is `null`.

---

### `public bool EnableRequestLogging { get; set; }`

Gets or sets a value indicating whether to enable request logging for workflow engine operations. When `true`, HTTP requests and responses are logged for debugging and monitoring purposes.

- **Default**: `false`
- **Thread Safety**: This property is not thread-safe; concurrent reads and writes may lead to race conditions.

---

### `public bool LogRequestBody { get; set; }`

Gets or sets a value indicating whether to log the request body. Only applicable when `EnableRequestLogging` is `true`.

- **Default**: `false`
- **Thread Safety**: This property is not thread-safe; concurrent reads and writes may lead to race conditions.

---
### `public bool LogResponseBody { get; set; }`

Gets or sets a value indicating whether to log the response body. Only applicable when `EnableRequestLogging` is `true`.

- **Default**: `false`
- **Thread Safety**: This property is not thread-safe; concurrent reads and writes may lead to race conditions.

---
### `public bool EnableRateLimiting { get; set; }`

Gets or sets a value indicating whether to enable rate limiting for workflow engine endpoints. When `true`, requests are throttled based on `RateLimit` configuration.

- **Default**: `false`
- **Thread Safety**: This property is not thread-safe; concurrent reads and writes may lead to race conditions.

---
### `public RateLimitConfiguration RateLimit { get; set; }`

Gets or sets the rate limiting configuration. Only applicable when `EnableRateLimiting` is `true`.

- **Default**: `null`
- **Thread Safety**: This property is not thread-safe; concurrent reads and writes may lead to race conditions.

---
### `public bool EnableCors { get; set; }`

Gets or sets a value indicating whether to enable CORS middleware for workflow engine endpoints.

- **Default**: `false`
- **Thread Safety**: This property is not thread-safe; concurrent reads and writes may lead to race conditions.

---
### `public int MaxRequests { get; set; }`

Gets or sets the maximum number of requests allowed within the rate limiting window. Only applicable when `EnableRateLimiting` is `true`.

- **Default**: `100`
- **Thread Safety**: This property is not thread-safe; concurrent reads and writes may lead to race conditions.

---
### `public int WindowSeconds { get; set; }`

Gets or sets the duration of the rate limiting window in seconds. Only applicable when `EnableRateLimiting` is `true`.

- **Default**: `60`
- **Thread Safety**: This property is not thread-safe; concurrent reads and writes may lead to race conditions.

---
### `public int RetryAfterSeconds { get; set; }`

Gets or sets the number of seconds to wait before allowing another request after rate limiting is triggered. Only applicable when `EnableRateLimiting` is `true`.

- **Default**: `30`
- **Thread Safety**: This property is not thread-safe; concurrent reads and writes may lead to race conditions.

## Usage

### Example 1: Basic Workflow Engine Setup
