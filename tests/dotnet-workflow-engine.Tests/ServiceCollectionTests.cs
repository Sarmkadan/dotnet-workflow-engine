// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Configuration;
using DotNetWorkflowEngine.Data.Context;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

// Disambiguate from DotNetWorkflowEngine.Configuration.ServiceCollection (extension host).
using ServiceCollection = Microsoft.Extensions.DependencyInjection.ServiceCollection;

namespace DotNetWorkflowEngine.Tests;

/// <summary>
/// Test suite for the <see cref="ServiceCollection"/> extension methods.
/// Contains unit tests that verify the behavior of workflow engine service registration and validation.
/// </summary>
public class ServiceCollectionTests
{
    [Fact]
    public void AddWorkflowEngine_WithValidConnectionString_RegistersServicesSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = "Server=localhost;Database=WorkflowEngine;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;";

        // Act
        services.AddWorkflowEngine(connectionString);

        // Assert
        services.Should().Contain(s => s.ServiceType == typeof(DatabaseContext));
        services.Should().Contain(s => s.ServiceType == typeof(IValidator<DotnetWorkflowEngineOptions>));
        services.Should().Contain(s => s.ServiceType == typeof(RetryPolicyService));
        services.Should().Contain(s => s.ServiceType == typeof(AuditService));
        services.Should().Contain(s => s.ServiceType == typeof(ActivityService));
        services.Should().Contain(s => s.ServiceType == typeof(WorkflowDefinitionService));
        services.Should().Contain(s => s.ServiceType == typeof(WorkflowExecutionService));

        // Verify options are registered
        services.Should().Contain(s => s.ServiceType == typeof(IOptions<DotnetWorkflowEngineOptions>));
        services.Should().Contain(s => s.ServiceType == typeof(IOptionsMonitor<DotnetWorkflowEngineOptions>));
    }

    [Fact]
    public void AddWorkflowEngine_WithNullConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddWorkflowEngine((string)null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddWorkflowEngine_WithEmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddWorkflowEngine(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddWorkflowEngine_WithActionConfiguration_RegistersServicesSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddWorkflowEngine(options =>
        {
            options.ConnectionString = "Server=localhost;Database=WorkflowEngine;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;";
            options.MaxConcurrentWorkflows = 50;
            options.DefaultActivityTimeoutSeconds = 100;
        });

        // Assert
        services.Should().Contain(s => s.ServiceType == typeof(DatabaseContext));
        services.Should().Contain(s => s.ServiceType == typeof(IValidator<DotnetWorkflowEngineOptions>));
        services.Should().Contain(s => s.ServiceType == typeof(RetryPolicyService));
        services.Should().Contain(s => s.ServiceType == typeof(AuditService));
        services.Should().Contain(s => s.ServiceType == typeof(ActivityService));
        services.Should().Contain(s => s.ServiceType == typeof(WorkflowDefinitionService));
        services.Should().Contain(s => s.ServiceType == typeof(WorkflowExecutionService));

        // Verify options are registered
        services.Should().Contain(s => s.ServiceType == typeof(IOptions<DotnetWorkflowEngineOptions>));
        services.Should().Contain(s => s.ServiceType == typeof(IOptionsMonitor<DotnetWorkflowEngineOptions>));
    }

    [Fact]
    public void AddWorkflowEngine_WithNullOptionsAction_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddWorkflowEngine((Action<DotnetWorkflowEngineOptions>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddWorkflowEngine_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        Action act = () => DotNetWorkflowEngine.Configuration.ServiceCollection.AddWorkflowEngine(services, options =>
        {
            options.ConnectionString = "Server=localhost;Database=WorkflowEngine;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;";
        });

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddWorkflowEngine_ValidatesOptions_OnStart_WithInvalidRetrySettings_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddWorkflowEngine(options =>
        {
            options.ConnectionString = "Server=localhost;Database=WorkflowEngine;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;";
            options.RetryCount = 3;
            options.RetryDelaySeconds = 0; // Invalid: should be positive when retry count > 0
        });

        var provider = services.BuildServiceProvider();

        // Assert
        Action act = () => provider.GetRequiredService<IOptions<DotnetWorkflowEngineOptions>>();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RetryDelaySeconds must be positive when RetryCount is greater than 0*");
    }

    [Fact]
    public void AddWorkflowEngine_ValidatesOptions_OnStart_WithValidRetrySettings_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddWorkflowEngine(options =>
        {
            options.ConnectionString = "Server=localhost;Database=WorkflowEngine;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;";
            options.RetryCount = 3;
            options.RetryDelaySeconds = 5; // Valid
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<DotnetWorkflowEngineOptions>>();
        options.Value.RetryCount.Should().Be(3);
        options.Value.RetryDelaySeconds.Should().Be(5);
    }

    [Fact]
    public async Task InitializeWorkflowEngineAsync_WithInvalidWorkflow_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddWorkflowEngine(options =>
        {
            options.ConnectionString = "Server=localhost;Database=WorkflowEngine;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;";
        });

        var provider = services.BuildServiceProvider();

        // Add an invalid workflow (missing required fields)
        var workflowService = provider.GetRequiredService<WorkflowDefinitionService>();
        var invalidWorkflow = new Workflow { Id = "", Name = "" }; // Invalid workflow
        workflowService.AddWorkflow(invalidWorkflow);

        // Act
        Func<Task> act = async () => await provider.GetRequiredService<DatabaseContext>().InitializeAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Workflow validation failed*");
    }

    [Fact]
    public async Task InitializeWorkflowEngineAsync_WithValidWorkflow_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddWorkflowEngine(options =>
        {
            options.ConnectionString = "Server=localhost;Database=WorkflowEngine;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;";
        });

        var provider = services.BuildServiceProvider();

        // Add a valid workflow
        var workflowService = provider.GetRequiredService<WorkflowDefinitionService>();
        var validWorkflow = new Workflow { Id = "valid-workflow", Name = "Valid Workflow" };
        workflowService.AddWorkflow(validWorkflow);
        workflowService.AddActivity("valid-workflow", new Activity { Id = "activity-1", Name = "Activity 1" });
        validWorkflow.StartActivityId = "activity-1";

        // Act
        Func<Task> act = async () => await provider.GetRequiredService<DatabaseContext>().InitializeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }
}