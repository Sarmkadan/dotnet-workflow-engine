# Migration Guide: v1.x to v2.0

This document covers all breaking changes and required steps to upgrade from v1.x to v2.0 of dotnet-workflow-engine.


## Overview

v2.0 introduces several new features and improvements, including:

- **Visual Workflow Designer** - Drag-and-drop workflow designer with live preview
- **Enhanced BPMN Support** - Improved BPMN 2.0 compatibility
- **Performance Optimizations** - Faster execution and reduced memory usage
- **New Activity Types** - Additional built-in activities for common scenarios
- **Improved Error Handling** - Better error messages and recovery options
- **Enhanced Monitoring** - More detailed metrics and health checks

## Breaking Changes

### 1. Docker Port Change (80 → 8080)

The default container port has changed from `80` to `8080` to support running as a non-root user and comply with modern container security standards.


**Before (v1.x):**
```yaml
ports:
  - "5000:80"
```

**After (v2.0):**
```yaml
ports:
  - "8080:8080"
```

If you reference the container port in:
- Kubernetes manifests
- Docker Compose files
- Reverse proxies (nginx, Apache, Traefik)
- CI/CD pipelines
- Health check configurations
- Load balancer configurations

Update all occurrences of port `80` to `8080`.


#### Kubernetes Configuration

Update `containerPort`, liveness/readiness probes, and Service `targetPort`:

```yaml
containers:
  - name: workflow-engine
    ports:
      - containerPort: 8080
    livenessProbe:
      httpGet:
        path: /health
        port: 8080
    readinessProbe:
      httpGet:
        path: /health
        port: 8080
---
apiVersion: v1
kind: Service
spec:
  ports:
    - port: 80
      targetPort: 8080
```

#### Docker Compose

```yaml
services:
  api:
    ports:
      - "8080:8080"
```

#### Reverse Proxy (nginx)

```nginx
upstream workflow_backend {
    server app1:8080;
    server app2:8080;
}

server {
    listen 80;
    server_name workflow.example.com;
    
    location / {
        proxy_pass http://workflow_backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

#### Environment Variables

If you explicitly set `ASPNETCORE_URLS`, update it:

```bash
# Before
ASPNETCORE_URLS=http://+:80

# After
ASPNETCORE_URLS=http://+:8080
```

### 2. Non-root Container User

The Docker image now runs as a dedicated `appuser` (UID 1001) instead of root for improved security. If you mount volumes that require write access, ensure the directory is owned by UID 1001:

```bash
# Set correct permissions for mounted volumes
chown -R 1001:1001 /path/to/mounted/volume

# Or set ACLs
setfacl -R -m u:1001:rwx /path/to/mounted/volume
```

Common directories that may need permission changes:
- `/app/Data` - For SQLite databases
- `/app/Logs` - For log files
- `/app/Uploads` - For file uploads
- Any custom volume mounts

### 3. Removed `version` Key from docker-compose.yml

The top-level `version` key has been removed from `docker-compose.yml` per the Compose Specification. Docker Compose v2.x ignores this field; if you use Compose v1, upgrade to v2 before migrating.

**Remove this from your docker-compose files:**
```yaml
version: '3.8'  # ← Remove this line
```

### 4. Restart Policy Changes

All services now include `restart: unless-stopped` by default. This is non-breaking but changes the default behavior from no restart policy to automatic restarts on container failure.

**To disable automatic restarts:**
```yaml
services:
  api:
    restart: "no"
```

### 5. API Endpoint Changes

#### New Endpoints
- `GET /api/workflows/designer` - Visual workflow designer
- `POST /api/workflows/validate` - Validate workflow definitions
- `GET /api/workflows/{id}/preview` - Get workflow preview

#### Changed Endpoints
- `POST /api/workflows` - Now accepts both JSON and BPMN XML formats
- `PUT /api/workflows/{id}` - Enhanced with versioning support

#### Deprecated Endpoints
- `POST /api/workflows/import` - Use `POST /api/workflows` with appropriate content-type

### 6. Activity Type Changes

#### Renamed Activity Types
- `ValidationActivity` → `ValidatorActivity`
- `NotificationActivity` → `EmailActivity`

#### New Activity Types
- `BPMNGatewayActivity` - For BPMN gateway routing
- `TimerActivity` - For scheduled/timed activities
- `ConditionalActivity` - For conditional branching

**Migration:**
```csharp
// Before
new Activity {
    ActivityType = "ValidationActivity"
}

