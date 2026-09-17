// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Utilities;
using DotNetWorkflowEngine.Exceptions;

namespace DotNetWorkflowEngine.Integration;

/// <summary>
/// Handles delivery of webhook notifications for workflow events.
/// Supports retries with exponential backoff, payload signing for security,
/// and delivery tracking for monitoring and debugging.
/// </summary>
public interface IWebhookHandler
{
    /// <summary>
    /// Registers a webhook endpoint to receive notifications for specific events.
    /// </summary>
    /// <exception cref="ConfigurationException">Thrown when webhook configuration is invalid.</exception>
    Task RegisterWebhookAsync(WebhookRegistration registration);

    /// <summary>
    /// Unregisters a webhook endpoint.
    /// </summary>
    Task UnregisterWebhookAsync(string webhookId);

    /// <summary>
    /// Fires a webhook event, delivering it to all registered endpoints.
    /// </summary>
    /// <exception cref="WorkflowException">Thrown when webhook delivery fails.</exception>
    Task FireWebhookAsync(WorkflowEvent workflowEvent);

    /// <summary>
    /// Gets delivery history for a webhook.
    /// </summary>
    Task<IEnumerable<WebhookDelivery>> GetDeliveryHistoryAsync(string webhookId, int limit = 100);
}

/// <summary>
/// Webhook registration details including URL, events, and security configuration.
/// </summary>
public class WebhookRegistration
{
    /// <summary>Gets or sets the unique identifier of the webhook registration.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the endpoint URL that will receive webhook notifications.</summary>
    public string? Url { get; set; }

    /// <summary>Gets or sets the list of event types this webhook subscribes to (e.g., "workflow.started", "instance.completed").</summary>
    public List<string> Events { get; set; } = new();

    /// <summary>Gets or sets the secret used for HMAC payload signing.</summary>
    public string? Secret { get; set; }

    /// <summary>Gets or sets custom HTTP headers to include with each webhook delivery.</summary>
    public Dictionary<string, string>? CustomHeaders { get; set; }

    /// <summary>Gets or sets a value indicating whether the webhook is active and receiving notifications.</summary>
    public bool Active { get; set; } = true;

    /// <summary>Gets or sets the UTC timestamp when the webhook was registered.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Workflow event that triggers webhook notifications.
/// </summary>
public class WorkflowEvent
{
    /// <summary>Gets or sets the type of event that triggered the webhook notification.</summary>
    public string? EventType { get; set; }

    /// <summary>Gets or sets the identifier of the workflow associated with the event.</summary>
    public string? WorkflowId { get; set; }

    /// <summary>Gets or sets the identifier of the workflow instance associated with the event.</summary>
    public string? InstanceId { get; set; }

    /// <summary>Gets or sets the identifier of the activity associated with the event.</summary>
    public string? ActivityId { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the event occurred.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets additional event-specific data.</summary>
    public Dictionary<string, object>? Data { get; set; }
}

/// <summary>
/// Record of a webhook delivery attempt.
/// </summary>
public class WebhookDelivery
{
    /// <summary>Gets or sets the unique identifier of the delivery attempt.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the identifier of the webhook that was delivered.</summary>
    public string? WebhookId { get; set; }

