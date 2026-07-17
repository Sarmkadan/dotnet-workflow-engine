# ParallelExecutionExampleExtensions

Extension methods for `ParallelExecutionExample` that provide validation, calculation, and transformation utilities for parallel workflow execution scenarios. These methods enable consistent order processing, standardized ID generation, and result formatting when executing workflow steps in parallel.


## API

### ValidateOrderDataAsync

Validates that order data contains all required fields for parallel execution workflows.

- **Parameters:**
  - `example`: The `ParallelExecutionExample` instance.
  - `orderData`: The order data to validate.
- **Returns:** An `ActionResult` indicating validation success or failure. Returns `OkResult` on success, or `BadRequestObjectResult` with error details on failure.
- **Throws:** `ArgumentNullException` if `orderData` is null.
- **Validation Rules:**
  - `OrderId` must not be null or whitespace
  - `Items` collection must not be null and must contain at least one item
  - `ShippingAddress` must not be null or whitespace
  - `PaymentMethod` must not be null or whitespace
  - `CustomerEmail` must not be null or whitespace and must contain '@' and '.' characters

### CalculateOrderTotal

Calculates the total monetary value of an order by summing the quantity × price for all items.

- **Parameters:**
  - `example`: The `ParallelExecutionExample` instance.
  - `orderData`: The order data containing items to calculate total from.
- **Returns:** The total order value as `decimal`.
- **Throws:** `ArgumentNullException` if `orderData` is null or if `orderData.Items` is null.

### CreateStandardizedOrderId

Generates a standardized order ID string from order data using timestamp and customer email hash.

- **Parameters:**
  - `example`: The `ParallelExecutionExample` instance.
  - `orderData`: The order data containing customer email and order information.
- **Returns:** A standardized order ID string in format `ORDER-{yyyyMMddHHmmss}-{emailHash}` where `emailHash` is the 8-digit hexadecimal representation of the email's hash code.
- **Throws:** `ArgumentNullException` if `orderData` is null.
- **Format:** `ORDER-{timestamp}-{emailHash}`
  - Example: `ORDER-20240717143022-A1B2C3D4`


### GenerateShippingLabel

Formats parallel execution results into a standardized shipping label string for display or printing.

- **Parameters:**
  - `example`: The `ParallelExecutionExample` instance.
  - `results`: The parallel execution results containing shipping and order information.
- **Returns:** A formatted multi-line shipping label string.
- **Throws:** `ArgumentNullException` if `results` is null.
- **Format:** Includes order ID, shipping address, shipping cost, processing status, and generation timestamp.


### ParallelExecutionResults

A container class for parallel execution results from `ParallelExecutionExample` workflows. This class holds the aggregated results of parallel workflow execution including validation flags, costs, promotions, and processing status.


- **Properties:**
  - `OrderId` (`string?`): The order identifier.
  - `InventoryValid` (`bool?`): Whether inventory validation passed.
  - `PaymentValid` (`bool?`): Whether payment validation passed.
  - `ShippingCost` (`decimal?`): The calculated shipping cost.
  - `AppliedPromotion` (`string?`): The promotion code applied to the order.
  - `FinalTotal` (`decimal?`): The final total after all calculations and promotions.
  - `ProcessingStatus` (`string?`): The overall processing status.
  - `ShippingAddress` (`string?`): The shipping destination address.


## Usage

### Example 1: Validating Order Data and Calculating Total


```csharp
using DotNetWorkflowEngine.Examples;
using DotNetWorkflowEngine.Models;
using Microsoft.AspNetCore.Mvc;


var example = new ParallelExecutionExample();
var orderData = new OrderData
{
    OrderId = "ORD-12345",
    CustomerEmail = "customer@example.com",
    ShippingAddress = "123 Main St, City, Country",
    PaymentMethod = "Credit Card",
    Items = new List<OrderItem>
    {
        new OrderItem { ProductId = "P001", Name = "Widget", Price = 19.99m, Quantity = 2 },
        new OrderItem { ProductId = "P002", Name = "Gadget", Price = 29.99m, Quantity = 1 }
    }
};

// Validate order data
ActionResult validationResult = await example.ValidateOrderDataAsync(orderData);
if (!validationResult.Equals(new OkResult()))
{
    Console.WriteLine("Validation failed!");
    return;
}

// Calculate order total
decimal total = example.CalculateOrderTotal(orderData);
Console.WriteLine($"Order total: {total:C}");

// Create standardized order ID
string orderId = example.CreateStandardizedOrderId(orderData);
Console.WriteLine($"Generated order ID: {orderId}");
```

### Example 2: Generating Shipping Label from Parallel Execution Results


```csharp
using DotNetWorkflowEngine.Examples;

var example = new ParallelExecutionExample();

// Simulate parallel execution results
var results = new ParallelExecutionResults
{
    OrderId = "ORDER-20240717143022-A1B2C3D4",
    InventoryValid = true,
    PaymentValid = true,
    ShippingCost = 9.99m,
    AppliedPromotion = "SUMMER2024",
    FinalTotal = 49.98m,
    ProcessingStatus = "Completed",
    ShippingAddress = "456 Oak Ave, Springfield, USA"
};

// Generate shipping label
string shippingLabel = example.GenerateShippingLabel(results);
Console.WriteLine(shippingLabel);
```

**Output:**
```
SHIPPING LABEL
========================================
Order ID: ORDER-20240717143022-A1B2C3D4
Shipping To: 456 Oak Ave, Springfield, USA
Shipping Cost: $9.99
Status: Completed
Generated: 2024-07-17 14:30:22 UTC
========================================
```

## Notes

- **Thread Safety:** All extension methods are thread-safe as they are pure functions that only read their parameters and do not modify shared state. The methods are designed to be called concurrently from multiple threads without synchronization.

- **Null Handling:** Methods throw `ArgumentNullException` for null parameters rather than returning null, ensuring explicit failure modes.

- **Culture Independence:** Calculations and formatting use `CultureInfo.InvariantCulture` to ensure consistent behavior across different system locales.

- **Order ID Generation:** The standardized order ID format includes timestamp down to the second and an 8-digit hexadecimal hash of the customer email, providing uniqueness while maintaining readability.

- **Validation:** `ValidateOrderDataAsync` returns HTTP-style results (`ActionResult`) to align with ASP.NET Core controller patterns, making it suitable for web API scenarios.

- **Optional Properties:** The `ParallelExecutionResults` class uses nullable properties (`bool?`, `decimal?`, `string?`) to represent optional or incomplete execution results, allowing partial workflow completion tracking.