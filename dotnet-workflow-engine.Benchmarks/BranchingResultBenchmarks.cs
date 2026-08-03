using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Benchmarks;

[MemoryDiagnoser]
public class BranchingResultBenchmarks
{
    [Params(10, 100, 1000)]
    public int N { get; set; }

    private List<Transition>? _transitions;
    private List<TransitionEvaluationError>? _errors;

    [GlobalSetup]
    public void Setup()
    {
        // Pre-allocate data to isolate the cost of BranchingResult instantiation
        _transitions = new List<Transition>(N);
        _errors = new List<TransitionEvaluationError>(N);

        for (int i = 0; i < N; i++)
        {
            _transitions.Add(new Transition { Id = i.ToString() });
            _errors.Add(new TransitionEvaluationError { TransitionId = i.ToString() });
        }
    }

    /// <summary>
    /// Benchmarks the static factory method for creating an empty result.
    /// </summary>
    [Benchmark]
    public BranchingResult Create_Empty()
    {
        return BranchingResult.Empty("activity_test");
    }

    /// <summary>
    /// Benchmarks instantiation with a list of selected transitions.
    /// </summary>
    [Benchmark]
    public BranchingResult Create_WithSelectedTransitions()
    {
        return new BranchingResult
        {
            ActivityId = "activity_test",
            SelectedTransitions = new List<Transition>(_transitions!)
        };
    }

    /// <summary>
    /// Benchmarks instantiation with a list of evaluation errors.
    /// </summary>
    [Benchmark]
    public BranchingResult Create_WithErrors()
    {
        return new BranchingResult
        {
            ActivityId = "activity_test",
            EvaluationErrors = new List<TransitionEvaluationError>(_errors!)
        };
    }

    /// <summary>
    /// Benchmarks full instantiation with all collections populated.
    /// </summary>
    [Benchmark]
    public BranchingResult Create_Full()
    {
        return new BranchingResult
        {
            ActivityId = "activity_test",
            SelectedTransitions = new List<Transition>(_transitions!),
            SkippedTransitions = new List<Transition>(_transitions!),
            EvaluationErrors = new List<TransitionEvaluationError>(_errors!),
            AnyConditionMatched = true,
            UsedDefaultTransition = false
        };
    }
}
