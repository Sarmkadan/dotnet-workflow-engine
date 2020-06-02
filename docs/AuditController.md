# AuditController
The `AuditController` class is responsible for managing and retrieving audit logs within the dotnet-workflow-engine project. It provides a set of APIs for fetching various types of audit logs, statistics, and exporting logs, enabling developers to track and analyze workflow-related events and activities.

## API
* `public AuditController`: The constructor for the `AuditController` class, used to instantiate a new instance.
* `public async Task<IActionResult> GetAuditLogs`: Retrieves a collection of audit logs. Returns an `IActionResult` containing the audit logs. May throw exceptions if there are issues with data retrieval or serialization.
* `public async Task<IActionResult> GetWorkflowAuditLog`: Fetches the audit log for a specific workflow. Returns an `IActionResult` containing the workflow's audit log. May throw exceptions if the workflow is not found or if there are issues with data retrieval.
* `public async Task<IActionResult> GetInstanceAuditLog`: Retrieves the audit log for a specific workflow instance. Returns an `IActionResult` containing the instance's audit log. May throw exceptions if the instance is not found or if there are issues with data retrieval.
* `public async Task<IActionResult> GetAuditLogEntry`: Fetches a specific audit log entry. Returns an `IActionResult` containing the audit log entry. May throw exceptions if the entry is not found or if there are issues with data retrieval.
* `public async Task<IActionResult> GetAuditStatistics`: Retrieves statistics related to audit logs. Returns an `IActionResult` containing the audit statistics. May throw exceptions if there are issues with data retrieval or calculation.
* `public async Task<IActionResult> ExportAuditLogs`: Exports audit logs to an external format. Returns an `IActionResult` containing the exported logs. May throw exceptions if there are issues with data retrieval, serialization, or export.

## Usage
The following examples demonstrate how to use the `AuditController` class:
```csharp
// Example 1: Retrieving audit logs for a workflow instance
var auditController = new AuditController();
var instanceId = 123;
var result = await auditController.GetInstanceAuditLog(instanceId);
if (result.IsSuccess)
{
    var auditLogs = (List<AuditLog>)result.Value;
    // Process the audit logs
}

// Example 2: Exporting audit logs
var auditController = new AuditController();
var exportResult = await auditController.ExportAuditLogs();
if (exportResult.IsSuccess)
{
    var exportedLogs = (byte[])exportResult.Value;
    // Save the exported logs to a file or database
}
```

## Notes
When using the `AuditController` class, consider the following:
* The `GetAuditLogs`, `GetWorkflowAuditLog`, `GetInstanceAuditLog`, and `GetAuditLogEntry` methods may return large amounts of data, which can impact performance. Implement pagination or filtering to reduce the amount of data transferred.
* The `GetAuditStatistics` method may perform complex calculations, which can be time-consuming. Consider caching the results or using an asynchronous approach to improve responsiveness.
* The `ExportAuditLogs` method may throw exceptions if the export format is not supported or if there are issues with serialization. Handle these exceptions accordingly and provide meaningful error messages to the user.
* The `AuditController` class is designed to be thread-safe, allowing multiple concurrent requests to be processed simultaneously. However, the underlying data storage and retrieval mechanisms may still be subject to concurrency issues. Implement appropriate locking or synchronization mechanisms to ensure data consistency.
