// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Troubleshooting Guide

Common issues and their solutions.

## Database Issues

### Connection Failed

**Error:** "Unable to connect to database" or "Connection timeout"

**Causes:**
- Database server not running
- Invalid connection string
- Network connectivity issues
- Database doesn't exist

**Solutions:**

1. Verify database server is running:
```bash
# SQL Server
sqlcmd -S localhost -U sa -P <password>

# PostgreSQL
psql -h localhost -U postgres
```

2. Check connection string in appsettings.json:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=WorkflowDb;"
}
```

3. Create database if it doesn't exist:
```bash
dotnet ef database create
```

4. Run migrations:
```bash
dotnet ef database update
```

### Migrations Failed

**Error:** "Unable to apply migration"

**Cause:** Schema mismatch or migration conflict

**Solutions:**

1. Check migration status:
```bash
dotnet ef migrations list
```

2. Revert to previous migration:
```bash
dotnet ef database update <previous-migration>
```

3. Remove bad migration:
```bash
dotnet ef migrations remove
```

4. Create fresh database:
```bash
dotnet ef database drop --force
dotnet ef database create
dotnet ef database update
```

## Workflow Execution Issues

### Activity Timeout

**Error:** "Activity execution timeout"

**Cause:** Activity takes longer than configured timeout

**Solutions:**

1. Increase default timeout:
```json
"WorkflowEngine": {
  "DefaultTimeout": "00:10:00"
}
```

2. Set activity-specific timeout:
```csharp
activity.Timeout = TimeSpan.FromMinutes(10);
```

3. Check activity implementation for long-running operations
4. Consider breaking activity into smaller steps

### Workflow Not Executing

**Error:** Instance created but not running

**Causes:**
- Invalid workflow definition
- Missing activities
- Circular dependencies

**Solutions:**

1. Validate workflow definition:
```bash
dotnet run -- workflow validate ./workflow.json
```

2. Check activity IDs in transitions:
```json
"transitions": [
  {
    "sourceActivityId": "existing_activity",
    "targetActivityId": "existing_activity"
  }
]
```

3. View execution logs:
```bash
dotnet run -- logs --level Debug --follow
```

### Activities Not Running in Parallel

**Error:** Activities execute sequentially despite parallel mode

**Causes:**
- ExecutionMode set to Sequential
- Dependencies between activities
- MaxConcurrentActivities limit

**Solutions:**

1. Set execution mode to Parallel:
```json
"WorkflowEngine": {
  "DefaultExecutionMode": "Parallel"
}
```

2. Remove unnecessary dependencies
3. Increase MaxConcurrentActivities:
```json
"MaxConcurrentActivities": 20
```

### Activity Failures Not Retrying

**Error:** Activity fails and doesn't retry

**Causes:**
- Retry policy not configured
- Max retries exceeded
- Non-retryable error

**Solutions:**

1. Configure retry policy:
```csharp
activity.RetryPolicy = RetryPolicy.Exponential;
activity.MaxRetries = 3;
```

2. Check error logs for root cause:
```bash
dotnet run -- logs --level Error
```

3. Implement custom retry logic if needed

## Performance Issues

### Slow Workflow Execution

**Symptom:** Workflows take longer than expected

**Causes:**
- Database query performance
- Cache misses
- Network latency
- Long-running activities

**Solutions:**

1. Enable caching:
```json
"Caching": {
  "Provider": "Redis",
  "DefaultExpiration": "01:00:00"
}
```

2. Check database indexes:
```sql
CREATE INDEX idx_workflow_status ON WorkflowInstances(Status);
CREATE INDEX idx_audit_instance ON AuditLogEntries(InstanceId);
```

3. Monitor activity execution times:
```bash
dotnet run -- metrics show --period week
```

4. Use async/await in activities:
```csharp
public async Task ExecuteAsync(Activity activity, ExecutionContext context)
{
    var result = await GetDataAsync();
    return result;
}
```

### High Memory Usage

**Error:** "OutOfMemoryException" or application crashes

**Causes:**
- Large result sets
- Memory leaks
- Unbounded caches

**Solutions:**

1. Implement pagination for queries:
```csharp
var page = await _service.ListAsync(pageSize: 50, pageNumber: 1);
```

2. Clear caches periodically:
```csharp
await _cacheService.ClearAsync();
```

3. Profile memory usage:
```bash
dotnet counters monitor -p <pid>
```

4. Reduce audit trail retention:
```sql
DELETE FROM AuditLogEntries WHERE Timestamp < DATEADD(YEAR, -1, GETDATE());
```

### High Database Load

**Error:** Slow queries, connection pool exhaustion

**Causes:**
- Too many concurrent queries
- Inefficient queries
- Missing indexes

**Solutions:**

1. Increase connection pool size:
```json
"Database": {
  "MaxPoolSize": 50
}
```

2. Add database indexes:
```sql
CREATE INDEX idx_workflow_created ON Workflows(CreatedAt DESC);
```

3. Archive old data:
```sql
ARCHIVE TABLE AuditLogEntries WHERE Timestamp < DATEADD(MONTH, -6, GETDATE());
```

## Caching Issues

### Redis Connection Failed

**Error:** "Redis connection failed" or cache disabled

**Causes:**
- Redis server not running
- Invalid connection string
- Network issues

**Solutions:**

1. Verify Redis is running:
```bash
redis-cli ping
# Should return: PONG
```

2. Check connection string:
```json
"Caching": {
  "RedisConnection": "localhost:6379"
}
```

3. Verify network connectivity:
```bash
telnet localhost 6379
```

4. Check Redis configuration:
```bash
redis-cli CONFIG GET maxmemory
```

### Cache Inconsistency

**Error:** Stale data in cache

**Solutions:**

1. Manually clear cache:
```bash
redis-cli FLUSHALL
```

2. Reduce cache TTL:
```json
"Caching": {
  "DefaultExpiration": "00:15:00"
}
```

3. Implement cache invalidation on updates:
```csharp
await _workflowService.UpdateAsync(workflow);
await _cacheService.InvalidateAsync($"workflow-{workflow.Id}");
```

## Authorization Issues

### Unauthorized Errors (401)

**Error:** "Invalid or missing authorization token"

**Causes:**
- Missing Authorization header
- Invalid token
- Expired token

**Solutions:**

1. Include Authorization header:
```bash
curl -H "Authorization: Bearer <token>" http://localhost:5000/api/workflows
```

2. Verify JWT configuration:
```json
"Security": {
  "JwtSecret": "your-secret-key",
  "JwtExpiration": "24:00:00"
}
```

3. Check token expiration:
```bash
# Use jwt.io to decode and verify token
```

### Forbidden Errors (403)

**Error:** "User does not have permission"

**Cause:** User lacks required role/permission

**Solutions:**

1. Check user roles in database
2. Verify authorization attributes:
```csharp
[Authorize(Roles = "Admin")]
public async Task<ActionResult> DeleteWorkflow(Guid id)
```

3. Review audit logs for failed authorization attempts

## Configuration Issues

### Invalid Configuration

**Error:** "Configuration error" on startup

**Solutions:**

1. Validate appsettings.json syntax:
```bash
# Use JSON validator at jsonlint.com
```

2. Check required settings exist
3. Verify environment variables:
```bash
echo $WORKFLOWENGINE__DEFAULTEXECUTIONMODE
```

4. Review appsettings.json:
```json
{
  "ConnectionStrings": { },
  "WorkflowEngine": { }
}
```

### Logging Not Working

**Error:** No logs appearing

**Causes:**
- Log level too high
- Logger not configured
- Output not configured

**Solutions:**

1. Lower log level:
```json
"Logging": {
  "LogLevel": {
    "Default": "Debug"
  }
}
```

2. Add file logging:
```csharp
builder.Logging.AddFile("logs/workflow-{Date}.log");
```

3. Check log directory exists and is writable

## Testing Issues

### Unit Tests Failing

**Error:** Test failures after upgrade

**Causes:**
- API changes
- Breaking changes in models
- Test data outdated

**Solutions:**

1. Run with verbose output:
```bash
dotnet test --verbosity detailed
```

2. Update test data
3. Check for breaking changes in CHANGELOG.md

### Integration Tests Flaky

**Error:** Tests sometimes pass, sometimes fail

**Causes:**
- Timing issues
- Database state not isolated
- Race conditions

**Solutions:**

1. Add explicit waits:
```csharp
await Task.Delay(TimeSpan.FromSeconds(1));
```

2. Use database transactions:
```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
// test code
await transaction.RollbackAsync();
```

3. Increase timeout:
```csharp
[Timeout(5000)]
public async Task TestLongRunningWorkflow()
```

## Deployment Issues

### Application Won't Start

**Error:** Application fails to start after deployment

**Solutions:**

1. Check logs immediately:
```bash
tail -f logs/application.log
```

2. Verify all dependencies installed:
```bash
dotnet restore
```

3. Check environment configuration:
```bash
env | grep WORKFLOWENGINE
```

4. Run database migrations:
```bash
dotnet ef database update
```

## Support and Resources

- Check logs in `logs/` directory
- Review audit trail: `dotnet run -- audit list`
- Check metrics: `dotnet run -- metrics show`
- Run health check: `dotnet run -- health check`
- See [FAQ](faq.md) for common questions
- Visit [GitHub Issues](https://github.com/Sarmkadan/dotnet-workflow-engine/issues)
