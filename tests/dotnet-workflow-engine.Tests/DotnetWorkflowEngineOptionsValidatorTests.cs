using FluentAssertions;
using Xunit;
using DotNetWorkflowEngine.Configuration;

namespace DotNetWorkflowEngine.Tests;

public class DotnetWorkflowEngineOptionsValidatorTests
{
    private readonly DotnetWorkflowEngineOptionsValidator _validator;

    public DotnetWorkflowEngineOptionsValidatorTests()
    {
        _validator = new DotnetWorkflowEngineOptionsValidator();
    }

    [Fact]
    public void Validate_ValidOptions_ReturnsSuccess()
    {
        var options = new DotnetWorkflowEngineOptions
        {
            ConnectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;",
            CacheProvider = "Memory",
            ExecutionMode = "Sequential"
        };

        var result = _validator.Validate(options);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidConnectionString_ReturnsError()
    {
        var options = new DotnetWorkflowEngineOptions
        {
            ConnectionString = "short",
            CacheProvider = "Memory",
            ExecutionMode = "Sequential"
        };

        var result = _validator.Validate(options);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConnectionString");
    }

    [Fact]
    public void Validate_InvalidMaxConcurrentWorkflows_ReturnsError()
    {
        var options = new DotnetWorkflowEngineOptions
        {
            ConnectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;",
            MaxConcurrentWorkflows = 0,
            CacheProvider = "Memory",
            ExecutionMode = "Sequential"
        };

        var result = _validator.Validate(options);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaxConcurrentWorkflows");
    }

    [Fact]
    public void Validate_InvalidCacheProvider_ReturnsError()
    {
        var options = new DotnetWorkflowEngineOptions
        {
            ConnectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;",
            CacheProvider = "InvalidProvider",
            ExecutionMode = "Sequential"
        };

        var result = _validator.Validate(options);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CacheProvider");
    }

    [Fact]
    public void Validate_InvalidDefaultCacheExpiration_ReturnsError()
    {
        var options = new DotnetWorkflowEngineOptions
        {
            ConnectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;",
            CacheProvider = "Memory",
            ExecutionMode = "Sequential",
            DefaultCacheExpiration = TimeSpan.Zero
        };

        var result = _validator.Validate(options);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DefaultCacheExpiration");
    }
}
