// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System.Collections.Concurrent;

namespace DotNetWorkflowEngine.Services;

/// <summary>
/// Registry for managing workflow instance subscriptions to external messages.
/// Tracks which workflow instances are waiting for which messages using correlation keys.
/// </summary>
public class MessageSubscriptionRegistry
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _waitingInstances = new();

    /// <summary>
    /// Registers a workflow instance as waiting for a specific message.
    /// </summary>
    /// <param name="correlationKey">The correlation key to match messages against.</param>
    /// <param name="messageName">The name of the message being waited for.</param>
    /// <param name="instanceId">The ID of the workflow instance.</param>
    public void RegisterSubscription(string correlationKey, string messageName, string instanceId)
    {
        if (string.IsNullOrWhiteSpace(correlationKey))
            throw new ArgumentException("Correlation key cannot be empty", nameof(correlationKey));

        if (string.IsNullOrWhiteSpace(messageName))
            throw new ArgumentException("Message name cannot be empty", nameof(messageName));

        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance ID cannot be empty", nameof(instanceId));

        var correlationDict = _waitingInstances.GetOrAdd(correlationKey, _ => new ConcurrentDictionary<string, string>());
        correlationDict.AddOrUpdate(messageName, instanceId, (_, _) => instanceId);
    }

    /// <summary>
    /// Unregisters a workflow instance from waiting for a specific message.
    /// </summary>
    /// <param name="correlationKey">The correlation key.</param>
    /// <param name="messageName">The message name.</param>
    /// <param name="instanceId">The instance ID to remove.</param>
    /// <returns>True if the instance was found and removed, false otherwise.</returns>
    public bool UnregisterSubscription(string correlationKey, string messageName, string instanceId)
    {
        if (string.IsNullOrWhiteSpace(correlationKey) || string.IsNullOrWhiteSpace(messageName) || string.IsNullOrWhiteSpace(instanceId))
            return false;

        if (_waitingInstances.TryGetValue(correlationKey, out var correlationDict))
        {
            return correlationDict.TryRemove(messageName, out var existingInstanceId) && existingInstanceId == instanceId;
        }

        return false;
    }

    /// <summary>
    /// Gets all workflow instances waiting for a message with the given correlation key and message name.
    /// </summary>
    /// <param name="correlationKey">The correlation key.</param>
    /// <param name="messageName">The message name.</param>
    /// <returns>A collection of instance IDs waiting for the message.</returns>
    public IEnumerable<string> GetWaitingInstances(string correlationKey, string messageName)
    {
        if (string.IsNullOrWhiteSpace(correlationKey) || string.IsNullOrWhiteSpace(messageName))
            return Enumerable.Empty<string>();

        if (_waitingInstances.TryGetValue(correlationKey, out var correlationDict))
        {
            if (correlationDict.TryGetValue(messageName, out var instanceId))
            {
                return new[] { instanceId };
            }
        }

        return Enumerable.Empty<string>();
    }

    /// <summary>
    /// Clears all subscriptions for a specific workflow instance.
    /// </summary>
    /// <param name="instanceId">The instance ID to clear subscriptions for.</param>
    public void ClearInstanceSubscriptions(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            return;

        foreach (var correlationDict in _waitingInstances.Values)
        {
            foreach (var messageName in correlationDict.Keys.ToList())
            {
                if (correlationDict.TryGetValue(messageName, out var existingInstanceId) && existingInstanceId == instanceId)
                {
                    correlationDict.TryRemove(messageName, out _);
                }
            }
        }
    }
}