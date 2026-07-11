# ParallelExecutionExample

Represents a workflow component that coordinates the parallel execution of an order processing pipeline within the dotnet-workflow-engine system. It encapsulates order data and provides asynchronous methods to initialize, run, and retrieve results from the workflow.

## API

### ParallelExecutionExample()
**Purpose:** Creates a new instance of the workflow component.  
**Parameters:** None.  
**Return:** A ready‑to‑configure `ParallelExecutionExample` object.  
**Throws:** May throw an exception if the underlying workflow engine cannot be instantiated (e.g., missing required services).

### Task<ActionResult> InitializeWorkflow()
**Purpose:** Prepares the internal workflow state for order processing. This method should be invoked after setting the order‑related properties.  
**Parameters:** None.  
**Return:** A `Task<ActionResult>` where a successful initialization yields an `ActionResult` with a status code of 200; failure returns an appropriate error code (e.g., 400 for invalid state, 500 for internal errors).  
**Throws:**  
- `InvalidOperationException` if the method is called more than once without resetting the instance.  
- `InvalidOperationException` if essential properties such as `OrderId` or `Items` are null or empty when the method is invoked.

### Task<ActionResult> ExecuteParallelOrder()
**Purpose:** Starts the parallel execution of the order workflow using the data supplied via the component’s properties.  
**Parameters:** None.  
**Return:** A `Task<ActionResult>` indicating the outcome of the execution. A status code of 200 signals successful start; non‑200 codes indicate validation or runtime errors.  
**Throws:**  
- `InvalidOperationException` if `InitializeWorkflow` has not been called successfully beforehand.  
- `InvalidOperationException` if the `Items` collection is empty or contains null entries.  
- `InvalidOperationException` if any of `OrderId`, `ShippingAddress`, `PaymentMethod`, or `CustomerEmail` are null or whitespace.

### Task<ActionResult> GetParallelResults()
**Purpose:** Retrieves the results produced by the parallel order execution once it has completed.  
**Parameters:** None.  
**Return:** A `Task<ActionResult>` whose `Value` property holds the result data (type defined by the workflow engine) when execution is finished; otherwise returns a status code indicating that results are not yet available (e.g., 202 Accepted) or an error.  
**Throws:**  
- `InvalidOperationException` if the workflow has not been started via `ExecuteParallelOrder`.  
- `InvalidOperationException` if the execution has failed and no results are produced.

### Public Properties
| Property | Type | Purpose |
|----------|------|---------|
| `OrderId` | `string` | Unique identifier for the order being processed. |
| `Items` | `List<OrderItem>` | Collection of line items that constitute the order. |
| `ShippingAddress` | `string` | Destination address where the order should be shipped. |
| `PaymentMethod` | `string` | Description of the payment method used for the order. |
| `CustomerEmail` | `string` | Email address of the customer associated with the order. |
| `ProductId` | `string` | Identifier of the product (relevant when the order concerns a single product). |
| `Quantity` | `int` | Number of units of the product ordered. |
| `Price` | `decimal` | Unit price of the product. |

All properties are public getters and setters; they are intended to be set prior to calling `InitializeWorkflow`.

## Usage

### Example 1: Basic workflow execution
```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

public class OrderController : ControllerBase
{
    public async Task<IActionResult> ProcessOrder()
    {
        var workflow = new ParallelExecutionExample
        {
            OrderId = "ORD-12345",
            Items = new List<OrderItem>
            {
                new OrderItem { ProductId = "PROD-A", Quantity = 2, Price = 19.99m },
                new OrderItem { ProductId = "PROD-B", Quantity = 1, Price = 45.50m }
            },
            ShippingAddress = "123 Main St, Springfield",
            PaymentMethod = "Credit Card",
            CustomerEmail = "customer@example.com"
        };

        // Initialize the workflow
        var initResult = await workflow.InitializeWorkflow();
        if (initResult.StatusCode != 200)
            return StatusCode((int)initResult.StatusCode, initResult.Value);

        // Execute the order in parallel
        var execResult = await workflow.ExecuteParallelOrder();
        if (execResult.StatusCode != 200)
            return StatusCode((int)execResult.StatusCode, execResult.Value);

        // Poll for results (simplified; in practice you might use a callback or websocket)
        var resultsResult = await workflow.GetParallelResults();
        while (resultsResult.StatusCode == 202) // Accepted – still processing
        {
            await Task.Delay(500);
            resultsResult = await workflow.GetParallelResults();
        }

        if (resultsResult.StatusCode != 200)
            return StatusCode((int)resultsResult.StatusCode, resultsResult.Value);

        return Ok(resultsResult.Value);
    }
}
```

### Example 2: Error handling and validation
```csharp
public async Task<IActionResult> TryProcessOrder()
{
    var workflow = new ParallelExecutionExample
    {
        // Intentionally omitting OrderId to trigger validation error
        Items = new List<OrderItem>(),
        ShippingAddress = "",
        PaymentMethod = "PayPal",
        CustomerEmail = "user@domain.com"
    };

    var initResult = await workflow.InitializeWorkflow();
    if (initResult.StatusCode != 200)
    {
        // Log initialization failure
        return BadRequest(new { Message = "Workflow initialization failed", Details = initResult.Value });
    }

    var execResult = await workflow.ExecuteParallelOrder();
    if (execResult.StatusCode != 200)
    {
        // Execution failed due to missing data
        return BadRequest(new { Message = "Order execution failed", Details = execResult.Value });
    }

    var resultsResult = await workflow.GetParallelResults();
    if (resultsResult.StatusCode != 200)
    {
        // Results not available or error occurred
        return StatusCode((int)resultsResult.StatusCode, new { Message = "Unable to retrieve results", Details = resultsResult.Value });
    }

    return Ok(resultsResult.Value);
}
```

## Notes
- The class is **not thread‑safe**. Concurrent calls to `InitializeWorkflow`, `ExecuteParallelOrder`, or `GetParallelResults` from multiple threads may lead to race conditions or inconsistent state. Use a single instance per request or synchronize access externally.
- All order‑related properties (`OrderId`, `Items`, `ShippingAddress`, `PaymentMethod`, `CustomerEmail`, `ProductId`, `Quantity`, `Price`) should be fully populated **before** invoking `InitializeWorkflow`. Changing them after initialization has no effect on the already‑started workflow.
- If `Items` is null, empty, or contains null entries, `ExecuteParallelOrder` will throw an `InvalidOperationException`. Similarly, missing or whitespace‑only values for `OrderId`, `ShippingAddress`, `PaymentMethod`, or `CustomerEmail` will cause the same exception.
- The workflow may internally modify the `Items` collection during execution (e.g., marking items as processed). After `ExecuteParallelOrder` completes, the collection should be treated as read‑only for further use.
- `GetParallelResults` returns a status code of 202 (Accepted) while the parallel operation is still in progress. Callers should retry after a short delay or use a notification mechanism provided by the engine.
- Any exception thrown by the underlying workflow engine is wrapped inside the returned `ActionResult`; however, unexpected fatal errors may bubble up as exceptions from the methods themselves. Consumers should guard against both error codes and exceptions.
