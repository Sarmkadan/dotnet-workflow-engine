# WorkflowAuthorizationHandler

`WorkflowAuthorizationHandler` is a utility class that provides authorization-related functionality for workflow engines in .NET applications. It centralizes common authorization checks, policy-based requirements, and user context extraction to simplify securing workflow operations. The class combines static helper methods for user metadata retrieval with attribute-based authorization and policy registration utilities.

## API

### `WorkflowAuthorizationHandler` (class)
Serves as a container for authorization-related utilities, including policy requirements, attribute definitions, and user context helpers. This class is not meant to be instantiated; all members are static or attribute constructors.

### `RequiredClaim` (property)
Gets or sets the claim type that must be present for authorization to succeed. When set, workflows using this handler will require the specified claim type to be present in the user's claims. Defaults to `null`, meaning no specific claim is required.

### `RequiredClaimValue` (property)
Gets or sets the expected value of the claim specified by `RequiredClaim`. If both `RequiredClaim` and `RequiredClaimValue` are set, the user must possess a claim of the specified type with the exact value. Defaults to `null`, meaning any value for the claim type is acceptable.

### `RequiredRole` (property)
Gets or sets the role that the user must possess for authorization to succeed. When set, workflows using this handler will require the user to be assigned the specified role. Defaults to `null`, meaning no specific role is required.

### `WorkflowRequirement()` (constructor)
Initializes a new instance of the `WorkflowRequirement` class with default values. This constructor creates a requirement that does not enforce any specific claim, role, or claim value by default.

### `WorkflowRequirement(string? requiredClaim, string? requiredClaimValue, string? requiredRole)` (constructor)
Initializes a new instance of the `WorkflowRequirement` class with specified authorization constraints.
- `requiredClaim`: The claim type required for authorization.
- `requiredClaimValue`: The required value of the claim; ignored if `requiredClaim` is `null`.
- `requiredRole`: The role required for authorization.
If none of the parameters are provided, the requirement will not enforce any constraints.

### `AddWorkflowAuthorizationPolicies(IServiceCollection services)` (method)
Registers default authorization policies for workflow operations with the dependency injection container.
- `services`: The `IServiceCollection` instance to which policies are added.
Returns the same `IServiceCollection` instance for method chaining.
Throws `ArgumentNullException` if `services` is `null`.

### `GetUserId(ClaimsPrincipal user)` (method)
Extracts the user identifier from the claims principal.
- `user`: The `ClaimsPrincipal` representing the current user.
Returns the value of the `ClaimTypes.NameIdentifier` claim if present; otherwise, `null`.

### `GetUserEmail(ClaimsPrincipal user)` (method)
Extracts the user's email address from the claims principal.
- `user`: The `ClaimsPrincipal` representing the current user.
Returns the value of the `ClaimTypes.Email` claim if present; otherwise, `null`.

### `GetUserName(ClaimsPrincipal user)` (method)
Extracts the user's name from the claims principal.
- `user`: The `ClaimsPrincipal` representing the current user.
Returns the value of the `ClaimTypes.Name` claim if present; otherwise, `null`.

### `HasClaim(ClaimsPrincipal user, string claimType)` (method)
Determines whether the user possesses a claim of the specified type.
- `user`: The `ClaimsPrincipal` representing the current user.
- `claimType`: The type of the claim to check.
Returns `true` if the user has a claim of the specified type; otherwise, `false`.

### `HasClaim(ClaimsPrincipal user, string claimType, string claimValue)` (method)
Determines whether the user possesses a claim of the specified type and value.
- `user`: The `ClaimsPrincipal` representing the current user.
- `claimType`: The type of the claim to check.
- `claimValue`: The value the claim must have.
Returns `true` if the user has a claim of the specified type and value; otherwise, `false`.

### `HasClaimType(ClaimsPrincipal user, string claimType)` (method)
Alias for `HasClaim(ClaimsPrincipal, string)`. Determines whether the user possesses a claim of the specified type.

### `WorkflowAuthorizeAttribute()` (constructor)
Initializes a new instance of the `WorkflowAuthorizeAttribute` class with default values. This attribute does not enforce any specific authorization constraints by default.

### `WorkflowAuthorizeAttribute(string? requiredClaim, string? requiredClaimValue, string? requiredRole)` (constructor)
Initializes a new instance of the `WorkflowAuthorizeAttribute` class with specified authorization constraints.
- `requiredClaim`: The claim type required for authorization.
- `requiredClaimValue`: The required value of the claim; ignored if `requiredClaim` is `null`.
- `requiredRole`: The role required for authorization.
If none of the parameters are provided, the attribute will not enforce any constraints.

## Usage

### Example 1: Registering authorization policies
