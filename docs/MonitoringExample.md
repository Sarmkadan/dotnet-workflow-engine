# MonitoringExample

MonitoringExample is an ASP.NET Core controller that provides read‑only endpoints for observing the state and performance of the workflow engine. It exposes metrics, health information, audit data, performance trends, and resource utilization without modifying any workflow state.

## API

### `public async Task<ActionResult> GetMetrics`
- **Purpose:** Returns a collection of runtime metrics (e.g., workflow execution counts, average latency) for the engine.
- **Parameters:** None.
- **Return Value:** `Task<ActionResult>` whose result serializes to JSON containing the metric payload; on success yields `200 OK`.
- **When it Throws:** Any exception thrown by the underlying metrics service propagates as a `500 Internal Server Error`. If the service is unavailable, a `503 Service Unavailable` may be returned.

### `public async Task<ActionResult> GetHealthStatus`
- **Purpose:** Provides the current health status of the workflow engine (e.g., healthy, degraded, unhealthy).
- **Parameters:** None.
- **Return Value:** `Task<ActionResult>` yielding a JSON object with health details; success results in `200 OK`.
- **When it Throws:** Propagates exceptions from health checks as `500 Internal Server Error`. Timeout or failure to reach dependent services results in `503 Service Unavailable`.

### `public async Task<ActionResult> GetAuditStatistics`
- **Purpose:** Returns aggregated audit information such as numbers of started, completed, and cancelled workflows within a configurable time window.
- **Parameters:** None.
- **Return Value:** `Task<ActionResult>` with JSON audit statistics; success yields `200 OK`.
- **When it Throws:** Exceptions from the audit store cause `500 Internal Server Error`. If the audit store is offline, a `503 Service Unavailable` may be produced.

### `public async Task<ActionResult> GetPerformanceTrends`
- **Purpose:** Supplies time‑series data illustrating performance trends (e.g., average execution time over the last hour).
- **Parameters:** None.
- **Return Value:** `Task<ActionResult>` returning JSON trend data; success yields `200 OK`.
- **When it Throws:** Errors in the performance collection layer lead to `500 Internal Server Error`. Missing data may result in `204 No Content`.

### `public async Task<ActionResult> GetSlowestWorkflows`
- **Purpose:** Lists the workflow definitions or instances that have exhibited the longest execution times.
- **Parameters:** None.
- **Return Value:** `Task<ActionResult>` with JSON array of slowest workflow entries; success yields `200 OK`.
- **When it Throws:** Exceptions from the workflow repository cause `500 Internal Server Error`. If no data is available, returns `204 No Content`.

### `public async Task<ActionResult> GetFailedWorkflows`
- **Purpose:** Provides details about workflows that have failed, including error messages and timestamps.
- **Parameters:** None.
- **Return Value:** `Task<ActionResult>` returning JSON collection of failed workflow records; success yields `200 OK`.
- **When it Throws:** Repository or logging failures raise `500 Internal Server Error`. Absence of failures yields `204 No Content`.

### `public ActionResult GetResourceUsage`
- **Purpose:** Returns current resource consumption (CPU, memory, disk) of the host process running the workflow engine.
- **Parameters:** None.
- **Return Value:** `ActionResult` with JSON resource usage data; success yields `200 OK`.
- **When it Throws:** If performance counters cannot be read, a `500 Internal Server Error` is returned.

## Usage

```csharp
// Example 1: Retrieving metrics via HttpClient
using var http = new HttpClient();
var response = await http.GetAsync("https://api.example.com/monitoring/metrics");
response.EnsureSuccessStatusCode();
var metricsJson = await response.Content.ReadAsStringAsync();
// Deserialize metricsJson into your preferred DTO
```

```csharp
// Example 2: Checking health status from a controller action
public async Task<IActionResult> CheckEngineHealth()
{
    using var client = new HttpClient();
    var healthResponse = await client.GetAsync("https://api.example.com/monitoring/healthstatus");
    if (!healthResponse.IsSuccessStatusCode)
    {
        return StatusCode((int)healthResponse.StatusCode, "Health check failed");
    }
    var health = await healthResponse.Content.ReadFromJsonAsync<HealthDto>();
    return Ok(health);
}
```

## Notes

- The controller is stateless; each request is processed independently, making it safe for concurrent invocations.
- All methods rely on external services (metrics, audit, performance counters). Failure of those services results in HTTP 5xx responses as described.
- Large result sets (e.g., extensive audit statistics) may cause response truncation or timeouts; consider paging or filtering if introduced in future versions.
- No method accepts input parameters, so there is no risk of argument‑validation exceptions arising from the controller itself.
- The synchronous `GetResourceUsage` method performs quick, in‑process counter reads; it does not block threads for extended periods, but under heavy load it may contribute to CPU usage.
