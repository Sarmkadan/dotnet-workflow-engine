# AuditServiceTests

Unit tests for the `AuditService` class, verifying that audit logging operations behave as expected. These tests cover instance creation events, various filtering scenarios on audit logs, pagination, and CSV export functionality.

## API

### Constructor `AuditServiceTests`
Initializes a new instance of the `AuditServiceTests` class with required dependencies for testing audit logging operations.

### `LogInstanceCreated_CallsRepositoryAddAsync`
- **Purpose**: Verifies that the `AuditService` correctly logs an instance creation event by calling the repository's `AddAsync` method.
- **Parameters**: None.
- **Return value**: `Task` representing the asynchronous operation.
- **Throws**: Propagates any exceptions thrown by the underlying repository.

### `GetFilteredAuditLogsAsync_NoFilters_ReturnsAllLogs`
- **Purpose**: Ensures that retrieving audit logs without any filters returns all available logs.
- **Parameters**: None.
- **Return value**: `Task` containing a collection of all audit logs.
- **Throws**: Propagates any exceptions thrown by the repository or query execution.

### `GetFilteredAuditLogsAsync_FilterByInstanceId_ReturnsFilteredLogs`
- **Purpose**: Validates filtering audit logs by a specific instance identifier.
- **Parameters**: None.
- **Return value**: `Task` containing a filtered collection of audit logs matching the instance identifier.
- **Throws**: Propagates any exceptions thrown by the repository or query execution.

### `GetFilteredAuditLogsAsync_FilterByActivityId_ReturnsFilteredLogs`
- **Purpose**: Validates filtering audit logs by a specific activity identifier.
- **Parameters**: None.
- **Return value**: `Task` containing a filtered collection of audit logs matching the activity identifier.
- **Throws**: Propagates any exceptions thrown by the repository or query execution.

### `GetFilteredAuditLogsAsync_FilterByEventType_ReturnsFilteredLogs`
- **Purpose**: Validates filtering audit logs by a specific event type.
- **Parameters**: None.
- **Return value**: `Task` containing a filtered collection of audit logs matching the event type.
- **Throws**: Propagates any exceptions thrown by the repository or query execution.

### `GetFilteredAuditLogsAsync_FilterBySeverity_ReturnsFilteredLogs`
- **Purpose**: Validates filtering audit logs by a specific severity level.
- **Parameters**: None.
- **Return value**: `Task` containing a filtered collection of audit logs matching the severity level.
- **Throws**: Propagates any exceptions thrown by the repository or query execution.

### `GetFilteredAuditLogsAsync_FilterByDateRange_ReturnsFilteredLogs`
- **Purpose**: Validates filtering audit logs by a date range.
- **Parameters**: None.
- **Return value**: `Task` containing a filtered collection of audit logs within the specified date range.
- **Throws**: Propagates any exceptions thrown by the repository or query execution.

### `GetFilteredAuditLogsAsync_FilterByActor_ReturnsFilteredLogs`
- **Purpose**: Validates filtering audit logs by the actor who performed the action.
- **Parameters**: None.
- **Return value**: `Task` containing a filtered collection of audit logs matching the actor.
- **Throws**: Propagates any exceptions thrown by the repository or query execution.

### `GetFilteredAuditLogsAsync_WithPagination_ReturnsPagedLogs`
- **Purpose**: Validates that pagination correctly limits and skips audit logs.
- **Parameters**: None.
- **Return value**: `Task` containing a paged collection of audit logs.
- **Throws**: Propagates any exceptions thrown by the repository or query execution.

### `ExportAuditLogAsCsv_ReturnsCorrectCsvFormat`
- **Purpose**: Ensures that exporting audit logs to CSV produces a correctly formatted file.
- **Parameters**: None.
- **Return value**: `Task` containing the CSV-formatted string of audit logs.
- **Throws**: Propagates any exceptions thrown by the repository or CSV serialization.

## Usage

### Example 1: Testing instance creation logging
