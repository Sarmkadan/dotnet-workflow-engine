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
    public string? Id { get; set; }
    public string? Url { get; set; }
    public List<string> Events { get; set; } = new(); // e.g., "workflow.started", "instance.completed"
    public string? Secret { get; set; } // Used for HMAC signing
    public Dictionary<string, string>? CustomHeaders { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Workflow event that triggers webhook notifications.
/// </summary>
public class WorkflowEvent
{
    public string? EventType { get; set; }
    public string? WorkflowId { get; set; }
    public string? InstanceId { get; set; }
    public string? ActivityId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? Data { get; set; }
}

/// <summary>
/// Record of a webhook delivery attempt.
/// </summary>
public class WebhookDelivery
{
    public string? Id { get; set; }
    public string? WebhookId { get; set; }
    public string? EventType { get; set; }
    public DateTime AttemptedAt { get; set; }
    public int StatusCode { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
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

    public WebhookHandler(HttpClient httpClient, ILogger<WebhookHandler> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a webhook for event notifications.
    /// </summary>
    /// <exception cref="ConfigurationException">Thrown when webhook configuration is invalid.</exception>
    public async Task RegisterWebhookAsync(WebhookRegistration registration)
    {
        if (registration == null)
            throw new ArgumentNullException(nameof(registration));

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
        if (string.IsNullOrEmpty(webhookId))
            throw new ArgumentException("Webhook ID cannot be null or empty", nameof(webhookId));

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
        if (workflowEvent == null)
            throw new ArgumentNullException(nameof(workflowEvent));

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
        if (string.IsNullOrEmpty(webhookId))
            throw new ArgumentException("Webhook ID cannot be null or empty", nameof(webhookId));

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