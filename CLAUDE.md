# CLAUDE.md

Lightweight .NET 10 workflow engine library (state machine, activities, retries, audit trail) packaged as NuGet `dotnet-workflow-engine`, with a demo console entry point.

## Build

```bash
dotnet restore
dotnet build --configuration Release        # or: make build
dotnet run                                  # runs Program.cs demo (needs SQL Server connection)
dotnet publish -c Release -o ./publish      # or: make publish
```

Solution file: `dotnet-workflow-engine.slnx` (main project + tests + benchmarks).

## Test

```bash
dotnet test --configuration Release                        # or: make test
dotnet test --filter "FullyQualifiedName~WorkflowCoreTests" # single class
dotnet test /p:CollectCoverage=true                         # or: make test-coverage
dotnet run -c Release --project dotnet-workflow-engine.Benchmarks   # BenchmarkDotNet
```

Tests: xUnit + FluentAssertions + Moq in `tests/dotnet-workflow-engine.Tests/`. CI (`.github/workflows/ci.yml`) runs restore/build/test on .NET 10.0.x.

## Lint / Format

```bash
dotnet format                               # make format
dotnet format --verify-no-changes           # make format-check
dotnet build /p:EnforceCodeStyleInBuild=true /p:EnableNETAnalyzers=true   # make lint
```

Style rules live in `.editorconfig` (4 spaces, LF, Allman braces, `csharp_prefer_braces`). `Nullable` and `ImplicitUsings` are enabled; `GenerateDocumentationFile=true`, so public members need XML docs.

## Key Directories

- `Program.cs` - demo entry point: configures `AddWorkflowEngine(options => ...)`, registers an activity handler, runs a sample workflow.
- `Configuration/` - `DotnetWorkflowEngineOptions`, validator, `ServiceCollection.AddWorkflowEngine` DI registration.
- `Models/` - `Workflow`, `WorkflowInstance`, `Activity`, `Transition`, `ExecutionContext`, `RetryPolicyConfig`, `WorkflowStatusMachine`.
- `Services/` - core logic: `WorkflowDefinitionService`, `WorkflowExecutionService`, `ActivityService`, `AuditService`, `RetryPolicyService`, `ConditionalBranchingService`, `MessageEventService`.
- `Data/Context`, `Data/Repositories` - `DatabaseContext` (System.Data.SqlClient), `IRepository`, workflow/audit repositories.
- `Utilities/` - `WorkflowBuilder`, `WorkflowValidator`, `ExpressionEvaluator`, `SerializationHelper`, extension helpers.
- `Events/`, `Caching/`, `Monitoring/`, `Security/`, `Middleware/`, `Filters/`, `Formatters/`, `BackgroundJobs/`, `Integration/`, `Cli/` - supporting subsystems.
- `Exceptions/`, `Enums/`, `Constants/` - shared types.
- `examples/` - usage samples (excluded from compile).
- `docs/` - per-class markdown docs plus guides (`architecture.md`, `configuration.md`, `workflow-patterns.md`).

Note: `DotNetWorkflowEngine.csproj` has `<Compile Remove>` entries excluding `Controllers/**`, `Configuration/DependencyInjection.cs`, `Integration/HttpClientFactory.cs`, `Middleware/ErrorHandlingMiddleware.cs`, `Cli/WorkflowCommand.cs`, `examples/**`, `tests/**` and the benchmarks project. Check the csproj before assuming a file is compiled.

## Conventions

- Namespaces mirror folders: `DotNetWorkflowEngine.<Folder>` (e.g. `DotNetWorkflowEngine.Services`), file-scoped namespace declarations.
- Every source file starts with the author header comment block (see `Program.cs`).
- Interfaces prefixed `I`; read-only service contracts named `I<Thing>Query` (e.g. `IWorkflowInstanceQuery`, `IAuditTrailQuery`).
- Helper logic is split into partial/extension files by suffix: `*Extensions.cs`, `*JsonExtensions.cs`, `*Validation.cs` beside the main type.
- Alias `ExecutionContext` when importing to avoid clashing with `System.Threading.ExecutionContext`.
- Custom exceptions derive from `WorkflowException` in `Exceptions/`.
- Test classes named `<Type>Tests.cs`, one per type; use `Mock<ILogger<T>>` and FluentAssertions `.Should()`.
- Conventional-commit messages (`docs:`, `chore:`, `feat:`, `fix:`).
