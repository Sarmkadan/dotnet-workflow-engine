# WorkflowAuthorizationHandlerExtensions
The `WorkflowAuthorizationHandlerExtensions` class provides a set of extension methods for workflow authorization handling. It offers a range of methods to check for required claims, roles, and retrieve user information, making it easier to implement authorization logic in workflow applications.

## API
* `public static bool HasRequiredClaim`: Checks if the current user has a required claim. This method returns `true` if the claim is present, `false` otherwise. It does not throw any exceptions.
* `public static bool HasRequiredClaimValue`: Checks if the current user has a required claim with a specific value. This method returns `true` if the claim with the specified value is present, `false` otherwise. It does not throw any exceptions.
* `public static bool HasRequiredRole`: Checks if the current user has a required role. This method returns `true` if the role is present, `false` otherwise. It does not throw any exceptions.
* `public static string? GetUserId`: Retrieves the user ID of the current user. This method returns the user ID as a string, or `null` if no user ID is available. It does not throw any exceptions.
* `public static string? GetUserEmail`: Retrieves the email address of the current user. This method returns the email address as a string, or `null` if no email address is available. It does not throw any exceptions.
* `public static string? GetUserName`: Retrieves the name of the current user. This method returns the user name as a string, or `null` if no user name is available. It does not throw any exceptions.

## Usage
The following examples demonstrate how to use the `WorkflowAuthorizationHandlerExtensions` class:
```csharp
// Example 1: Checking for required claims and roles
if (WorkflowAuthorizationHandlerExtensions.HasRequiredClaim() && WorkflowAuthorizationHandlerExtensions.HasRequiredRole())
{
    // User has required claim and role, proceed with workflow execution
}

// Example 2: Retrieving user information
string? userId = WorkflowAuthorizationHandlerExtensions.GetUserId();
string? userEmail = WorkflowAuthorizationHandlerExtensions.GetUserEmail();
string? userName = WorkflowAuthorizationHandlerExtensions.GetUserName();

if (userId != null && userEmail != null && userName != null)
{
    // User information is available, use it for logging or auditing purposes
}
```

## Notes
When using the `WorkflowAuthorizationHandlerExtensions` class, consider the following edge cases:
* If the current user is not authenticated, the `GetUserId`, `GetUserEmail`, and `GetUserName` methods will return `null`.
* If the required claim or role is not configured, the `HasRequiredClaim` and `HasRequiredRole` methods will return `false`.
* The `WorkflowAuthorizationHandlerExtensions` class is designed to be thread-safe, as it does not maintain any internal state. However, the underlying authorization mechanisms may have their own thread-safety considerations.
