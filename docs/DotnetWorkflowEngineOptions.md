# DotnetWorkflowEngineOptions

Configuration options for the .NET Workflow Engine, controlling runtime behavior, performance, logging, caching, and resilience features.

## API

### `ConnectionString`
Gets or sets the connection string used by the workflow engine to connect to the underlying persistence store (e.g., database). This value is required for most persistence-backed operations unless an in-memory provider is used.

### `DefaultRetryPolicy`
Gets or sets the default retry policy configuration for transient fault handling during workflow execution and persistence operations. If `null`, no automatic retries are performed. The policy defines backoff intervals, maximum retry attempts, and exception filtering rules.

### `EnableAuditLogging`
Gets or sets a value indicating whether detailed audit logs are emitted for workflow state changes, activity executions, and lifecycle events. When `true`, logs include timestamps, user context (if available), and serialized event data.

### `MaxConcurrentWorkflows`
Gets or sets the maximum number of workflows that can be executed concurrently across all workflow hosts. This limits resource usage and prevents system overload. Must be a positive integer; default is typically derived from available CPU cores.

### `DefaultActivityTimeoutSeconds`
Gets or sets the default timeout, in seconds, applied to individual activity executions if not explicitly specified. Activities exceeding this duration are automatically marked as failed. Must be a non-negative integer; zero implies no timeout.

### `ValidateWorkflowsOnLoad`
Gets or sets a value indicating whether workflow definitions are validated against schema and structural rules when loaded into the engine. Enables early detection of misconfigured workflows but adds startup overhead.

### `EnableMetrics`
Gets or sets a value indicating whether runtime metrics (e.g., workflow throughput, activity durations, queue depths) are collected and exposed via monitoring endpoints. Metrics are useful for observability but incur minimal performance overhead.

### `EnableBackgroundJobs`
Gets or sets a value indicating whether background processing tasks (e.g., cleanup, heartbeat monitoring, cache eviction) are enabled. Disabling may reduce resource usage but can impact system reliability and cleanup.

### `EnableAuditTrail`
Gets or sets a value indicating whether a complete audit trail of workflow events is persisted for compliance and replay. When `true`, events are stored in the audit store; when `false`, only summary logs are emitted.

### `CachingEnabled`
Gets or sets a value indicating whether runtime caching of workflow definitions, activities, and compiled expressions is enabled. Improves performance by reducing I/O and reflection overhead but increases memory usage.

### `CacheProvider`
Gets or sets the name of the caching provider to use (e.g., "Memory", "Redis"). Determines where cached items are stored and how they are invalidated. Must match a registered provider name.

### `RedisConnectionString`
Gets or sets the connection string for the Redis cache provider. Used only when `CacheProvider` is set to "Redis". Must be a valid Redis connection string; ignored for other providers.

### `DefaultCacheExpiration`
Gets or sets the default expiration duration for cached workflow definitions and activities. Defines how long items remain valid before being evicted or refreshed. Must be a positive `TimeSpan`.

### `UseDistributedCache`
Gets or sets a value indicating whether a distributed cache (e.g., Redis) is used instead of a local in-memory cache. Enables multi-node consistency but increases latency and dependency on external systems.

### `EnableRequestLogging`
Gets or sets a value indicating whether HTTP request and response logging is enabled for workflow API endpoints. Includes headers, status codes, and timing information. Useful for debugging but may expose sensitive data.

### `LogRequestBody`
Gets or sets a value indicating whether request bodies are included in request logs. Enables detailed inspection of input payloads but may log sensitive or large data. Only effective when `EnableRequestLogging` is `true`.

### `LogResponseBody`
Gets or sets a value indicating whether response bodies are included in response logs. Enables detailed inspection of output payloads but may log sensitive or large data. Only effective when `EnableRequestLogging` is `true`.

### `EnableRateLimiting`
Gets or sets a value indicating whether rate limiting is enforced on workflow API endpoints. Prevents abuse and ensures fair resource allocation. Requires `RateLimit` to be configured.

### `RateLimit`
Gets or sets the rate limit configuration, including requests per time window and burst allowances. Applied globally or per-route depending on engine implementation. Must be non-null when `EnableRateLimiting` is `true`.

### `EnableCors`
Gets or sets a value indicating whether Cross-Origin Resource Sharing (CORS) headers are added to API responses. Enables browser-based clients to interact with the workflow engine. Defaults to `true` in most configurations.

## Usage

### Example 1: Basic Configuration