// After
new Activity {
    ActivityType = "ValidatorActivity"
}
```

### 7. Configuration Changes

#### New Configuration Options
```json
{
  "WorkflowEngine": {
    "EnableVisualDesigner": true,
    "BPMNCompatibilityMode": "Strict",
    "ActivityTimeout": "00:05:00",
    "MaxParallelBranches": 20
  }
}
```

#### Changed Configuration Options
- `DefaultExecutionMode` now accepts: `Sequential`, `Parallel`, `BPMN`
- `CachingEnabled` default changed from `false` to `true`

### 8. Retry Policy Changes

The `RetryPolicy` enum has been updated:

```csharp
// Before
enum RetryPolicy {
    None,
    Linear,
    Exponential
}

// After
enum RetryPolicy {
    None,
    Linear,
    Exponential,
    FixedDelay,
    Randomized
}
```

**Migration:**
```csharp
// Before
RetryPolicy = RetryPolicy.Exponential

// After (choose appropriate policy)
RetryPolicy = RetryPolicy.Randomized
```

### 9. Database Schema Changes

v2.0 introduces new tables for visual designer state:
- `WorkflowDesignerStates` - Stores visual designer layout and positions
- `ActivityConnections` - Stores visual connections between activities

**No migration required** - these tables are created automatically on first run.

### 10. CLI Changes

#### New Commands
```bash
# Visual designer commands
dotnet run -- designer start <workflow-id>
dotnet run -- designer export <workflow-id> --format svg
dotnet run -- designer import <file-path>

# Validation commands
dotnet run -- workflow validate --bpmn ./workflow.bpmn
dotnet run -- workflow lint ./workflow.json
```

#### Changed Commands
- `workflow create` now accepts both JSON and BPMN formats
- `workflow export` now supports multiple formats: json, bpmn, svg, png

**Migration:**
```bash
# Before
dotnet run -- workflow create ./workflow.json

# After (still works)
dotnet run -- workflow create ./workflow.json

# New BPMN support
dotnet run -- workflow create ./workflow.bpmn
```

## Migration Steps


Follow these steps to migrate from v1.x to v2.0:


### Step 1: Update Port Mappings
Update all deployment configurations to use port 8080:


- [ ] Kubernetes manifests (deployment.yaml, service.yaml)
- [ ] Docker Compose files (docker-compose.yml, docker-compose.*.yml)
- [ ] Reverse proxy configurations (nginx, Apache, Traefik)
- [ ] CI/CD pipeline configurations
- [ ] Health check URLs in monitoring systems
- [ ] Load balancer configurations
- [ ] Container orchestration manifests

### Step 2: Update Health Check URLs
Replace all health check URLs:

```bash
# Before
http://localhost/health
http://localhost:80/health

# After
http://localhost:8080/health
```

### Step 3: Fix Volume Permissions
If you mount volumes with write access:


```bash
# Check if you have volume mounts
chown -R 1001:1001 /path/to/volume
```

### Step 4: Update Activity Types
Search for `ActivityType =` in your codebase and update:

```csharp
// Search for:
ActivityType = "ValidationActivity"
ActivityType = "NotificationActivity"

// Replace with:
ActivityType = "ValidatorActivity"
ActivityType = "EmailActivity"
```

### Step 5: Update Retry Policies
Review retry policy configurations:

```csharp
// Before
RetryPolicy = RetryPolicy.Exponential

