// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DotNetWorkflowEngine.Middleware;

/// <summary>
/// Rate limiting middleware that enforces request quotas per client/user.
/// Uses a token bucket algorithm to allow burst traffic while maintaining
/// average rate limits. Configured limits prevent abuse and ensure fair resource
/// allocation. Bypasses rate limiting for service-to-service communication.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitConfig _config;
    private readonly Dictionary<string, RateLimitBucket> _buckets = new();

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        RateLimitConfig? config = null)
    {
        _next = next;
        _logger = logger;
        _config = config ?? new RateLimitConfig();
    }

    /// <summary>
    /// Invokes the middleware, checking rate limit quota for the current request.
    /// Returns 429 Too Many Requests if quota is exceeded, otherwise proceeds.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for health checks and internal endpoints
        if (IsExemptPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var clientId = GetClientIdentifier(context);
        var bucket = GetOrCreateBucket(clientId);

        if (!bucket.TryConsume(1))
        {
            _logger.LogWarning(
                "Rate limit exceeded for client {ClientId}. Limit: {Limit} req/{Window}",
                clientId,
                _config.MaxRequests,
                _config.WindowSeconds);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.Add("Retry-After", _config.RetryAfterSeconds.ToString());
            context.Response.Headers.Add("X-RateLimit-Limit", _config.MaxRequests.ToString());
            context.Response.Headers.Add("X-RateLimit-Remaining", "0");

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                retryAfter = _config.RetryAfterSeconds
            });

            return;
        }

        // Add rate limit headers to response
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Add("X-RateLimit-Limit", _config.MaxRequests.ToString());
            context.Response.Headers.Add("X-RateLimit-Remaining", bucket.TokensRemaining.ToString());
            context.Response.Headers.Add("X-RateLimit-Reset", bucket.ResetTime.ToUnixTimeSeconds().ToString());
            return Task.CompletedTask;
        });

        await _next(context);
    }

    /// <summary>
    /// Extracts or creates a client identifier from the request.
    /// Priority: authenticated user ID > API key > IP address
    /// </summary>
    private string GetClientIdentifier(HttpContext context)
    {
        // Use authenticated user if available
        if (!string.IsNullOrEmpty(context.User?.Identity?.Name))
            return $"user:{context.User.Identity.Name}";

        // Use API key from header if present
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
            return $"apikey:{apiKey}";

        // Fall back to IP address
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{remoteIp}";
    }

    /// <summary>
    /// Gets or creates a rate limit bucket for a client.
    /// Buckets are cleaned up when they expire to prevent memory leaks.
    /// </summary>
    private RateLimitBucket GetOrCreateBucket(string clientId)
    {
        lock (_buckets)
        {
            // Clean up expired buckets periodically
            var expiredKeys = _buckets
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
                _buckets.Remove(key);

            if (!_buckets.TryGetValue(clientId, out var bucket))
            {
                bucket = new RateLimitBucket(_config.MaxRequests, _config.WindowSeconds);
                _buckets[clientId] = bucket;
            }

            return bucket;
        }
    }

    /// <summary>
    /// Determines if a request path should be exempt from rate limiting.
    /// Typically exempts health checks and status endpoints.
    /// </summary>
    private bool IsExemptPath(PathString path)
    {
        var exemptPaths = new[] { "/health", "/status", "/ping" };
        return exemptPaths.Any(p => path.StartsWithSegments(p));
    }
}

/// <summary>
/// Configuration for rate limiting behavior.
/// </summary>
public class RateLimitConfig
{
    public int MaxRequests { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60;
    public int RetryAfterSeconds { get; set; } = 60;
}

/// <summary>
/// Token bucket for tracking and limiting requests per client.
/// Implements the token bucket algorithm for fair rate limiting.
/// </summary>
internal class RateLimitBucket
{
    private readonly int _maxTokens;
    private readonly double _refillRate;
    private double _tokens;
    private DateTime _lastRefill;

    public DateTime ResetTime { get; private set; }
    public int TokensRemaining => (int)_tokens;
    public bool IsExpired => DateTime.UtcNow > ResetTime;

    public RateLimitBucket(int maxTokens, int windowSeconds)
    {
        _maxTokens = maxTokens;
        _tokens = maxTokens;
        _refillRate = maxTokens / (double)windowSeconds; // tokens per second
        _lastRefill = DateTime.UtcNow;
        ResetTime = DateTime.UtcNow.AddSeconds(windowSeconds);
    }

    /// <summary>
    /// Attempts to consume a token from the bucket. Refills tokens based on
    /// elapsed time since the last refill, then checks if quota allows consumption.
    /// </summary>
    public bool TryConsume(int count)
    {
        lock (this)
        {
            RefillTokens();

            if (_tokens >= count)
            {
                _tokens -= count;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Refills tokens based on elapsed time and the refill rate.
    /// Ensures a smooth flow of available quota over the time window.
    /// </summary>
    private void RefillTokens()
    {
        var now = DateTime.UtcNow;
        var elapsedSeconds = (now - _lastRefill).TotalSeconds;
        var tokensToAdd = elapsedSeconds * _refillRate;

        _tokens = Math.Min(_maxTokens, _tokens + tokensToAdd);
        _lastRefill = now;
    }
}
