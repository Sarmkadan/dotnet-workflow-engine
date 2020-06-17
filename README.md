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

## StateException

The `StateException` class represents an exception thrown when an invalid state transition is attempted. It provides information about the current state, requested state, and entity ID (if applicable). You can use this exception to handle and log state transition errors.

Here's an example of using `StateException`:

```csharp
try
{
    // Attempt a state transition
    workflow.TransitionToState("InvalidState");
}
catch (StateException ex)
{
    var currentState = ex.CurrentState;
    var entityId = ex.EntityId ?? "Unknown";
    Console.WriteLine(ex.GetTransitionDetails()); // Output: Cannot transition from InvalidState to InvalidState (Entity: Unknown)
}
```
