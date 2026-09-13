// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using DotNetWorkflowEngine.Events;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Exceptions;

namespace DotNetWorkflowEngine.Services;

/// <summary>
/// Handles external workflow messages by correlating them with waiting workflow
/// instances and resuming the matching instances.
/// </summary>
public class MessageEventService
{
    private readonly IEventBus _eventBus;
    private readonly WorkflowExecutionService _workflowExecutionService;
    private readonly AuditService _auditService;
    private readonly MessageSubscriptionRegistry _subscriptionRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageEventService"/> class.
    /// </summary>
    /// <param name="eventBus">The event bus used to publish message-received events.</param>
    /// <param name="workflowExecutionService">The service used to locate and resume workflow instances.</param>
    /// <param name="auditService">The service used to record message-processing outcomes.</param>
    /// <param name="subscriptionRegistry">The registry that tracks workflow message subscriptions.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="eventBus"/>, <paramref name="workflowExecutionService"/>,
    /// <paramref name="auditService"/>, or <paramref name="subscriptionRegistry"/> is <see langword="null"/>.
    /// </exception>
    public MessageEventService(IEventBus eventBus, WorkflowExecutionService workflowExecutionService, AuditService auditService, MessageSubscriptionRegistry subscriptionRegistry)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        _eventBus = eventBus;

        ArgumentNullException.ThrowIfNull(workflowExecutionService);
        _workflowExecutionService = workflowExecutionService;

        ArgumentNullException.ThrowIfNull(auditService);
        _auditService = auditService;

        ArgumentNullException.ThrowIfNull(subscriptionRegistry);
        _subscriptionRegistry = subscriptionRegistry;
    }

    /// <summary>
    /// Publishes an external message and attempts to resume a waiting workflow instance
    /// with the same message name and correlation key.
    /// </summary>
    /// <param name="message">The workflow message to publish and correlate.</param>
    /// <returns>
    /// A task whose result is <see langword="true"/> when a matching workflow instance is resumed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ValidationException">
    /// Thrown when the message name or correlation key is missing or consists only of white-space characters.
    /// </exception>
    /// <exception cref="WorkflowException">Thrown when workflow message processing fails.</exception>
    public async Task<bool> PublishMessageAsync(IWorkflowMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(message.MessageName))
            throw new ValidationException("Message name cannot be empty", "MESSAGE_NAME_REQUIRED");

        if (string.IsNullOrWhiteSpace(message.CorrelationKey))
            throw new ValidationException("Correlation key cannot be empty", "CORRELATION_KEY_REQUIRED");

        // Publish a generic message received event
        var messageReceivedEvent = new MessageReceivedEvent
        {
            CorrelationKey = message.CorrelationKey,
            MessageName = message.MessageName,
            Payload = message.Payload,
            Timestamp = DateTime.UtcNow
        };
        await _eventBus.PublishAsync(messageReceivedEvent);

        // Attempt to find and resume a waiting instance
        var waitingInstance = _workflowExecutionService.GetInstancesByCorrelation(message.CorrelationKey)
            .FirstOrDefault(i => i.Status == WorkflowStatus.WaitingForMessage &&
                i.GetContextVariable("WaitingForMessageName")?.ToString() == message.MessageName);

        if (waitingInstance != null)
        {
            try
            {
                await _workflowExecutionService.ResumeFromMessageAsync(
                    waitingInstance.Id,
                    message.MessageName,
                    message.CorrelationKey,
                    message.Payload
                );
                messageReceivedEvent.InstanceId = waitingInstance.Id;
                messageReceivedEvent.WorkflowResumed = true;
                return true;
            }
            catch (Exception ex)
            {
                await _auditService.LogCustomEvent(
                    waitingInstance.Id,
                    "MessageResumeFailed",
                    $"Failed to resume workflow instance with message '{message.MessageName}' and key '{message.CorrelationKey}': {ex.Message}",
                    "Error"
                );
                await _auditService.LogInstanceFailed(
                    waitingInstance.Id,
                    $"Message resume failed at MessageCatchEvent: {ex.Message}"
                );
                _workflowExecutionService.FailInstance(
                    waitingInstance.Id,
                    $"Message resume failed at MessageCatchEvent: {ex.Message}"
                );
                throw; // Re-throw to propagate the original error
            }
        }
        else
        {
            await _auditService.LogCustomEvent(
                string.Empty,
                "UncorrelatedMessage",
                $"Received message '{message.MessageName}' with key '{message.CorrelationKey}' but no waiting workflow instance found.",
                "Warning"
            );
        }

        return false;
    }
}
