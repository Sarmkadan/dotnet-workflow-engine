# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-06-16

### Added
- Comprehensive REST API with pagination and filtering
- Prometheus metrics integration for monitoring
- Health check endpoints for service diagnostics
- Webhook integration for external system calls
- Rate limiting middleware (100 requests/minute default)
- Role-based access control (RBAC)
- Activity result caching with TTL configuration
- CSV export functionality for audit trails
- CLI tool for workflow management
- Docker and docker-compose support
- GitHub Actions CI/CD workflow
- Comprehensive documentation and examples

### Changed
- Refactored ExecutionContext for better type safety
- Improved retry policy flexibility with configurable backoff strategies
- Enhanced error messages with detailed context
- Optimized database queries with indexes
- Updated all dependencies to stable releases

### Fixed
- Memory leak in long-running workflow instances
- Race condition in parallel activity execution
- Audit trail inconsistency with failed activities
- Connection pool exhaustion under high load

## [0.9.0] - 2025-05-26

### Added
- CLI tool scaffolding (CommandParser, CommandContext, WorkflowCommand)
- Webhook handler for calling external systems during execution
- Expression evaluator for conditional transitions
- HttpClientFactory integration wrapper
- ConditionalBranchingService and BranchingResult model
- Rate limiting and logging middleware
- Validation filter for API controllers
- ReflectionHelper and SerializationHelper utilities

### Changed
- Unified error handling through ErrorHandlingMiddleware
- WorkflowValidator now supports pluggable rule sets

## [0.8.0] - 2025-05-12

### Added
- Redis caching provider via StackExchange.Redis
- In-memory caching provider with configurable TTL
- CacheService abstraction over both providers
- Hangfire background job integration (WorkflowJobProcessor)
- WorkflowMetrics with Prometheus counters and histograms
- EventBus pub/sub system for workflow lifecycle events

### Changed
- ActivityService now publishes events on activity start/complete/fail
- Background job processor handles deferred workflow steps

### Fixed
- Event bus subscription memory leaks on long-lived instances

## [0.7.0] - 2025-04-28

### Added
- Parallel activity execution with fork/join semantics
- ExecutionMode enum (Sequential, Parallel)
- Concurrent branch coordination in WorkflowExecutionService
- WorkflowBuilder fluent API for programmatic workflow definition
- CollectionExtensions and DateTimeExtensions utilities
- WorkflowConstants for shared configuration values

### Changed
- WorkflowExecutionService refactored to support both sequential and parallel paths
- ActivityResult model extended with branch tracking fields

## [0.6.0] - 2025-04-14

### Added
- WorkflowController, WorkflowInstanceController, AuditController REST endpoints
- AuditService with immutable append-only log
- AuditLogEntry model and AuditRepository
- IOutputFormatter abstraction with JSON and CSV implementations
- WorkflowInstanceRepository with status filtering queries
- Pagination and filtering support in list endpoints

### Changed
- Audit trail now captures actor identity and change reason per entry

### Fixed
- Entity mapping inconsistency causing duplicate audit records on retry

## [0.5.0] - 2025-03-31

### Added
- RetryPolicyService with exponential, fixed-delay, and linear-backoff strategies
- RetryPolicyConfig model and RetryPolicy enum
- Activity timeout support with configurable cancellation tokens
- StateException and custom exception hierarchy
- WorkflowValidator framework with extensible rule pipeline

### Changed
- ActivityService wraps execution in retry loop driven by RetryPolicyService
- WorkflowDefinitionService validates definition before persisting

## [0.4.0] - 2025-03-17

### Added
- WorkflowDefinitionService for CRUD operations on workflow definitions
- WorkflowExecutionService with sequential execution orchestration
- ActivityService for individual activity execution and input validation
- IRepository generic base with CRUD contract
- WorkflowRepository and WorkflowInstanceRepository
- Dependency injection configuration (ServiceCollection, DependencyInjection extensions)

### Changed
- Separated workflow definition storage from runtime execution concerns

## [0.3.0] - 2025-03-03

### Added
- DatabaseContext via Entity Framework Core
- Entity mappings for Workflow, WorkflowInstance, Activity, AuditLogEntry
- Database persistence for workflow state
- WorkflowInstance model tracking execution progress
- ActivityResult model for capturing step outcomes
- StringExtensions utility methods

### Fixed
- Database migration failures on clean first run

## [0.2.0] - 2025-02-17

### Added
- Core domain models: Workflow, Activity, Transition, ExecutionContext
- Enums: WorkflowStatus, ActivityStatus, RetryPolicy, ExecutionMode
- Custom exception types: WorkflowException, ActivityException, ValidationException
- Program.cs with minimal API host setup
- Initial .editorconfig and project structure

## [0.1.0] - 2025-02-03

### Added
- Project skeleton (DotNetWorkflowEngine.csproj, solution file)
- Initial architecture design and namespace layout
- Dependency injection scaffold
- Unit test project with xUnit, FluentAssertions, and Moq

---

## Version History Summary

| Version | Date | Focus |
|---------|------|-------|
| 1.0.0 | 2025-06-16 | Production release with full docs, API, monitoring |
| 0.9.0 | 2025-05-26 | CLI, webhooks, expression evaluation |
| 0.8.0 | 2025-05-12 | Caching, background jobs, event bus, metrics |
| 0.7.0 | 2025-04-28 | Parallel execution, WorkflowBuilder fluent API |
| 0.6.0 | 2025-04-14 | REST API, audit trail, output formatters |
| 0.5.0 | 2025-03-31 | Retry policies, activity timeouts, validation |
| 0.4.0 | 2025-03-17 | Service layer, DI configuration |
| 0.3.0 | 2025-03-03 | Database persistence, EF Core |
| 0.2.0 | 2025-02-17 | Core models, exception types |
| 0.1.0 | 2025-02-03 | Project skeleton and test framework |

## Upgrade Guide

### 0.9.0 → 1.0.0
No breaking changes. New monitoring and CLI features are additive only.
Recommended: register health check endpoints in your load balancer configuration.

### 0.8.0 → 0.9.0
No breaking changes. Add `WebhookHandler` registration to DI if you use webhook activities.

### 0.7.0 → 0.8.0
Add Redis connection string to `appsettings.json` if using the Redis cache provider.
In-memory provider requires no configuration changes.

## Contributors

- [Vladyslav Zaiets](https://sarmkadan.com) - Creator and maintainer

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**For detailed information about each release, visit the [GitHub Releases](https://github.com/Sarmkadan/dotnet-workflow-engine/releases) page.**
