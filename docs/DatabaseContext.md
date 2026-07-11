# DatabaseContext

Central data-access component for the workflow engine that encapsulates Entity Framework Core operations against the underlying store. It provides repositories for workflow definitions, running instances, and audit logs, plus transactional helpers and lifecycle management.

## API

### `WorkflowRepository Workflows`
Read-only accessor for workflow definitions stored in the database. The returned repository exposes query methods to retrieve workflow metadata and blueprints.

### `WorkflowInstanceRepository Instances`
Read-write accessor for active workflow instances. Provides methods to create, update, query, and terminate instances while maintaining referential integrity with the workflow definitions.

### `AuditRepository AuditLogs`
Read-only accessor for the immutable audit trail. Exposes query methods to retrieve change history, decisions, and runtime events produced by workflow executions.

### `DatabaseContext()`
Default constructor. Initializes a new context with default connection settings and change-tracking enabled.

### `async Task InitializeAsync()`
Ensures the database schema is created and migrations applied if necessary. Idempotent; safe to call repeatedly. Throws `DbUpdateException` if schema creation fails.

### `async Task SaveChangesAsync()`
Persists all tracked changes to the underlying store within a single transaction. Returns only after the commit succeeds. Throws `DbUpdateException` on concurrency or constraint violations.

### `async Task<IDbTransaction> BeginTransactionAsync()`
Begins a new ambient transaction and returns its handle. The transaction remains open until explicitly committed or rolled back. Subsequent calls return the same ambient transaction until it is disposed. Throws `InvalidOperationException` if a transaction is already active on the current context.

### `async Task<Dictionary<string, int>> GetStatisticsAsync()`
Aggregates counts for workflows, instances, and audit entries and returns them as a dictionary keyed by entity name. Executes a lightweight query; never throws under normal conditions.

### `async Task ClearAllAsync()`
Removes all workflow definitions, instances, and audit logs in a single transaction. Intended for test environments; not suitable for production data loss scenarios. Throws `DbUpdateException` if any table is locked or referenced by a foreign key.

### `public ValueTask DisposeAsync()`
Asynchronously releases all managed and unmanaged resources held by the context, including any open transaction. Safe to call multiple times. Does not throw.

### `InMemoryTransaction`
Exposes the current ambient transaction if one is active; otherwise `null`. Intended for advanced scenarios where external code needs to enlist in the same transaction.

### `async Task CommitAsync()`
Commits the current ambient transaction. Must be called outside any existing `using` block that would dispose the context prematurely. Throws `InvalidOperationException` if no transaction is active.

### `Task RollbackAsync()`
Rolls back the current ambient transaction without disposing the context. Subsequent operations continue against the same context. Throws `InvalidOperationException` if no transaction is active.

### `public async ValueTask DisposeAsync()`
Alias for the primary `DisposeAsync` method; provided for symmetry with other async-disposable components. Safe to call multiple times.

## Usage