// After
RetryPolicy = RetryPolicy.Randomized  // or FixedDelay
```

### Step 6: Update Configuration Files
Add new configuration options if needed:

```json
{
  "WorkflowEngine": {
    "EnableVisualDesigner": true,
    "BPMNCompatibilityMode": "Strict"
  }
}
```

### Step 7: Update NuGet Package
Update to the latest version:

```bash
# Using dotnet CLI
dotnet add package DotNetWorkflowEngine

# Or update in Visual Studio
```

### Step 8: Test the Migration

1. Start the container: `docker run -p 8080:8080 dotnet-workflow-engine:latest`
2. Verify health: `curl http://localhost:8080/health`
3. Test workflow execution
4. Test visual designer
5. Verify audit trail is working

## Code Examples: Old vs New

### Example 1: Creating a Workflow

**Before (v1.x):**
```csharp
var workflow = new Workflow
{
    Id = Guid.NewGuid(),
    Name = "OrderProcessing",
    Version = 1,
    Status = WorkflowStatus.Active,
    Activities = new List<Activity>
    {
        new Activity
        {
            Id = "validate_order",
            Name = "Validate Order",
            ActivityType = "ValidationActivity",
            Timeout = TimeSpan.FromSeconds(30)
        },
        new Activity
        {
            Id = "process_payment",
            Name = "Process Payment",
            ActivityType = "PaymentActivity",
            RetryPolicy = RetryPolicy.Exponential,
            MaxRetries = 3
        }
    },
    Transitions = new List<Transition>
    {
        new Transition
        {
            Id = "t1",
            SourceActivityId = "validate_order",
            TargetActivityId = "process_payment"
        }
    }
};
```

**After (v2.0):**
```csharp
var workflow = new Workflow
{
    Id = Guid.NewGuid(),
    Name = "OrderProcessing",
    Version = 1,
    Status = WorkflowStatus.Active,
    Activities = new List<Activity>
    {
        new Activity
        {
            Id = "validate_order",
            Name = "Validate Order",
            ActivityType = "ValidatorActivity",  // Changed from ValidationActivity
            Timeout = TimeSpan.FromSeconds(30),
            Display = new ActivityDisplay
            {
                PositionX = 100,
                PositionY = 200,
                Color = "#4CAF50"
            }
        },
        new Activity
        {
            Id = "process_payment",
            Name = "Process Payment",
            ActivityType = "PaymentActivity",
            RetryPolicy = RetryPolicy.Randomized,  // Enhanced retry options
            MaxRetries = 3,
            Display = new ActivityDisplay
            {
                PositionX = 300,
                PositionY = 200,
                Color = "#2196F3"
            }
        }
    },
    Transitions = new List<Transition>
    {
        new Transition
        {
            Id = "t1",
            SourceActivityId = "validate_order",
            TargetActivityId = "process_payment",
            Display = new TransitionDisplay
            {
                Path = "M 150 225 L 300 225",
                LabelPosition = "Middle"
            }
        }
    }
};
```

### Example 2: Using BPMN Format

**Before (v1.x):**
```csharp
// Only JSON format supported
var workflowJson = """{...}""";
```

**After (v2.0):**
```csharp
// Both JSON and BPMN XML formats supported
var workflowBpmn = """<?xml version="1.0" encoding="UTF-8"?>
<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL">
  <bpmn:process id="order_process" isExecutable="true">
    <bpmn:startEvent id="start" />
    <bpmn:task id="validate" name="Validate Order" />
    <bpmn:endEvent id="end" />
    <bpmn:sequenceFlow id="flow1" sourceRef="start" targetRef="validate" />
    <bpmn:sequenceFlow id="flow2" sourceRef="validate" targetRef="end" />
  </bpmn:process>
</bpmn:definitions>""";

// Import BPMN workflow
var workflow = await workflowService.ImportFromBpmnAsync(workflowBpmn);
```

### Example 3: Using Visual Designer

**Before (v1.x):**
```csharp
// Manual layout required
var workflow = CreateManuallyLayoutedWorkflow();
```

