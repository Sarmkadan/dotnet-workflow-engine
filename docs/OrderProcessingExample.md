# OrderProcessingExample

The `OrderProcessingExample` class encapsulates the workflow for processing an order within the dotnet-workflow-engine system. It provides methods to initialize a workflow, process an order, retrieve status and summary information, and export an audit trail, while exposing the core order data as public properties.

## API

### OrderProcessingExample()
**Purpose**  
Creates a new instance of the order processing workflow container.

**Parameters**  
None.

**Return Value**  
A new `OrderProcessingExample` object.

**Exceptions**  
None thrown under normal circumstances.

### InitializeWorkflow()
**Purpose**  
Prepares the internal workflow engine for order processing, setting up any required state or dependencies.

**Parameters**  
None.

**Return Value**  
A `Task<ActionResult>` that completes when the workflow initialization finishes. The result indicates success or failure of the initialization step.

**Exceptions**  
- `InvalidOperationException` if the workflow has already been initialized.  
- `WorkflowEngineException` (or derived) if the underlying engine fails to start.

### ProcessOrder()
**Purpose**  
Executes the order processing logic using the current property values (OrderId, CustomerId, Amount, etc.).

**Parameters**  
None.

**Return Value**  
A `Task<ActionResult>` representing the outcome of the order processing operation (e.g., accepted, rejected, or requiring further action).

**Exceptions**  
- `InvalidOperationException` if `InitializeWorkflow` has not been called first.  
- `ArgumentException` if any required order property is null or invalid.  
- `ProcessingException` if the order cannot be processed due to business rule violations.

### GetOrderStatus()
**Purpose**  
Retrieves the current status of the order within the workflow.

**Parameters**  
None.

**Return Value**  
A `Task<ActionResult>` containing a status descriptor (e.g., Pending, Processing, Completed, Failed).

**Exceptions**  
- `InvalidOperationException` if the workflow has not been initialized.  
- `NotFoundException` if the order identifier is not recognized by the engine.

### GetOrderSummary()
**Purpose**  
Returns a summary of the order details after processing.

**Parameters**  
None.

**Return Value**  
A `Task<ActionResult>` whose payload includes a summary object (e.g., total amount, item count, shipping address).

**Exceptions**  
- `InvalidOperationException` if the order has not been processed yet.  
- `ObjectDisposedException` if the workflow instance has been disposed.

### ExportAuditTrail()
**Purpose**  
Exports the complete audit trail for the order as a downloadable file or stream.

**Parameters**  
None.

**Return Value**  
A `Task<ActionResult>` that yields a file result (e.g., CSV, JSON) containing the audit entries.

**Exceptions**  
- `InvalidOperationException` if no audit data is available (workflow not run).  
- `IOException` if there is an issue writing the export file.

### OrderId
**Purpose**  
Unique identifier for the order being processed.

**Type**  
`string` (read/write).

### CustomerId
**Purpose**  
Identifier of the customer placing the order.

**Type**  
`string` (read/write).

### Amount
**Purpose**  
Total monetary value of the order.

**Type**  
`decimal` (read/write).

### ShippingAddress
**Purpose**  
Destination address for order shipment.

**Type**  
`string` (read/write).

### Items
**Purpose**  
Collection of line items that constitute the order.

**Type**  
`List<OrderItem>` (read/write). Each `OrderItem` describes a product, quantity, and price.

### ProductId
**Purpose**  
Identifier of a product associated with an order item (used when manipulating individual items).

**Type**  
`string` (read/write).

### Quantity
**Purpose**  
Number of units of the product in an order item.

**Type**  
`int` (read/write).

### UnitPrice
**Purpose**  
Price per unit of the product in an order item.

**Type**  
`decimal` (read/write).

## Usage

### Basic order processing flow
```csharp
var workflow = new OrderProcessingExample
{
    OrderId = "ORD-12345",
    CustomerId = "CUST-987",
    Amount = 149.99m,
    ShippingAddress = "123 Main St, Anytown, USA",
    Items = new List<OrderItem>
    {
        new OrderItem { ProductId = "PROD-A1", Quantity = 2, UnitPrice = 49.99m },
        new OrderItem { ProductId = "PROD-B2", Quantity = 1, UnitPrice = 50.01m }
    }
};

// Prepare the workflow
await workflow.InitializeWorkflow();

// Process the order
var processResult = await workflow.ProcessOrder();
if (processResult is OkObjectResult ok)
{
    // Order processed successfully
}

// Retrieve status and summary
var status = await workflow.GetOrderStatus();
var summary = await workflow.GetOrderSummary();

// Export audit trail for compliance
var audit = await workflow.ExportAuditTrail();
```

### Error handling scenario
```csharp
try
{
    var workflow = new OrderProcessingExample();
    // Forgetting to set required properties will cause validation errors
    await workflow.InitializeWorkflow();
    await workflow.ProcessOrder(); // Throws ArgumentException
}
catch (ArgumentException ex)
{
    // Log missing or invalid order data
    Console.WriteLine($"Order validation failed: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    // Workflow not initialized or already processed
    Console.WriteLine($"Workflow error: {ex.Message}");
}
```

## Notes

- The class is **not thread-safe**. Concurrent calls to `InitializeWorkflow`, `ProcessOrder`, or any other method on the same instance may result in race conditions or inconsistent state. For parallel processing, create separate instances per order.
- All string‑based identifiers (`OrderId`, `CustomerId`, `ProductId`, `ShippingAddress`) should be non‑null and non‑empty before invoking workflow methods; otherwise, an `ArgumentException` will be thrown.
- The `Amount` property should reflect the sum of `Quantity * UnitPrice` across all `Items`. The workflow does not automatically recompute this value; discrepancies may lead to validation failures.
- After a successful call to `ProcessOrder`, the instance is considered to be in a processed state. Subsequent calls to `GetOrderStatus` or `GetOrderSummary` will return valid results, while another call to `ProcessOrder` will throw an `InvalidOperationException` to prevent duplicate processing.
- The `ExportAuditTrail` method will only produce meaningful output after the order has progressed through the workflow; invoking it prior to processing yields an empty or placeholder result and may throw an `InvalidOperationException` depending on the underlying engine's implementation.
