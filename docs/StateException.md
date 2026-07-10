# StateException

An exception thrown by the workflow engine when a requested state transition cannot be completed due to the current state of an entity. This exception carries contextual information about the attempted transition, including the current state, the requested state, and the entity identifier, to aid in debugging and error handling.

## API

### `CurrentState` (property)
**Purpose**
Gets the current state of the entity at the time the exception was thrown.

**Type**
`public string CurrentState`

**Remarks**
This property is read-only and always returns a non-null string representing the state the entity was in when the invalid transition was attempted.

---

### `RequestedState` (property)
**Purpose**
Gets the state that was requested but could not be transitioned to.

**Type**
`public string RequestedState`

**Remarks**
This property is read-only and always returns a non-null string representing the invalid target state.

---

### `EntityId` (property)
**Purpose**
Gets the unique identifier of the entity involved in the failed transition.

**Type**
`public string? EntityId`

**Remarks**
This property may return `null` if the entity identifier is not available or applicable. It is read-only.

---
### Constructor `StateException(string currentState, string requestedState)`
**Purpose**
Initializes a new instance of the `StateException` class with the current and requested states.

**Parameters**
- `currentState` (string): The current state of the entity.
- `requestedState` (string): The state that was requested but could not be transitioned to.

**Remarks**
This constructor sets the `CurrentState` and `RequestedState` properties to the provided values. The `EntityId` property will be `null`.

---
### Constructor `StateException(string currentState, string requestedState, string? entityId)`
**Purpose**
Initializes a new instance of the `StateException` class with the current state, requested state, and entity identifier.

**Parameters**
- `currentState` (string): The current state of the entity.
- `requestedState` (string): The state that was requested but could not be transitioned to.
- `entityId` (string?): The unique identifier of the entity, or `null`.

**Remarks**
This constructor sets the `CurrentState`, `RequestedState`, and `EntityId` properties to the provided values.

---
### Method `GetTransitionDetails()`
**Purpose**
Returns a human-readable string summarizing the attempted transition.

**Returns**
`string`: A string containing the current state, requested state, and entity identifier (if available).

**Remarks**
The returned string is formatted as:
`"Transition from [CurrentState] to [RequestedState] (Entity: [EntityId])"`
If `EntityId` is `null`, the entity portion is omitted.

## Usage

### Example 1: Catching and logging a state transition failure
