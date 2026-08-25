// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotNetWorkflowEngine.Examples;

/// <summary>
/// Example: Parallel task execution with synchronization.
/// Process order components simultaneously then combine results.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ParallelExecutionExample : ControllerBase
{
    private readonly IWorkflowDefinitionService _workflowService;
    private readonly IWorkflowExecutionService _executionService;

    public ParallelExecutionExample(
        IWorkflowDefinitionService workflowService,
        IWorkflowExecutionService executionService)
    {
        _workflowService = workflowService;
        _executionService = executionService;
            public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }

    /// <summary>
    /// Creates a workflow that executes independent tasks in parallel.
    /// Validates inventory, checks payment, and gets shipping estimate concurrently.
    /// </summary>
    private Workflow CreateParallelWorkflow()
    {
        return new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "ParallelOrderProcessing",
            Version = 1,
            Description = "Process order components in parallel for faster completion",
            Status = WorkflowStatus.Active,
            Activities = new List<Activity>
            {
                new Activity
                {
                    Id = "start",
                    Name = "Start Processing",
                    ActivityType = "InitializationActivity",
                    Timeout = TimeSpan.FromSeconds(5)
                        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
                // Parallel activities (independent, can run concurrently)
                new Activity
                {
                    Id = "validate_inventory",
                    Name = "Validate Inventory",
                    ActivityType = "InventoryActivity",
                    Description = "Check product availability",
                    Timeout = TimeSpan.FromSeconds(20)
                        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
                new Activity
                {
                    Id = "validate_payment",
                    Name = "Validate Payment",
                    ActivityType = "PaymentValidationActivity",
                    Description = "Verify payment method",
                    Timeout = TimeSpan.FromSeconds(15),
                    RetryPolicy = RetryPolicy.Exponential,
                    MaxRetries = 2
                        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
                new Activity
                {
                    Id = "get_shipping_quote",
                    Name = "Get Shipping Quote",
                    ActivityType = "ShippingActivity",
                    Description = "Retrieve shipping cost estimate",
                    Timeout = TimeSpan.FromSeconds(25)
                        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
                new Activity
                {
                    Id = "check_promotions",
                    Name = "Check Promotions",
                    ActivityType = "PromotionActivity",
                    Description = "Apply applicable discounts",
                    Timeout = TimeSpan.FromSeconds(10)
                        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
                // Synchronization point
                new Activity
                {
                    Id = "combine_results",
                    Name = "Combine Results",
                    ActivityType = "AggregationActivity",
                    Description = "Wait for all parallel tasks and combine results",
                    Timeout = TimeSpan.FromSeconds(30)
                        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
                // Post-combination tasks
                new Activity
                {
                    Id = "final_calculation",
                    Name = "Final Price Calculation",
                    ActivityType = "PricingActivity",
                    Description = "Calculate final order total",
                    Timeout = TimeSpan.FromSeconds(15)
                        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
                new Activity
                {
                    Id = "confirm_order",
                    Name = "Confirm Order",
                    ActivityType = "ConfirmationActivity",
                    Description = "Confirm order details with customer",
                    Timeout = TimeSpan.FromSeconds(20)
                        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }
                    public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
            Transitions = new List<Transition>
            {
                // Start to parallel tasks
                new Transition
                {
                    Id = "t1",
                    SourceActivityId = "start",
                    TargetActivityId = "validate_inventory"
                        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
                new Transition
                {
                    Id = "t2",
                    SourceActivityId = "start",
                    TargetActivityId = "validate_payment"
                        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
                new Transition
                {
                    Id = "t3",
                    SourceActivityId = "start",
                    TargetActivityId = "get_shipping_quote"
                        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
                new Transition
                {
                    Id = "t4",
                    SourceActivityId = "start",
                    TargetActivityId = "check_promotions"
                        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
                // All parallel tasks to synchronization point
                new Transition
                {
                    Id = "t5",
                    SourceActivityId = "validate_inventory",
                    TargetActivityId = "combine_results"
                        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
                new Transition
                {
                    Id = "t6",
                    SourceActivityId = "validate_payment",
                    TargetActivityId = "combine_results"
                        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
                new Transition
                {
                    Id = "t7",
                    SourceActivityId = "get_shipping_quote",
                    TargetActivityId = "combine_results"
                        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
                new Transition
                {
                    Id = "t8",
                    SourceActivityId = "check_promotions",
                    TargetActivityId = "combine_results"
                        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
                // After sync, continue sequentially
                new Transition
                {
                    Id = "t9",
                    SourceActivityId = "combine_results",
                    TargetActivityId = "final_calculation"
                        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
                new Transition
                {
                    Id = "t10",
                    SourceActivityId = "final_calculation",
                    TargetActivityId = "confirm_order"
                        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }
                    public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }
                public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    };
            public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }

    [HttpPost("initialize")]
    public async Task<ActionResult> InitializeWorkflow()
    {
        var workflow = CreateParallelWorkflow();

        try
        {
            await _workflowService.CreateWorkflowAsync(workflow);
            await _workflowService.PublishWorkflowAsync(workflow.Id);

            return Ok(new
            {
                message = "Parallel workflow initialized",
                workflowId = workflow.Id,
                parallelActivities = new[]
                {
                    "validate_inventory",
                    "validate_payment",
                    "get_shipping_quote",
                    "check_promotions"
                        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }
                    public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    });
                public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message         public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    });
                public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }
            public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }

    /// <summary>
    /// Execute order with parallel processing.
    /// </summary>
    [HttpPost("execute")]
    public async Task<ActionResult> ExecuteParallelOrder([FromBody] OrderData order)
    {
        try
        {
            var workflows = await _workflowService.GetWorkflowsByNameAsync("ParallelOrderProcessing");
            var workflow = workflows?.FirstOrDefault();

            if (workflow == null)
                return NotFound("Workflow not found");

            var context = new ExecutionContext
            {
                WorkflowId = workflow.Id,
                InstanceId = Guid.NewGuid(),
                ExecutionMode = ExecutionMode.Parallel,
                Variables = new Dictionary<string, object>
                {
                    { "OrderId", order.OrderId         public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
                    { "Items", order.Items         public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
                    { "ShippingAddress", order.ShippingAddress         public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
                    { "PaymentMethod", order.PaymentMethod         public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
                    { "CustomerEmail", order.CustomerEmail         public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    },
                    { "StartTime", DateTime.UtcNow         public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }
                        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }
                    public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    };

            var startTime = DateTime.UtcNow;
            var result = await _executionService.ExecuteAsync(context);
            var duration = DateTime.UtcNow - startTime;

            return Ok(new
            {
                instanceId = result.InstanceId,
                status = result.Status,
                executionTime = $"{duration.TotalSeconds:F2        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    } seconds",
                note = "Parallel execution of independent tasks reduces total processing time"
                    public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    });
                public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message         public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    });
                public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }
            public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }

    /// <summary>
    /// Get combined results from all parallel activities.
    /// </summary>
    [HttpGet("{instanceId        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }/results")]
    public async Task<ActionResult> GetParallelResults(Guid instanceId)
    {
        try
        {
            var instance = await _executionService.GetInstanceAsync(instanceId);
            if (instance == null)
                return NotFound();

            return Ok(new
            {
                orderId = instance.Variables.ContainsKey("OrderId")
                    ? instance.Variables["OrderId"]
                    : null,
                inventoryValid = instance.Variables.ContainsKey("InventoryValid")
                    ? instance.Variables["InventoryValid"]
                    : null,
                paymentValid = instance.Variables.ContainsKey("PaymentValid")
                    ? instance.Variables["PaymentValid"]
                    : null,
                shippingCost = instance.Variables.ContainsKey("ShippingCost")
                    ? instance.Variables["ShippingCost"]
                    : null,
                appliedPromotion = instance.Variables.ContainsKey("AppliedPromotion")
                    ? instance.Variables["AppliedPromotion"]
                    : null,
                finalTotal = instance.Variables.ContainsKey("FinalTotal")
                    ? instance.Variables["FinalTotal"]
                    : null,
                processingStatus = instance.Status
                    public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    });
                public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message         public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    });
                public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }
            public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }
        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }

public class OrderData
{
    public string OrderId { get; set;         public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    } = string.Empty;
    public List<OrderItem> Items { get; set;         public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    } = new();
    public string ShippingAddress { get; set;         public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    } = string.Empty;
    public string PaymentMethod { get; set;         public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    } = string.Empty;
    public string CustomerEmail { get; set;         public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    } = string.Empty;
        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }

public class OrderItem
{
    public string ProductId { get; set;         public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    } = string.Empty;
    public int Quantity { get; set;         public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }
    public decimal Price { get; set;         public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }
        public override string ToString() => "$ParallelExecutionExample {{ OrderId = {OrderId}, Items = {Items}, ShippingAddress = {ShippingAddress}, PaymentMethod = {PaymentMethod}, CustomerEmail = {CustomerEmail}, ProductId = {ProductId} }}";
    }
