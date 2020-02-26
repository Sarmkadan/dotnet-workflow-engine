# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

.PHONY: help build restore clean test run dev docker-build docker-up docker-down \
        db-migrate db-seed lint format docs publish

# Configuration
DOTNET := dotnet
DOTNET_VERSION := 10.0
PROJECT := DotNetWorkflowEngine
DOCKER_IMAGE := sarmkadan/dotnet-workflow-engine
DOCKER_TAG := latest

help:
	@echo "dotnet-workflow-engine - Build and Development Commands"
	@echo ""
	@echo "Available targets:"
	@echo "  make build           - Build the project"
	@echo "  make restore         - Restore NuGet packages"
	@echo "  make clean           - Clean build artifacts"
	@echo "  make test            - Run all tests"
	@echo "  make test-coverage   - Run tests with code coverage"
	@echo "  make run             - Run the application"
	@echo "  make dev             - Run with hot reload"
	@echo "  make docker-build    - Build Docker image"
	@echo "  make docker-up       - Start Docker containers"
	@echo "  make docker-down     - Stop Docker containers"
	@echo "  make db-migrate      - Run database migrations"
	@echo "  make db-seed         - Seed database with sample data"
	@echo "  make lint            - Run code linting"
	@echo "  make format          - Format code"
	@echo "  make docs            - Generate documentation"
	@echo "  make publish         - Publish release build"
	@echo "  make help            - Show this help message"

restore:
	@echo "Restoring NuGet packages..."
	$(DOTNET) restore

build: restore
	@echo "Building project..."
	$(DOTNET) build --configuration Release

clean:
	@echo "Cleaning build artifacts..."
	$(DOTNET) clean
	rm -rf bin obj dist
	rm -rf .vs .vscode
	find . -type d -name "obj" -exec rm -rf {} +
	find . -type d -name "bin" -exec rm -rf {} +

test: build
	@echo "Running unit tests..."
	$(DOTNET) test --configuration Release --no-build --verbosity normal

test-coverage: build
	@echo "Running tests with code coverage..."
	$(DOTNET) test --configuration Release --no-build /p:CollectCoverage=true

run: build
	@echo "Running application..."
	$(DOTNET) run --configuration Release

dev:
	@echo "Running application with hot reload..."
	$(DOTNET) watch run

docker-build:
	@echo "Building Docker image..."
	docker build -t $(DOCKER_IMAGE):$(DOCKER_TAG) .
	docker tag $(DOCKER_IMAGE):$(DOCKER_TAG) $(DOCKER_IMAGE):latest

docker-up:
	@echo "Starting Docker containers..."
	docker-compose up -d
	@echo "Waiting for services to be ready..."
	@sleep 5
	docker-compose logs

docker-down:
	@echo "Stopping Docker containers..."
	docker-compose down

docker-logs:
	@echo "Displaying Docker logs..."
	docker-compose logs -f

db-migrate:
	@echo "Running database migrations..."
	$(DOTNET) ef database update

db-seed:
	@echo "Seeding database..."
	$(DOTNET) run -- db seed

db-drop:
	@echo "Dropping database..."
	$(DOTNET) ef database drop --force

lint:
	@echo "Running code analysis..."
	$(DOTNET) build /p:EnforceCodeStyleInBuild=true /p:EnableNETAnalyzers=true

format:
	@echo "Formatting code..."
	$(DOTNET) format

format-check:
	@echo "Checking code format..."
	$(DOTNET) format --verify-no-changes --verbosity diagnostic

docs:
	@echo "Generating documentation..."
	@echo "Documentation files are in 'docs/' directory"
	@echo "View docs/getting-started.md to get started"

publish:
	@echo "Publishing release build..."
	$(DOTNET) publish -c Release -o ./publish

version:
	@echo "$(PROJECT) Build Information:"
	@echo "  .NET Version: $(DOTNET_VERSION)"
	@echo "  Docker Image: $(DOCKER_IMAGE):$(DOCKER_TAG)"
	@$(DOTNET) --version

health-check:
	@echo "Checking service health..."
	@curl -s http://localhost:5000/health | $(DOTNET) -

metrics:
	@echo "Fetching metrics..."
	@curl -s http://localhost:5000/metrics | head -20

logs:
	@echo "Displaying application logs..."
	@tail -f logs/workflow-engine.log 2>/dev/null || echo "Log file not found. Start the application first."

install-tools:
	@echo "Installing global .NET tools..."
	$(DOTNET) tool install -g dotnet-ef
	$(DOTNET) tool install -g dotnet-format
	$(DOTNET) tool install -g dotnet-watch

update-packages:
	@echo "Checking for package updates..."
	$(DOTNET) outdated

all: clean restore build test lint format
	@echo "Build completed successfully!"

.DEFAULT_GOAL := help
