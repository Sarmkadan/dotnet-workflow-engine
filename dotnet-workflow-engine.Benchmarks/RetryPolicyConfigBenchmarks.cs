using BenchmarkDotNet.Attributes;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Enums;

namespace DotNetWorkflowEngine.Benchmarks;

[MemoryDiagnoser]
public class RetryPolicyConfigBenchmarks
{
    private RetryPolicyConfig _fixedDelayConfig = null!;
    private RetryPolicyConfig _exponentialBackoffConfig = null!;
    private List<string> _retryableExceptionTypes = null!;

    [Params(1, 5, 10)]
    public int AttemptNumber { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _fixedDelayConfig = RetryPolicyConfig.CreateFixedDelay(10, 1000);
        _exponentialBackoffConfig = RetryPolicyConfig.CreateExponentialBackoff(10, 1000, 300000);
        
        _retryableExceptionTypes = new List<string> { "TimeoutException", "HttpRequestException", "SocketException" };
        _exponentialBackoffConfig.RetryableExceptionTypes = _retryableExceptionTypes;
    }

    [Benchmark]
    public int CalculateFixedDelay()
    {
        return _fixedDelayConfig.CalculateDelayMs(AttemptNumber);
    }

    [Benchmark]
    public int CalculateExponentialBackoff()
    {
        return _exponentialBackoffConfig.CalculateDelayMs(AttemptNumber);
    }

    [Benchmark]
    public bool ShouldRetryBasic()
    {
        return _exponentialBackoffConfig.ShouldRetry(AttemptNumber);
    }

    [Benchmark]
    public bool ShouldRetryWithExceptionMatch()
    {
        return _exponentialBackoffConfig.ShouldRetry(AttemptNumber, "TimeoutException");
    }

    [Benchmark]
    public bool ShouldRetryWithNoExceptionMatch()
    {
        return _exponentialBackoffConfig.ShouldRetry(AttemptNumber, "UnknownException");
    }
}