**After (v2.0):**
```csharp
// Use visual designer to create and layout workflow
// Then export to code

// Or import from visual designer state
var designerState = await workflowDesignerService.GetDesignerStateAsync(workflowId);

// Apply visual layout to workflow
foreach (var activity in workflow.Activities)
{
    var display = designerState.Activities.FirstOrDefault(a => a.Id == activity.Id);
    if (display != null)
    {
        activity.Display = new ActivityDisplay
        {
            PositionX = display.PositionX,
            PositionY = display.PositionY,
            Color = display.Color
        };
    }
}
```

### Example 4: Enhanced Error Handling

**Before (v1.x):**
```csharp
try
{
    await executionService.ExecuteAsync(context);
}
catch (ActivityException ex)
{
    // Handle activity error
}
```

**After (v2.0):**
```csharp
var result = await executionService.ExecuteAsync(context);

if (result.Status == WorkflowStatus.Failed)
{
    // Enhanced error information
    var errorDetails = result.ErrorDetails;
    var failedActivity = result.FailedActivityId;
    
    // Automatic retry with new retry policies
    if (result.ShouldRetry)
    {
        await executionService.RetryAsync(result.InstanceId);
    }
}
```

### Example 5: Configuration Changes

**Before (v1.x):**
```json
{
  "WorkflowEngine": {
    "DefaultExecutionMode": "Sequential",
    "CachingEnabled": false
  }
}
```

**After (v2.0):**
```json
{
  "WorkflowEngine": {
    "DefaultExecutionMode": "BPMN",  // New option
    "CachingEnabled": true,          // Changed default
    "EnableVisualDesigner": true,
    "BPMNCompatibilityMode": "Strict",
    "MaxParallelBranches": 20
  }
}
```

## Common Migration Issues

### Issue 1: Port conflicts
**Symptom:** Container fails to start or health check times out
**Solution:** Ensure no other service is using port 8080

```bash
# Check port usage
netstat -tuln | grep 8080

# Or on Windows
netstat -ano | findstr 8080
```

### Issue 2: Permission denied on volume mounts
**Symptom:** Application crashes with permission errors
**Solution:** Set correct permissions for UID 1001
```bash
chown -R 1001:1001 /path/to/volume
```

### Issue 3: Workflow execution fails after migration
**Symptom:** Workflows that worked in v1.x fail in v2.0
**Solution:** Check activity types and retry policies
```csharp
// Verify all activity types are correct
foreach (var activity in workflow.Activities)
{
    if (activity.ActivityType == "ValidationActivity")
    {
        activity.ActivityType = "ValidatorActivity";
    }
}
```

### Issue 4: Visual designer not loading
**Symptom:** Designer UI shows blank screen or errors
**Solution:** Ensure visual designer is enabled in configuration
```json
{
  "WorkflowEngine": {
    "EnableVisualDesigner": true
  }
}
```

### Issue 5: API endpoints return 404
**Symptom:** New endpoints not found
**Solution:** Update API routes in your client code
```bash
# Before
http://localhost:80/api/workflows/designer

# After
http://localhost:8080/api/workflows/designer
```

## Rollback Plan

If you encounter issues with v2.0, you can roll back to v1.x:

1. Stop all v2.0 containers
2. Revert Docker image tags to v1.x
3. Restore port mappings from v1.x configuration
4. Restore volume permissions if changed
5. Update health check URLs back to port 80

**Note:** No database schema changes were introduced in v2.0, so your data remains compatible.

## Additional Resources

- [Docker Guide](docker-guide.md) - Detailed Docker usage instructions
- [Visual Designer Documentation](visual-designer.md) - How to use the visual designer
- [BPMN Support](bpmn-support.md) - BPMN 2.0 compatibility guide
- [Configuration Reference](configuration.md) - Complete configuration options
- [Troubleshooting Guide](troubleshooting.md) - Common issues and solutions

## Support

If you encounter issues during migration:

1. Check the [Troubleshooting Guide](troubleshooting.md)
2. Review the [FAQ](faq.md)
3. Open an issue on GitHub with:
   - v1.x and v2.0 versions
   - Error logs
   - Reproduction steps
   - Configuration files (redact sensitive data)
