## AuditServiceJsonExtensions

The `AuditServiceJsonExtensions` class provides extension methods for serializing and deserializing audit log entries to and from JSON using System.Text.Json. It offers both strict and try-based parsing methods, along with configurable serialization options for compact or indented output.

This extension class is particularly useful for workflow applications that need to persist audit logs or communicate with external systems via JSON APIs.

Example usage:

```csharp
using DotNetWorkflowEngine.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Create sample audit log data
var auditLog = new AuditLogEntry
{
    WorkflowId = "order-processing-workflow",
    InstanceId = "wf-order-processing-001",
    EventType = "ActivityExecuted",
    ActivityId = "validate-order",
    Timestamp = DateTime.UtcNow,
    Description = "Order validation completed successfully",
    Severity = "Information",
    Actor = "admin@company.com"
};

// Serialize to compact JSON
string jsonCompact = auditLog.ToJson();
Console.WriteLine("Compact JSON:");
Console.WriteLine(jsonCompact);

// Serialize to indented JSON for readability
string jsonIndented = auditLog.ToJson(indented: true);
Console.WriteLine("\nIndented JSON:");
Console.WriteLine(jsonIndented);

// Parse from JSON (returns null for invalid input)
var parsedAuditLog = AuditLogEntryExtensions.FromJson(jsonCompact);
Console.WriteLine($"\nParsed workflow ID: {parsedAuditLog?.WorkflowId}");

// Try parse from JSON (safe parsing)
if (AuditLogEntryExtensions.TryFromJson(jsonCompact, out var safeParsedAuditLog))
{
    Console.WriteLine($"Safe parsed instance ID: {safeParsedAuditLog?.InstanceId}");
}
```
