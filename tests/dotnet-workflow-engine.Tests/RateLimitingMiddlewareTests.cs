// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
//
// Tests for RateLimitingMiddleware to verify rate limiting behavior
// =============================================================================

using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using DotNetWorkflowEngine.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetWorkflowEngine.Tests.Middleware;

/// <summary>
/// Contains unit tests for the <see cref="RateLimitingMiddleware"/> class, verifying rate limiting behavior.
/// </summary>
public class RateLimitingMiddlewareTests
{
    private readonly Mock<ILogger<RateLimitingMiddleware>> _loggerMock = new();
    private readonly global::DotNetWorkflowEngine.Middleware.RateLimitConfig _testConfig;

    public RateLimitingMiddlewareTests()
    {
        _testConfig = new global::DotNetWorkflowEngine.Middleware.RateLimitConfig
        {
            MaxRequests = 5,
            WindowSeconds = 1,
            RetryAfterSeconds = 1
        };
    }

    private DefaultHttpContext CreateHttpContext(string? userName = null, string? apiKey = null, string? ipAddress = "127.0.0.1")
    {
        var context = new DefaultHttpContext();

        // Set up connection with IP address
        if (!string.IsNullOrEmpty(ipAddress))
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
        }

        // Set up user if provided
        if (!string.IsNullOrEmpty(userName))
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userName)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            context.User = principal;
        }

        // Set up API key header if provided
        if (!string.IsNullOrEmpty(apiKey))
        {
            context.Request.Headers["X-API-Key"] = apiKey;
        }

        return context;
    }

    [Fact]
    public async Task InvokeAsync_RequestUnderLimit_PassesThrough()
    {
        // Arrange
        var nextMock = new Mock<RequestDelegate>();
        var middleware = new RateLimitingMiddleware(nextMock.Object, _loggerMock.Object, _testConfig);
        var context = CreateHttpContext();
        context.Request.Path = "/api/test";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextMock.Verify(n => n.Invoke(context), Times.Once);
        context.Response.StatusCode.Should().Be(200); // Default status code when next delegate is called
    }

    [Fact]
    public async Task InvokeAsync_RequestOverLimit_Returns429()
    {
        // Arrange
        var nextMock = new Mock<RequestDelegate>();
        var middleware = new RateLimitingMiddleware(nextMock.Object, _loggerMock.Object, _testConfig);
        var context = CreateHttpContext();
        context.Request.Path = "/api/test";

        // Exhaust the rate limit (5 requests)
        for (int i = 0; i < 5; i++)
        {
            await middleware.InvokeAsync(context);
            // Reset context for each call since it gets modified
            context = CreateHttpContext();
            context.Request.Path = "/api/test";
        }

        // Act - 6th request should be rate limited
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.TooManyRequests);
        context.Response.Headers.Should().ContainKey("Retry-After");
        context.Response.Headers["Retry-After"].ToString().Should().Be("1");
        context.Response.Headers.Should().ContainKey("X-RateLimit-Limit");
        context.Response.Headers["X-RateLimit-Limit"].ToString().Should().Be("5");
        context.Response.Headers.Should().ContainKey("X-RateLimit-Remaining");
        context.Response.Headers["X-RateLimit-Remaining"].ToString().Should().Be("0");
    }

    [Fact]
    public async Task InvokeAsync_LimitIsPerClient_KeyedByUser()
    {
        // Arrange
        var nextMock = new Mock<RequestDelegate>();
        var middleware = new RateLimitingMiddleware(nextMock.Object, _loggerMock.Object, _testConfig);

        // User 1 - exhaust their limit
        var user1Context = CreateHttpContext(userName: "user1");
        user1Context.Request.Path = "/api/test";

        for (int i = 0; i < 5; i++)
        {
            await middleware.InvokeAsync(user1Context);
            user1Context = CreateHttpContext(userName: "user1"); // Fresh context
            user1Context.Request.Path = "/api/test";
        }

        // User 2 - should still be able to make requests
        var user2Context = CreateHttpContext(userName: "user2");
        user2Context.Request.Path = "/api/test";

        // Act
        await middleware.InvokeAsync(user2Context);

        // Assert
        nextMock.Verify(n => n.Invoke(user2Context), Times.Once);
        user2Context.Response.StatusCode.Should().Be(200); // Should pass through
    }

    [Fact]
    public async Task InvokeAsync_LimitIsPerClient_KeyedByApiKey()
    {
        // Arrange
        var nextMock = new Mock<RequestDelegate>();
        var middleware = new RateLimitingMiddleware(nextMock.Object, _loggerMock.Object, _testConfig);

        // API Key 1 - exhaust their limit
        var apiKey1Context = CreateHttpContext(apiKey: "key1");
        apiKey1Context.Request.Path = "/api/test";

        for (int i = 0; i < 5; i++)
        {
            await middleware.InvokeAsync(apiKey1Context);
            apiKey1Context = CreateHttpContext(apiKey: "key1"); // Fresh context
            apiKey1Context.Request.Path = "/api/test";
        }

        // API Key 2 - should still be able to make requests
        var apiKey2Context = CreateHttpContext(apiKey: "key2");
        apiKey2Context.Request.Path = "/api/test";

        // Act
        await middleware.InvokeAsync(apiKey2Context);

        // Assert
        nextMock.Verify(n => n.Invoke(apiKey2Context), Times.Once);
        apiKey2Context.Response.StatusCode.Should().Be(200); // Should pass through
    }

    [Fact]
    public async Task InvokeAsync_LimitIsPerClient_KeyedByIpAddress()
    {
        // Arrange
        var nextMock = new Mock<RequestDelegate>();
        var middleware = new RateLimitingMiddleware(nextMock.Object, _loggerMock.Object, _testConfig);

        // IP 1 - exhaust their limit
        var ip1Context = CreateHttpContext(ipAddress: "192.168.1.1");
        ip1Context.Request.Path = "/api/test";

        for (int i = 0; i < 5; i++)
        {
            await middleware.InvokeAsync(ip1Context);
            ip1Context = CreateHttpContext(ipAddress: "192.168.1.1"); // Fresh context
            ip1Context.Request.Path = "/api/test";
        }

        // IP 2 - should still be able to make requests
        var ip2Context = CreateHttpContext(ipAddress: "192.168.1.2");
        ip2Context.Request.Path = "/api/test";

        // Act
        await middleware.InvokeAsync(ip2Context);

        // Assert
        nextMock.Verify(n => n.Invoke(ip2Context), Times.Once);
        ip2Context.Response.StatusCode.Should().Be(200); // Should pass through
    }

    [Fact]
    public async Task InvokeAsync_ExemptPaths_BypassRateLimiting()
    {
        // Arrange
        var nextMock = new Mock<RequestDelegate>();
        var middleware = new RateLimitingMiddleware(nextMock.Object, _loggerMock.Object, _testConfig);

        // Test various exempt paths
        var exemptPaths = new[] { "/health", "/status", "/ping", "/health/check", "/status/detailed" };

        foreach (var path in exemptPaths)
        {
            // Exhaust rate limit first
            var context = CreateHttpContext();
            context.Request.Path = "/api/test"; // Non-exempt path to consume limit

            for (int i = 0; i < 5; i++)
            {
                await middleware.InvokeAsync(context);
                context = CreateHttpContext(); // Fresh context
                context.Request.Path = "/api/test";
            }

            // Now test exempt path - should still pass through even though limit is exhausted
            var exemptContext = CreateHttpContext();
            exemptContext.Request.Path = path;

            // Act
            await middleware.InvokeAsync(exemptContext);

            // Assert
            nextMock.Verify(n => n.Invoke(exemptContext), Times.Once);
            exemptContext.Response.StatusCode.Should().Be(200); // Should pass through
        }
    }

    [Fact]
    public async Task InvokeAsync_WindowReset_RestoresCapacity()
    {
        // Arrange
        var nextMock = new Mock<RequestDelegate>();
        var middleware = new RateLimitingMiddleware(nextMock.Object, _loggerMock.Object, _testConfig);
        var context = CreateHttpContext();
        context.Request.Path = "/api/test";

        // Exhaust the rate limit
        for (int i = 0; i < 5; i++)
        {
            await middleware.InvokeAsync(context);
            context = CreateHttpContext(); // Fresh context
            context.Request.Path = "/api/test";
        }

        // Act - Request should be rate limited
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.TooManyRequests);

        // Reset the middleware's internal state by creating a new instance
        // This simulates a new window period
        var nextMockReset = new Mock<RequestDelegate>();
        var middlewareReset = new RateLimitingMiddleware(nextMockReset.Object, _loggerMock.Object, _testConfig);
        var resetContext = CreateHttpContext();
        resetContext.Request.Path = "/api/test";

        // Act - Request should now pass through
        await middlewareReset.InvokeAsync(resetContext);

        // Assert
        nextMockReset.Verify(n => n.Invoke(resetContext), Times.Once);
        resetContext.Response.StatusCode.Should().Be(200); // Should pass through after reset
    }

    [Fact]
    public async Task InvokeAsync_AddsRateLimitHeadersToRateLimitedResponses()
    {
        // Arrange
        var nextMock = new Mock<RequestDelegate>();
        var middleware = new RateLimitingMiddleware(nextMock.Object, _loggerMock.Object, _testConfig);
        var context = CreateHttpContext();
        context.Request.Path = "/api/test";

        // Exhaust the rate limit
        for (int i = 0; i < 5; i++)
        {
            await middleware.InvokeAsync(context);
            context = CreateHttpContext();
            context.Request.Path = "/api/test";
        }

        // Act - 6th request should be rate limited
        await middleware.InvokeAsync(context);

        // Assert - Rate limited responses have headers added directly (not via OnStarting)
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.TooManyRequests);
        context.Response.Headers.Should().ContainKey("X-RateLimit-Limit");
        context.Response.Headers["X-RateLimit-Limit"].ToString().Should().Be("5");
    }

    [Fact]
    public async Task InvokeAsync_UsesCorrectClientIdentifierPriority()
    {
        // Arrange
        var nextMock = new Mock<RequestDelegate>();
        var middleware = new RateLimitingMiddleware(nextMock.Object, _loggerMock.Object, _testConfig);

        // Test priority: User > API Key > IP

        // 1. User should take precedence
        var userContext = CreateHttpContext(
            userName: "testuser",
            apiKey: "test-key",
            ipAddress: "192.168.1.100"
        );
        userContext.Request.Path = "/api/test";

        // Consume limit via user identity
        for (int i = 0; i < 5; i++)
        {
            await middleware.InvokeAsync(userContext);
            userContext = CreateHttpContext(
                userName: "testuser",
                apiKey: "test-key",
                ipAddress: "192.168.1.100"
            );
            userContext.Request.Path = "/api/test";
        }

        // 2. Same user, different API key/IP should still be blocked (user takes precedence)
        var userContextSameUser = CreateHttpContext(
            userName: "testuser", // Same user
            apiKey: "different-key",
            ipAddress: "10.0.0.1"
        );
        userContextSameUser.Request.Path = "/api/test";

        // Act
        await middleware.InvokeAsync(userContextSameUser);

        // Assert - Should be rate limited because user is same
        userContextSameUser.Response.StatusCode.Should().Be((int)HttpStatusCode.TooManyRequests);

        // 3. Different user should work (different client identifier)
        var differentUserContext = CreateHttpContext(
            userName: "different-user", // Different user
            apiKey: "test-key",
            ipAddress: "192.168.1.100"
        );
        differentUserContext.Request.Path = "/api/test";

        // Act
        await middleware.InvokeAsync(differentUserContext);

        // Assert - Should pass through because different user
        nextMock.Verify(n => n.Invoke(differentUserContext), Times.Once);
        differentUserContext.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_DefaultConfig_UsesSensibleDefaults()
    {
        // Arrange
        var nextMock = new Mock<RequestDelegate>();
        var middleware = new RateLimitingMiddleware(nextMock.Object, _loggerMock.Object, config: null);
        var context = CreateHttpContext();
        context.Request.Path = "/api/test";

        // Act
        await middleware.InvokeAsync(context);

        // Assert - should use default values and pass through
        nextMock.Verify(n => n.Invoke(context), Times.Once);
        context.Response.StatusCode.Should().Be(200);
    }
}
