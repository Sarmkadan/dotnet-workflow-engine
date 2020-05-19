# ErrorHandlingMiddleware

The `ErrorHandlingMiddleware` class intercepts unhandled exceptions thrown during HTTP request processing in ASP.NET Core applications. It captures exception details, formats them into a standardized error response, and ensures consistent error reporting to API consumers while preventing sensitive information leakage.

## API

### `ErrorHandlingMiddleware`

**Purpose**
Constructor that initializes the middleware instance. Typically invoked by the ASP.NET Core dependency injection system during application startup.

**Parameters**
- `next` (`RequestDelegate`): The next middleware in the pipeline. Automatically provided by the framework.

**Throws**
- `ArgumentNullException`: Thrown if `next` is `null`.

---

### `InvokeAsync`

**Purpose**
Executes the middleware logic. Wraps the subsequent middleware invocation in a try-catch block to handle exceptions.

**Parameters**
- `context` (`HttpContext`): The HTTP context for the current request.

**Returns**
A `Task` representing the asynchronous operation.

**Throws**
- None. All exceptions are caught and processed internally.

---

### `ErrorCode`

**Purpose**
A machine-readable error code identifying the category of the failure (e.g., `INVALID_INPUT`, `INTERNAL_ERROR`).

**Type**
`string`

**Remarks**
Read-only after middleware execution. Populated based on the caught exception type or context.

---

### `Message`

**Purpose**
A human-readable summary of the error suitable for client display.

**Type**
`string`

**Remarks**
Read-only after middleware execution. Derived from the exception message or a predefined template.

---

### `Details`

**Purpose**
Optional additional diagnostic information, such as stack traces or inner exception messages, intended for debugging.

**Type**
`string?`

**Remarks**
Read-only after middleware execution. May be `null` if detailed information is suppressed for security reasons.

---

### `Timestamp`

**Purpose**
The UTC date and time when the error occurred.

**Type**
`DateTime`

**Remarks**
Read-only after middleware execution. Always expressed in UTC.

## Usage

### Example 1: Basic Integration in ASP.NET Core
