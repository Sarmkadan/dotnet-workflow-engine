// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
//
// Extension methods for ParallelExecutionExample to provide additional functionality
// for working with parallel workflow execution patterns.
// =============================================================================

using System.Globalization;
using System.Text.RegularExpressions;
using DotNetWorkflowEngine.Models;
using Microsoft.AspNetCore.Mvc;

namespace DotNetWorkflowEngine.Examples;

/// <summary>
/// Provides extension methods for <see cref="ParallelExecutionExample"/> to enhance
/// parallel workflow execution capabilities with validation, transformation, and
/// result processing utilities.
/// </summary>
public static class ParallelExecutionExampleExtensions
{
    /// <summary>
    /// Validates that the order data contains all required fields for parallel execution.
    /// </summary>
    /// <param name="example">The ParallelExecutionExample instance.</param>
    /// <param name="orderData">The order data to validate.</param>
    /// <returns>ActionResult indicating validation success or failure.</returns>
    /// <exception cref="ArgumentNullException">Thrown when orderData is null.</exception>
    public static async Task<ActionResult> ValidateOrderDataAsync(
        this ParallelExecutionExample example,
        OrderData orderData)
    {
        ArgumentNullException.ThrowIfNull(orderData);

        if (string.IsNullOrWhiteSpace(orderData.OrderId))
        {
            return new BadRequestObjectResult(new { error = "OrderId is required" });
        }

        if (orderData.Items is not { Count: > 0 })
        {
            return new BadRequestObjectResult(new { error = "At least one order item is required" });
        }

        ArgumentException.ThrowIfNullOrEmpty(orderData.ShippingAddress, nameof(orderData.ShippingAddress));
        ArgumentException.ThrowIfNullOrEmpty(orderData.PaymentMethod, nameof(orderData.PaymentMethod));
        ArgumentException.ThrowIfNullOrEmpty(orderData.CustomerEmail, nameof(orderData.CustomerEmail));

        if (!IsValidEmail(orderData.CustomerEmail))
        {
            return new BadRequestObjectResult(new { error = "CustomerEmail must be a valid email address" });
        }

        return new OkResult();
    }

    /// <summary>
    /// Validates an email address format.
    /// </summary>
    /// <param name="email">The email address to validate.</param>
    /// <returns>True if the email is valid; otherwise, false.</returns>
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            // More robust email validation using regex
            var emailRegex = new Regex(
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
                TimeSpan.FromMilliseconds(250));
            return emailRegex.IsMatch(email);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    /// <summary>
    /// Calculates the total order value from the order items.
    /// </summary>
    /// <param name="example">The ParallelExecutionExample instance.</param>
    /// <param name="orderData">The order data containing items.</param>
    /// <returns>The total order value as decimal.</returns>
    /// <exception cref="ArgumentNullException">Thrown when orderData is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when orderData.Items is null.</exception>
    public static decimal CalculateOrderTotal(
        this ParallelExecutionExample example,
        OrderData orderData)
    {
        ArgumentNullException.ThrowIfNull(orderData);
        ArgumentNullException.ThrowIfNull(orderData.Items);

        return orderData.Items.Sum(static item => item.Quantity * item.Price);
    }

    /// <summary>
    /// Creates a standardized order ID from the provided order data.
    /// </summary>
    /// <param name="example">The ParallelExecutionExample instance.</param>
    /// <param name="orderData">The order data containing order information.</param>
    /// <returns>A standardized order ID string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when orderData is null.</exception>
    /// <exception cref="ArgumentException">Thrown when CustomerEmail is null or empty.</exception>
    public static string CreateStandardizedOrderId(
        this ParallelExecutionExample example,
        OrderData orderData)
    {
        ArgumentNullException.ThrowIfNull(orderData);
        ArgumentException.ThrowIfNullOrEmpty(orderData.CustomerEmail, nameof(orderData.CustomerEmail));

        // Create a standardized order ID: ORDER-{timestamp}-{customerEmailHash}
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var emailHash = orderData.CustomerEmail.GetHashCode().ToString("X8", CultureInfo.InvariantCulture);
        var orderId = $"ORDER-{timestamp}-{emailHash}";

        return orderId;
    }

    /// <summary>
    /// Generates a shipping label format from the parallel execution results.
    /// </summary>
    /// <param name="example">The ParallelExecutionExample instance.</param>
    /// <param name="results">The parallel execution results containing shipping information.</param>
    /// <returns>A formatted shipping label string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when results is null.</exception>
    public static string GenerateShippingLabel(
        this ParallelExecutionExample example,
        ParallelExecutionResults results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var shippingAddress = results.ShippingAddress ?? "N/A";
        var orderId = results.OrderId ?? "Unknown";
        var shippingCost = results.ShippingCost?.ToString("F2", CultureInfo.InvariantCulture) ?? "$0.00";

        return $$
"""SHIPPING LABEL
========================================
Order ID: {{orderId}}
Shipping To: {{shippingAddress}}
Shipping Cost: {{shippingCost}}
Status: {{results.ProcessingStatus}}
Generated: {{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}}
========================================
""";
    }
}

/// <summary>
/// Container for parallel execution results from ParallelExecutionExample.
/// </summary>
public sealed class ParallelExecutionResults
{
    public string? OrderId { get; set; }
    public bool? InventoryValid { get; set; }
    public bool? PaymentValid { get; set; }
    public decimal? ShippingCost { get; set; }
    public string? AppliedPromotion { get; set; }
    public decimal? FinalTotal { get; set; }
    public string? ProcessingStatus { get; set; }
    public string? ShippingAddress { get; set; }
}