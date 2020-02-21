// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotNetWorkflowEngine.Examples;

/// <summary>
/// Example: Creating custom activity implementations for domain-specific logic.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CustomActivityExample : ControllerBase
{
    private readonly IWorkflowDefinitionService _workflowService;
    private readonly IWorkflowExecutionService _executionService;
    private readonly IActivityService _activityService;

    public CustomActivityExample(
        IWorkflowDefinitionService workflowService,
        IWorkflowExecutionService executionService,
        IActivityService activityService)
    {
        _workflowService = workflowService;
        _executionService = executionService;
        _activityService = activityService;
    }

    /// <summary>
    /// Initialize workflow with custom activities.
    /// </summary>
    private Workflow CreateWorkflowWithCustomActivities()
    {
        return new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "CustomActivityWorkflow",
            Version = 1,
            Description = "Workflow demonstrating custom activity implementations",
            Status = WorkflowStatus.Active,
            Activities = new List<Activity>
            {
                new Activity
                {
                    Id = "send_sms",
                    Name = "Send SMS Notification",
                    ActivityType = "CustomSmsActivity",
                    Description = "Send SMS using custom activity",
                    Timeout = TimeSpan.FromSeconds(30),
                    RetryPolicy = RetryPolicy.Exponential,
                    MaxRetries = 2
                },
                new Activity
                {
                    Id = "image_processing",
                    Name = "Process Image",
                    ActivityType = "CustomImageProcessingActivity",
                    Description = "Process image with custom activity",
                    Timeout = TimeSpan.FromSeconds(60)
                },
                new Activity
                {
                    Id = "generate_report",
                    Name = "Generate Report",
                    ActivityType = "CustomReportGenerationActivity",
                    Description = "Generate report with custom logic",
                    Timeout = TimeSpan.FromSeconds(45)
                },
                new Activity
                {
                    Id = "complete",
                    Name = "Complete",
                    ActivityType = "CompletionActivity",
                    Timeout = TimeSpan.FromSeconds(5)
                }
            },
            Transitions = new List<Transition>
            {
                new Transition
                {
                    Id = "t1",
                    SourceActivityId = "send_sms",
                    TargetActivityId = "image_processing"
                },
                new Transition
                {
                    Id = "t2",
                    SourceActivityId = "image_processing",
                    TargetActivityId = "generate_report"
                },
                new Transition
                {
                    Id = "t3",
                    SourceActivityId = "generate_report",
                    TargetActivityId = "complete"
                }
            }
        };
    }

    /// <summary>
    /// Custom activity for SMS notifications.
    /// </summary>
    public class CustomSmsActivity : IActivityHandler
    {
        private readonly ILogger<CustomSmsActivity> _logger;

        public CustomSmsActivity(ILogger<CustomSmsActivity> logger)
        {
            _logger = logger;
        }

        public async Task<Dictionary<string, object?>> ExecuteAsync(
            Activity activity,
            ExecutionContext context)
        {
            try
            {
                var phoneNumber = context.Variables.ContainsKey("PhoneNumber")
                    ? context.Variables["PhoneNumber"].ToString()
                    : null;

                var message = context.Variables.ContainsKey("Message")
                    ? context.Variables["Message"].ToString()
                    : "Default message";

                _logger.LogInformation($"Sending SMS to {phoneNumber}: {message}");

                // Simulate SMS sending
                await Task.Delay(1000);

                return new Dictionary<string, object?>
                {
                    { "success", true },
                    { "messageId", Guid.NewGuid() },
                    { "recipient", phoneNumber },
                    { "sentAt", DateTime.UtcNow }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMS activity failed");
                return new Dictionary<string, object?>
                {
                    { "success", false },
                    { "error", ex.Message }
                };
            }
        }
    }

    /// <summary>
    /// Custom activity for image processing.
    /// </summary>
    public class CustomImageProcessingActivity : IActivityHandler
    {
        private readonly ILogger<CustomImageProcessingActivity> _logger;

        public CustomImageProcessingActivity(ILogger<CustomImageProcessingActivity> logger)
        {
            _logger = logger;
        }

        public async Task<Dictionary<string, object?>> ExecuteAsync(
            Activity activity,
            ExecutionContext context)
        {
            try
            {
                var imageUrl = context.Variables.ContainsKey("ImageUrl")
                    ? context.Variables["ImageUrl"].ToString()
                    : null;

                var processingType = context.Variables.ContainsKey("ProcessingType")
                    ? context.Variables["ProcessingType"].ToString()
                    : "resize";

                _logger.LogInformation($"Processing image {imageUrl} with {processingType}");

                // Simulate image processing
                await Task.Delay(2000);

                return new Dictionary<string, object?>
                {
                    { "success", true },
                    { "originalUrl", imageUrl },
                    { "processedUrl", $"{imageUrl}?processed=true" },
                    { "processingType", processingType },
                    { "width", 1920 },
                    { "height", 1080 },
                    { "fileSize", 2048000 },
                    { "processingTime", "2000ms" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Image processing activity failed");
                return new Dictionary<string, object?>
                {
                    { "success", false },
                    { "error", ex.Message }
                };
            }
        }
    }

    /// <summary>
    /// Custom activity for report generation.
    /// </summary>
    public class CustomReportGenerationActivity : IActivityHandler
    {
        private readonly ILogger<CustomReportGenerationActivity> _logger;

        public CustomReportGenerationActivity(ILogger<CustomReportGenerationActivity> logger)
        {
            _logger = logger;
        }

        public async Task<Dictionary<string, object?>> ExecuteAsync(
            Activity activity,
            ExecutionContext context)
        {
            try
            {
                var reportType = context.Variables.ContainsKey("ReportType")
                    ? context.Variables["ReportType"].ToString()
                    : "summary";

                var format = context.Variables.ContainsKey("Format")
                    ? context.Variables["Format"].ToString()
                    : "pdf";

                _logger.LogInformation($"Generating {reportType} report in {format} format");

                // Simulate report generation
                await Task.Delay(1500);

                var reportId = Guid.NewGuid();
                return new Dictionary<string, object?>
                {
                    { "success", true },
                    { "reportId", reportId },
                    { "reportType", reportType },
                    { "format", format },
                    { "downloadUrl", $"https://api.example.com/reports/{reportId}/download" },
                    { "generatedAt", DateTime.UtcNow },
                    { "pageCount", 25 },
                    { "fileSize", 5242880 }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Report generation activity failed");
                return new Dictionary<string, object?>
                {
                    { "success", false },
                    { "error", ex.Message }
                };
            }
        }
    }

    [HttpPost("initialize")]
    public async Task<ActionResult> InitializeWorkflow()
    {
        var workflow = CreateWorkflowWithCustomActivities();

        try
        {
            // Register custom activities
            var smsActivity = new CustomSmsActivity(
                new LoggerFactory().CreateLogger<CustomSmsActivity>()
            );
            var imageActivity = new CustomImageProcessingActivity(
                new LoggerFactory().CreateLogger<CustomImageProcessingActivity>()
            );
            var reportActivity = new CustomReportGenerationActivity(
                new LoggerFactory().CreateLogger<CustomReportGenerationActivity>()
            );

            // Note: In real implementation, register these in the activity service
            // _activityService.RegisterHandler("CustomSmsActivity", smsActivity);
            // _activityService.RegisterHandler("CustomImageProcessingActivity", imageActivity);
            // _activityService.RegisterHandler("CustomReportGenerationActivity", reportActivity);

            await _workflowService.CreateWorkflowAsync(workflow);
            await _workflowService.PublishWorkflowAsync(workflow.Id);

            return Ok(new
            {
                message = "Custom activity workflow initialized",
                workflowId = workflow.Id,
                customActivities = new[]
                {
                    "CustomSmsActivity",
                    "CustomImageProcessingActivity",
                    "CustomReportGenerationActivity"
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Execute workflow with custom activities.
    /// </summary>
    [HttpPost("execute")]
    public async Task<ActionResult> ExecuteWithCustomActivities([FromBody] CustomActivityRequest request)
    {
        try
        {
            var workflows = await _workflowService.GetWorkflowsByNameAsync("CustomActivityWorkflow");
            var workflow = workflows?.FirstOrDefault();

            if (workflow == null)
                return NotFound("Workflow not found");

            var context = new ExecutionContext
            {
                WorkflowId = workflow.Id,
                InstanceId = Guid.NewGuid(),
                Variables = new Dictionary<string, object>
                {
                    { "PhoneNumber", request.PhoneNumber },
                    { "Message", request.Message },
                    { "ImageUrl", request.ImageUrl },
                    { "ProcessingType", request.ProcessingType },
                    { "ReportType", request.ReportType },
                    { "Format", request.Format }
                }
            };

            var result = await _executionService.ExecuteAsync(context);

            return Ok(new
            {
                instanceId = result.InstanceId,
                status = result.Status,
                message = "Custom activity workflow executed"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{instanceId}")]
    public async Task<ActionResult> GetExecutionResults(Guid instanceId)
    {
        try
        {
            var instance = await _executionService.GetInstanceAsync(instanceId);
            if (instance == null)
                return NotFound();

            return Ok(new
            {
                instanceId = instance.Id,
                status = instance.Status,
                results = new
                {
                    smsResult = instance.Variables.ContainsKey("smsSuccess")
                        ? instance.Variables["smsSuccess"]
                        : null,
                    imageResult = instance.Variables.ContainsKey("processedUrl")
                        ? instance.Variables["processedUrl"]
                        : null,
                    reportResult = instance.Variables.ContainsKey("reportId")
                        ? instance.Variables["reportId"]
                        : null
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class CustomActivityRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ProcessingType { get; set; } = "resize";
    public string ReportType { get; set; } = "summary";
    public string Format { get; set; } = "pdf";
}

/// <summary>
/// Interface for custom activity handlers (would be in main codebase).
/// </summary>
public interface IActivityHandler
{
    Task<Dictionary<string, object?>> ExecuteAsync(Activity activity, ExecutionContext context);
}
