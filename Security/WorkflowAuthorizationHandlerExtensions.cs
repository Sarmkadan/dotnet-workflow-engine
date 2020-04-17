// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace DotNetWorkflowEngine.Security;

/// <summary>
/// Extension methods for WorkflowAuthorizationHandler providing additional
/// authorization utilities and convenience methods for common workflow scenarios.
/// </summary>
public static class WorkflowAuthorizationHandlerExtensions
{
    /// <summary>
    /// Checks if the current user has the required claim for workflow operations.
    /// </summary>
    /// <param name="handler">The authorization handler instance</param>
    /// <param name="context">The authorization context</param>
    /// <param name="requiredClaim">The claim type to check</param>
    /// <returns>True if the user has the required claim, false otherwise</returns>
    public static bool HasRequiredClaim(
        this WorkflowAuthorizationHandler handler,
        AuthorizationHandlerContext context,
        string requiredClaim)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrEmpty(requiredClaim))
        {
            throw new ArgumentNullException(nameof(requiredClaim));
        }

        var user = context.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return false;
        }

        return user.HasClaim(c => c.Type == requiredClaim);
    }

    /// <summary>
    /// Checks if the current user has the required claim with a specific value.
    /// </summary>
    /// <param name="handler">The authorization handler instance</param>
    /// <param name="context">The authorization context</param>
    /// <param name="requiredClaim">The claim type to check</param>
    /// <param name="requiredClaimValue">The claim value to check</param>
    /// <returns>True if the user has the required claim with the specified value, false otherwise</returns>
    public static bool HasRequiredClaimValue(
        this WorkflowAuthorizationHandler handler,
        AuthorizationHandlerContext context,
        string requiredClaim,
        string requiredClaimValue)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrEmpty(requiredClaim))
        {
            throw new ArgumentNullException(nameof(requiredClaim));
        }

        if (string.IsNullOrEmpty(requiredClaimValue))
        {
            throw new ArgumentNullException(nameof(requiredClaimValue));
        }

        var user = context.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return false;
        }

        return user.HasClaim(c => c.Type == requiredClaim && c.Value == requiredClaimValue);
    }

    /// <summary>
    /// Checks if the current user has the required role for workflow operations.
    /// </summary>
    /// <param name="handler">The authorization handler instance</param>
    /// <param name="context">The authorization context</param>
    /// <param name="requiredRole">The role to check</param>
    /// <returns>True if the user has the required role, false otherwise</returns>
    public static bool HasRequiredRole(
        this WorkflowAuthorizationHandler handler,
        AuthorizationHandlerContext context,
        string requiredRole)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrEmpty(requiredRole))
        {
            throw new ArgumentNullException(nameof(requiredRole));
        }

        var user = context.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return false;
        }

        return user.IsInRole(requiredRole);
    }

    /// <summary>
    /// Gets the user ID from the authorization context.
    /// </summary>
    /// <param name="handler">The authorization handler instance</param>
    /// <param name="context">The authorization context</param>
    /// <returns>The user ID if available, null otherwise</returns>
    public static string? GetUserId(
        this WorkflowAuthorizationHandler handler,
        AuthorizationHandlerContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var user = context.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return null;
        }

        return ClaimsHelper.GetUserId(user);
    }

    /// <summary>
    /// Gets the user's email from the authorization context.
    /// </summary>
    /// <param name="handler">The authorization handler instance</param>
    /// <param name="context">The authorization context</param>
    /// <returns>The user's email if available, null otherwise</returns>
    public static string? GetUserEmail(
        this WorkflowAuthorizationHandler handler,
        AuthorizationHandlerContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var user = context.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return null;
        }

        return ClaimsHelper.GetUserEmail(user);
    }

    /// <summary>
    /// Gets the user's name from the authorization context.
    /// </summary>
    /// <param name="handler">The authorization handler instance</param>
    /// <param name="context">The authorization context</param>
    /// <returns>The user's name if available, null otherwise</returns>
    public static string? GetUserName(
        this WorkflowAuthorizationHandler handler,
        AuthorizationHandlerContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var user = context.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return null;
        }

        return ClaimsHelper.GetUserName(user);
    }
}