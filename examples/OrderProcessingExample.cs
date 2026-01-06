// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using DotNetWorkflowEngine.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace DotNetWorkflowEngine.Examples;

/// <summary>
/// Example: Order processing workflow with validation, payment, and shipping.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrderProcessingExample : ControllerBase
{
    private readonly IWorkflowDefinitionService _workflowService;
    private readonly IWorkflowExecutionService _executionService;
    private readonly IAuditService _auditService;

    public OrderProcessingExample(
        IWorkflowDefinitionService workflowService,
        IWorkflowExecutionService executionService,
        IAuditService auditService)
    {
        _workflowService = workflowService;
        _executionService = executionService;
        _auditService = auditService;
    }

    /// <summary>
    /// Creates a sample order processing workflow.
    /// </summary>
    private Workflow CreateOrderWorkflow()
    {
        return new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "OrderProcessing",
            Version = 1,
            Description = "Multi-step order processing with validation, payment, and shipping",
            Status = WorkflowStatus.Active,
            Activities = new List<Activity>
            {
                new Activity
                {
                    Id = "validate_order",
                    Name = "Validate Order",
                    ActivityType = "ValidationActivity",
                    Description = "Validate order data and inventory",
                    Timeout = TimeSpan.FromSeconds(30),
                    RetryPolicy = RetryPolicy.None,
                    MaxRetries = 0
                },
                new Activity
                {
                    Id = "calculate_tax",
                    Name = "Calculate Tax",
                    ActivityType = "TaxCalculationActivity",
                    Description = "Calculate applicable taxes",
                    Timeout = TimeSpan.FromSeconds(15),
                    RetryPolicy = RetryPolicy.None
                },
                new Activity
                {
                    Id = "process_payment",
                    Name = "Process Payment",
                    ActivityType = "PaymentActivity",
                    Description = "Charge customer payment method",
                    Timeout = TimeSpan.FromSeconds(60),
                    RetryPolicy = RetryPolicy.Exponential,
                    MaxRetries = 3
                },
                new Activity
                {
                    Id = "prepare_shipment",
                    Name = "Prepare Shipment",
                    ActivityType = "ShippingActivity",
                    Description = "Prepare order for shipment",
                    Timeout = TimeSpan.FromSeconds(45),
                    RetryPolicy = RetryPolicy.FixedDelay,
                    MaxRetries = 2
                },
                new Activity
                {
                    Id = "send_confirmation",
                    Name = "Send Confirmation Email",
                    ActivityType = "EmailActivity",
                    Description = "Send order confirmation to customer",
                    Timeout = TimeSpan.FromSeconds(20),
                    RetryPolicy = RetryPolicy.None
                }
            },
            Transitions = new List<Transition>
            {
                new Transition
                {
                    Id = "t1",
                    SourceActivityId = "validate_order",
                    TargetActivityId = "calculate_tax",
                    Description = "Proceed to tax calculation if order is valid"
                },
                new Transition
                {
                    Id = "t2",
                    SourceActivityId = "calculate_tax",
                    TargetActivityId = "process_payment",
                    Description = "Proceed to payment processing"
                },
                new Transition
                {
                    Id = "t3",
                    SourceActivityId = "process_payment",
                    TargetActivityId = "prepare_shipment",
                    Condition = "${paymentStatus == 'Success'}",
                    Description = "Ship order if payment successful"
                },
                new Transition
                {
                    Id = "t4",
                    SourceActivityId = "prepare_shipment",
                    TargetActivityId = "send_confirmation",
                    Description = "Send confirmation after shipment prepared"
                }
            }
        };
    }

    /// <summary>
    /// Initialize the order processing workflow.
    /// </summary>
    [HttpPost("initialize")]
    public async Task<ActionResult> InitializeWorkflow()
    {
        var workflow = CreateOrderWorkflow();

        try
        {
            await _workflowService.CreateWorkflowAsync(workflow);
            await _workflowService.PublishWorkflowAsync(workflow.Id);

            return Ok(new
            {
                message = "Order processing workflow initialized successfully",
                workflowId = workflow.Id,
                workflowName = workflow.Name,
                activitiesCount = workflow.Activities.Count
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Process a customer order.
    /// </summary>
    [HttpPost("process")]
    public async Task<ActionResult> ProcessOrder([FromBody] OrderRequest request)
    {
        try
        {
            var workflows = await _workflowService.GetWorkflowsByNameAsync("OrderProcessing");
            if (workflows == null || !workflows.Any())
                return NotFound("Workflow not found");

            var workflow = workflows.First();

            var context = new ExecutionContext
            {
                WorkflowId = workflow.Id,
                InstanceId = Guid.NewGuid(),
                Variables = new Dictionary<string, object>
                {
                    { "OrderId", request.OrderId },
                    { "CustomerId", request.CustomerId },
                    { "OrderAmount", request.Amount },
                    { "ShippingAddress", request.ShippingAddress },
                    { "Items", request.Items },
                    { "CreatedAt", DateTime.UtcNow }
                }
            };

            var result = await _executionService.ExecuteAsync(context);

            return Ok(new
            {
                instanceId = result.InstanceId,
                status = result.Status,
                message = "Order processing started",
                estimatedCompletion = DateTime.UtcNow.AddMinutes(10)
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get order processing status.
    /// </summary>
    [HttpGet("{instanceId}")]
    public async Task<ActionResult> GetOrderStatus(Guid instanceId)
    {
        try
        {
            var instance = await _executionService.GetInstanceAsync(instanceId);
            if (instance == null)
                return NotFound();

            var auditLogs = await _auditService.GetInstanceAuditTrailAsync(instanceId);

            return Ok(new
            {
                instanceId = instance.Id,
                status = instance.Status,
                currentActivity = instance.CurrentActivityId,
                variables = instance.Variables,
                auditTrail = auditLogs
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get order summary with metrics.
    /// </summary>
    [HttpGet("{instanceId}/summary")]
    public async Task<ActionResult> GetOrderSummary(Guid instanceId)
    {
        try
        {
            var instance = await _executionService.GetInstanceAsync(instanceId);
            if (instance == null)
                return NotFound();

            var duration = instance.CompletedAt.HasValue
                ? (instance.CompletedAt.Value - instance.StartedAt).TotalSeconds
                : (DateTime.UtcNow - instance.StartedAt).TotalSeconds;

            return Ok(new
            {
                orderId = instance.Variables.ContainsKey("OrderId")
                    ? instance.Variables["OrderId"]
                    : "Unknown",
                status = instance.Status,
                duration = $"{duration:F2} seconds",
                startedAt = instance.StartedAt,
                completedAt = instance.CompletedAt,
                isSuccessful = instance.Status == WorkflowStatus.Completed
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Export order audit trail.
    /// </summary>
    [HttpGet("{instanceId}/export")]
    public async Task<ActionResult> ExportAuditTrail(Guid instanceId)
    {
        try
        {
            var csv = await _auditService.ExportAuditTrailAsCsvAsync(instanceId);

            return File(
                System.Text.Encoding.UTF8.GetBytes(csv),
                "text/csv",
                $"order-audit-{instanceId}.csv"
            );
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class OrderRequest
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
