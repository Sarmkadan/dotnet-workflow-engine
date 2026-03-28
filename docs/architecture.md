// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Architecture Guide

This document provides a deep dive into the architecture and design of dotnet-workflow-engine.

## High-Level Architecture

```
┌──────────────────────────────────────┐
│        HTTP Clients / Web UI          │
└──────────────┬───────────────────────┘
               │
┌──────────────▼───────────────────────┐
│        ASP.NET Core API Layer        │
│  ├─ WorkflowController               │
│  ├─ InstanceController               │
│  └─ AuditController                  │
└──────────────┬───────────────────────┘
               │
┌──────────────▼───────────────────────┐
│         Service Layer                │
│  ├─ WorkflowExecutionService         │
│  ├─ WorkflowDefinitionService        │
│  ├─ ActivityService                  │
│  ├─ AuditService                     │
│  ├─ RetryPolicyService               │
│  └─ CacheService                     │
└──────────────┬───────────────────────┘
               │
┌──────────────▼───────────────────────┐
│      Domain Models & Utilities        │
│  ├─ Workflow, Activity, Transition   │
│  ├─ ExecutionContext                 │
│  ├─ EventBus                         │
│  └─ Helper Utilities                 │
└──────────────┬───────────────────────┘
               │
┌──────────────▼───────────────────────┐
│       Data Access Layer               │
│  ├─ WorkflowRepository                │
│  ├─ InstanceRepository                │
│  ├─ AuditRepository                   │
│  └─ DatabaseContext (EF Core)         │
└──────────────┬───────────────────────┘
               │
┌──────────────▼───────────────────────┐
│        External Systems               │
│  ├─ SQL Database                      │
│  ├─ Redis Cache                       │
│  ├─ Hangfire Job Queue                │
│  └─ Prometheus Metrics                │
└──────────────────────────────────────┘
```

## Core Components

### 1. Workflow Model

The `Workflow` class represents a workflow definition:

```csharp
public class Workflow
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int Version { get; set; }
    public WorkflowStatus Status { get; set; }
    public List<Activity> Activities { get; set; }
    public List<Transition> Transitions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}
```

**Responsibilities:**
- Define workflow structure
- Store activity and transition definitions
- Track workflow versions
- Manage publication state

### 2. WorkflowInstance Model

The `WorkflowInstance` class represents a runtime execution instance:

```csharp
public class WorkflowInstance
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public WorkflowStatus Status { get; set; }
    public string CurrentActivityId { get; set; }
    public Dictionary<string, object> Variables { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
```

**Responsibilities:**
- Track execution state
- Store variable data
- Maintain execution history
- Record timestamps

### 3. ExecutionContext

The `ExecutionContext` class encapsulates runtime execution state:

```csharp
public class ExecutionContext
{
    public Guid InstanceId { get; set; }
    public Guid WorkflowId { get; set; }
    public Dictionary<string, object> Variables { get; set; }
    public ExecutionMode Mode { get; set; }
    public CancellationToken CancellationToken { get; set; }
}
```

**Responsibilities:**
- Provide access to variables during execution
- Track execution mode
- Support cancellation
- Pass data between activities

### 4. Services

#### WorkflowDefinitionService
Manages workflow definitions and versions:
- Create and update workflows
- Retrieve workflows by ID or name
- Manage workflow versions
- Validate workflow definitions

#### WorkflowExecutionService
Orchestrates workflow execution:
- Create execution instances
- Execute workflows sequentially or in parallel
- Manage activity transitions
- Handle workflow completion

#### ActivityService
Executes individual activities:
- Execute activity handlers
- Apply retry policies
- Handle activity errors
- Validate activity inputs

#### AuditService
Maintains audit trail:
- Log all workflow events
- Query audit logs
- Export audit trails
- Support compliance requirements

#### RetryPolicyService
Implements retry logic:
- Exponential backoff
- Fixed delay
- Linear backoff
- Custom retry logic

## Execution Flow

### Sequential Execution

```
Start → Activity 1 → Activity 2 → Activity 3 → End
        (wait)      (wait)      (wait)
```

1. Execute Activity 1
2. Wait for completion
3. Transition to Activity 2
4. Wait for completion
5. Transition to Activity 3
6. Mark workflow as completed

### Parallel Execution

