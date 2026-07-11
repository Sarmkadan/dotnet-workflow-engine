// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace DotNetWorkflowEngine.Security;

/// <summary>
/// Extension methods for <see cref="WorkflowAuthorizationHandler"/> providing additional
/// authorization utilities and convenience methods for common workflow scenarios.
/// </summary>
public static class WorkflowAuthorizationHandlerExtensions
{
    /// <summary>
    /// Checks if the current user has the required claim for workflow operations.
    /// </summary>
    /// <param name="handler">The authorization handler instance.</param>
    /// <param name="context">The authorization context.</param>
    /// <param name="requiredClaim">The claim type to check.</param>
    /// <returns>True if the user has the required claim; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> or <paramref name="requiredClaim"/> is null or empty.</exception>
    public static bool HasRequiredClaim(
        this WorkflowAuthorizationHandler handler,
        AuthorizationHandlerContext context,
        string requiredClaim)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNullOrEmpty(requiredClaim);

        var user = context.User;
        return user.Identity?.IsAuthenticated == true && user.HasClaim(c => c.Type == requiredClaim);
    }

    /// <summary>
    /// Checks if the current user has the required claim with a specific value.
    /// </summary>
    /// <param name="handler">The authorization handler instance.</param>
    /// <param name="context">The authorization context.</param>
    /// <param name="requiredClaim">The claim type to check.</param>
    /// <param name="requiredClaimValue">The claim value to check.</param>
    /// <returns>True if the user has the required claim with the specified value; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/>, <paramref name="requiredClaim"/>, or <paramref name="requiredClaimValue"/> is null or empty.</exception>
    public static bool HasRequiredClaimValue(
        this WorkflowAuthorizationHandler handler,
        AuthorizationHandlerContext context,
        string requiredClaim,
        string requiredClaimValue)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNullOrEmpty(requiredClaim);
        ArgumentNullException.ThrowIfNullOrEmpty(requiredClaimValue);

        var user = context.User;
        return user.Identity?.IsAuthenticated == true
            && user.HasClaim(c => c.Type == requiredClaim && c.Value == requiredClaimValue);
    }

    /// <summary>
    /// Checks if the current user has the required role for workflow operations.
    /// </summary>
    /// <param name="handler">The authorization handler instance.</param>
    /// <param name="context">The authorization context.</param>
    /// <param name="requiredRole">The role to check.</param>
    /// <returns>True if the user has the required role; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> or <paramref name="requiredRole"/> is null or empty.</exception>
    public static bool HasRequiredRole(
        this WorkflowAuthorizationHandler handler,
        AuthorizationHandlerContext context,
        string requiredRole)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNullOrEmpty(requiredRole);

        var user = context.User;
        return user.Identity?.IsAuthenticated == true && user.IsInRole(requiredRole);
    }

    /// <summary>
    /// Gets the user ID from the authorization context.
    /// </summary>
    /// <param name="handler">The authorization handler instance.</param>
    /// <param name="context">The authorization context.</param>
    /// <returns>The user ID if available; otherwise, null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    public static string? GetUserId(
        this WorkflowAuthorizationHandler handler,
        AuthorizationHandlerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var user = context.User;
        return user.Identity?.IsAuthenticated == true ? ClaimsHelper.GetUserId(user) : null;
    }

    /// <summary>
    /// Gets the user's email from the authorization context.
    /// </summary>
    /// <param name="handler">The authorization handler instance.</param>
    /// <param name="context">The authorization context.</param>
    /// <returns>The user's email if available; otherwise, null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    public static string? GetUserEmail(
        this WorkflowAuthorizationHandler handler,
        AuthorizationHandlerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var user = context.User;
        return user.Identity?.IsAuthenticated == true ? ClaimsHelper.GetUserEmail(user) : null;
    }

    /// <summary>
    /// Gets the user's name from the authorization context.
    /// </summary>
    /// <param name="handler">The authorization handler instance.</param>
    /// <param name="context">The authorization context.</param>
    /// <returns>The user's name if available; otherwise, null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    public static string? GetUserName(
        this WorkflowAuthorizationHandler handler,
        AuthorizationHandlerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var user = context.User;
        return user.Identity?.IsAuthenticated == true ? ClaimsHelper.GetUserName(user) : null;
    }
}