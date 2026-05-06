# Contributing to dotnet-workflow-engine

First off, thank you for considering contributing to dotnet-workflow-engine!

## How to Contribute

### 1. Fork and Clone
Fork the repository and clone your fork to your local machine.

```bash
git clone https://github.com/your-username/dotnet-workflow-engine.git
cd dotnet-workflow-engine
```

### 2. Create a Branch
Create a branch for your feature or bug fix.

```bash
git checkout -b feature/your-feature-name
```

### 3. Development Requirements
- .NET 10.0 SDK or later — download from https://dotnet.microsoft.com/download
- Optional: Docker (for container builds)
- Optional: Redis (for caching integration tests)

### 4. Build Locally

```bash
# Restore dependencies
dotnet restore

# Build in Debug mode
dotnet build

# Build in Release mode
dotnet build --configuration Release
```

### 5. Running Tests

```bash
# Run all tests
dotnet test

# Run with verbosity
dotnet test --verbosity normal

# Run a specific test project
dotnet test tests/dotnet-workflow-engine.Tests/

# Run with code coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov

# Generate coverage report (requires reportgenerator tool)
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage"
```

All new public methods must have corresponding unit tests. Target 80%+ code coverage on changed files.

### 6. Code Style
- Follow the existing coding conventions in the repository.
- The `.editorconfig` at the root enforces formatting — most editors respect it automatically.
- Include XML documentation comments for public APIs.
- Use file-scoped namespaces and `var` where the type is apparent.

### 7. Submitting a Pull Request
Once you're ready, submit a Pull Request to the main repository. Your PR should include:
- A clear description of what changed and why.
- Links to related issues (use `Fixes #N` syntax).
- Evidence that tests pass locally.

### 8. Commit Message Format

Follow the conventional commit format:
```
<type>(<scope>): <subject>

<body>
```

Types: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`
Scopes: `models`, `services`, `controllers`, `middleware`, `events`

## Reporting Issues
If you find a bug or have a feature request, please use GitHub Issues.

**Bug reports** should include:
- Clear reproduction steps
- Expected vs actual behavior
- .NET SDK version and OS
- Relevant logs or error messages

**Feature requests** should include:
- The problem you are solving
- Proposed API design (if applicable)
- Alternatives considered

## License
By contributing, you agree that your contributions will be licensed under its MIT License.