```
        ┌→ Activity 1 ┐
Start → ┼→ Activity 2 ├→ Sync → Activity 4 → End
        └→ Activity 3 ┘
```

1. Fork to parallel activities
2. Execute all activities concurrently
3. Synchronize at join point
4. Continue with next activity

### Conditional Routing

```
Start → Activity 1 → Decision → Activity 2 (if approved)
                    ↓
                  Activity 3 (if rejected)
```

1. Execute activity
2. Evaluate condition using variables
3. Route to appropriate next activity

## Data Flow

### During Workflow Execution

```
ExecutionContext
    ↓
Variables Dictionary → Activity Handler
    ↓                      ↓
  Result            Variable Updates
    ↓                      ↓
AuditLogEntry    ← Updated Context
```

1. Load workflow and create ExecutionContext
2. Pass ExecutionContext to activity handler
3. Activity reads variables from context
4. Activity updates variables
5. Log changes to audit trail
6. Transition to next activity

### Error Handling

```
Activity Execution
    ↓
Exception Thrown
    ↓
Retry Policy Evaluation
    ├→ Retry Allowed → Wait & Retry
    └→ Max Retries Exceeded → Mark as Failed
    ↓
Log Error to Audit Trail
    ↓
Determine Next State
├→ Compensate (rollback)
├→ Skip Activity
└→ Terminate Workflow
```

## Key Design Patterns

### Repository Pattern

All data access goes through repositories:
- `IRepository<T>` interface
- Implementations for Workflow, Instance, Audit
- Abstraction over database

### Dependency Injection

All services are registered in DI container:
- Constructor injection for dependencies
- ServiceCollection extension methods
- Testable and loosely coupled

### Event-Driven Architecture

EventBus provides pub/sub capability:
- Workflow started/completed events
- Activity started/failed events
- Custom event handlers

### Async/Await

All I/O operations are async:
- Database queries
- HTTP calls
- Background jobs

## Database Schema

### Workflows Table
- Id (Guid, PK)
- Name (string)
- Version (int)
- Status (enum)
- Definition (JSON)
- CreatedAt (datetime)
- PublishedAt (datetime)

### WorkflowInstances Table
- Id (Guid, PK)
- WorkflowId (Guid, FK)
- Status (enum)
- CurrentActivityId (string)
- Variables (JSON)
- StartedAt (datetime)
- CompletedAt (datetime)

### AuditLogEntries Table
- Id (Guid, PK)
- WorkflowId (Guid, FK)
- InstanceId (Guid, FK)
- ActivityId (string)
- Action (string)
- Changes (JSON)
- Timestamp (datetime)
- UserId (string)
- IpAddress (string)

## Performance Considerations

### Caching Strategy

- **Workflow Definitions**: Cache for 1 hour
- **Active Instances**: Cache in memory
- **Activity Results**: Cache based on TTL

### Database Indexing

```sql
CREATE INDEX idx_workflow_status ON Workflows(Status);
CREATE INDEX idx_instance_workflow ON WorkflowInstances(WorkflowId);
CREATE INDEX idx_instance_status ON WorkflowInstances(Status);
CREATE INDEX idx_audit_instance ON AuditLogEntries(InstanceId);
```

### Parallel Execution Limits

Control concurrent activities to avoid resource exhaustion:
```json
"MaxConcurrentActivities": 10
```

## Security Considerations

- Authorization attributes on controllers
- Input validation before activity execution
- Audit trail for compliance
- Encryption of sensitive variables
- Rate limiting on API endpoints

## Extension Points

### Custom Activities

Register custom activity handlers:

```csharp
services.AddScoped<IActivityHandler>(sp =>
    new CustomActivityHandler()
);
```

### Custom Validators

Implement `IWorkflowValidator`:

```csharp
public class CustomValidator : IWorkflowValidator
{
    public ValidationResult Validate(Workflow workflow) { ... }
}
```

### Event Handlers

Subscribe to workflow events:

```csharp
eventBus.Subscribe<WorkflowCompletedEvent>(
    async e => await Handle(e)
);
```

## Deployment Considerations

- Stateless services (can scale horizontally)
- Distributed caching for multi-instance deployments
- Message queue for background jobs
- Centralized logging and monitoring
- Health checks and graceful shutdown

For deployment guidance, see [Deployment Guide](deployment.md).
