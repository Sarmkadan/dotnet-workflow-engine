// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Configuration;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using DotNetWorkflowEngine.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

partial class Program
{
    static async Task Main(string[] args)
    {
        // Configure services with IOptions pattern
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddWorkflowEngine(options =>
        {
            options.ConnectionString = "Server=localhost;Database=WorkflowEngine;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;";
            options.DefaultRetryPolicy = RetryPolicyConfig.CreateExponentialBackoff(3, 1000, 300000);
            options.EnableAuditLogging = true;
            options.MaxConcurrentWorkflows = 100;
            options.DefaultActivityTimeoutSeconds = 300;
            options.ValidateWorkflowsOnLoad = true;
            options.CachingEnabled = true;
            options.CacheProvider = "Memory";
            options.EnableBackgroundJobs = true;
            options.EnableMetrics = true;
            options.EnableAuditTrail = true;
            options.EnableRequestLogging = true;
            options.EnableRateLimiting = true;
            options.EnableCors = true;
            options.EnableWebhookValidation = true;
            options.EnableActivityValidation = true;
            options.EnableWorkflowValidation = true;
            options.EnableExpressionEvaluation = true;
            options.MaxExpressionDepth = 20;
            options.MaxWorkflowVariables = 1000;
            options.MaxWorkflowDepth = 50;
            options.ExecutionMode = "Sequential";
            options.EnableParallelExecution = true;
            options.MaxParallelActivities = 10;
            options.EnableConditionalBranching = true;
            options.EnableErrorRecovery = true;
            options.EnableCircuitBreaker = true;
            options.EnableImmutableAuditTrail = true;
            options.AuditTrailRetentionDays = 365;
            options.EnableHealthChecks = true;
            options.HealthCheckIntervalSeconds = 30;
            options.EnablePrometheusMetrics = false;
            options.MetricsPort = 9090;
        });

        var provider = services.BuildServiceProvider();

        // Initialize database
        await DotNetWorkflowEngine.Configuration.ServiceCollection.InitializeWorkflowEngineAsync(provider);

        // Get services
        var workflowService = provider.GetRequiredService<WorkflowDefinitionService>();
        var executionService = provider.GetRequiredService<WorkflowExecutionService>();
        var activityService = provider.GetRequiredService<ActivityService>();
        var auditService = provider.GetRequiredService<AuditService>();

        // Register a simple activity handler
        var handler = new SimpleActivityHandler();
        activityService.RegisterHandler("SimpleTask", handler);

        try
        {
            Console.WriteLine("=== Workflow Engine Demo ===\n");

            // Build a workflow
            var builder = WorkflowBuilder.CreateSerial(
                "order-processing",
                "Order Processing Workflow",
                workflowService,
                "Validate Order",
                "Process Payment",
                "Ship Order",
                "Notify Customer"
            );

            var workflow = builder.BuildAndRegister();

            Console.WriteLine($"✓ Created workflow: {workflow.Name} (v{workflow.Version})");
            Console.WriteLine($"  Activities: {workflow.Activities.Count}");
            Console.WriteLine($"  Transitions: {workflow.Transitions.Count}\n");

            // Publish workflow
            workflowService.PublishWorkflow(workflow.Id);
            Console.WriteLine($"✓ Workflow published and ready for execution\n");

            // Create workflow instance
            var instance = executionService.CreateInstance(
                workflow.Id,
                correlationId: $"order-{DateTime.Now:yyyyMMddHHmmss}",
                initiatedBy: "system"
            );

            Console.WriteLine($"✓ Created instance: {instance.Id}");
            Console.WriteLine($"  Correlation ID: {instance.CorrelationId}");
            Console.WriteLine($"  Status: {instance.Status}\n");

            // Start execution
            Console.WriteLine("Starting workflow execution...\n");
            instance = await executionService.StartAsync(instance.Id);

            Console.WriteLine($"✓ Workflow completed successfully");
            Console.WriteLine($"  Execution time: {instance.ExecutionTimeMs}ms");
            Console.WriteLine($"  Activities executed: {instance.ExecutedActivities.Count}\n");

            // Show audit log
            var auditLog = await auditService.GetAuditLog(instance.Id);
            Console.WriteLine($"Audit Log ({auditLog.Count} entries):");
            foreach (var entry in auditLog.OrderBy(e => e.Timestamp))
            {
                Console.WriteLine($"  [{entry.GetFormattedTimestamp()}] {entry.EventType}: {entry.Description}");
            }

            // Show statistics
            var (total, active, completed, failed) = executionService.GetStatistics();
            Console.WriteLine($"\nStatistics:");
            Console.WriteLine($"  Total instances: {total}");
            Console.WriteLine($"  Active: {active}");
            Console.WriteLine($"  Completed: {completed}");
            Console.WriteLine($"  Failed: {failed}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"  Inner: {ex.InnerException.Message}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}

/// <summary>
/// Simple activity handler for demonstration.
/// </summary>
class SimpleActivityHandler : ActivityService.IActivityHandler
{
    public Task<Dictionary<string, object?>> ExecuteAsync(Activity activity, DotNetWorkflowEngine.Models.ExecutionContext context)
    {
        // Simulate work
        Thread.Sleep(100);

        var output = new Dictionary<string, object?>
        {
            ["status"] = "success",
            ["timestamp"] = DateTime.UtcNow,
            ["message"] = $"Activity {activity.Name} completed"
        };

        Console.WriteLine($"  → Executed: {activity.Name}");

        return Task.FromResult(output);
    }
}
