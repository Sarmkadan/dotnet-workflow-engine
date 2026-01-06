// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotNetWorkflowEngine.Examples;

/// <summary>
/// Example: Multi-level document approval workflow.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ApprovalChainExample : ControllerBase
{
    private readonly IWorkflowDefinitionService _workflowService;
    private readonly IWorkflowExecutionService _executionService;

    public ApprovalChainExample(
        IWorkflowDefinitionService workflowService,
        IWorkflowExecutionService executionService)
    {
        _workflowService = workflowService;
        _executionService = executionService;
    }

    /// <summary>
    /// Creates a multi-level approval workflow.
    /// Document must be approved by manager, director, and CFO.
    /// </summary>
    private Workflow CreateApprovalWorkflow()
    {
        return new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "DocumentApprovalChain",
            Version = 1,
            Description = "Multi-level document approval requiring manager, director, and CFO sign-off",
            Status = WorkflowStatus.Active,
            Activities = new List<Activity>
            {
                new Activity
                {
                    Id = "submit_document",
                    Name = "Submit Document",
                    ActivityType = "SubmissionActivity",
                    Description = "Document submitted for approval",
                    Timeout = TimeSpan.FromSeconds(10)
                },
                new Activity
                {
                    Id = "manager_review",
                    Name = "Manager Review",
                    ActivityType = "ReviewActivity",
                    Description = "Manager reviews and approves/rejects",
                    Timeout = TimeSpan.FromHours(24),
                    RetryPolicy = RetryPolicy.FixedDelay,
                    MaxRetries = 1
                },
                new Activity
                {
                    Id = "director_review",
                    Name = "Director Review",
                    ActivityType = "ReviewActivity",
                    Description = "Director reviews if manager approved",
                    Timeout = TimeSpan.FromHours(24)
                },
                new Activity
                {
                    Id = "cfo_review",
                    Name = "CFO Review",
                    ActivityType = "ReviewActivity",
                    Description = "CFO final approval for budget items",
                    Timeout = TimeSpan.FromHours(48)
                },
                new Activity
                {
                    Id = "send_approval_notice",
                    Name = "Send Approval Notice",
                    ActivityType = "NotificationActivity",
                    Description = "Send approval notification to submitter",
                    Timeout = TimeSpan.FromSeconds(20)
                },
                new Activity
                {
                    Id = "send_rejection_notice",
                    Name = "Send Rejection Notice",
                    ActivityType = "NotificationActivity",
                    Description = "Send rejection notification with feedback",
                    Timeout = TimeSpan.FromSeconds(20)
                },
                new Activity
                {
                    Id = "archive_document",
                    Name = "Archive Document",
                    ActivityType = "ArchiveActivity",
                    Description = "Archive approved document",
                    Timeout = TimeSpan.FromSeconds(30)
                }
            },
            Transitions = new List<Transition>
            {
                new Transition
                {
                    Id = "t1",
                    SourceActivityId = "submit_document",
                    TargetActivityId = "manager_review"
                },
                new Transition
                {
                    Id = "t2",
                    SourceActivityId = "manager_review",
                    TargetActivityId = "director_review",
                    Condition = "${managerApproved == true}",
                    Description = "Proceed to director if manager approved"
                },
                new Transition
                {
                    Id = "t3",
                    SourceActivityId = "manager_review",
                    TargetActivityId = "send_rejection_notice",
                    Condition = "${managerApproved == false}",
                    Description = "Send rejection if manager rejected"
                },
                new Transition
                {
                    Id = "t4",
                    SourceActivityId = "director_review",
                    TargetActivityId = "cfo_review",
                    Condition = "${directorApproved == true && documentAmount > 10000}",
                    Description = "Route to CFO if over 10k and director approved"
                },
                new Transition
                {
                    Id = "t5",
                    SourceActivityId = "director_review",
                    TargetActivityId = "send_approval_notice",
                    Condition = "${directorApproved == true && documentAmount <= 10000}",
                    Description = "Send approval if under 10k and director approved"
                },
                new Transition
                {
                    Id = "t6",
                    SourceActivityId = "director_review",
                    TargetActivityId = "send_rejection_notice",
                    Condition = "${directorApproved == false}"
                },
                new Transition
                {
                    Id = "t7",
                    SourceActivityId = "cfo_review",
                    TargetActivityId = "send_approval_notice",
                    Condition = "${cfoApproved == true}"
                },
                new Transition
                {
                    Id = "t8",
                    SourceActivityId = "cfo_review",
                    TargetActivityId = "send_rejection_notice",
                    Condition = "${cfoApproved == false}"
                },
                new Transition
                {
                    Id = "t9",
                    SourceActivityId = "send_approval_notice",
                    TargetActivityId = "archive_document"
                }
            }
        };
    }

    [HttpPost("initialize")]
    public async Task<ActionResult> InitializeWorkflow()
    {
        var workflow = CreateApprovalWorkflow();

        try
        {
            await _workflowService.CreateWorkflowAsync(workflow);
            await _workflowService.PublishWorkflowAsync(workflow.Id);

            return Ok(new
            {
                message = "Approval chain workflow initialized",
                workflowId = workflow.Id
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Submit a document for approval.
    /// </summary>
    [HttpPost("submit")]
    public async Task<ActionResult> SubmitForApproval([FromBody] DocumentSubmission submission)
    {
        try
        {
            var workflows = await _workflowService.GetWorkflowsByNameAsync("DocumentApprovalChain");
            var workflow = workflows?.FirstOrDefault();

            if (workflow == null)
                return NotFound("Workflow not found");

            var context = new ExecutionContext
            {
                WorkflowId = workflow.Id,
                InstanceId = Guid.NewGuid(),
                Variables = new Dictionary<string, object>
                {
                    { "DocumentId", submission.DocumentId },
                    { "DocumentTitle", submission.Title },
                    { "DocumentAmount", submission.Amount },
                    { "SubmittedBy", submission.SubmittedBy },
                    { "SubmissionDate", DateTime.UtcNow },
                    { "Status", "Pending" }
                }
            };

            var result = await _executionService.ExecuteAsync(context);

            return Ok(new
            {
                instanceId = result.InstanceId,
                status = "Submitted for approval",
                approvalLevels = new[] { "Manager", "Director", "CFO (if > 10k)" }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Approve document at current stage.
    /// </summary>
    [HttpPost("{instanceId}/approve")]
    public async Task<ActionResult> ApproveDocument(Guid instanceId, [FromBody] ApprovalDecision decision)
    {
        try
        {
            var instance = await _executionService.GetInstanceAsync(instanceId);
            if (instance == null)
                return NotFound();

            var approvalKey = instance.CurrentActivityId switch
            {
                "manager_review" => "managerApproved",
                "director_review" => "directorApproved",
                "cfo_review" => "cfoApproved",
                _ => null
            };

            if (approvalKey == null)
                return BadRequest("Document is not awaiting approval");

            instance.Variables[approvalKey] = true;
            instance.Variables["ApprovedBy"] = decision.ApprovedBy;
            instance.Variables["ApprovalDate"] = DateTime.UtcNow;
            instance.Variables["ApprovalComments"] = decision.Comments;

            await _executionService.UpdateInstanceAsync(instance);

            return Ok(new { message = "Document approved" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Reject document at current stage.
    /// </summary>
    [HttpPost("{instanceId}/reject")]
    public async Task<ActionResult> RejectDocument(Guid instanceId, [FromBody] ApprovalDecision decision)
    {
        try
        {
            var instance = await _executionService.GetInstanceAsync(instanceId);
            if (instance == null)
                return NotFound();

            var approvalKey = instance.CurrentActivityId switch
            {
                "manager_review" => "managerApproved",
                "director_review" => "directorApproved",
                "cfo_review" => "cfoApproved",
                _ => null
            };

            if (approvalKey == null)
                return BadRequest("Document is not awaiting approval");

            instance.Variables[approvalKey] = false;
            instance.Variables["RejectedBy"] = decision.ApprovedBy;
            instance.Variables["RejectionDate"] = DateTime.UtcNow;
            instance.Variables["RejectionReason"] = decision.Comments;

            await _executionService.UpdateInstanceAsync(instance);

            return Ok(new { message = "Document rejected" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class DocumentSubmission
{
    public string DocumentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string SubmittedBy { get; set; } = string.Empty;
}

public class ApprovalDecision
{
    public string ApprovedBy { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
}
