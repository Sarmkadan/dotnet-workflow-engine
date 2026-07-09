// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace DotNetWorkflowEngine.Caching;

/// <summary>
/// No-operation cache service that does nothing (used when caching is disabled).
/// Implements ICacheService interface to maintain consistency.
/// </summary>
public class NoOpCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key) where T : class
    {
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        return Task.FromResult(false);
    }

    public Task<T> GetOrLoadAsync<T>(string key, Func<Task<T>> provider, TimeSpan? expiration = null) where T : class
    {
        return provider();
    }
}