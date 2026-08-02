// =============================================================================
// Benchmark for WorkflowException
// =============================================================================

using System;
using BenchmarkDotNet.Attributes;
using DotNetWorkflowEngine.Exceptions;

namespace DotNetWorkflowEngine.Benchmarks;

/// <summary>
/// Benchmarks various ways of creating and handling <see cref="WorkflowException"/>.
/// </summary>
[MemoryDiagnoser]
public class WorkflowExceptionBenchmarks
{
    // ------------------------------------------------------------------------
    // Parameters
    // ------------------------------------------------------------------------
    /// <summary>
    /// Number of iterations / exceptions to create in each benchmark.
    /// </summary>
    [Params(10, 100, 1000)]
    public int Count { get; set; }

    private string _message;
    private Exception _innerException;

    // ------------------------------------------------------------------------
    // Global setup
    // ------------------------------------------------------------------------
    [GlobalSetup]
    public void GlobalSetup()
    {
        _message = new string('x', 50); // a reasonably sized message
        _innerException = new InvalidOperationException("inner");
    }

    // ------------------------------------------------------------------------
    // Benchmarks
    // ------------------------------------------------------------------------

    /// <summary>
    /// Benchmark creating simple WorkflowException instances (message only).
    /// </summary>
    [Benchmark]
    public void CreateSimpleException()
    {
        for (int i = 0; i < Count; i++)
        {
            var ex = new WorkflowException(_message);
            // Prevent the compiler from optimizing away the variable
            GC.KeepAlive(ex);
        }
    }

    /// <summary>
    /// Benchmark creating WorkflowException instances with an inner exception.
    /// </summary>
    [Benchmark]
    public void CreateExceptionWithInner()
    {
        for (int i = 0; i < Count; i++)
        {
            var ex = new WorkflowException(_message, _innerException);
            GC.KeepAlive(ex);
        }
    }

    /// <summary>
    /// Benchmark throwing and catching WorkflowException in a tight loop.
    /// </summary>
    [Benchmark]
    public void ThrowAndCatchException()
    {
        for (int i = 0; i < Count; i++)
        {
            try
            {
                throw new WorkflowException(_message);
            }
            catch (WorkflowException ex)
            {
                // Swallow; keep reference to avoid elimination
                GC.KeepAlive(ex);
            }
        }
    }

    /// <summary>
    /// Benchmark creating WorkflowException and populating its Data dictionary.
    /// </summary>
    [Benchmark]
    public void CreateExceptionWithData()
    {
        for (int i = 0; i < Count; i++)
        {
            var ex = new WorkflowException(_message);
            // Populate a few entries to simulate realistic usage
            ex.Data["Index"] = i;
            ex.Data["Timestamp"] = DateTime.UtcNow;
            GC.KeepAlive(ex);
        }
    }

    /// <summary>
    /// Benchmark calling ToString on a pre‑created WorkflowException.
    /// </summary>
    [Benchmark]
    public void ExceptionToString()
    {
        var ex = new WorkflowException(_message, _innerException);
        for (int i = 0; i < Count; i++)
        {
            var s = ex.ToString();
            GC.KeepAlive(s);
        }
    }
}
