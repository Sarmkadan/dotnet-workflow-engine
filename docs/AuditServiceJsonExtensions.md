# AuditServiceJsonExtensions

Provides JSON serialization and deserialization helpers for audit-related types within the workflow engine. This static class centralizes conversion logic between `AuditLogEntry` instances and their JSON string representations, offering both exception-throwing and safe-try variants for single entries and collections.

## API

### ToJson(AuditLogEntry entry)

Serializes a single `AuditLogEntry` to its JSON string representation.

- **Parameters:** `AuditLogEntry entry` — the audit log entry to serialize.
- **Returns:** `string` — the JSON representation of the entry.
- **Throws:** `ArgumentNullException` if `entry` is `null`.

### FromJsonToAuditLogEntry(string json)

Deserializes a JSON string into a single `AuditLogEntry`.

- **Parameters:** `string json` — the JSON string to deserialize.
- **Returns:** `AuditLogEntry?` — the deserialized entry, or `null` if the input is `null` or whitespace.
- **Throws:** `JsonException` if the JSON is malformed or cannot be mapped to an `AuditLogEntry`.

### TryFromJsonToAuditLogEntry(string json, out AuditLogEntry? entry)

Attempts to deserialize a JSON string into a single `AuditLogEntry` without throwing on failure.

- **Parameters:**
  - `string json` — the JSON string to deserialize.
  - `out AuditLogEntry? entry` — the deserialized entry if successful; `null` otherwise.
- **Returns:** `bool` — `true` if deserialization succeeded; `false` if the input is `null`, whitespace, or malformed JSON.

### ToJson(IReadOnlyList<AuditLogEntry>)

Serializes a collection of `AuditLogEntry` instances to a JSON array string.

- **Parameters:** `IReadOnlyList<AuditLogEntry> entries` — the entries to serialize.
- **Returns:** `string` — the JSON array representation.
- **Throws:** `ArgumentNullException` if `entries` is null.

### FromJsonToAuditLogEntries(string json)

Deserializes a JSON array string into a list of `AuditLogEntry` instances.

- **Parameters:** `string json` — the JSON string to deserialize.
- **Returns:** `IReadOnlyList<AuditLogEntry>` — the deserialized entries. Returns an empty list if the input is `null` or whitespace.
- **Throws:** `JsonException` if the JSON is malformed or cannot be mapped to the expected collection type.

### TryFromJsonToAuditLogEntries(string json, out IReadOnlyList<AuditLogEntry>? entries)

Attempts to deserialize a JSON array string into a list of `AuditLogEntry` instances without throwing on invalid.

- **Parameters:**
  - `string json` — the JSON string to deserialize.
  - `out IReadOnlyList<AuditLogEntry>? entries` — the deserialized entries if successful; `null` otherwise.
- **Returns:** `bool` — `true` if deserialization succeeded; `false` if the input is null, whitespace, or malformed JSON.

## Usage

### Serializing and Deserializing a Single Entry

```csharp
using WorkflowEngine.Audit;

AuditLogEntry entry = new AuditLogEntry
{
    Id = Guid.NewGuid(),
    WorkflowId = "wf-123",
    Timestamp = DateTime.UtcNow,
    Action = "WorkflowStarted"
};

// Serialize
string json = AuditServiceJsonExtensions.ToJson(entry);

// Deserialize safely
if (AuditServiceJsonExtensions.TryFromJsonToAuditLogEntry(json, out AuditLogEntry? restored))
{
    Console.WriteLine($"Restored entry: {restored.WorkflowId}");
}
```

### Batch Processing a Collection of Entries

```csharp
using WorkflowEngine.AuditExtensions;

IReadOnlyList<AuditLogEntry> entries = new List<AuditLogEntry>
{
    new AuditLogEntry { Id = Guid.NewGuid(), WorkflowId = "wf-1", Action = "Step1" },
    new AuditLogEntry { Id = Guid.NewGuid(), WorkflowId = "wf-1", Action = "Step2" }
};

// Serialize the collection
string batchJson = AuditServiceJsonExtensions.ToJson(entries);

// Deserialize safely
if (AuditServiceJsonExtensions.TryFromJsonToAuditLogEntries(batchJson, out var restoredEntries))
{
    foreach (var e in restoredEntries)
    {
        Console.WriteLine($"Action: {e.Action}");
    }
}
```

## Notes

- All methods are static and thread-safe; they operate purely on their input arguments without shared mutable state.
- The `Try*` variants never throw — they return `false` for null, whitespace, or structurally invalid JSON, making them suitable for parsing untrusted or external input.
- The `FromJsonToAuditLogEntry` method returns `null` for null/whitespace input but throws on malformed JSON; use `TryFromJsonToAuditLogEntry` when the caller cannot guarantee well-formed input.
- The collection deserialization methods return an empty list for null/whitespace input in the non-try variant, and `null` in the try variant's output parameter on failure — callers should check the boolean return value before accessing the out parameter.
- These methods are thin wrappers over a configured JSON serializer; they do not perform semantic validation of the deserialized objects (e.g., required fields, value ranges).
