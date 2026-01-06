// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Configuration Reference

Complete reference for all configuration options in dotnet-workflow-engine.

## appsettings.json Structure

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "WorkflowEngine": {
    "DefaultExecutionMode": "Sequential",
    "MaxConcurrentActivities": 10,
    "DefaultTimeout": "00:05:00",
    "EnableAuditTrail": true,
    "EnableMetrics": true,
    "CachingEnabled": true,
    "CacheProvider": "Redis"
  },
  "Database": {
    "Provider": "SqlServer",
    "CommandTimeout": 300,
    "MaxPoolSize": 20
  },
  "Caching": {
    "Provider": "Redis",
    "RedisConnection": "localhost:6379",
    "DefaultExpiration": "01:00:00"
  },
  "Hangfire": {
    "Enabled": true,
    "ConnectionString": "...",
    "DashboardEnabled": true,
    "DashboardPort": 5555
  },
  "Security": {
    "EnableAuthorization": true,
    "JwtSecret": "...",
    "JwtExpiration": "24:00:00"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "WorkflowEngine": "Debug"
    }
  }
}
```

## Workflow Engine Configuration

### DefaultExecutionMode
**Type:** `enum` (Sequential, Parallel, AsyncFlow)  
**Default:** `Sequential`  
**Description:** Default execution mode for all workflows

```json
"DefaultExecutionMode": "Sequential"
```

- **Sequential**: Execute activities one at a time
- **Parallel**: Execute independent activities concurrently
- **AsyncFlow**: Fire-and-forget execution without waiting

### MaxConcurrentActivities
**Type:** `int`  
**Default:** `10`  
**Description:** Maximum number of concurrent activities in parallel mode

```json
"MaxConcurrentActivities": 10
```

Use lower values to reduce resource consumption, higher values for throughput.

### DefaultTimeout
**Type:** `TimeSpan`  
**Default:** `00:05:00` (5 minutes)  
**Description:** Default timeout for all activities

```json
"DefaultTimeout": "00:05:00"
```

Can be overridden per activity.

### EnableAuditTrail
**Type:** `bool`  
**Default:** `true`  
**Description:** Enable comprehensive audit logging

```json
"EnableAuditTrail": true
```

Disable to improve performance if not required.

### EnableMetrics
**Type:** `bool`  
**Default:** `true`  
**Description:** Enable Prometheus metrics collection

```json
"EnableMetrics": true
```

### CachingEnabled
**Type:** `bool`  
**Default:** `true`  
**Description:** Enable workflow caching

```json
"CachingEnabled": true
```

### CacheProvider
**Type:** `string` (Redis, InMemory)  
**Default:** `Redis`  
**Description:** Cache provider implementation

```json
"CacheProvider": "Redis"
```

## Database Configuration

### Provider
**Type:** `string` (SqlServer, PostgreSQL, SQLite, InMemory)  
**Default:** `SqlServer`  
**Description:** Database provider

```json
"Provider": "SqlServer"
```

### ConnectionString
**Type:** `string`  
**Description:** Database connection string

SQL Server:
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=WorkflowDb;Integrated Security=true;"
```

PostgreSQL:
```json
"DefaultConnection": "Host=localhost;Database=workflowdb;Username=postgres;Password=password"
```

SQLite:
```json
"DefaultConnection": "Data Source=workflow.db"
```

### CommandTimeout
**Type:** `int`  
**Default:** `300`  
**Description:** Database command timeout in seconds

```json
"CommandTimeout": 300
```

### MaxPoolSize
**Type:** `int`  
**Default:** `20`  
**Description:** Maximum connection pool size

```json
"MaxPoolSize": 20
```

## Caching Configuration

### Provider
**Type:** `string` (Redis, InMemory)  
**Default:** `Redis`  
**Description:** Cache provider

```json
"Provider": "Redis"
```

### RedisConnection
**Type:** `string`  
**Description:** Redis connection string

```json
"RedisConnection": "localhost:6379"
```

Multi-node setup:
```json
"RedisConnection": "node1:6379,node2:6379,node3:6379"
```

### DefaultExpiration
**Type:** `TimeSpan`  
**Default:** `01:00:00`  
**Description:** Default cache expiration time

```json
"DefaultExpiration": "01:00:00"
```

## Hangfire Configuration

### Enabled
**Type:** `bool`  
**Default:** `true`  
**Description:** Enable Hangfire background jobs

