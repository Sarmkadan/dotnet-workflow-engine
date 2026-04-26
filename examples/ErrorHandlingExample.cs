// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotNetWorkflowEngine.Examples;

/// <summary>
/// Example: Error handling with retry policies and fallback activities.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ErrorHandlingExample : ControllerBase
{
    private readonly IWorkflowDefinitionService _workflowService;
    private readonly IWorkflowExecutionService _executionService;
    private readonly IRetryPolicyService _retryService;

    public ErrorHandlingExample(
        IWorkflowDefinitionService workflowService,
        IWorkflowExecutionService executionService,
        IRetryPolicyService retryService)
    {
        _workflowService = workflowService;
        _executionService = executionService;
        _retryService = retryService;
    }

    /// <summary>
    /// Creates a workflow demonstrating comprehensive error handling.
    /// Includes retry policies and fallback paths for various failure scenarios.
    /// </summary>
    private Workflow CreateErrorHandlingWorkflow()
    {
        return new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "ResilientDataProcessing",
            Version = 1,
            Description = "Demonstrates retry policies, fallback activities, and error recovery",
            Status = WorkflowStatus.Active,
            Activities = new List<Activity>
            {
                new Activity
                {
                    Id = "fetch_data",
                    Name = "Fetch Data from API",
                    ActivityType = "ApiCallActivity",
                    Description = "Fetch data from external API with exponential backoff",
                    Timeout = TimeSpan.FromSeconds(30),
                    RetryPolicy = RetryPolicy.Exponential,
                    MaxRetries = 3
                },
                new Activity
                {
                    Id = "validate_data",
                    Name = "Validate Data",
                    ActivityType = "ValidationActivity",
                    Description = "Validate fetched data format and content",
                    Timeout = TimeSpan.FromSeconds(15),
                    RetryPolicy = RetryPolicy.None
                },
                new Activity
                {
                    Id = "transform_data",
                    Name = "Transform Data",
                    ActivityType = "TransformationActivity",
                    Description = "Transform data with fixed retry delay",
                    Timeout = TimeSpan.FromSeconds(20),
                    RetryPolicy = RetryPolicy.FixedDelay,
                    MaxRetries = 2
                },
                new Activity
                {
                    Id = "store_data",
                    Name = "Store Data",
                    ActivityType = "StorageActivity",
                    Description = "Store data in database with linear backoff",
                    Timeout = TimeSpan.FromSeconds(25),
                    RetryPolicy = RetryPolicy.LinearBackoff,
                    MaxRetries = 3
                },
                // Fallback activities
                new Activity
                {
                    Id = "fetch_data_fallback",
                    Name = "Fetch Data from Cache",
                    ActivityType = "CacheFallbackActivity",
                    Description = "Use cached data if API fails",
                    Timeout = TimeSpan.FromSeconds(10)
                },
                new Activity
                {
                    Id = "store_data_fallback",
                    Name = "Store Data Offline",
                    ActivityType = "OfflineStorageActivity",
                    Description = "Store in local queue if database unavailable",
                    Timeout = TimeSpan.FromSeconds(15)
                },
                new Activity
                {
                    Id = "send_error_notification",
                    Name = "Send Error Notification",
                    ActivityType = "NotificationActivity",
                    Description = "Notify admin of processing failure",
                    Timeout = TimeSpan.FromSeconds(20)
                },
                new Activity
                {
                    Id = "log_error",
                    Name = "Log Error Details",
                    ActivityType = "LoggingActivity",
                    Description = "Log comprehensive error information",
                    Timeout = TimeSpan.FromSeconds(10)
                },
                new Activity
                {
                    Id = "complete_success",
                    Name = "Complete Successfully",
                    ActivityType = "SuccessActivity",
                    Description = "Mark processing as successful",
                    Timeout = TimeSpan.FromSeconds(5)
                },
                new Activity
                {
                    Id = "complete_with_fallback",
                    Name = "Complete with Fallback",
                    ActivityType = "PartialSuccessActivity",
                    Description = "Mark as completed despite using fallback",
                    Timeout = TimeSpan.FromSeconds(5)
                },
                new Activity
                {
                    Id = "complete_failure",
                    Name = "Mark as Failed",
                    ActivityType = "FailureActivity",
                    Description = "Mark processing as failed",
                    Timeout = TimeSpan.FromSeconds(5)
                }
            },
            Transitions = new List<Transition>
            {
                // Normal path
                new Transition
                {
                    Id = "t1",
                    SourceActivityId = "fetch_data",
                    TargetActivityId = "validate_data",
                    Condition = "${dataFetched == true}",
                    Description = "Proceed if data fetched successfully"
                },
                new Transition
                {
                    Id = "t2",
                    SourceActivityId = "fetch_data",
                    TargetActivityId = "fetch_data_fallback",
                    Condition = "${dataFetched == false}",
                    Description = "Use cache if API fails after retries"
                },
                new Transition
                {
                    Id = "t3",
                    SourceActivityId = "validate_data",
                    TargetActivityId = "transform_data",
                    Condition = "${dataValid == true}"
                },
                new Transition
                {
                    Id = "t4",
                    SourceActivityId = "validate_data",
                    TargetActivityId = "log_error",
                    Condition = "${dataValid == false}",
                    Description = "Log if validation fails"
                },
                new Transition
                {
                    Id = "t5",
                    SourceActivityId = "transform_data",
                    TargetActivityId = "store_data",
                    Condition = "${dataTransformed == true}"
                },
                new Transition
                {
                    Id = "t6",
                    SourceActivityId = "transform_data",
                    TargetActivityId = "log_error",
                    Condition = "${dataTransformed == false}"
                },
                new Transition
                {
                    Id = "t7",
                    SourceActivityId = "store_data",
                    TargetActivityId = "complete_success",
                    Condition = "${dataStored == true}",
                    Description = "Success if stored successfully"
                },
                new Transition
                {
                    Id = "t8",
                    SourceActivityId = "store_data",
                    TargetActivityId = "store_data_fallback",
                    Condition = "${dataStored == false}",
                    Description = "Use offline storage if database fails"
                },
                // Fallback paths
                new Transition
                {
                    Id = "t9",
                    SourceActivityId = "fetch_data_fallback",
                    TargetActivityId = "validate_data"
                },
                new Transition
                {
                    Id = "t10",
                    SourceActivityId = "store_data_fallback",
                    TargetActivityId = "complete_with_fallback"
                },
                // Error handling
                new Transition
                {
                    Id = "t11",
                    SourceActivityId = "log_error",
                    TargetActivityId = "send_error_notification"
                },
                new Transition
                {
                    Id = "t12",
                    SourceActivityId = "send_error_notification",
                    TargetActivityId = "complete_failure"
                }
            }
        };
    }

    [HttpPost("initialize")]
    public async Task<ActionResult> InitializeWorkflow()
    {
        var workflow = CreateErrorHandlingWorkflow();

        try
        {
            await _workflowService.CreateWorkflowAsync(workflow);
            await _workflowService.PublishWorkflowAsync(workflow.Id);

            return Ok(new
            {
                message = "Error handling workflow initialized",
                workflowId = workflow.Id,
                retryStrategies = new[]
                {
                    "Exponential backoff (fetch_data)",
                    "Fixed delay (transform_data)",
                    "Linear backoff (store_data)"
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Execute data processing with error handling.
    /// </summary>
    [HttpPost("process")]
    public async Task<ActionResult> ProcessData([FromBody] ProcessingRequest request)
    {
        try
        {
            var workflows = await _workflowService.GetWorkflowsByNameAsync("ResilientDataProcessing");
            var workflow = workflows?.FirstOrDefault();

            if (workflow == null)
                return NotFound("Workflow not found");

            var context = new ExecutionContext
            {
                WorkflowId = workflow.Id,
                InstanceId = Guid.NewGuid(),
                Variables = new Dictionary<string, object>
                {
                    { "DataSourceUrl", request.DataSourceUrl },
                    { "ProcessingRules", request.ProcessingRules },
                    { "RetryAttempts", 0 },
                    { "FallbackUsed", false },
                    { "StartTime", DateTime.UtcNow }
                }
            };

            var result = await _executionService.ExecuteAsync(context);

            return Ok(new
            {
                instanceId = result.InstanceId,
                status = result.Status,
                message = "Data processing started with error handling and fallbacks"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get error handling details and retry information.
    /// </summary>
    [HttpGet("{instanceId}/error-info")]
    public async Task<ActionResult> GetErrorInfo(Guid instanceId)
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
                retryCount = instance.Variables.ContainsKey("RetryAttempts")
                    ? instance.Variables["RetryAttempts"]
                    : 0,
                fallbackUsed = instance.Variables.ContainsKey("FallbackUsed")
                    ? instance.Variables["FallbackUsed"]
                    : false,
                errorMessage = instance.Variables.ContainsKey("LastError")
                    ? instance.Variables["LastError"]
                    : null,
                recoveryStrategy = instance.Variables.ContainsKey("RecoveryMethod")
                    ? instance.Variables["RecoveryMethod"]
                    : null
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class ProcessingRequest
{
    public string DataSourceUrl { get; set; } = string.Empty;
    public Dictionary<string, object> ProcessingRules { get; set; } = new();
}
