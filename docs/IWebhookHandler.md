# IWebhookHandler

Defines the contract for handling webhook events within the workflow engine, including configuration, execution tracking, and retry metadata.

## API

### Properties

- **`Id`** (string?)
  A unique identifier for the webhook handler instance. Used for tracking and correlation.

- **`Url`** (string?)
  The target URL where the webhook payload will be delivered. Must be a valid HTTPS endpoint for production use.

- **`Events`** (List<string>)
  A list of event types this handler is subscribed to. Events are matched against incoming workflow events to determine delivery.

- **`Secret`** (string?)
  An optional shared secret used to compute a signature for payload verification. If provided, the `X-Signature` header must match the HMAC-SHA256 of the payload using this secret.

- **`CustomHeaders`** (Dictionary<string, string>?)
  Optional HTTP headers to include in the webhook request. Useful for authentication (e.g., `Authorization`) or metadata.

- **`Active`** (bool)
  Indicates whether the webhook handler is enabled. Disabled handlers are skipped during event processing.

- **`CreatedAt`** (DateTime)
  The timestamp when the webhook handler was created or registered in the system.

- **`EventType`** (string?)
  The specific event type being handled by this instance. Used for filtering and routing.

- **`WorkflowId`** (string?)
  The identifier of the workflow associated with this webhook handler. Used for context in logs and debugging.

- **`InstanceId`** (string?)
  The identifier of the workflow instance that triggered this webhook. Enables tracing across distributed systems.

- **`ActivityId`** (string?)
  The identifier of the specific activity within the workflow that generated the event. Useful for granular tracking.

- **`Timestamp`** (DateTime)
  The time when the webhook event was generated or processed by the workflow engine.

- **`Data`** (Dictionary<string, object>?)
  Arbitrary payload data associated with the event. May include workflow state, variables, or custom metadata.

- **`WebhookId`** (string?)
  The identifier of the registered webhook configuration. Used to link attempts to their source.

- **`AttemptedAt`** (DateTime)
  The timestamp when the webhook was last attempted to be delivered. Updated on each retry or initial attempt.

- **`StatusCode`** (int)
  The HTTP status code received from the target URL during delivery. `0` indicates no attempt was made or the request failed to reach the server.

- **`Success`** (bool)
  Indicates whether the last delivery attempt was successful. `true` only if the HTTP status code was in the 2xx range.

- **`ErrorMessage`** (string?)
  A human-readable error message if the delivery failed. Contains exception details or HTTP response body snippets for debugging.

## Usage

### Registering a Webhook Handler
