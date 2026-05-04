// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace DotNetWorkflowEngine.Security;

/// <summary>
/// Custom authorization handler for workflow-specific permissions.
/// Implements fine-grained access control for workflow operations based on
/// user claims, roles, and resource ownership.
/// </summary>
public class WorkflowAuthorizationHandler : AuthorizationHandler<WorkflowRequirement>
{
    private readonly ILogger<WorkflowAuthorizationHandler> _logger;

    public WorkflowAuthorizationHandler(ILogger<WorkflowAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles authorization for workflow-specific requirements.
    /// Checks user claims and resource permissions.
    /// </summary>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WorkflowRequirement requirement)
    {
        var user = context.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("Authorization failed: User is not authenticated");
            context.Fail();
            return Task.CompletedTask;
        }

        // Check required claim
        if (!string.IsNullOrEmpty(requirement.RequiredClaim))
        {
            var hasClaim = user.HasClaim(c =>
                c.Type == requirement.RequiredClaim &&
                (!string.IsNullOrEmpty(requirement.RequiredClaimValue) ?
                    c.Value == requirement.RequiredClaimValue :
                    true));

            if (!hasClaim)
            {
                _logger.LogWarning(
                    "Authorization failed: Missing required claim {Claim}",
                    requirement.RequiredClaim);
                context.Fail();
                return Task.CompletedTask;
            }
        }

        // Check required role
        if (!string.IsNullOrEmpty(requirement.RequiredRole))
        {
            var hasRole = user.IsInRole(requirement.RequiredRole);

            if (!hasRole)
            {
                _logger.LogWarning(
                    "Authorization failed: User not in required role {Role}",
                    requirement.RequiredRole);
                context.Fail();
                return Task.CompletedTask;
            }
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Authorization requirement for workflow operations.
/// Specifies what claims/roles are needed to perform an operation.
/// </summary>
public class WorkflowRequirement : IAuthorizationRequirement
{
    public string? RequiredClaim { get; set; }
    public string? RequiredClaimValue { get; set; }
    public string? RequiredRole { get; set; }

    public WorkflowRequirement() { }

    public WorkflowRequirement(string requiredClaim, string? requiredClaimValue = null)
    {
        RequiredClaim = requiredClaim;
        RequiredClaimValue = requiredClaimValue;
    }

    public WorkflowRequirement(string requiredClaim, string requiredClaimValue, string requiredRole)
    {
        RequiredClaim = requiredClaim;
        RequiredClaimValue = requiredClaimValue;
        RequiredRole = requiredRole;
    }
}

/// <summary>
/// Extension methods for authorization policy registration.
/// </summary>
public static class AuthorizationPolicies
{
    public const string CanCreateWorkflow = "CanCreateWorkflow";
    public const string CanExecuteWorkflow = "CanExecuteWorkflow";
    public const string CanViewAudit = "CanViewAudit";
    public const string IsAdministrator = "IsAdministrator";

    /// <summary>
    /// Registers workflow-specific authorization policies.
    /// Call this during startup in ConfigureServices.
    /// </summary>
    public static IServiceCollection AddWorkflowAuthorizationPolicies(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Policy to create workflows - requires "workflow:create" claim
            options.AddPolicy(CanCreateWorkflow,
                policy => policy.Requirements.Add(
                    new WorkflowRequirement("workflow:create")));

            // Policy to execute workflows - requires "workflow:execute" claim or admin role
            options.AddPolicy(CanExecuteWorkflow,
                policy =>
                {
                    policy.Requirements.Add(new WorkflowRequirement("workflow:execute"));
                });

            // Policy to view audit logs - requires "audit:read" claim
            options.AddPolicy(CanViewAudit,
                policy => policy.Requirements.Add(
                    new WorkflowRequirement("audit:read")));

            // Policy for administrators
            options.AddPolicy(IsAdministrator,
                policy => policy.RequireRole("Administrator"));
        });

        services.AddScoped<IAuthorizationHandler, WorkflowAuthorizationHandler>();

        return services;
    }
}

/// <summary>
/// Helper for working with user claims and permissions.
/// </summary>
public class ClaimsHelper
{
    /// <summary>
    /// Gets the user ID from JWT claims.
    /// </summary>
    public static string? GetUserId(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;
    }

    /// <summary>
    /// Gets the user's email from JWT claims.
    /// </summary>
    public static string? GetUserEmail(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Gets the user's name from JWT claims.
    /// </summary>
    public static string? GetUserName(ClaimsPrincipal user)
    {
        return user.Identity?.Name
            ?? user.FindFirst(ClaimTypes.Name)?.Value;
    }

    /// <summary>
    /// Checks if a user has a specific claim with a value.
    /// </summary>
    public static bool HasClaim(ClaimsPrincipal user, string claimType, string claimValue)
    {
        return user.HasClaim(c => c.Type == claimType && c.Value == claimValue);
    }

    /// <summary>
    /// Checks if a user has any claim of a specific type.
    /// </summary>
    public static bool HasClaimType(ClaimsPrincipal user, string claimType)
    {
        return user.HasClaim(c => c.Type == claimType);
    }
}

/// <summary>
/// Custom authorization attribute for workflow operations.
/// Use on controller actions to enforce specific authorization policies.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class WorkflowAuthorizeAttribute : Microsoft.AspNetCore.Authorization.AuthorizeAttribute
{
    public WorkflowAuthorizeAttribute() { }

    public WorkflowAuthorizeAttribute(string policy)
    {
        Policy = policy;
    }
}
