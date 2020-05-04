# AuditLogEntry

The `AuditLogEntry` class represents a single, immutable-by-convention record of an event that occurred within a workflow engine. Each entry captures the context of the event—such as the workflow instance, the type of event, the actor responsible, and the state before and after the change—enabling detailed auditing, debugging, and monitoring of workflow executions. The class provides factory methods for common event types and a method to retrieve a formatted timestamp string.

## API

### Properties

- **`Id`** (`string`)  
  A unique identifier for this audit log entry. Typically assigned by the system at creation time.

- **`WorkflowInstanceId`** (`string`)  
  The identifier of the workflow instance with which this entry is associated.

- **`EventType`** (`string`)  
  A classification of the event (e.g., `"ActivityExecution"`, `"StateChange"`, `"Error"`). Used for filtering and categorization.

- **`ActivityId`** (`string?`)  
  The identifier of the activity that triggered the event, if applicable. May be `null` for events not tied to a specific activity.

- **`Description`** (`string`)  
  A human-readable summary of the event.

- **`Severity`** (`string`)  
  The severity level of the event (e.g., `"Information"`, `"Warning"`, `"Error"`). Used for log filtering and alerting.

- **`Timestamp`** (`DateTime`)  
  The date and time when the event occurred, in UTC.

- **`Actor`** (`string?`)  
  The identity of the user or system component that caused the event. May be `null` if the actor is unknown or not applicable.

- **`PreviousState`** (`Dictionary<string, object?>`)  
  A dictionary representing the state of the workflow or activity before the event. Keys are state variable names; values are their previous values (or `null`).

- **`CurrentState`** (`Dictionary<string, object?>`)  
  A dictionary representing the state after the event. Keys are state variable names; values are their current values (or `null`).

- **`Details`** (`Dictionary<string, object?>`)  
  A dictionary for additional, event-specific metadata. The contents depend on the event type and the data provided at creation.

- **`CorrelationId`** (`string?`)  
  An optional identifier used to correlate this entry with other entries or external systems. May be `null`.

### Constructors

- **`AuditLogEntry()`**  
  Initializes a new instance of the `AuditLogEntry` class with default property values. All dictionaries are initialized as empty.

- **`AuditLogEntry(…)`**  
  An overloaded constructor that accepts parameters to populate the properties at creation time. Refer to the source code for the exact parameter list.

### Static Factory Methods

- **`static AuditLogEntry CreateActivityExecution(…)`**  
  Creates an `AuditLogEntry` specifically for an activity execution event.  
  **Returns:** A new `AuditLogEntry` instance with `EventType` set to `"ActivityExecution"` and the provided parameters mapped to the appropriate properties.  
  **Throws:** `ArgumentNullException` if required parameters (e.g., workflow instance ID, activity ID) are `null` or empty.

- **`static AuditLogEntry CreateStateChange(…)`**  
  Creates an `AuditLogEntry` for a state change event.  
  **Returns:** A new `AuditLogEntry` instance with `EventType` set to `"StateChange"`, and `PreviousState` and `CurrentState` populated from the provided dictionaries.  
  **Throws:** `ArgumentNullException` if required parameters are `null`.

- **`static AuditLogEntry CreateError(…)`**  
  Creates an `AuditLogEntry` for an error event.  
  **Returns:** A new `AuditLogEntry` instance with `EventType` set to `"Error"` and `Severity` set to `"Error"`.  
  **Throws:** `ArgumentNullException` if required parameters are `null`.

### Methods

- **`string GetFormattedTimestamp()`**  
  Returns the `Timestamp` property formatted as a string using the current culture’s long date and long time pattern.  
  **Returns:** A string representation of the timestamp (e.g., `"Monday, 14 April 2025 10:30:00"`).  
  **Throws:** No exceptions under normal circumstances.

## Usage

### Example 1: Creating an activity execution audit entry

```csharp
using WorkflowEngine.Audit;

var entry = AuditLogEntry.CreateActivityExecution(
    workflowInstanceId: "wf-001",
    activityId: "act-send-email",
    description: "Sending confirmation email",
    actor: "system",
    previousState: new Dictionary<string, object?> { { "EmailSent", false } },
    currentState: new Dictionary<string, object?> { { "EmailSent", true } },
    details: new Dictionary<string, object?> { { "Recipient", "user@example.com" } }
);

Console.WriteLine(entry.GetFormattedTimestamp());
// Output: "Monday, 14 April 2025 10:30:00"
```

### Example 2: Logging an error with state snapshots

```csharp
var errorEntry = AuditLogEntry.CreateError(
    workflowInstanceId: "wf-042",
    activityId: "act-process-payment",
    description: "Payment gateway timeout",
    actor: "payment-service",
    previousState: new Dictionary<string, object?> { { "Attempts", 2 } },
    currentState: new Dictionary<string, object?> { { "Attempts", 3 }, { "Status", "Failed" } },
    details: new Dictionary<string, object?> { { "ErrorCode", "TIMEOUT" }, { "Retryable", true } }
);

// Manually override severity if needed
errorEntry.Severity = "Critical";

// Store the entry (e.g., in a database)
SaveAuditEntry(errorEntry);
```

## Notes

- **Null handling:** All string properties (`Id`, `WorkflowInstanceId`, `EventType`, `Description`, `Severity`) are expected to be non-null after construction. The factory methods enforce this by throwing `ArgumentNullException` if required parameters are `null` or empty. The nullable properties (`ActivityId`, `Actor`, `CorrelationId`) may remain `null` and are safe to read.
- **Dictionary mutability:** The `PreviousState`, `CurrentState`, and `Details` dictionaries are mutable. Modifying them after the entry is created will affect the entry’s data. For audit integrity, avoid mutating these dictionaries once the entry is persisted.
- **Timestamp precision:** The `Timestamp` property is set to `DateTime.UtcNow` at the moment of construction by the factory methods. If you use the parameterless constructor, you must assign a value manually.
- **Thread safety:** Instances of `AuditLogEntry` are not thread-safe. Concurrent reads and writes to the same instance (especially to the dictionary properties) can lead to inconsistent state. The static factory methods are thread-safe for creation, but the returned instance should not be shared across threads without synchronization.
- **Formatting:** `GetFormattedTimestamp()` uses the current culture’s settings. On servers with invariant culture, the output may differ from expected regional formats. For consistent formatting, consider using a custom format string instead.
