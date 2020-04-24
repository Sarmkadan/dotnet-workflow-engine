using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using DotNetWorkflowEngine.Models;
using ExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

namespace DotNetWorkflowEngine.Services
{
    /// <summary>
    /// Extension methods for <see cref="ActivityService"/> that provide additional functionality
    /// for activity handling, validation, and execution management.
    /// </summary>
    public static class ActivityServiceExtensions
    {
        /// <summary>
        /// Determines whether the specified activity type is currently registered for handling.
        /// </summary>
        /// <param name="service">The activity service instance.</param>
        /// <param name="handlerType">Type of the handler to check.</param>
        /// <returns>True if the handler type is registered; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="handlerType"/> is null.</exception>
        public static bool IsHandlerRegistered(this ActivityService service, string handlerType)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentException.ThrowIfNullOrEmpty(handlerType);

            return service.GetRegisteredHandlerTypes().Contains(handlerType, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates an activity from the specified handler type and input data.
        /// </summary>
        /// <param name="service">The activity service instance.</param>
        /// <param name="handlerType">Type of the handler to execute.</param>
        /// <param name="input">The input data for the activity.</param>
        /// <returns>An activity instance configured with the handler type and input.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="handlerType"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="handlerType"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the handler type is not registered.</exception>
        public static Activity CreateActivity(
            this ActivityService service,
            string handlerType,
            object input = null)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentException.ThrowIfNullOrEmpty(handlerType);

            if (!service.IsHandlerRegistered(handlerType))
            {
                throw new InvalidOperationException(
                    $"Handler type '{handlerType}' is not registered. Call RegisterHandler first.");
            }

            var activity = new Activity
            {
                Id = Guid.NewGuid().ToString(),
                HandlerType = handlerType
            };

            // Set input parameters if provided
            if (input != null)
            {
                activity.InputParameters["input"] = input;
            }

            return activity;
        }

        /// <summary>
        /// Validates all registered activity handlers to ensure they can be instantiated and initialized.
        /// </summary>
        /// <param name="service">The activity service instance.</param>
        /// <returns>A dictionary mapping handler types to validation results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="service"/> is null.</exception>
        public static IReadOnlyDictionary<string, bool> ValidateAllHandlers(this ActivityService service)
        {
            ArgumentNullException.ThrowIfNull(service);

            var handlers = service.GetRegisteredHandlerTypes();
            var results = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            foreach (var handlerType in handlers)
            {
                try
                {
                    // Create a test activity and validate it
                    var activity = new Activity
                    {
                        Id = Guid.NewGuid().ToString(),
                        HandlerType = handlerType
                    };

                    var isValid = service.ValidateActivity(activity, out _);
                    results[handlerType] = isValid;
                }
                catch
                {
                    results[handlerType] = false;
                }
            }

            return results.AsReadOnly();
        }

        /// <summary>
        /// Gets the count of currently registered activity handlers.
        /// </summary>
        /// <param name="service">The activity service instance.</param>
        /// <returns>The number of registered activity handlers.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="service"/> is null.</exception>
        public static int GetHandlerCount(this ActivityService service)
        {
            ArgumentNullException.ThrowIfNull(service);

            return service.GetRegisteredHandlerTypes().Count;
        }

        /// <summary>
        /// Executes multiple activities asynchronously in parallel and returns a combined result.
        /// </summary>
        /// <param name="service">The activity service instance.</param>
        /// <param name="activities">Collection of activity executions to perform.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of activity results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="activities"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="activities"/> contains null elements.</exception>
        public static async Task<IReadOnlyList<ActivityResult>> ExecuteActivitiesAsync(
            this ActivityService service,
            IEnumerable<(string HandlerType, object Input)> activities,
            ExecutionContext context)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentNullException.ThrowIfNull(activities);
            ArgumentNullException.ThrowIfNull(context);

            var tasks = activities
                .Select(activity => ExecuteActivityAsync(service, activity.HandlerType, activity.Input, context))
                .ToList();

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            return results.AsReadOnly();
        }

        /// <summary>
        /// Executes an activity asynchronously with the specified handler type and returns the result.
        /// </summary>
        /// <param name="service">The activity service instance.</param>
        /// <param name="handlerType">Type of the handler to execute.</param>
        /// <param name="input">The input data for the activity.</param>
        /// <param name="context">The execution context.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the activity result.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="service"/>, <paramref name="handlerType"/>, or <paramref name="context"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="handlerType"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the handler type is not registered.</exception>
        public static async Task<ActivityResult> ExecuteActivityAsync(
            this ActivityService service,
            string handlerType,
            object input,
            ExecutionContext context)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentException.ThrowIfNullOrEmpty(handlerType);
            ArgumentNullException.ThrowIfNull(context);

            if (!service.IsHandlerRegistered(handlerType))
            {
                throw new InvalidOperationException(
                    $"Handler type '{handlerType}' is not registered. Call RegisterHandler first.");
            }

            var activity = new Activity
            {
                Id = Guid.NewGuid().ToString(),
                HandlerType = handlerType
            };

            // Set input parameters if provided
            if (input != null)
            {
                activity.InputParameters["input"] = input;
            }

            return await service.ExecuteAsync(activity, context).ConfigureAwait(false);
        }
    }
}