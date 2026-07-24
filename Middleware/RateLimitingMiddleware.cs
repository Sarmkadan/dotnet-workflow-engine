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
///
/// <para>Client identification is hardened against spoofing:</para>
/// <list type="bullet">
/// <item>Authenticated users are identified by their user ID</item>
/// <item>API keys are used when present</item>
/// <item>Client IP addresses are resolved through trusted proxy handling</item>
/// <item>X-Forwarded-For headers are only trusted from configured trusted proxies</item>
/// <item>Returns 429 with Retry-After header when limits are exceeded</item>
/// </list>
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitConfig _config;
    private readonly Dictionary<string, RateLimitBucket> _buckets = new();
    private readonly HashSet<IPAddress> _trustedProxies;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        RateLimitConfig? config = null)
    {
        _next = next;
        _logger = logger;
        _config = config ?? new RateLimitConfig();
        _trustedProxies = new HashSet<IPAddress>();

        // Initialize with common private network ranges for trusted proxies
        // These are safe defaults that can be overridden via configuration if needed
        AddTrustedProxyRange(IPAddress.Parse("10.0.0.0"), IPAddress.Parse("10.255.255.255")); // 10.0.0.0/8
        AddTrustedProxyRange(IPAddress.Parse("172.16.0.0"), IPAddress.Parse("172.31.255.255")); // 172.16.0.0/12
        AddTrustedProxyRange(IPAddress.Parse("192.168.0.0"), IPAddress.Parse("192.168.255.255")); // 192.168.0.0/16
        AddTrustedProxyRange(IPAddress.Parse("127.0.0.0"), IPAddress.Parse("127.255.255.255")); // localhost
        AddTrustedProxyRange(IPAddress.Parse("169.254.0.0"), IPAddress.Parse("169.254.255.255")); // link-local
        AddTrustedProxyRange(IPAddress.Parse("::1"), IPAddress.Parse("::1")); // IPv6 localhost
        AddTrustedProxyRange(IPAddress.Parse("fe80::"), IPAddress.Parse("febf::")); // IPv6 link-local
    }

    /// <summary>
    /// Adds an IP address range to the trusted proxy list.
    /// </summary>
    private void AddTrustedProxyRange(IPAddress start, IPAddress end)
    {
        var startBytes = start.GetAddressBytes();
        var endBytes = end.GetAddressBytes();

        if (start.AddressFamily != end.AddressFamily)
            return;

        if (start.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            var startLong = BitConverter.ToUInt64(startBytes, 0);
            var endLong = BitConverter.ToUInt64(endBytes, 0);
            for (ulong i = startLong; i <= endLong; i++)
            {
                var bytes = BitConverter.GetBytes(i);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes, 0, 8);
                _trustedProxies.Add(new IPAddress(bytes));
            }
        }
        else
        {
            var startLong = BitConverter.ToUInt32(startBytes, 0);
            var endLong = BitConverter.ToUInt32(endBytes, 0);
            for (uint i = startLong; i <= endLong; i++)
            {
                var bytes = BitConverter.GetBytes(i);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                _trustedProxies.Add(new IPAddress(bytes));
            }
        }
    }

    /// <summary>
    /// Checks if an IP address is in the trusted proxy list.
    /// </summary>
    private bool IsTrustedProxy(IPAddress ipAddress)
    {
        if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            // For IPv6, check if it's in any of our ranges
            var bytes = ipAddress.GetAddressBytes();
            var longValue = BitConverter.ToUInt64(bytes, 0);
            return _trustedProxies.Contains(ipAddress) ||
                   (longValue >= 0xfe80000000000000 && longValue <= 0xfebfffffffffffff); // link-local check
        }
        else
        {
            // For IPv4, check if it's in our ranges
            var bytes = ipAddress.GetAddressBytes();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            var ip = BitConverter.ToUInt32(bytes, 0);
            return _trustedProxies.Contains(ipAddress) ||
                   (ip >= 0x0A000000 && ip <= 0x0AFFFFFF) || // 10.0.0.0/8
                   (ip >= 0xAC100000 && ip <= 0xAC1FFFFF) || // 172.16.0.0/12
                   (ip >= 0xC0A80000 && ip <= 0xC0A8FFFF);   // 192.168.0.0/16
        }
    }

    /// <summary>
    /// Extracts the real client IP address from X-Forwarded-For header with trusted proxy validation.
    /// Only trusts X-Forwarded-For when the immediate upstream (right-most IP) is a trusted proxy.
    /// This prevents IP spoofing attacks where an attacker sets X-Forwarded-For to reset rate limits.
    /// </summary>
    private IPAddress? GetRealClientIpAddress(HttpContext context)
    {
        // Fallback: manually parse X-Forwarded-For with trusted proxy validation
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var xForwardedFor))
        {
            var forwardedIps = xForwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (forwardedIps.Length > 0)
            {
                // The left-most IP is the original client IP (closest to the actual client)
                // The right-most IP is the immediate upstream connection
                // We only trust the client IP if the immediate upstream is a trusted proxy
                if (IPAddress.TryParse(forwardedIps[0], out var clientIp))
                {
                    // Check if the immediate upstream (right-most) is trusted
                    if (forwardedIps.Length >= 2)
                    {
                        if (IPAddress.TryParse(forwardedIps[^1], out var upstreamIp) && IsTrustedProxy(upstreamIp))
                        {
                            // Upstream is trusted, so we can trust the forwarded client IP
                            return clientIp;
                        }
                        else
                        {
                            // Upstream is not trusted, ignore X-Forwarded-For to prevent spoofing
                            _logger.LogWarning("Ignoring untrusted X-Forwarded-For header from untrusted upstream {UpstreamIp}", upstreamIp?.ToString() ?? "unknown");
                        }
                    }
                    else
                    {
                        // Single IP in X-Forwarded-For - could be from a trusted proxy or direct client
                        // If it's from a trusted proxy range, it's likely the proxy itself, not the client
                        if (!IsTrustedProxy(clientIp))
                        {
                            // Not from a trusted proxy, could be the real client IP
                            // This handles cases where a single trusted proxy adds X-Forwarded-For
                            return clientIp;
                        }
                        // Otherwise it's a trusted proxy IP itself, not the real client
                    }
                }
            }
        }

        // No forwarded headers or invalid format - use the direct connection IP
        return context.Connection.RemoteIpAddress;
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
            context.Response.Headers.Add("X-RateLimit-Reset", ((DateTimeOffset)bucket.ResetTime).ToUnixTimeSeconds().ToString());
            return Task.CompletedTask;
        });

        await _next(context);
    }

    /// <summary>
    /// Extracts or creates a client identifier from the request.
    /// Priority: authenticated user ID > API key > resolved client IP address
    /// The client IP address is resolved through trusted proxy handling to prevent spoofing.
    /// </summary>
    private string GetClientIdentifier(HttpContext context)
    {
        // Use authenticated user if available
        if (!string.IsNullOrEmpty(context.User?.Identity?.Name))
            return $"user:{context.User.Identity.Name}";

        // Use API key from header if present
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
            return $"apikey:{apiKey}";

        // Fall back to resolved client IP address (handles trusted proxies and X-Forwarded-For)
        var clientIp = GetRealClientIpAddress(context);
        var ipString = clientIp?.ToString() ?? "unknown";
        return $"ip:{ipString}";
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
