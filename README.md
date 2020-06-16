// ... (rest of the README content remains unchanged)

## WorkflowAuthorizationHandler

The `WorkflowAuthorizationHandler` class provides custom authorization logic for workflow-specific permissions. It checks user claims and roles to determine access to workflow operations.

Here's an example of registering and using workflow authorization policies:

```csharp
services.AddWorkflowAuthorizationPolicies();

[WorkflowAuthorize(CanCreateWorkflow)]
public class MyWorkflowController
{
    // Actions requiring the CanCreateWorkflow policy
}
```
```csharp
// Alternatively, create a custom policy
var policyName = "MyCustomPolicy";
services.AddAuthorization(options =>
{
    options.AddPolicy(policyName, policy => policy.Requirements.Add(
        new WorkflowRequirement("my:custom:claim", "custom:claim:value")));
});

// Usage
[WorkflowAuthorize(policyName)]
public class MyCustomController
{
    // Actions requiring the custom policy
}
```
```csharp
// Claims helper usage
var user = new ClaimsPrincipal();
var userId = ClaimsHelper.GetUserId(user);
var userEmail = ClaimsHelper.GetUserEmail(user);
var hasClaim = ClaimsHelper.HasClaim(user, "claim:type", "claim:value");
``` 
