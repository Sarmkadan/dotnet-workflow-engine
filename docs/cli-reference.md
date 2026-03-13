// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# CLI Reference

Command-line interface reference for dotnet-workflow-engine.

## Overview

The CLI provides tools for workflow management, execution, and monitoring:

```bash
dotnet run -- [command] [options]
```

## Workflow Commands

### list
List all workflows.

```bash
dotnet run -- workflow list
```

**Options:**
- `--status Active|Inactive|Archived`: Filter by status
- `--limit 10`: Maximum results (default: 20)
- `--sort name|date`: Sort by name or date

**Example:**
```bash
dotnet run -- workflow list --status Active --limit 50
```

### get
Get detailed workflow information.

```bash
dotnet run -- workflow get <workflow-id>
```

**Example:**
```bash
dotnet run -- workflow get 550e8400-e29b-41d4-a716-446655440000
```

**Output:**
```
Workflow: OrderProcessing (v1)
Status: Active
Created: 2026-01-15 10:30:00
Published: 2026-01-15 10:35:00
Activities: 5
Instances: 42
```

### create
Create a workflow from definition file.

```bash
dotnet run -- workflow create <definition-file>
```

**Definition file format (JSON):**
```json
{
  "name": "OrderProcessing",
  "description": "Process customer orders",
  "activities": [
    {
      "id": "validate",
      "name": "Validate Order",
      "type": "ValidationActivity"
    }
  ],
  "transitions": [
    {
      "id": "t1",
      "sourceActivityId": "validate",
      "targetActivityId": "payment"
    }
  ]
}
```

**Example:**
```bash
dotnet run -- workflow create ./definitions/order-workflow.json
```

### validate
Validate workflow definition file.

```bash
dotnet run -- workflow validate <definition-file>
```

**Example:**
```bash
dotnet run -- workflow validate ./definitions/order-workflow.json
```

**Output:**
```
✓ Workflow definition is valid
✓ All activities are defined
✓ All transitions are valid
✓ No circular dependencies
```

### export
Export workflow definition.

```bash
dotnet run -- workflow export <workflow-id> --output <file>
```

**Example:**
```bash
dotnet run -- workflow export 550e8400-e29b-41d4-a716-446655440000 \
  --output order-workflow.json
```

### publish
Publish workflow version.

```bash
dotnet run -- workflow publish <workflow-id>
```

### delete
Delete workflow.

```bash
dotnet run -- workflow delete <workflow-id>
```

## Instance Commands

### execute
Execute a workflow.

```bash
dotnet run -- instance execute <workflow-id> [options]
```

**Options:**
- `--variables <json>`: Variable data (JSON format)
- `--mode Sequential|Parallel|AsyncFlow`: Execution mode

**Example:**
```bash
dotnet run -- instance execute 550e8400-e29b-41d4-a716-446655440000 \
  --variables '{"orderId":"ORD-123","amount":99.99}' \
  --mode Sequential
```

**Output:**
```
Instance created: 650e8400-e29b-41d4-a716-446655440001
Status: Running
Started: 2026-01-15 10:35:00
```

### list
List workflow instances.

```bash
dotnet run -- instance list [options]
```

**Options:**
- `--workflow <id>`: Filter by workflow ID
- `--status Running|Completed|Failed`: Filter by status
- `--limit 10`: Maximum results (default: 20)

**Example:**
```bash
dotnet run -- instance list --workflow 550e8400-e29b-41d4-a716-446655440000 \
  --status Running
```

### get
Get instance details.

```bash
dotnet run -- instance get <instance-id>
```

**Example:**
```bash
dotnet run -- instance get 650e8400-e29b-41d4-a716-446655440001
```

**Output:**
```
Instance: 650e8400-e29b-41d4-a716-446655440001
Workflow: OrderProcessing
Status: Running
Current Activity: process_payment
Started: 2026-01-15 10:35:00
Duration: 00:05:30
```

### status
Get instance execution status.

```bash
dotnet run -- instance status <instance-id>
```

### cancel
Cancel running instance.

```bash
dotnet run -- instance cancel <instance-id>
```

### retry
Retry failed activity.

```bash
dotnet run -- instance retry <instance-id> --activity <activity-id>
```

**Example:**
```bash
dotnet run -- instance retry 650e8400-e29b-41d4-a716-446655440001 \
  --activity process_payment
```

## Audit Commands

### list
List audit logs.

```bash
dotnet run -- audit list [options]
```

