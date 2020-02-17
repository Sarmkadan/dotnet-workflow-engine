// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Contributing Guide

Thank you for your interest in contributing to dotnet-workflow-engine! This guide explains how to contribute effectively.

## Code of Conduct

Be respectful, inclusive, and professional. Harassment or discriminatory behavior is not tolerated.

## Getting Started

### 1. Fork the Repository

```bash
# Visit https://github.com/Sarmkadan/dotnet-workflow-engine
# Click "Fork" button
git clone https://github.com/YOUR_USERNAME/dotnet-workflow-engine.git
cd dotnet-workflow-engine
git remote add upstream https://github.com/Sarmkadan/dotnet-workflow-engine.git
```

### 2. Create a Feature Branch

```bash
git checkout -b feature/amazing-feature
```

Branch naming:
- `feature/...` for new features
- `fix/...` for bug fixes
- `docs/...` for documentation
- `test/...` for tests
- `refactor/...` for refactoring

### 3. Set Up Development Environment

```bash
dotnet restore
dotnet build
dotnet test
```

## Development Standards

### Code Style

Follow C# naming and formatting conventions:

```csharp
// Good
public class WorkflowExecutionService
{
    private readonly IRepository<Workflow> _repository;
    
    public async Task<WorkflowInstance> ExecuteAsync(Workflow workflow)
    {
        var instance = new WorkflowInstance();
        return await _repository.AddAsync(instance);
    }
}

// Avoid
public class workflowexecutionservice
{
    private IRepository<Workflow> repo;
    
    public WorkflowInstance Execute(Workflow workflow)
    {
        // Synchronous code
    }
}
```

### Async/Await

Always use async/await for I/O operations:

```csharp
// Good
public async Task<WorkflowInstance> CreateAsync(Workflow workflow)
{
    return await _repository.AddAsync(workflow);
}

// Avoid
public Task<WorkflowInstance> Create(Workflow workflow)
{
    return _repository.AddAsync(workflow);
}
```

### Dependency Injection

Use constructor injection:

```csharp
public class WorkflowService
{
    private readonly IRepository<Workflow> _repository;
    private readonly ILogger<WorkflowService> _logger;
    
    public WorkflowService(
        IRepository<Workflow> repository,
        ILogger<WorkflowService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
}
```

### Comments

Write minimal comments; code should be self-explanatory:

```csharp
// Good - necessary clarification
// Exponential backoff caps at 30 seconds to prevent thundering herd
var delay = Math.Min(baseDelay * Math.Pow(2, retries), MaxDelay);

// Avoid - obvious code
// Increment retry count
retries++;
```

### Method Length

Keep methods under 50 lines:

```csharp
// Good
public async Task<bool> IsValidAsync(Workflow workflow)
{
    return await ValidateActivityReferencesAsync(workflow)
        && await ValidateTransitionsAsync(workflow)
        && await ValidateStartActivityAsync(workflow);
}

// Avoid
public async Task<bool> IsValidAsync(Workflow workflow)
{
    // 100+ lines of validation logic
}
```

## Testing

### Unit Tests

Test business logic in isolation:

```csharp
[TestClass]
public class WorkflowValidatorTests
{
    [TestMethod]
    public async Task Validate_WithValidWorkflow_ReturnsTrue()
    {
        // Arrange
        var workflow = CreateValidWorkflow();
        var validator = new WorkflowValidator();
        
        // Act
        var result = await validator.ValidateAsync(workflow);
        
        // Assert
        Assert.IsTrue(result.IsValid);
    }
    
    [TestMethod]
    [ExpectedException(typeof(ValidationException))]
    public async Task Validate_WithMissingActivity_ThrowsException()
    {
        // Arrange
        var workflow = CreateWorkflowWithMissingActivity();
        var validator = new WorkflowValidator();
        
        // Act
        await validator.ValidateAsync(workflow);
    }
}
```

### Integration Tests

Test system behavior with real dependencies:

```csharp
[TestClass]
public class WorkflowExecutionIntegrationTests
{
    private DatabaseContext _context;
    private IWorkflowExecutionService _service;
    
    [TestInitialize]
    public async Task Setup()
    {
        _context = CreateTestDatabase();
        _service = new WorkflowExecutionService(_context);
    }
    
    [TestCleanup]
    public async Task Cleanup()
    {
        await _context.Database.EnsureDeletedAsync();
    }
    
    [TestMethod]
    public async Task Execute_WithSimpleWorkflow_CompletesSuccessfully()
    {
        // Arrange
        var workflow = await CreateWorkflowAsync();
        
        // Act
        var result = await _service.ExecuteAsync(workflow.Id);
        
        // Assert
        Assert.AreEqual(WorkflowStatus.Completed, result.Status);
    }
}
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run with code coverage
dotnet test /p:CollectCoverage=true

# Run specific test class
dotnet test --filter "ClassName=WorkflowValidatorTests"

# Run with verbose output
dotnet test --verbosity detailed
```

## Documentation

### Code Documentation

Add XML comments to public members:

```csharp
/// <summary>
/// Executes a workflow instance with the provided context.
/// </summary>
/// <param name="context">The execution context containing workflow and variable data.</param>
/// <returns>The completed workflow instance.</returns>
/// <exception cref="ValidationException">Thrown if the workflow is invalid.</exception>
public async Task<WorkflowInstance> ExecuteAsync(ExecutionContext context)
{
    // Implementation
}
```

### File Changes

Update documentation for changes:

- README.md for user-facing features
- docs/api-reference.md for API changes
- docs/configuration.md for configuration changes
- CHANGELOG.md for release notes
- docs/contributing.md for process changes

## Pull Request Process

### 1. Commit Changes

```bash
git add .
git commit -m "feat: add parallel activity execution support

- Implement ExecutionMode.Parallel in WorkflowExecutionService
- Add MaxConcurrentActivities configuration option
- Add tests for parallel execution

Closes #123"
```

Commit message format:
- Type: feat, fix, docs, test, refactor, perf
- Scope: optional, the affected area
- Subject: imperative mood, lowercase
- Body: explanation of the change
- Footer: reference issues with "Closes #123"

### 2. Keep Branch Updated

```bash
git fetch upstream
git rebase upstream/main
```

### 3. Push Changes

```bash
git push origin feature/amazing-feature
```

### 4. Create Pull Request

On GitHub:
- Title: Descriptive title
- Description: Explain what, why, and how
- Link related issues
- Add screenshots if UI changes
- Request review from maintainers

**PR Template:**

```markdown
## Description
Brief description of changes.

## Type
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation

## Related Issues
Closes #123

## How Has This Been Tested?
Describe testing approach.

## Checklist
- [ ] Code follows style guidelines
- [ ] Comments added for complex logic
- [ ] Tests added/updated
- [ ] Documentation updated
- [ ] No new warnings
- [ ] CHANGELOG.md updated
```

## Review Process

Reviewers will:
1. Check code quality and style
2. Verify tests are included
3. Ensure documentation is updated
4. Test the changes
5. Request modifications if needed

Address feedback constructively:

```bash
# Make requested changes
git add .
git commit -m "refactor: address review feedback"
git push origin feature/amazing-feature
```

Don't force-push unless requested.

## Release Process

Maintainers will:
1. Verify all tests pass
2. Update CHANGELOG.md
3. Update version number
4. Create GitHub release
5. Publish to NuGet

## Common Issues

### Tests Failing

```bash
# Clean and rebuild
dotnet clean
dotnet build

# Run tests with verbose output
dotnet test --verbosity detailed

# Run single failing test
dotnet test --filter "ClassName.MethodName"
```

### Merge Conflicts

```bash
# Update from main
git fetch upstream
git rebase upstream/main

# Resolve conflicts in editor
# Then:
git add .
git rebase --continue
```

### Linting Issues

```bash
# Check code style
dotnet format

# Fix issues
dotnet format --verify-no-changes --verbosity diagnostic
```

## Questions?

- Check existing issues and discussions
- Ask in pull request comments
- Email: vladyslav@sarmkadan.com

## Recognition

Contributors will be recognized in:
- CONTRIBUTORS.md file
- Release notes
- GitHub contributors page

Thank you for contributing! 🎉
