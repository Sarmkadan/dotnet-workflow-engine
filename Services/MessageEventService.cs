// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using DotNetWorkflowEngine.Events;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Enums;
using System.Collections.Concurrent;
using DotNetWorkflowEngine.Exceptions;

namespace DotNetWorkflowEngine.Services;

/// <summary>
/// Service responsible for handling external messages, correlating them with
/// waiting workflow instances, and resuming those instances.
/// </summary>
public class MessageEventService
{
    private readonly IEventBus _eventBus;
    private readonly WorkflowExecutionService _workflowExecutionService;
    private readonly AuditService _auditService;

    // A mapping of CorrelationKey to a list of InstanceIds that are currently waiting for a message
    // with that correlation key and message name. This is an in-memory solution.
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _waitingInstances = new();

    /// <summary>
    /// Initializes the message event service.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
    public MessageEventService(IEventBus eventBus, WorkflowExecutionService workflowExecutionService, AuditService auditService)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _workflowExecutionService = workflowExecutionService ?? throw new ArgumentNullException(nameof(workflowExecutionService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    /// <summary>
    /// Publishes an external message to the workflow engine, attempting to correlate it
    /// with a waiting workflow instance and resume its execution.
    /// </summary>
    /// <param name="message">The incoming message.</param>
    /// <returns>True if a workflow was successfully correlated and resumed, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when message is null.</exception>
    /// <exception cref="WorkflowException">Thrown when message processing fails.</exception>
    public async Task<bool> PublishMessageAsync(IWorkflowMessage message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

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