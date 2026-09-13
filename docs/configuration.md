# Configuration

This page documents the configuration surface defined by
`Configuration/DotnetWorkflowEngineOptions.cs` and every section included in
`appsettings.example.json`.

## Binding the workflow engine section

`DotnetWorkflowEngineOptions` follows the .NET options pattern. The engine's service
registration does not automatically bind the `WorkflowEngine` section, so bind it
explicitly and pass its connection string when registering the engine:

```csharp
using DotNetWorkflowEngine.Configuration;

var workflowSection = builder.Configuration.GetSection("WorkflowEngine");

builder.Services.Configure<DotnetWorkflowEngineOptions>(workflowSection);
builder.Services.AddWorkflowEngine(workflowSection["ConnectionString"]);
```

Configuration keys are case-insensitive. Environment variables use a double
underscore in place of `:`, for example:

```text
WorkflowEngine__MaxConcurrentWorkflows=200
WorkflowEngine__RateLimit__MaxRequests=500
```

Do not commit real database passwords, Redis credentials, webhook secrets, or JWT
secrets. The values in `appsettings.example.json` are placeholders.

## `WorkflowEngine`

The `WorkflowEngine` section binds to `DotnetWorkflowEngineOptions`. Defaults below
are the defaults created by the class when a key is omitted.

### Core and infrastructure

