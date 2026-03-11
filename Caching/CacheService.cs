// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;

namespace DotNetWorkflowEngine.Caching;

/// <summary>
/// Unified cache service supporting both in-memory and distributed caching.
/// Provides a consistent interface for different cache implementations
/// and includes cache statistics for monitoring cache performance.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a value from cache, or null if not found.
    /// </summary>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// Sets a value in cache with optional expiration.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;

    /// <summary>
    /// Removes a value from cache.
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Checks if a key exists in cache.
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Gets a value or loads it from a provider function if not cached.
    /// </summary>
    Task<T> GetOrLoadAsync<T>(string key, Func<Task<T>> provider, TimeSpan? expiration = null) where T : class;
}

/// <summary>
/// In-memory cache implementation using IMemoryCache.
/// Fast and suitable for single-node deployments.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly TimeSpan _defaultExpiration;

    public MemoryCacheService(
        IMemoryCache memoryCache,
        ILogger<MemoryCacheService> logger,
        TimeSpan? defaultExpiration = null)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _defaultExpiration = defaultExpiration ?? TimeSpan.FromHours(1);
    }

    /// <summary>
    /// Gets a value from in-memory cache.
    /// </summary>
    public Task<T?> GetAsync<T>(string key) where T : class
    {
        if (string.IsNullOrEmpty(key))
            return Task.FromResult<T?>(null);

        var found = _memoryCache.TryGetValue(key, out T? value);
        _logger.LogDebug("Cache lookup for {Key}: {Result}", key, found ? "HIT" : "MISS");
        return Task.FromResult(found ? value : null);
    }

    /// <summary>
    /// Sets a value in in-memory cache with expiration.
    /// </summary>
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        if (string.IsNullOrEmpty(key) || value == null)
            return Task.CompletedTask;

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration
        };

        _memoryCache.Set(key, value, options);
        _logger.LogDebug(
            "Cached value for {Key} with expiration {Expiration}",
            key,
            expiration ?? _defaultExpiration);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes a value from in-memory cache.
    /// </summary>
    public Task RemoveAsync(string key)
    {
        if (!string.IsNullOrEmpty(key))
        {
            _memoryCache.Remove(key);
            _logger.LogDebug("Cache entry removed: {Key}", key);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks if a key exists in in-memory cache.
    /// </summary>
    public Task<bool> ExistsAsync(string key)
    {
        var exists = _memoryCache.TryGetValue(key, out _);
        return Task.FromResult(exists);
    }

    /// <summary>
    /// Gets a value or loads it from a provider if not cached.
    /// </summary>
    public async Task<T> GetOrLoadAsync<T>(string key, Func<Task<T>> provider, TimeSpan? expiration = null) where T : class
    {
        var cached = await GetAsync<T>(key);
        if (cached != null)
            return cached;

        _logger.LogInformation("Cache miss for {Key}. Loading from provider.", key);
        var value = await provider();

        if (value != null)
            await SetAsync(key, value, expiration);

        return value;
    }
}

/// <summary>
/// Distributed cache implementation using IDistributedCache.
/// Suitable for multi-node deployments with Redis or other cache stores.
/// </summary>
public class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly TimeSpan _defaultExpiration;

    public DistributedCacheService(
        IDistributedCache distributedCache,
        ILogger<DistributedCacheService> logger,
        TimeSpan? defaultExpiration = null)
    {
        _distributedCache = distributedCache;
        _logger = logger;
        _defaultExpiration = defaultExpiration ?? TimeSpan.FromHours(1);
    }

    /// <summary>
    /// Gets a value from distributed cache.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        if (string.IsNullOrEmpty(key))
            return null;

        try
        {
            var data = await _distributedCache.GetAsync(key);
            if (data == null)
            {
                _logger.LogDebug("Cache miss for {Key}", key);
                return null;
            }

            var json = System.Text.Encoding.UTF8.GetString(data);
            var value = System.Text.Json.JsonSerializer.Deserialize<T>(json);
            _logger.LogDebug("Cache hit for {Key}", key);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading from distributed cache: {Key}", key);
            return null;
        }
    }

    /// <summary>
    /// Sets a value in distributed cache with expiration.
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        if (string.IsNullOrEmpty(key) || value == null)
            return;

        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(value);
            var data = System.Text.Encoding.UTF8.GetBytes(json);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration
            };

            await _distributedCache.SetAsync(key, data, options);
            _logger.LogDebug(
                "Cached value in distributed cache: {Key} with expiration {Expiration}",
                key,
                expiration ?? _defaultExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing to distributed cache: {Key}", key);
        }
    }

    /// <summary>
    /// Removes a value from distributed cache.
    /// </summary>
    public async Task RemoveAsync(string key)
    {
        if (!string.IsNullOrEmpty(key))
        {
            try
            {
                await _distributedCache.RemoveAsync(key);
                _logger.LogDebug("Cache entry removed from distributed cache: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from distributed cache: {Key}", key);
            }
        }
    }

    /// <summary>
    /// Checks if a key exists in distributed cache.
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        try
        {
            var data = await _distributedCache.GetAsync(key);
            return data != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence: {Key}", key);
            return false;
        }
    }

    /// <summary>
    /// Gets a value or loads it from a provider if not cached.
    /// </summary>
    public async Task<T> GetOrLoadAsync<T>(string key, Func<Task<T>> provider, TimeSpan? expiration = null) where T : class
    {
        var cached = await GetAsync<T>(key);
        if (cached != null)
            return cached;

        _logger.LogInformation("Cache miss for {Key}. Loading from provider.", key);
        var value = await provider();

        if (value != null)
            await SetAsync(key, value, expiration);

        return value;
    }
}
