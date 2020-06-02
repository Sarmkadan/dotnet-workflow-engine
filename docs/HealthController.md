# HealthController

The `HealthController` is an ASP.NET Core controller that exposes endpoints and status information for monitoring the operational state of the workflow engine. It provides standardized health checks (`/liveness`, `/readiness`, `/health`) and a status endpoint (`/status`) to report component health, timestamps, and optional diagnostic details.

## API

### `public HealthController`
Constructor for the controller. Initializes a new instance of the `HealthController` with default or injected dependencies for health check services.

### `public IActionResult Liveness()`
Returns a lightweight health check indicating whether the application is running and responsive.

- **Returns**: `IActionResult` – HTTP 200 OK if the application is running; otherwise HTTP 503 Service Unavailable.
- **Throws**: May throw if the underlying system is in an unrecoverable fault state (e.g., critical process failure).

### `public async Task<IActionResult> Readiness()`
Performs an asynchronous readiness check to determine if the application is ready to serve traffic. Typically includes checks for database connectivity, external service availability, and internal initialization.

- **Returns**: `Task<IActionResult>` – HTTP 200 OK if all readiness checks pass; otherwise HTTP 503 Service Unavailable.
- **Throws**: May throw if a required dependency is unavailable or initialization is incomplete.

### `public async Task<IActionResult> Health()`
Performs a comprehensive health check, including readiness and optional component-specific diagnostics. Returns a detailed status payload.

- **Returns**: `Task<IActionResult>` – HTTP 200 OK with a `HealthStatus` payload if healthy; otherwise HTTP 503 or HTTP 424 (Failed Dependency) with error details.
- **Throws**: May throw if a critical health check fails or a dependency throws unexpectedly.

### `public HealthStatus Status`
Gets the current health status of the component (`Healthy`, `Degraded`, `Unhealthy`).

- **Type**: `HealthStatus`
- **Access**: Public getter
- **Note**: This reflects the overall state derived from internal health checks.

### `public string Component`
Gets the name of the component being monitored (e.g., `"WorkflowEngine"`).

- **Type**: `string`
- **Access**: Public getter
- **Note**: Used to disambiguate health reports in distributed systems.

### `public string Message`
Gets a human-readable message describing the current health state (e.g., `"All systems operational"`).

- **Type**: `string`
- **Access**: Public getter
- **Note**: May be `null` or empty if no message is provided.

### `public DateTime Timestamp`
Gets the UTC timestamp when the health status was last updated.

- **Type**: `DateTime`
- **Access**: Public getter
- **Note**: Always reflects the time of the most recent health evaluation.

### `public object? Details`
Gets additional diagnostic details about the health state (e.g., service version, uptime, dependency statuses).

- **Type**: `object?`
- **Access**: Public getter
- **Note**: May be `null` if no details are available.

### `public string? Exception`
Gets the exception message or summary if the last health evaluation resulted in an error.

- **Type**: `string?`
- **Access**: Public getter
- **Note**: May be `null` if no exception occurred.

## Usage
