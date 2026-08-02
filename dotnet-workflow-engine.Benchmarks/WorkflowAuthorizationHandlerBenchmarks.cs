using System.Security.Claims;
using DotNetWorkflowEngine.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace DotNetWorkflowEngine.Benchmarks;

[MemoryDiagnoser]
public class WorkflowAuthorizationHandlerBenchmarks
{
    private WorkflowAuthorizationHandler _handler = null!;
    private AuthorizationHandlerContext _context = null!;
    private WorkflowRequirement _requirement = null!;
    private ClaimsPrincipal _user = null!;

    [Params(1, 10, 100)]
    public int ClaimCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Simple NopLogger to avoid Moq dependency
        var logger = new LoggerFactory().CreateLogger<WorkflowAuthorizationHandler>();
        _handler = new WorkflowAuthorizationHandler(logger);

        var claims = new List<Claim>();
        for (int i = 0; i < ClaimCount; i++)
        {
            claims.Add(new Claim($"claim:{i}", $"value:{i}"));
        }
        claims.Add(new Claim("workflow:create", "true"));
        claims.Add(new Claim(ClaimTypes.Role, "User"));
        
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        _user = new ClaimsPrincipal(identity);
    }

    [Benchmark]
    public async Task HandleRequirementAsync_WithValidClaim()
    {
        var requirement = new WorkflowRequirement("workflow:create", "true");
        var context = new AuthorizationHandlerContext(new[] { requirement }, _user, null);
        
        // Reflection is required to access protected HandleRequirementAsync
        var methodInfo = typeof(WorkflowAuthorizationHandler)
            .GetMethod("HandleRequirementAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        await (Task)methodInfo!.Invoke(_handler, new object[] { context, requirement })!;
    }

    [Benchmark]
    public async Task HandleRequirementAsync_WithInvalidClaim()
    {
        var requirement = new WorkflowRequirement("invalid:claim", "true");
        var context = new AuthorizationHandlerContext(new[] { requirement }, _user, null);
        
        var methodInfo = typeof(WorkflowAuthorizationHandler)
            .GetMethod("HandleRequirementAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        await (Task)methodInfo!.Invoke(_handler, new object[] { context, requirement })!;
    }

    [Benchmark]
    public async Task HandleRequirementAsync_WithValidRole()
    {
        var requirement = new WorkflowRequirement { RequiredRole = "User" };
        var context = new AuthorizationHandlerContext(new[] { requirement }, _user, null);
        
        var methodInfo = typeof(WorkflowAuthorizationHandler)
            .GetMethod("HandleRequirementAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        await (Task)methodInfo!.Invoke(_handler, new object[] { context, requirement })!;
    }
}