```json
"Enabled": true
```

### ConnectionString
**Type:** `string`  
**Description:** Connection string for Hangfire storage

```json
"ConnectionString": "Server=localhost;Database=Hangfire;Integrated Security=true;"
```

### DashboardEnabled
**Type:** `bool`  
**Default:** `true`  
**Description:** Enable Hangfire dashboard

```json
"DashboardEnabled": true
```

Visit `http://localhost:5555/hangfire` to access the dashboard.

### DashboardPort
**Type:** `int`  
**Default:** `5555`  
**Description:** Port for Hangfire dashboard

```json
"DashboardPort": 5555
```

## Security Configuration

### EnableAuthorization
**Type:** `bool`  
**Default:** `true`  
**Description:** Enable authorization checks

```json
"EnableAuthorization": true
```

### JwtSecret
**Type:** `string`  
**Description:** Secret for JWT token signing

```json
"JwtSecret": "your-very-long-secret-key-at-least-32-characters"
```

### JwtExpiration
**Type:** `TimeSpan`  
**Default:** `24:00:00`  
**Description:** JWT token expiration

```json
"JwtExpiration": "24:00:00"
```

## Logging Configuration

### LogLevel
**Type:** `Dictionary<string, string>`  
**Description:** Log levels for different categories

```json
"LogLevel": {
  "Default": "Information",
  "WorkflowEngine": "Debug",
  "Microsoft": "Warning"
}
```

Log levels: Trace, Debug, Information, Warning, Error, Critical, None

## Environment-Specific Configuration

### appsettings.Development.json

```json
{
  "WorkflowEngine": {
    "EnableAuditTrail": true,
    "EnableMetrics": false
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "WorkflowEngine": "Debug"
    }
  }
}
```

### appsettings.Production.json

```json
{
  "WorkflowEngine": {
    "DefaultTimeout": "00:10:00",
    "MaxConcurrentActivities": 20,
    "EnableMetrics": true
  },
  "Caching": {
    "DefaultExpiration": "06:00:00"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "WorkflowEngine": "Warning"
    }
  }
}
```

## Programmatic Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Override configuration programmatically
builder.Services.AddWorkflowEngine(options =>
{
    options.DefaultExecutionMode = ExecutionMode.Parallel;
    options.MaxConcurrentActivities = 20;
    options.DefaultTimeout = TimeSpan.FromMinutes(10);
    options.EnableAuditTrail = true;
    options.EnableMetrics = true;
});
```

## Performance Tuning

### For High Throughput

```json
{
  "WorkflowEngine": {
    "DefaultExecutionMode": "Parallel",
    "MaxConcurrentActivities": 50,
    "EnableAuditTrail": false
  },
  "Caching": {
    "Provider": "Redis",
    "DefaultExpiration": "06:00:00"
  },
  "Database": {
    "MaxPoolSize": 50,
    "CommandTimeout": 600
  }
}
```

### For Development

```json
{
  "WorkflowEngine": {
    "DefaultExecutionMode": "Sequential",
    "EnableAuditTrail": true,
    "EnableMetrics": false
  },
  "Caching": {
    "Provider": "InMemory"
  },
  "Database": {
    "Provider": "SQLite",
    "CommandTimeout": 30
  }
}
```

### For Compliance

```json
{
  "WorkflowEngine": {
    "EnableAuditTrail": true,
    "DefaultTimeout": "01:00:00"
  },
  "Security": {
    "EnableAuthorization": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

## Configuration Validation

The engine validates configuration on startup:

```csharp
// Validation occurs automatically
// Invalid configuration throws ConfigurationException
try
{
    var app = builder.Build();
    app.Run();
}
catch (ConfigurationException ex)
{
    Console.WriteLine($"Configuration error: {ex.Message}");
}
```

## Secrets Management

Use User Secrets for sensitive data in development:

```bash
dotnet user-secrets init
dotnet user-secrets set "WorkflowEngine:JwtSecret" "your-secret"
dotnet user-secrets set "Database:ConnectionString" "your-connection-string"
```

For production, use:
- Azure Key Vault
- AWS Secrets Manager
- Environment variables
- HashiCorp Vault

Set via environment variables:

```bash
export WORKFLOWENGINE__JWTSECRET="your-secret"
export CONNECTIONSTRINGS__DEFAULTCONNECTION="your-connection-string"
```

The configuration system will bind these automatically.
