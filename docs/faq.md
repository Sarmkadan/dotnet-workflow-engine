// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Frequently Asked Questions

## General Questions

### Q: What is dotnet-workflow-engine?

A: It's a modern workflow orchestration framework for .NET 10 that allows you to define and execute complex business processes programmatically. It supports sequential, parallel, and conditional execution with comprehensive audit trails and retry policies.

### Q: What are the main use cases?

A: 
- Order processing workflows
- Approval chains with multiple reviewers
- ETL and data pipeline orchestration
- Microservice coordination
- Business process automation
- Compliance automation

### Q: Is it suitable for production use?

A: Yes, dotnet-workflow-engine is designed for production with enterprise-grade features like comprehensive audit trails, error handling, monitoring, and security.

### Q: What .NET versions does it support?

A: The project is built on .NET 10. It may work on .NET 9 or later with minimal changes, but .NET 10 is recommended.

### Q: How does it compare to other workflow engines?

A: 
- **vs Hangfire**: More flexible for complex workflows, built-in BPMN patterns
- **vs Apache Airflow**: .NET-native, better integration with C# ecosystem
- **vs Temporal/Durable Functions**: Simpler for most use cases, easier to self-host

## Installation & Setup

### Q: What are the prerequisites?

A: You need:
- .NET 10 SDK
- A database (SQL Server, PostgreSQL, SQLite)
- Optional: Redis for caching
- Optional: Hangfire for background jobs

### Q: How do I install the package?

A: Via NuGet:
```bash
dotnet add package DotNetWorkflowEngine
```

Or clone from source:
```bash
git clone https://github.com/Sarmkadan/dotnet-workflow-engine.git
```

### Q: Can I use it without a database?

A: No, you need a database to persist workflows and instances. However, you can use SQLite for lightweight deployments without an additional database server.

### Q: Can I run it in Docker?

A: Yes, Dockerfile and docker-compose.yml are provided. See [Deployment Guide](deployment.md).

## Usage Questions

### Q: How do I create my first workflow?

A: See [Getting Started Guide](getting-started.md). Briefly:
1. Define a Workflow with Activities and Transitions
2. Register services in Program.cs
3. Create a controller to execute workflows
4. Call the API or CLI

### Q: Can I have conditional logic in workflows?

A: Yes, use Transitions with conditions:
```csharp
new Transition 
{ 
    SourceActivityId = "decision",
    TargetActivityId = "approved_path",
    Condition = "${approvalStatus == 'approved'}"
}
```

### Q: How do I handle long-running activities?

A: Configure timeout and use async/await:
```csharp
activity.Timeout = TimeSpan.FromMinutes(30);

public async Task ExecuteAsync(Activity activity, ExecutionContext context)
{
    await LongRunningOperationAsync();
}
```

### Q: Can I execute activities in parallel?

A: Yes, set ExecutionMode to Parallel:
```csharp
context.ExecutionMode = ExecutionMode.Parallel;
```

### Q: How do I pass data between activities?

A: Use the ExecutionContext variables:
```csharp
// Set in one activity
context.Variables["orderId"] = "ORD-123";

// Access in another activity
var orderId = context.Variables["orderId"];
```

### Q: Can I call external systems during workflow execution?

A: Yes, use WebhookHandler or custom activity:
```csharp
var handler = new WebhookHandler();
await handler.InvokeAsync("https://api.example.com/webhook", data);
```

## Configuration Questions

### Q: Where do I configure the engine?

A: In appsettings.json under "WorkflowEngine" section. See [Configuration Guide](configuration.md).

### Q: How do I enable caching?

A: Configure caching provider:
```json
"Caching": {
  "Provider": "Redis",
  "RedisConnection": "localhost:6379"
}
```

### Q: How do I set up retry policies?

A: Per-activity configuration:
```csharp
activity.RetryPolicy = RetryPolicy.Exponential;
activity.MaxRetries = 3;
```

### Q: How do I enable audit trail?

A: It's enabled by default:
```json
"WorkflowEngine": {
  "EnableAuditTrail": true
}
```

### Q: Can I configure different settings per environment?

A: Yes, use appsettings.{Environment}.json:
```
appsettings.json
appsettings.Development.json
appsettings.Production.json
```

## Performance Questions

### Q: How many workflows can it handle?

A: Depends on your infrastructure, but thousands per minute are possible with proper tuning. Start with monitoring to identify bottlenecks.

