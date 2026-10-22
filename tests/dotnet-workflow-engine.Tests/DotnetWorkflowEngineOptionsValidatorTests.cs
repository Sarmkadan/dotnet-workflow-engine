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

    [Fact]
    public void Validate_InvalidRetryCount_ReturnsError()
    {
        var options = new DotnetWorkflowEngineOptions
        {
            ConnectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;",
            RetryCount = 11, // Outside range 0-10
            RetryDelaySeconds = 5
        };

        var result = _validator.Validate(options);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RetryCount");
    }

    [Fact]
    public void Validate_InvalidRetryDelaySeconds_ReturnsError()
    {
        var options = new DotnetWorkflowEngineOptions
        {
            ConnectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;",
            RetryCount = 3,
            RetryDelaySeconds = 0 // Outside range 1-3600
        };

        var result = _validator.Validate(options);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RetryDelaySeconds");
    }

    [Fact]
    public void Validate_RetryCountZeroWithZeroDelay_IsValid()
    {
        var options = new DotnetWorkflowEngineOptions
        {
            ConnectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;",
            RetryCount = 0,
            RetryDelaySeconds = 0 // Should be valid when retry count is 0
        };

        var result = _validator.Validate(options);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_RetryCountGreaterThanZeroWithZeroDelay_IsInvalid()
    {
        var options = new DotnetWorkflowEngineOptions
        {
            ConnectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;",
            RetryCount = 3,
            RetryDelaySeconds = 0 // Should be invalid when retry count > 0
        };

        var result = _validator.Validate(options);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("RetryDelaySeconds must be positive when RetryCount is greater than 0"));
    }

    [Fact]
    public void Validate_RetryCountGreaterThanZeroWithPositiveDelay_IsValid()
    {
        var options = new DotnetWorkflowEngineOptions
        {
            ConnectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;",
            RetryCount = 3,
            RetryDelaySeconds = 5
        };

        var result = _validator.Validate(options);

        result.IsValid.Should().BeTrue();
    }
}
