// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace DotNetWorkflowEngine.Integration;

/// <summary>
/// Factory for creating and managing HTTP clients with standardized configuration.
/// Provides connection pooling, timeout management, and retry policies for
/// external API communication. Each named client has its own configuration.
/// </summary>
public interface IHttpClientFactory
{
    /// <summary>
    /// Gets or creates a named HTTP client with configured defaults.
    /// </summary>
    HttpClient GetClient(string name = "default");

    /// <summary>
    /// Registers a custom HTTP client configuration.
    /// </summary>
    void RegisterClient(string name, HttpClientConfig config);
}

/// <summary>
/// Configuration for an HTTP client including base URL, timeouts, and headers.
/// </summary>
public class HttpClientConfig
{
    /// <summary>
    /// Base URL for API requests.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Request timeout in seconds. Default is 30 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Default headers to include with every request.
    /// </summary>
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();

    /// <summary>
    /// Maximum number of retry attempts for failed requests.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay in milliseconds before the first retry. Subsequent retries use exponential backoff.
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;
}

/// <summary>
/// Implementation of HTTP client factory using built-in IHttpClientFactory pattern.
/// </summary>
public class StandardHttpClientFactory : IHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Dictionary<string, HttpClientConfig> _configs = new();
    private readonly ILogger<StandardHttpClientFactory> _logger;

    public StandardHttpClientFactory(
        IHttpClientFactory httpClientFactory,
        ILogger<StandardHttpClientFactory> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets or creates an HTTP client with registered configuration.
    /// Applies base URL and default headers if configured.
    /// </summary>
    public HttpClient GetClient(string name = "default")
    {
        var client = _httpClientFactory.GetHttpClient(name);

        if (_configs.TryGetValue(name, out var config))
        {
            // Set base URL if configured
            if (!string.IsNullOrEmpty(config.BaseUrl))
                client.BaseAddress = new Uri(config.BaseUrl);

            // Set timeout
            client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);

            // Add default headers
            foreach (var header in config.DefaultHeaders)
            {
                if (!client.DefaultRequestHeaders.Contains(header.Key))
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        _logger.LogDebug("Created HTTP client: {ClientName}", name);
        return client;
    }

    /// <summary>
    /// Registers a custom HTTP client configuration.
    /// </summary>
    public void RegisterClient(string name, HttpClientConfig config)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Client name cannot be null or empty");

        _configs[name] = config;
        _logger.LogInformation("Registered HTTP client: {ClientName} -> {BaseUrl}", name, config.BaseUrl);
    }
}

/// <summary>
/// Helper extension for making HTTP requests with automatic retry logic.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Makes a GET request with automatic retry on transient failures.
    /// </summary>
    public static async Task<HttpResponseMessage> GetWithRetryAsync(
        this HttpClient client,
        string requestUri,
        int maxRetries = 3,
        int delayMs = 1000,
        ILogger? logger = null)
    {
        return await ExecuteWithRetryAsync(
            () => client.GetAsync(requestUri),
            maxRetries,
            delayMs,
            logger);
    }

    /// <summary>
    /// Makes a POST request with automatic retry on transient failures.
    /// </summary>
    public static async Task<HttpResponseMessage> PostWithRetryAsync(
        this HttpClient client,
        string requestUri,
        HttpContent content,
        int maxRetries = 3,
        int delayMs = 1000,
        ILogger? logger = null)
    {
        return await ExecuteWithRetryAsync(
            () => client.PostAsync(requestUri, content),
            maxRetries,
            delayMs,
            logger);
    }

    /// <summary>
    /// Executes an HTTP request with exponential backoff retry logic.
    /// Retries on transient failures (timeouts, 5xx errors).
    /// </summary>
    private static async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<Task<HttpResponseMessage>> request,
        int maxRetries,
        int delayMs,
        ILogger? logger)
    {
        int attempt = 0;
        while (true)
        {
            try
            {
                attempt++;
                var response = await request();

                // Success on 2xx or non-transient errors
                if (response.IsSuccessStatusCode || ShouldNotRetry(response.StatusCode))
                    return response;

                // Transient error - retry if attempts remaining
                if (attempt < maxRetries)
                {
                    var delayForThisAttempt = delayMs * (int)Math.Pow(2, attempt - 1);
                    logger?.LogWarning(
                        "HTTP request failed with {StatusCode}. Attempt {Attempt}/{MaxRetries}. Retrying in {DelayMs}ms",
                        response.StatusCode, attempt, maxRetries, delayForThisAttempt);

                    await Task.Delay(delayForThisAttempt);
                    continue;
                }

                return response;
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                var delayForThisAttempt = delayMs * (int)Math.Pow(2, attempt - 1);
                logger?.LogWarning(
                    ex,
                    "HTTP request failed with exception. Attempt {Attempt}/{MaxRetries}. Retrying in {DelayMs}ms",
                    attempt, maxRetries, delayForThisAttempt);

                await Task.Delay(delayForThisAttempt);
            }
        }
    }

    /// <summary>
    /// Determines if an HTTP status code should trigger a retry.
    /// Returns true for transient errors, false for permanent errors.
    /// </summary>
    private static bool ShouldNotRetry(System.Net.HttpStatusCode statusCode)
    {
        // Don't retry client errors (4xx) except 408 (timeout) and 429 (rate limit)
        return (int)statusCode >= 400 && (int)statusCode < 500
            && statusCode != System.Net.HttpStatusCode.RequestTimeout
            && statusCode != System.Net.HttpStatusCode.TooManyRequests;
    }
}