**Options:**
- `--workflow <id>`: Filter by workflow
- `--instance <id>`: Filter by instance
- `--activity <id>`: Filter by activity
- `--action <action>`: Filter by action
- `--start-date <date>`: Start date (ISO 8601)
- `--end-date <date>`: End date (ISO 8601)
- `--limit 50`: Maximum results (default: 50)

**Example:**
```bash
dotnet run -- audit list \
  --workflow 550e8400-e29b-41d4-a716-446655440000 \
  --start-date 2026-01-15 \
  --limit 100
```

### export
Export audit trail.

```bash
dotnet run -- audit export [options]
```

**Options:**
- `--output <file>`: Output file path (required)
- `--format csv|json|xml`: Format (default: csv)
- `--workflow <id>`: Filter by workflow
- `--start-date <date>`: Start date
- `--end-date <date>`: End date

**Example:**
```bash
dotnet run -- audit export \
  --output audit-report.csv \
  --format csv \
  --workflow 550e8400-e29b-41d4-a716-446655440000
```

### view
View specific audit entry.

```bash
dotnet run -- audit view <audit-id>
```

## Database Commands

### migrate
Run database migrations.

```bash
dotnet run -- db migrate
```

### rollback
Rollback migrations.

```bash
dotnet run -- db rollback [options]
```

**Options:**
- `--steps <n>`: Number of steps to rollback (default: 1)

**Example:**
```bash
dotnet run -- db rollback --steps 2
```

### seed
Seed database with sample data.

```bash
dotnet run -- db seed
```

### health-check
Check database connectivity.

```bash
dotnet run -- db health-check
```

## Health & Monitoring Commands

### health
Check service health.

```bash
dotnet run -- health check
```

**Output:**
```
Service Status: Healthy

Components:
  Database: Healthy
  Cache: Healthy
  Hangfire: Healthy
```

### metrics
Show metrics and statistics.

```bash
dotnet run -- metrics show [options]
```

**Options:**
- `--period today|week|month`: Time period (default: today)

**Example:**
```bash
dotnet run -- metrics show --period week
```

**Output:**
```
Metrics for last 7 days:

Workflows:
  Total: 12
  Active: 10

Instances:
  Total Executed: 342
  Running: 8
  Completed: 331
  Failed: 3

Performance:
  Average Execution Time: 00:05:30
  Min Execution Time: 00:01:15
  Max Execution Time: 00:45:20
```

### logs
View application logs.

```bash
dotnet run -- logs [options]
```

**Options:**
- `--level Trace|Debug|Information|Warning|Error`: Log level
- `--lines 100`: Number of lines to show
- `--follow`: Follow logs in real-time

**Example:**
```bash
dotnet run -- logs --level Error --lines 50
dotnet run -- logs --follow
```

## Configuration Commands

### config
Manage configuration.

```bash
dotnet run -- config show
```

**Output:**
```
DefaultExecutionMode: Sequential
MaxConcurrentActivities: 10
DefaultTimeout: 00:05:00
EnableAuditTrail: true
EnableMetrics: true
```

## Help and Information

### help
Display help for a command.

```bash
dotnet run -- help
dotnet run -- help workflow
dotnet run -- help instance execute
```

### version
Show CLI and library versions.

```bash
dotnet run -- version
```

## Exit Codes

- `0`: Success
- `1`: General error
- `2`: Invalid arguments
- `3`: Resource not found
- `4`: Connection error
- `5`: Authorization error

## Examples

### Complete Workflow Execution

```bash
# 1. Create workflow from definition
dotnet run -- workflow create ./order-workflow.json

# 2. List workflows to find the ID
dotnet run -- workflow list

# 3. Publish workflow
dotnet run -- workflow publish <workflow-id>

# 4. Execute workflow
dotnet run -- instance execute <workflow-id> \
  --variables '{"orderId":"ORD-001","amount":99.99}'

# 5. Check instance status
dotnet run -- instance get <instance-id>

# 6. View audit trail
dotnet run -- audit list --instance <instance-id>

# 7. Export audit trail
dotnet run -- audit export --output audit.csv --instance <instance-id>
```

### Database Setup

```bash
# 1. Check database connectivity
dotnet run -- db health-check

# 2. Run migrations
dotnet run -- db migrate

# 3. Seed sample data
dotnet run -- db seed

# 4. Check service health
dotnet run -- health check
```

### Monitoring

```bash
# View metrics for the week
dotnet run -- metrics show --period week

# View error logs
dotnet run -- logs --level Error

# Follow logs in real-time
dotnet run -- logs --follow

# Export audit trail for compliance
dotnet run -- audit export --output compliance-audit.csv \
  --start-date 2026-01-01 --end-date 2026-01-31
```