    /// <summary>Gets or sets the event type that was delivered.</summary>
    public string? EventType { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the delivery was attempted.</summary>
    public DateTime AttemptedAt { get; set; }

    /// <summary>Gets or sets the HTTP status code returned by the delivery attempt.</summary>
    public int StatusCode { get; set; }

    /// <summary>Gets or sets a value indicating whether the delivery attempt succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the error message when the delivery attempt failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the zero-based attempt number for this delivery.</summary>
    public int AttemptNumber { get; set; }
}

/// <summary>
/// Implementation of webhook handler with storage and delivery tracking.
/// </summary>
public class WebhookHandler : IWebhookHandler
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookHandler> _logger;
    private readonly List<WebhookRegistration> _registrations = new();
    private readonly List<WebhookDelivery> _deliveryHistory = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookHandler"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used for webhook delivery.</param>
    /// <param name="logger">The logger used for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClient"/> or <paramref name="logger"/> is null.</exception>
    public WebhookHandler(HttpClient httpClient, ILogger<WebhookHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Registers a webhook for event notifications.
    /// </summary>
    /// <exception cref="ConfigurationException">Thrown when webhook configuration is invalid.</exception>
    public async Task RegisterWebhookAsync(WebhookRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        if (string.IsNullOrEmpty(registration.Url))
            throw new ConfigurationException("Webhook URL is required", "WEBHOOK_URL_REQUIRED");

        if (registration.Events == null || registration.Events.Count == 0)
            throw new ConfigurationException("Webhook must subscribe to at least one event", "WEBHOOK_EVENTS_REQUIRED");

        registration.Id = registration.Id ?? Guid.NewGuid().ToString();
        registration.CreatedAt = DateTime.UtcNow;
        registration.Active = true;

        _registrations.Add(registration);

        _logger.LogInformation(
            "Registered webhook {WebhookId} for events: {Events}",
            registration.Id,
            string.Join(", ", registration.Events));

        await Task.CompletedTask;
    }

    /// <summary>
    /// Unregisters a webhook endpoint.
    /// </summary>
    public async Task UnregisterWebhookAsync(string webhookId)
    {
        ArgumentNullException.ThrowIfNull(webhookId);

        if (string.IsNullOrEmpty(webhookId))
            throw new ArgumentException("Webhook ID cannot be empty", nameof(webhookId));

        var registration = _registrations.FirstOrDefault(w => w.Id == webhookId);
        if (registration != null)
        {
            _registrations.Remove(registration);
            _logger.LogInformation("Unregistered webhook {WebhookId}", webhookId);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Fires a webhook event to all matching registered webhooks.
    /// Uses background tasks for async delivery without blocking caller.
    /// </summary>
    /// <exception cref="WorkflowException">Thrown when webhook delivery fails.</exception>
    public async Task FireWebhookAsync(WorkflowEvent workflowEvent)
    {
        ArgumentNullException.ThrowIfNull(workflowEvent);

        if (string.IsNullOrEmpty(workflowEvent.EventType))
            throw new ConfigurationException("Event type is required", "EVENT_TYPE_REQUIRED");

        var matchingWebhooks = _registrations
            .Where(w => w.Active && w.Events.Contains(workflowEvent.EventType))
            .ToList();

        _logger.LogInformation(
            "Firing webhook event {EventType} to {WebhookCount} registered endpoints",
            workflowEvent.EventType,
            matchingWebhooks.Count);

        foreach (var webhook in matchingWebhooks)
        {
            // Fire webhook delivery in background to avoid blocking caller
            _ = Task.Run(() => DeliverWebhookAsync(webhook, workflowEvent));
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets delivery history for a specific webhook.
    /// </summary>
    public async Task<IEnumerable<WebhookDelivery>> GetDeliveryHistoryAsync(string webhookId, int limit = 100)
    {
        ArgumentNullException.ThrowIfNull(webhookId);

        if (string.IsNullOrEmpty(webhookId))
            throw new ArgumentException("Webhook ID cannot be empty", nameof(webhookId));

        if (limit <= 0)
            throw new ArgumentException("Limit must be positive", nameof(limit));

        var history = _deliveryHistory
            .Where(d => d.WebhookId == webhookId)
            .OrderByDescending(d => d.AttemptedAt)
            .Take(limit)
            .ToList();

        return await Task.FromResult(history.AsEnumerable());
    }

    /// <summary>
    /// Delivers a webhook with retry logic and timeout handling.
    /// Tracks all delivery attempts for monitoring.
    /// </summary>
    private async Task DeliverWebhookAsync(WebhookRegistration webhook, WorkflowEvent workflowEvent)
    {
        if (webhook == null)
            throw new ArgumentNullException(nameof(webhook));

        if (workflowEvent == null)
            throw new ArgumentNullException(nameof(workflowEvent));

        const int maxAttempts = 3;
        int baseDelayMs = 1000;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var payload = SerializationHelper.ToJson(workflowEvent);
                var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

                // Add custom headers if provided
                if (webhook.CustomHeaders != null)
                {
                    foreach (var header in webhook.CustomHeaders)
                        content.Headers.Add(header.Key, header.Value);
                }

                // Add signature if secret is configured
                if (!string.IsNullOrEmpty(webhook.Secret))
                {
                    var signature = GenerateSignature(payload, webhook.Secret);
                    content.Headers.Add("X-Webhook-Signature", signature);
                }

                var response = await _httpClient.PostAsync(webhook.Url, content);

                var delivery = new WebhookDelivery
                {
                    Id = Guid.NewGuid().ToString(),
                    WebhookId = webhook.Id,
                    EventType = workflowEvent.EventType,
                    AttemptedAt = DateTime.UtcNow,
                    StatusCode = (int)response.StatusCode,
                    Success = response.IsSuccessStatusCode,
                    AttemptNumber = attempt
                };

                _deliveryHistory.Add(delivery);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Webhook {WebhookId} delivered successfully: {EventType}",
                        webhook.Id,
                        workflowEvent.EventType);
                    return;
                }

                // Transient error - retry if attempts remain
                if (attempt < maxAttempts && IsTransientError((int)response.StatusCode))
                {
                    var delay = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                    _logger.LogWarning(
                        "Webhook {WebhookId} failed with {StatusCode}. Retrying in {DelayMs}ms",
                        webhook.Id,
                        response.StatusCode,
                        delay);

                    await Task.Delay(delay);
                }
                else
                {
                    delivery.Success = false;
                    delivery.ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
                    _logger.LogError(
                        "Webhook {WebhookId} delivery failed: {StatusCode} {ReasonPhrase}",
                        webhook.Id,
                        response.StatusCode,
                        response.ReasonPhrase);
                    return;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Webhook {WebhookId} delivery exception (attempt {Attempt})", webhook.Id, attempt);

                var delivery = new WebhookDelivery
                {
                    Id = Guid.NewGuid().ToString(),
                    WebhookId = webhook.Id,
                    EventType = workflowEvent.EventType,
                    AttemptedAt = DateTime.UtcNow,
                    Success = false,
                    ErrorMessage = ex.Message,
                    AttemptNumber = attempt
                };

                _deliveryHistory.Add(delivery);

                if (attempt < maxAttempts)
                {
                    var delay = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delay);
                }
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Webhook {WebhookId} delivery timeout (attempt {Attempt})", webhook.Id, attempt);

                var delivery = new WebhookDelivery
                {
                    Id = Guid.NewGuid().ToString(),
                    WebhookId = webhook.Id,
                    EventType = workflowEvent.EventType,
                    AttemptedAt = DateTime.UtcNow,
                    Success = false,
                    ErrorMessage = $"Request timeout: {ex.Message}",
                    AttemptNumber = attempt
                };

                _deliveryHistory.Add(delivery);

                if (attempt < maxAttempts)
                {
                    var delay = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delay);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook {WebhookId} delivery unexpected exception (attempt {Attempt})", webhook.Id, attempt);

                var delivery = new WebhookDelivery
                {
                    Id = Guid.NewGuid().ToString(),
                    WebhookId = webhook.Id,
                    EventType = workflowEvent.EventType,
                    AttemptedAt = DateTime.UtcNow,
                    Success = false,
                    ErrorMessage = ex.Message,
                    AttemptNumber = attempt
                };

                _deliveryHistory.Add(delivery);

                if (attempt < maxAttempts)
                {
                    var delay = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delay);
                }
            }
        }

        throw new WorkflowException(
            $"Webhook delivery failed after {maxAttempts} attempts for event {workflowEvent.EventType}",
            "WEBHOOK_DELIVERY_FAILED",
            null,
            null);
    }

    /// <summary>
    /// Generates HMAC-SHA256 signature for webhook payload.
    /// </summary>
    private string GenerateSignature(string payload, string secret)
    {
        if (string.IsNullOrEmpty(payload))
            throw new ArgumentException("Payload cannot be null or empty", nameof(payload));

        if (string.IsNullOrEmpty(secret))
            throw new ArgumentException("Secret cannot be null or empty", nameof(secret));

        using (var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret)))
        {
            var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
            return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
        }
    }

    /// <summary>
    /// Determines if an HTTP status code represents a transient error worth retrying.
    /// </summary>
    private bool IsTransientError(int statusCode)
    {
        return statusCode == 408 // Request timeout
            || statusCode == 429 // Too many requests
            || statusCode >= 500; // Server errors
    }
}