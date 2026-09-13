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

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowAuthorizationHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger used to record authorization failures.</param>
    public WorkflowAuthorizationHandler(ILogger<WorkflowAuthorizationHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Returns a string representation of the authorization handler.
    /// </summary>
    /// <returns>A string that represents the authorization handler.</returns>
    public override string ToString()
    {
        return $"WorkflowAuthorizationHandler {{ RequiredClaim = {string.Empty}, RequiredClaimValue = {string.Empty}, RequiredRole = {string.Empty} }}";
    }

    /// <summary>
    /// Handles authorization for workflow-specific requirements.
    /// Checks user claims and resource permissions.
    /// </summary>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WorkflowRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        var user = context.User;

        if (user == null || user.Identity?.IsAuthenticated != true)
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
    /// <summary>
    /// Gets or sets the claim type required for authorization.
    /// </summary>
    public string? RequiredClaim { get; set; }

    /// <summary>
    /// Gets or sets the value that the required claim must contain.
    /// </summary>
    public string? RequiredClaimValue { get; set; }

    /// <summary>
    /// Gets or sets the role required for authorization.
    /// </summary>
    public string? RequiredRole { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowRequirement"/> class.
    /// </summary>
    public WorkflowRequirement() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowRequirement"/> class with a required claim.
    /// </summary>
    /// <param name="requiredClaim">The claim type required for authorization.</param>
    /// <param name="requiredClaimValue">The optional value that the required claim must contain.</param>
    public WorkflowRequirement(string requiredClaim, string? requiredClaimValue = null)
    {
        ArgumentNullException.ThrowIfNull(requiredClaim);
        RequiredClaim = requiredClaim;
        RequiredClaimValue = requiredClaimValue;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowRequirement"/> class with a required claim and role.
    /// </summary>
    /// <param name="requiredClaim">The claim type required for authorization.</param>
    /// <param name="requiredClaimValue">The value that the required claim must contain.</param>
    /// <param name="requiredRole">The role required for authorization.</param>
    public WorkflowRequirement(string requiredClaim, string requiredClaimValue, string requiredRole)
    {
        ArgumentNullException.ThrowIfNull(requiredClaim);
        ArgumentNullException.ThrowIfNull(requiredClaimValue);
        ArgumentNullException.ThrowIfNull(requiredRole);
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
    /// <summary>
    /// The name of the policy that authorizes workflow creation.
    /// </summary>
    public const string CanCreateWorkflow = "CanCreateWorkflow";

    /// <summary>
    /// The name of the policy that authorizes workflow execution.
    /// </summary>
    public const string CanExecuteWorkflow = "CanExecuteWorkflow";

    /// <summary>
    /// The name of the policy that authorizes access to audit information.
    /// </summary>
    public const string CanViewAudit = "CanViewAudit";

    /// <summary>
    /// The name of the policy that requires the administrator role.
    /// </summary>
    public const string IsAdministrator = "IsAdministrator";

    /// <summary>
    /// Registers workflow-specific authorization policies.
    /// Call this during startup in ConfigureServices.
    /// </summary>
    /// <param name="services">The service collection to which authorization services are added.</param>
    /// <returns>The service collection so that additional calls can be chained.</returns>
    public static IServiceCollection AddWorkflowAuthorizationPolicies(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
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
    /// <param name="user">The claims principal whose identifier is retrieved.</param>
    /// <returns>The name identifier or subject claim value, or <see langword="null"/> if neither is present.</returns>
    public static string? GetUserId(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;
    }

    /// <summary>
    /// Gets the user's email from JWT claims.
    /// </summary>
    /// <param name="user">The claims principal whose email is retrieved.</param>
    /// <returns>The email claim value, or <see langword="null"/> if it is not present.</returns>
    public static string? GetUserEmail(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Gets the user's name from JWT claims.
    /// </summary>
    /// <param name="user">The claims principal whose name is retrieved.</param>
    /// <returns>The identity name or name claim value, or <see langword="null"/> if neither is present.</returns>
    public static string? GetUserName(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.Identity?.Name
            ?? user.FindFirst(ClaimTypes.Name)?.Value;
    }

    /// <summary>
    /// Checks if a user has a specific claim with a value.
    /// </summary>
    /// <param name="user">The claims principal to inspect.</param>
    /// <param name="claimType">The claim type to find.</param>
    /// <param name="claimValue">The value that the claim must contain.</param>
    /// <returns><see langword="true"/> if the user has a matching claim; otherwise, <see langword="false"/>.</returns>
    public static bool HasClaim(ClaimsPrincipal user, string claimType, string claimValue)
    {
        return user.HasClaim(c => c.Type == claimType && c.Value == claimValue);
    }

    /// <summary>
    /// Checks if a user has any claim of a specific type.
    /// </summary>
    /// <param name="user">The claims principal to inspect.</param>
    /// <param name="claimType">The claim type to find.</param>
    /// <returns><see langword="true"/> if the user has a claim of the specified type; otherwise, <see langword="false"/>.</returns>
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
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowAuthorizeAttribute"/> class.
    /// </summary>
    public WorkflowAuthorizeAttribute() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowAuthorizeAttribute"/> class for a policy.
    /// </summary>
    /// <param name="policy">The name of the authorization policy to require.</param>
    public WorkflowAuthorizeAttribute(string policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        Policy = policy;
    }
}
