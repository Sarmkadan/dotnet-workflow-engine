using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using ExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

namespace DotNetWorkflowEngine.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for expression evaluation and conditional logic.
/// Measures performance of condition evaluation in workflows.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net100)]
public class ExpressionEvaluationBenchmarks
{
    private ActivityService _activityService;
    private RetryPolicyService _retryPolicyService;
    private Activity _trueConditionActivity;
    private Activity _falseConditionActivity;
    private Activity _complexConditionActivity;
    private ExecutionContext _trueContext;
    private ExecutionContext _falseContext;
    private ExecutionContext _complexContext;

    [GlobalSetup]
    public void Setup()
    {
        _retryPolicyService = new RetryPolicyService();
        _activityService = new ActivityService(_retryPolicyService);
        _activityService.RegisterHandler("Simple", new SimpleActivityHandler());

        // Create activities with different condition expressions
        _trueConditionActivity = new Activity
        {
            Id = "true_condition_activity",
            Name = "True Condition Activity",
            ConditionExpression = "true",
            HandlerType = "Simple",
            ActivityType = "TestActivity"
        };

        _falseConditionActivity = new Activity
        {
            Id = "false_condition_activity",
            Name = "False Condition Activity",
            ConditionExpression = "false",
            HandlerType = "Simple",
            ActivityType = "TestActivity"
        };

        _complexConditionActivity = new Activity
        {
            Id = "complex_condition_activity",
            Name = "Complex Condition Activity",
            ConditionExpression = "${orderAmount} > 1000 && ${customerTier} == 'premium'",
            HandlerType = "Simple",
            ActivityType = "TestActivity"
        };

        // Create execution contexts with different variable values
        _trueContext = new ExecutionContext
        {
            WorkflowInstanceId = Guid.NewGuid().ToString(),
            ActivityId = "test_activity",
            Variables = new Dictionary<string, object?> { { "testVar", true } }
        };

        _falseContext = new ExecutionContext
        {
            WorkflowInstanceId = Guid.NewGuid().ToString(),
            ActivityId = "test_activity",
            Variables = new Dictionary<string, object?> { { "testVar", false } }
        };

        _complexContext = new ExecutionContext
        {
            WorkflowInstanceId = Guid.NewGuid().ToString(),
            ActivityId = "test_activity",
            Variables = new Dictionary<string, object?>
            {
                { "orderAmount", 1500 },
                { "customerTier", "premium" }
            }
        };
    }

    [Benchmark]
    public async Task Execute_Activity_With_True_Condition()
    {
        await _activityService.ExecuteAsync(_trueConditionActivity, _trueContext);
    }

    [Benchmark]
    public async Task Execute_Activity_With_False_Condition()
    {
        await _activityService.ExecuteAsync(_falseConditionActivity, _falseContext);
    }

    [Benchmark]
    public async Task Execute_Activity_With_Complex_Condition()
    {
        await _activityService.ExecuteAsync(_complexConditionActivity, _complexContext);
    }

    [Benchmark]
    public bool Evaluate_Simple_True_Condition()
    {
        return EvaluateCondition("true", _trueContext);
    }

    [Benchmark]
    public bool Evaluate_Simple_False_Condition()
    {
        return EvaluateCondition("false", _falseContext);
    }

    [Benchmark]
    public bool Evaluate_Variable_Reference_Condition()
    {
        return EvaluateCondition("${testVar}", _trueContext);
    }

    [Benchmark]
    public bool Evaluate_Complex_Expression()
    {
        return EvaluateCondition("${orderAmount} > 1000 && ${customerTier} == 'premium'", _complexContext);
    }

    /// <summary>
    /// Simple activity handler for benchmarks.
    /// </summary>
    private class SimpleActivityHandler : ActivityService.IActivityHandler
    {
        public async Task<Dictionary<string, object?>> ExecuteAsync(Activity activity, ExecutionContext context)
        {
            await Task.Delay(1); // Simulate minimal work
            return new Dictionary<string, object?> { { "Result", "Success" } };
        }
    }

    /// <summary>
    /// Evaluates a condition expression (copied from ActivityService for benchmarking).
    /// </summary>
    private bool EvaluateCondition(string expression, ExecutionContext context)
    {
        // Simple condition evaluation - can be extended with expression evaluator
        if (expression == "true")
            return true;
        if (expression == "false")
            return false;

        // Check if it's a variable reference
        if (expression.StartsWith("${") && expression.EndsWith("}"))
        {
            var varName = expression.Substring(2, expression.Length - 3);
            var value = context.GetVariable(varName);
            return value is true or 1 or "true";
        }

        return true;
    }
}