| Key | Type | Default | Validation / purpose |
| --- | --- | --- | --- |
| `ConnectionString` | string | `""` | Required; at least 10 characters. Persistence connection used by `DatabaseContext`. |
| `DefaultRetryPolicy` | `RetryPolicyConfig?` | `null` | Optional default activity retry policy; see [Retry policy](#retry-policy). |
| `EnableAuditLogging` | bool | `true` | Enables audit logging. |
| `MaxConcurrentWorkflows` | int | `100` | Range: 1–1,000. Maximum concurrent workflows. |
| `DefaultActivityTimeoutSeconds` | int | `300` | Range: 1–86,400. Default activity timeout in seconds. |
| `ValidateWorkflowsOnLoad` | bool | `true` | Enables validation when workflow definitions are loaded. |
| `EnableMetrics` | bool | `true` | Enables engine metrics. |
| `EnableBackgroundJobs` | bool | `true` | Enables background job processing. |
| `EnableAuditTrail` | bool | `true` | Enables the audit trail. |

### Caching

| Key | Type | Default | Validation / purpose |
| --- | --- | --- | --- |
| `CachingEnabled` | bool | `true` | Enables caching. |
| `CacheProvider` | string | `"Memory"` | Must be `Memory` or `Redis` (case-insensitive). |
| `RedisConnectionString` | string? | `null` | Required by the validator when `UseDistributedCache` is `true`. |
| `DefaultCacheExpiration` | `TimeSpan` | `01:00:00` | Must be greater than zero; JSON uses the .NET `TimeSpan` string format. |
| `UseDistributedCache` | bool | `false` | Selects distributed rather than local caching. |

### HTTP middleware and security

| Key | Type | Default | Validation / purpose |
| --- | --- | --- | --- |
| `EnableRequestLogging` | bool | `true` | Enables request logging. |
| `LogRequestBody` | bool | `false` | Includes request bodies in logs; avoid enabling when bodies may contain secrets or personal data. |
| `LogResponseBody` | bool | `false` | Includes response bodies in logs; avoid enabling when responses may contain sensitive data. |
| `EnableRateLimiting` | bool | `true` | Enables rate limiting. |
| `RateLimit` | object | See below | Rate-limiting thresholds. |
| `EnableCors` | bool | `true` | Enables CORS support. |
| `EnableWebhookValidation` | bool | `true` | Enables webhook validation. |
| `WebhookSecret` | string? | `null` | Secret used for webhook validation. Supply it from a secret store or environment variable. |
| `EnableActivityValidation` | bool | `true` | Enables activity validation. |
| `EnableWorkflowValidation` | bool | `true` | Enables workflow validation. |

`RateLimit` binds to `RateLimitConfig`:

| Key | Type | Default | Validation / purpose |
| --- | --- | --- | --- |
| `MaxRequests` | int | `100` | Range: 1–10,000. Requests allowed per window. |
| `WindowSeconds` | int | `60` | Range: 1–3,600. Window length in seconds. |
| `RetryAfterSeconds` | int | `60` | Range: 1–3,600. Retry delay advertised after a limit is reached. |

### Expressions and execution

| Key | Type | Default | Validation / purpose |
| --- | --- | --- | --- |
| `EnableExpressionEvaluation` | bool | `true` | Enables expression evaluation. |
| `MaxExpressionDepth` | int | `20` | Range: 1–100. Maximum nested expression depth. |
| `MaxWorkflowVariables` | int | `1,000` | Range: 1–10,000. Maximum variables in a workflow. |
| `MaxWorkflowDepth` | int | `50` | Range: 1–200. Maximum workflow nesting depth. |
| `ExecutionMode` | string | `"Sequential"` | Must be `Sequential` or `Parallel` (case-insensitive). |
| `EnableParallelExecution` | bool | `true` | Enables parallel activity execution. |
| `MaxParallelActivities` | int | `10` | Range: 1–100. Maximum parallel activities. |
| `EnableConditionalBranching` | bool | `true` | Enables conditional branches. |
| `EnableErrorRecovery` | bool | `true` | Enables error recovery. |
| `EnableCircuitBreaker` | bool | `true` | Enables circuit-breaker behavior. |
| `CircuitBreaker` | object | See below | Circuit-breaker thresholds and durations. |

`CircuitBreaker` binds to `CircuitBreakerConfig`:

| Key | Type | Default | Validation / purpose |
| --- | --- | --- | --- |
| `FailureThreshold` | int | `5` | Range: 1–100. Failures required to open the circuit. |
| `SamplingDurationSeconds` | int | `60` | Range: 10–600. Failure sampling period. |
| `MinimumThroughput` | int | `10` | Range: 1–100. Minimum sampled operations before the circuit can open. |
| `BreakDurationSeconds` | int | `30` | Range: 1–600. Time the circuit remains open. |

### Audit and monitoring

| Key | Type | Default | Validation / purpose |
| --- | --- | --- | --- |
| `EnableImmutableAuditTrail` | bool | `true` | Enables immutable audit-trail handling. |
| `AuditTrailRetentionDays` | int | `365` | Range: 30–3,650. Audit retention period. |
| `EnableHealthChecks` | bool | `true` | Enables health checks. |
| `HealthCheckIntervalSeconds` | int | `30` | Range: 10–3,600. Health-check interval. |
| `EnablePrometheusMetrics` | bool | `false` | Enables Prometheus metric exposure. |
| `MetricsPort` | int | `9090` | Range: 1,024–65,535. Metrics endpoint port. |

### Retry policy

`DefaultRetryPolicy` is a `Models.RetryPolicyConfig`. Its bindable properties are:

| Key | Type | Default | Purpose |
| --- | --- | --- | --- |
| `PolicyType` | enum | `NoRetry` | `NoRetry`, `FixedDelay`, `ExponentialBackoff`, or `LinearBackoff`. |
| `MaxAttempts` | int | `1` | Maximum total attempts. |
| `InitialDelayMs` | int | `1,000` | Initial retry delay in milliseconds. |
| `MaxDelayMs` | int | `300,000` | Maximum retry delay in milliseconds. |
| `BackoffMultiplier` | double | `2.0` | Multiplier used by backoff calculations. |
| `JitterFactor` | double | `0.1` | Symmetric jitter factor applied to calculated delays. |
| `RetryableExceptionTypes` | string array | empty | Optional allowlist of exception type names. |
| `RetryOnTimeout` | bool | `true` | Indicates whether timeouts are retryable. |

For example:

```json
"DefaultRetryPolicy": {
  "PolicyType": "ExponentialBackoff",
  "MaxAttempts": 3,
  "InitialDelayMs": 1000,
  "MaxDelayMs": 300000,
  "BackoffMultiplier": 2.0,
  "JitterFactor": 0.1,
  "RetryableExceptionTypes": ["System.TimeoutException"],
  "RetryOnTimeout": true
}
```

> Note: `appsettings.example.json` currently uses the legacy-looking keys
> `MaxRetries`, `InitialDelayMilliseconds`, `MaxDelayMilliseconds`, and
> `BackoffFactor`. Those names do not match `RetryPolicyConfig`, so the standard
> .NET configuration binder ignores them. Use the property names in the table above.

## Other sections in `appsettings.example.json`

The remaining sections are application-host examples. They are not properties of
`DotnetWorkflowEngineOptions`, and this repository does not define typed option
classes or bind them in `AddWorkflowEngine`. A host application must bind and consume
them itself (or map them to the relevant library configuration).

### Host and logging

- `Logging:LogLevel` sets category log levels in the standard .NET logging section.
  The example supplies `Default`, `Microsoft.AspNetCore`,
  `Microsoft.EntityFrameworkCore`, `Microsoft.Extensions.Caching`, and
  `Microsoft.Hosting.Lifetime` categories.
- `AllowedHosts` is the ASP.NET Core host-filtering value; the example uses `*`.

### `Database`

| Key | Example value | Intended meaning |
| --- | --- | --- |
| `ConnectionString` | SQL Server connection string | Database connection. This is separate from `WorkflowEngine:ConnectionString`. |
| `Provider` | `SqlServer` | Database provider name. |
| `EnableSensitiveDataLogging` | `false` | Include sensitive values in data-access logs. |
| `EnableDetailedErrors` | `false` | Enable detailed data-access errors. |
| `CommandTimeout` | `30` | Command timeout in seconds. |
| `MaxRetryCount` | `3` | Maximum transient-failure retry count. |
| `MaxRetryDelaySeconds` | `30` | Maximum retry delay in seconds. |
| `Pooling` | `true` | Enable connection pooling. |
| `MinimumPoolSize` | `0` | Minimum connections in the pool. |
| `MaximumPoolSize` | `100` | Maximum connections in the pool. |

### `Redis`

| Key | Example value | Intended meaning |
| --- | --- | --- |
| `ConnectionString` | `localhost:6379,...` | Redis connection string. This is separate from `WorkflowEngine:RedisConnectionString`. |
| `InstanceName` | `WorkflowEngine` | Cache instance/key prefix. |
| `EnableTls` | `false` | Enable TLS. |
| `DefaultDatabase` | `0` | Default Redis database number. |
| `ConnectTimeout` | `5000` | Connection timeout in milliseconds. |
| `SyncTimeout` | `5000` | Synchronous operation timeout in milliseconds. |
| `AbortOnConnectFail` | `false` | Abort initial connection when no endpoint is available. |

### `Hangfire`

| Key | Example value | Intended meaning |
| --- | --- | --- |
| `ConnectionString` | SQL Server connection string | Hangfire storage connection. |
| `DashboardPath` | `/hangfire` | Dashboard route. |
| `WorkerCount` | `4` | Background workers. |
| `UseDashboard` | `true` | Enable the dashboard. |
| `EnableGlobalLocks` | `true` | Enable storage global locks. |
| `HeartbeatInterval` | `10` | Heartbeat interval in seconds. |
| `ServerTimeout` | `300` | Server timeout in seconds. |
| `ShutdownTimeout` | `60` | Graceful shutdown timeout in seconds. |

### `Cors`

| Key | Example value | Intended meaning |
| --- | --- | --- |
| `AllowedOrigins` | `["*"]` | Allowed origins. |
| `AllowedMethods` | common HTTP methods | Allowed methods. |
| `AllowedHeaders` | `["*"]` | Allowed request headers. |
| `ExposedHeaders` | selected response headers | Headers exposed to browser clients. |
| `AllowCredentials` | `true` | Allow credentialed cross-origin requests. Do not combine wildcard origins with credentials in a production CORS policy. |
| `MaxAge` | `86400` | Preflight cache duration in seconds. |

### `Security`

| Key | Example value | Intended meaning |
| --- | --- | --- |
| `JwtSecret` | placeholder | JWT signing secret. Store outside source control. |
| `JwtIssuer` | `workflow-engine` | Expected token issuer. |
| `JwtAudience` | `workflow-engine-users` | Expected token audience. |
| `TokenExpirationMinutes` | `60` | Token lifetime in minutes. |
| `RequireHttps` | `true` | Require HTTPS. |
| `EnableHsts` | `true` | Enable HTTP Strict Transport Security. |
| `HstsMaxAge` | `31536000` | HSTS maximum age in seconds. |
| `EnableXssProtection` | `true` | Enable the host's configured XSS protection. |
| `EnableContentSecurityPolicy` | `true` | Enable Content Security Policy headers. |
| `CspReportUri` | `/csp-report` | CSP violation report endpoint. |

### `Monitoring`

| Key | Example value | Intended meaning |
| --- | --- | --- |
| `EnableLogging` | `true` | Enable monitoring logs. |
| `LogLevel` | `Information` | Monitoring log threshold. |
| `EnableMetrics` | `true` | Enable monitoring metrics. |
| `MetricsProvider` | `Prometheus` | Metrics backend/provider name. |
| `PrometheusPort` | `9090` | Prometheus endpoint port. |
| `EnableHealthChecks` | `true` | Enable monitoring health checks. |
| `HealthCheckEndpoints` | health, readiness, liveness routes | Health endpoint paths. |
| `EnableRequestTracing` | `true` | Enable request tracing. |
| `TraceSamplingRate` | `0.1` | Requested trace sample ratio (10% in the example). |

## Validation

Data-annotation attributes on `DotnetWorkflowEngineOptions`, `RateLimitConfig`, and
`CircuitBreakerConfig` express the required values and numeric ranges. The repository
also supplies `DotnetWorkflowEngineOptionsValidator`, which enforces the constraints
listed above plus the cache-provider, execution-mode, positive cache-expiration, and
distributed-cache connection rules.

`AddWorkflowEngine` registers that FluentValidation validator as
`IValidator<DotnetWorkflowEngineOptions>`, but it does not enable automatic
`ValidateOnStart`. Applications that require startup failure for invalid settings
should explicitly resolve and run the validator during startup.
