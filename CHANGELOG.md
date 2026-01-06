# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-01-15

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
- Kubernetes manifests and deployment examples
- GitHub Actions CI/CD workflow
- Comprehensive documentation and examples

### Changed
- Refactored ExecutionContext for better type safety
- Improved retry policy flexibility with configurable backoff strategies
- Enhanced error messages with detailed context
- Optimized database queries with indexes
- Updated dependencies to latest versions

### Fixed
- Memory leak in long-running workflow instances
- Race condition in parallel activity execution
- Audit trail inconsistency with failed activities
- Connection pool exhaustion under high load

### Deprecated
- Old WorkflowBuilder constructor (use fluent API instead)
- Direct database context access (use repositories)

## [1.1.0] - 2025-11-20

### Added
- Parallel activity execution support
- Conditional transitions with expression evaluation
- Hangfire background job integration
- Redis caching provider
- In-memory caching provider
- Comprehensive audit trail logging
- Retry policy configurations (exponential, fixed delay, linear backoff)
- Activity timeout support
- Expression evaluation utility
- Collection and string extension methods
- Workflow validation framework

### Changed
- Improved WorkflowExecutionService architecture
- Better separation of concerns with service layer
- Enhanced repository pattern implementation
- More intuitive API for activity execution

### Fixed
- Database migration issues on first run
- Entity mapping inconsistencies
- Event bus subscription memory leaks

## [1.0.0] - 2025-09-10

### Added
- Initial release
- Core workflow engine with sequential execution
- Activity and transition definitions
- Workflow instance management
- Basic audit logging
- Database persistence (EF Core)
- Entity Framework Core integration
- Dependency injection configuration
- Basic error handling
- Workflow definition validation
- Activity execution engine
- Status enumeration for workflows and activities
- Custom exception types (WorkflowException, ActivityException, ValidationException)
- Logging middleware
- Basic REST controller structure

### Known Issues
- Parallel execution not yet optimized
- No built-in clustering support
- Limited monitoring capabilities

## [0.9.0] - 2025-08-15

### Added
- Initial alpha release for testing
- Core data models (Workflow, Activity, Transition)
- Basic execution engine
- Database context setup
- Unit test framework

## [0.8.0] - 2025-07-01

### Added
- Project skeleton and structure
- Initial architecture design
- Dependency injection setup

---

## Version History Summary

| Version | Date | Focus |
|---------|------|-------|
| 1.2.0 | 2026-01-15 | Production-ready with full docs and examples |
| 1.1.0 | 2025-11-20 | Advanced features (parallel, caching, retry) |
| 1.0.0 | 2025-09-10 | Stable initial release |
| 0.9.0 | 2025-08-15 | Alpha for community testing |
| 0.8.0 | 2025-07-01 | Initial project structure |

## Upgrade Guide

### 1.0.0 → 1.1.0
No breaking changes. New features are additive only.

### 1.1.0 → 1.2.0
- Update configuration with new Prometheus and cache settings
- No code changes required for existing workflows
- Recommended: Add health check endpoints to load balancer

## Future Roadmap

### 2.0.0 (Q3 2026)
- [ ] GraphQL API support
- [ ] Workflow versioning with side-by-side deployment
- [ ] Distributed workflow execution across multiple nodes
- [ ] Event sourcing implementation
- [ ] Enhanced security with encryption at rest
- [ ] Web-based workflow designer UI

### 2.1.0 (Q4 2026)
- [ ] Machine learning-based performance optimization
- [ ] Advanced monitoring dashboard
- [ ] Workflow analytics and insights
- [ ] Custom DSL parser

### 3.0.0 (2027)
- [ ] Zero-downtime deployment support
- [ ] BPMN 2.0 full compliance
- [ ] Cloud-native optimizations
- [ ] Multi-tenant support

## Contributors

- [Vladyslav Zaiets](https://sarmkadan.com) - Creator and maintainer

See [CONTRIBUTORS.md](CONTRIBUTORS.md) for a full list of contributors.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**For detailed information about each release, visit the [GitHub Releases](https://github.com/Sarmkadan/dotnet-workflow-engine/releases) page.**
