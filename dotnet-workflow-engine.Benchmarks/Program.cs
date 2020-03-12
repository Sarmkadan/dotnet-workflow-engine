using BenchmarkDotNet.Running;
using DotNetWorkflowEngine.Benchmarks.Benchmarks;

Console.WriteLine("dotnet-workflow-engine Benchmarks");
Console.WriteLine("==============================");
Console.WriteLine();
Console.WriteLine("Available benchmarks:");
Console.WriteLine("  1. ActivityExecutionBenchmarks - Measures activity execution performance");
Console.WriteLine("  2. WorkflowExecutionBenchmarks - Measures workflow execution performance");
Console.WriteLine("  3. WorkflowDefinitionBenchmarks - Measures workflow definition operations");
Console.WriteLine("  4. ConcurrentExecutionBenchmarks - Measures concurrent execution scalability");
Console.WriteLine("  5. ExpressionEvaluationBenchmarks - Measures expression evaluation performance");
Console.WriteLine();
Console.WriteLine("Usage:");
Console.WriteLine("  dotnet run -- [benchmark-name]");
Console.WriteLine();
Console.WriteLine("Examples:");
Console.WriteLine("  dotnet run -- ActivityExecutionBenchmarks");
Console.WriteLine("  dotnet run -- *");
Console.WriteLine();

var cliArgs = Environment.GetCommandLineArgs();

if (cliArgs.Length > 1)
{
    var benchmarkName = cliArgs[1];

    switch (benchmarkName.ToLower())
    {
        case "activityexecutionbenchmarks":
            BenchmarkRunner.Run<ActivityExecutionBenchmarks>();
            break;
        case "workflowexecutionbenchmarks":
            BenchmarkRunner.Run<WorkflowExecutionBenchmarks>();
            break;
        case "workflowdefinitionbenchmarks":
            BenchmarkRunner.Run<WorkflowDefinitionBenchmarks>();
            break;
        case "concurrentexecutionbenchmarks":
            BenchmarkRunner.Run<ConcurrentExecutionBenchmarks>();
            break;
        case "expressionevaluationbenchmarks":
            BenchmarkRunner.Run<ExpressionEvaluationBenchmarks>();
            break;
        case "*":
        case "all":
            Console.WriteLine("Running all benchmarks...");
            BenchmarkRunner.Run<ActivityExecutionBenchmarks>();
            BenchmarkRunner.Run<WorkflowExecutionBenchmarks>();
            BenchmarkRunner.Run<WorkflowDefinitionBenchmarks>();
            BenchmarkRunner.Run<ConcurrentExecutionBenchmarks>();
            BenchmarkRunner.Run<ExpressionEvaluationBenchmarks>();
            break;
        default:
            Console.WriteLine($"Unknown benchmark: {benchmarkName}");
            Console.WriteLine("Valid benchmarks: ActivityExecutionBenchmarks, WorkflowExecutionBenchmarks, WorkflowDefinitionBenchmarks, ConcurrentExecutionBenchmarks, ExpressionEvaluationBenchmarks, *");
            break;
    }
}
else
{
    Console.WriteLine("No benchmark specified. Running all benchmarks...");
    BenchmarkRunner.Run<ActivityExecutionBenchmarks>();
    BenchmarkRunner.Run<WorkflowExecutionBenchmarks>();
    BenchmarkRunner.Run<WorkflowDefinitionBenchmarks>();
    BenchmarkRunner.Run<ConcurrentExecutionBenchmarks>();
    BenchmarkRunner.Run<ExpressionEvaluationBenchmarks>();
}