### Q: How do I improve performance?

A: 
1. Enable caching (Redis)
2. Add database indexes
3. Use parallel execution for independent activities
4. Archive old audit logs
5. Monitor with metrics and adjust MaxConcurrentActivities

### Q: What's the overhead per workflow execution?

A: Typically 50-100ms for orchestration overhead, plus activity execution time.

### Q: Can I run multiple instances?

A: Yes, the system is stateless. For multi-instance deployments, use centralized caching (Redis) and job queue (Hangfire).

## Security Questions

### Q: Is authorization mandatory?

A: It's enabled by default. You can disable it for development:
```json
"Security": {
  "EnableAuthorization": false
}
```

### Q: How do I authenticate users?

A: Use JWT tokens:
```bash
curl -H "Authorization: Bearer <token>" http://localhost:5000/api/workflows
```

### Q: How is sensitive data handled?

A: Sensitive variables should be encrypted. Implement encryption for specific fields as needed.

### Q: Are audit logs immutable?

A: Audit logs should not be modified. Implement database-level constraints to enforce immutability.

## API Questions

### Q: What API formats are supported?

A: REST API (HTTP/JSON). GraphQL can be added as custom extension.

### Q: Is there an API for monitoring?

A: Yes, health check and metrics endpoints:
```bash
GET /health
GET /metrics
```

### Q: Can I paginate large result sets?

A: Yes, all list endpoints support pageSize and pageNumber:
```bash
GET /workflows?pageSize=50&pageNumber=1
```

### Q: Is rate limiting implemented?

A: Yes, default limits are 100 requests/minute per user.

## CLI Questions

### Q: How do I use the CLI?

A: Command format: `dotnet run -- [command] [options]`

Examples:
```bash
dotnet run -- workflow list
dotnet run -- instance execute <id>
dotnet run -- audit list
```

See [CLI Reference](cli-reference.md) for complete reference.

### Q: Can I use CLI in production?

A: Yes, but use API for integration. CLI is useful for management and debugging.

## Monitoring Questions

### Q: What metrics are available?

A: See available metrics with:
```bash
dotnet run -- metrics show
```

Includes: total workflows, running/completed/failed instances, average execution time.

### Q: How do I integrate with Prometheus?

A: Metrics endpoint exports Prometheus format:
```bash
curl http://localhost:5000/metrics
```

### Q: How do I set up alerting?

A: Use your monitoring platform (Datadog, New Relic, etc.) to query metrics and set alert thresholds.

## Troubleshooting Questions

### Q: My workflow won't execute. What do I do?

A: Check logs and validate definition:
```bash
dotnet run -- logs --level Debug
dotnet run -- workflow validate ./definition.json
```

See [Troubleshooting Guide](troubleshooting.md) for detailed solutions.

### Q: Activities are timing out. How do I fix it?

A: Increase timeout or check for long-running operations:
```json
"DefaultTimeout": "00:10:00"
```

### Q: Database connection is slow. What can I do?

A: Check MaxPoolSize, add indexes, verify network connectivity. See performance section.

### Q: How do I debug a failing activity?

A: Enable debug logging:
```json
"Logging": {
  "LogLevel": {
    "WorkflowEngine": "Debug"
  }
}
```

## Contributing Questions

### Q: Can I contribute to the project?

A: Yes! See [Contributing Guide](contributing.md). Submit pull requests with tests and documentation.

### Q: What's the code style?

A: Follow C# conventions, write async code, add XML comments, keep methods under 50 lines.

### Q: How do I report bugs?

A: Open an issue on GitHub with:
- Description of the problem
- Steps to reproduce
- Expected vs actual behavior
- Environment details

### Q: How do I request features?

A: Open a GitHub discussion with your use case and proposed solution.

## License & Support

### Q: What license is this project under?

A: MIT License. See LICENSE file.

### Q: Do you provide commercial support?

A: Contact vladyslav@sarmkadan.com for support options.

### Q: Where can I get help?

A:
- [GitHub Issues](https://github.com/Sarmkadan/dotnet-workflow-engine/issues)
- [GitHub Discussions](https://github.com/Sarmkadan/dotnet-workflow-engine/discussions)
- [Documentation](../README.md)
- Email: vladyslav@sarmkadan.com

## More Questions?

If your question isn't answered here:
1. Check the documentation in `docs/`
2. Search existing GitHub issues
3. Open a new GitHub discussion
4. Contact the maintainer
