// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DotNetWorkflowEngine.Services;

namespace DotNetWorkflowEngine.BackgroundJobs;

/// <summary>
/// Background worker that processes pending workflow jobs from a queue.
/// Executes workflow instances asynchronously without blocking the main
/// request/response cycle. Includes automatic retry and error handling.
/// </summary>
public interface IWorkflowJobProcessor
{
    /// <summary>
    /// Enqueues a workflow instance for execution.
    /// </summary>
    Task EnqueueAsync(WorkflowJob job);

    /// <summary>
    /// Gets the count of pending jobs in the queue.
    /// </summary>
    Task<int> GetPendingCountAsync();

    /// <summary>
    /// Gets statistics about processed jobs.
    /// </summary>
    Task<JobProcessorStats> GetStatsAsync();
}

/// <summary>
/// Represents a workflow job to be executed in the background.
/// </summary>
public class WorkflowJob
{
    /// <summary>
    /// Gets or sets the unique identifier of the job.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the workflow definition to execute.
    /// </summary>
    public string? WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the workflow instance created for this job.
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// Gets or sets the input data passed to the workflow instance.
    /// </summary>
    public Dictionary<string, object>? InputData { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the job was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC timestamp before which the job should not be processed.
    /// </summary>
    public DateTime? ScheduledFor { get; set; }

    /// <summary>
    /// Gets or sets the number of times the job has been retried.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the priority of the job. Valid values: high, normal, low.
    /// </summary>
    public string? Priority { get; set; } = "normal"; // high, normal, low
}

/// <summary>
/// Statistics about job processing.
/// </summary>
public class JobProcessorStats
{
    /// <summary>
    /// Gets or sets the total number of jobs successfully processed.
    /// </summary>
    public int TotalProcessed { get; set; }

    /// <summary>
    /// Gets or sets the total number of jobs that failed permanently.
    /// </summary>
    public int TotalFailed { get; set; }

    /// <summary>
    /// Gets or sets the total number of jobs that were retried.
    /// </summary>
    public int TotalRetried { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp of the last successfully processed job.
    /// </summary>
    public DateTime? LastProcessedAt { get; set; }

    /// <summary>
    /// Gets or sets the average processing time of successfully processed jobs.
    /// </summary>
    public TimeSpan AvgProcessingTime { get; set; }
}

/// <summary>
/// Background job processor service using a queue-based approach.
/// Runs continuously processing jobs in the background.
/// </summary>
public class WorkflowJobProcessor : BackgroundService, IWorkflowJobProcessor
{
    private readonly Queue<WorkflowJob> _jobQueue = new();
    private readonly ILogger<WorkflowJobProcessor> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly JobProcessorStats _stats = new();
    private readonly object _queueLock = new();
    private readonly int _maxRetries = 3;
    private readonly int _processingDelayMs = 100; // Poll queue every 100ms

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowJobProcessor"/> class.
    /// </summary>
    /// <param name="logger">The logger used to record processing activity.</param>
    /// <param name="serviceProvider">The service provider used to resolve workflow services.</param>
    public WorkflowJobProcessor(
        ILogger<WorkflowJobProcessor> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Enqueues a job for processing.
    /// </summary>
    public Task EnqueueAsync(WorkflowJob job)
    {
        if (job == null)
            throw new ArgumentNullException(nameof(job));

        job.Id = job.Id ?? Guid.NewGuid().ToString();

        lock (_queueLock)
        {
            _jobQueue.Enqueue(job);
            _logger.LogInformation(
                "Job enqueued: {JobId} for workflow {WorkflowId}. Queue depth: {QueueCount}",
                job.Id,
                job.WorkflowId,
                _jobQueue.Count);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the number of pending jobs.
    /// </summary>
    public Task<int> GetPendingCountAsync()
    {
        lock (_queueLock)
        {
            return Task.FromResult(_jobQueue.Count);
        }
    }

    /// <summary>
    /// Gets processor statistics.
    /// </summary>
    public Task<JobProcessorStats> GetStatsAsync()
    {
        return Task.FromResult(_stats);
    }

    /// <summary>
    /// Main background processing loop. Continuously polls the queue
    /// and processes jobs with delay between iterations.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Workflow job processor started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessQueueAsync(stoppingToken);
                await Task.Delay(_processingDelayMs, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Workflow job processor stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in workflow job processor");
            throw;
        }
    }

    /// <summary>
    /// Processes one job from the queue if available.
    /// </summary>
    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        WorkflowJob? job = null;

        lock (_queueLock)
        {
            if (_jobQueue.Count > 0)
                job = _jobQueue.Dequeue();
        }

        if (job == null)
            return;

        // Skip jobs scheduled for the future
        if (job.ScheduledFor.HasValue && job.ScheduledFor > DateTime.UtcNow)
        {
            // Re-queue for later processing
            await EnqueueAsync(job);
            return;
        }

        try
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Processing job: {JobId}", job.Id);

            if (string.IsNullOrWhiteSpace(job.WorkflowId))
                throw new InvalidOperationException($"Job '{job.Id}' has no workflow ID to execute");

            var executionService = _serviceProvider.GetRequiredService<WorkflowExecutionService>();
            var instance = executionService.CreateInstance(job.WorkflowId, initiatedBy: "WorkflowJobProcessor");
            job.InstanceId = instance.Id;
            await executionService.StartAsync(instance.Id);

            var processingTime = DateTime.UtcNow - startTime;
            _stats.TotalProcessed++;
            _stats.LastProcessedAt = DateTime.UtcNow;

            // Update average processing time
            _stats.AvgProcessingTime = TimeSpan.FromMilliseconds(
                (_stats.AvgProcessingTime.TotalMilliseconds * (_stats.TotalProcessed - 1) + processingTime.TotalMilliseconds)
                / _stats.TotalProcessed);

            _logger.LogInformation(
                "Job completed: {JobId} in {ProcessingTime}ms",
                job.Id,
                processingTime.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job: {JobId}", job.Id);

            job.RetryCount++;
            if (job.RetryCount < _maxRetries)
            {
                _stats.TotalRetried++;
                // Exponential backoff: delay before retry
                job.ScheduledFor = DateTime.UtcNow.AddSeconds(Math.Pow(2, job.RetryCount));
                await EnqueueAsync(job);
                _logger.LogWarning(
                    "Job {JobId} re-queued for retry {RetryCount}/{MaxRetries}",
                    job.Id,
                    job.RetryCount,
                    _maxRetries);
            }
            else
            {
                _stats.TotalFailed++;
                _logger.LogError(
                    "Job {JobId} failed after {RetryCount} retries",
                    job.Id,
                    job.RetryCount);
            }
        }
    }
}
