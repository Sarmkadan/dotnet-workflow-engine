# Examples

This directory contains comprehensive examples demonstrating various features and use cases of dotnet-workflow-engine.

## Overview

Each example is a complete, runnable controller that demonstrates specific workflow patterns and best practices.

## Examples

### 1. OrderProcessingExample.cs

**Purpose:** Multi-step order processing workflow

**Demonstrates:**
- Sequential activity execution
- Workflow state management
- Variable passing between activities
- Audit trail logging
- Error handling

**Key Activities:**
- Validate Order
- Calculate Tax
- Process Payment (with retries)
- Prepare Shipment
- Send Confirmation Email

**Usage:**
```bash
# Initialize workflow
POST /api/order-processing/initialize

# Process an order
POST /api/order-processing/process
{
  "orderId": "ORD-001",
  "customerId": "CUST-001",
  "amount": 99.99,
  "shippingAddress": "123 Main St",
  "items": [...]
}

# Check status
GET /api/order-processing/{instanceId}

# Get summary
GET /api/order-processing/{instanceId}/summary

# Export audit trail
GET /api/order-processing/{instanceId}/export
```

### 2. ApprovalChainExample.cs

**Purpose:** Multi-level document approval workflow

**Demonstrates:**
- Conditional routing based on business logic
- Multi-stage approval processes
- Decision branching
- Rejection handling

**Key Activities:**
- Submit Document
- Manager Review (approve/reject)
- Director Review (conditional)
- CFO Review (if amount > $10k)
- Send Approval/Rejection Notice
- Archive Document

**Usage:**
```bash
# Initialize workflow
POST /api/approval-chain/initialize

# Submit document
POST /api/approval-chain/submit
{
  "documentId": "DOC-001",
  "title": "Budget Request",
  "amount": 15000,
  "submittedBy": "john@example.com"
}

# Approve
POST /api/approval-chain/{instanceId}/approve
{
  "approvedBy": "manager@example.com",
  "comments": "Looks good"
}

# Reject
POST /api/approval-chain/{instanceId}/reject
{
  "approvedBy": "manager@example.com",
  "comments": "Needs revision"
}
```

### 3. ParallelExecutionExample.cs

**Purpose:** Concurrent task execution with synchronization

**Demonstrates:**
- Parallel activity execution
- Fork/join patterns
- ExecutionMode.Parallel
- Result aggregation
- Synchronization points

**Key Activities:**
- Start Processing
- Validate Inventory (parallel)
- Validate Payment (parallel)
- Get Shipping Quote (parallel)
- Check Promotions (parallel)
- Combine Results (sync point)
- Final Price Calculation
- Confirm Order

**Performance Benefit:** Executing 4 tasks in parallel reduces processing time from ~60s to ~25s

**Usage:**
```bash
# Initialize workflow
POST /api/parallel-execution/initialize

# Execute order with parallel processing
POST /api/parallel-execution/execute
{
  "orderId": "ORD-002",
  "items": [...],
  "shippingAddress": "456 Oak Ave",
  "paymentMethod": "credit_card",
  "customerEmail": "customer@example.com"
}

# Get combined results
GET /api/parallel-execution/{instanceId}/results
```

### 4. ErrorHandlingExample.cs

**Purpose:** Resilient workflows with retry policies and fallbacks

**Demonstrates:**
- Exponential backoff retry policy
- Fixed delay retry policy
- Linear backoff retry policy
- Fallback activities
- Error recovery paths
- Comprehensive error logging

**Retry Strategies:**
- **Exponential Backoff:** fetch_data (up to 3 attempts)
- **Fixed Delay:** transform_data (2-second delays)
- **Linear Backoff:** store_data (progressively longer delays)

**Fallback Paths:**
- API fails → Use cached data
- Database fails → Store offline

**Usage:**
```bash
# Initialize workflow
POST /api/error-handling/initialize

# Process data with error handling
POST /api/error-handling/process
{
  "dataSourceUrl": "https://api.example.com/data",
  "processingRules": {...}
}

# Get error information
GET /api/error-handling/{instanceId}/error-info
```

### 5. MonitoringExample.cs

**Purpose:** System monitoring, metrics, and health checks

**Demonstrates:**
- Metrics collection and reporting
- Health check implementation
- Performance trend analysis
- Slow workflow identification
- Failed workflow analysis
- Resource usage monitoring

**Key Endpoints:**
- `GET /api/monitoring/metrics` - Overall metrics
- `GET /api/monitoring/health` - Service health
- `GET /api/monitoring/audit-stats` - Audit trail statistics
- `GET /api/monitoring/performance-trends` - Performance over time
- `GET /api/monitoring/slow-workflows` - Slowest workflows
- `GET /api/monitoring/failed-workflows` - Failed workflow analysis
- `GET /api/monitoring/resource-usage` - System resource usage

**Usage:**
```bash
# Get metrics for today
GET /api/monitoring/metrics?period=today

# Get metrics for the week
GET /api/monitoring/metrics?period=week

# Check system health
GET /api/monitoring/health

# Get performance trends
GET /api/monitoring/performance-trends?days=7

# Find slowest workflows
GET /api/monitoring/slow-workflows?limit=10

# Analyze failures
GET /api/monitoring/failed-workflows?days=7

# Check resource usage
GET /api/monitoring/resource-usage
```

### 6. CustomActivityExample.cs

**Purpose:** Extending the framework with custom activities

**Demonstrates:**
- IActivityHandler interface implementation
- Custom business logic execution
- Activity registration
- Result handling
- Dependency injection in activities

**Custom Activities:**
- **CustomSmsActivity:** Send SMS notifications
- **CustomImageProcessingActivity:** Process images
- **CustomReportGenerationActivity:** Generate reports

**How to Implement:**
1. Implement `IActivityHandler` interface
2. Define ExecuteAsync method
3. Return result dictionary
4. Register in activity service
5. Reference in workflow definition

**Usage:**
```bash
# Initialize workflow
POST /api/custom-activity/initialize

# Execute workflow with custom activities
POST /api/custom-activity/execute
{
  "phoneNumber": "+1234567890",
  "message": "Your order is ready",
  "imageUrl": "https://example.com/image.jpg",
  "processingType": "resize",
  "reportType": "summary",
  "format": "pdf"
}

# Get execution results
GET /api/custom-activity/{instanceId}
```

## Running the Examples

### Prerequisites
- .NET 10 SDK
- SQL Server/PostgreSQL/SQLite
- Running workflow engine service

### Setup

1. **Start the application:**
```bash
dotnet run
```

2. **Access Swagger UI:**
```
http://localhost:5000/swagger
```

3. **Or use curl/Postman** to call the endpoints

### Step-by-Step Walkthrough

#### Order Processing Example

```bash
# 1. Initialize the workflow
curl -X POST http://localhost:5000/api/order-processing/initialize \
  -H "Content-Type: application/json"

# 2. Process an order (from the returned workflowId)
curl -X POST http://localhost:5000/api/order-processing/process \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "ORD-12345",
    "customerId": "CUST-789",
    "amount": 299.99,
    "shippingAddress": "123 Main Street, New York, NY 10001",
    "items": [
      {"productId": "PROD-001", "quantity": 2, "unitPrice": 99.99},
      {"productId": "PROD-002", "quantity": 1, "unitPrice": 100.01}
    ]
  }'

# 3. Check the order status (from the returned instanceId)
curl -X GET http://localhost:5000/api/order-processing/{instanceId} \
  -H "Authorization: Bearer YOUR_TOKEN"

# 4. Get order summary
curl -X GET http://localhost:5000/api/order-processing/{instanceId}/summary \
  -H "Authorization: Bearer YOUR_TOKEN"

# 5. Export audit trail
curl -X GET http://localhost:5000/api/order-processing/{instanceId}/export \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -o order-audit.csv
```

## Best Practices Demonstrated

### 1. Error Handling
- Use appropriate retry policies
- Provide fallback activities
- Log comprehensive error information

### 2. Performance
- Use parallel execution for independent tasks
- Implement proper synchronization points
- Cache frequently accessed data

### 3. Monitoring
- Collect metrics at each stage
- Implement health checks
- Track audit trails for compliance

### 4. Extensibility
- Implement custom activities for domain logic
- Use dependency injection
- Keep activities focused and reusable

### 5. State Management
- Use ExecutionContext for variable passing
- Immutable audit trail
- Clear variable naming

## Common Patterns

### Pattern 1: Sequential Processing
Order Processing → Validation → Payment → Shipping

### Pattern 2: Approval Chain
Document → Manager → Director → CFO (conditional)

### Pattern 3: Parallel Processing
Start → [Inventory, Payment, Shipping, Promotions] → Sync → Result

### Pattern 4: Error Recovery
Primary Path → Fallback → Notification → Logging

### Pattern 5: Monitoring
Metrics Collection → Analysis → Alerting

## Testing the Examples

```bash
# Run with test data
make test

# Run specific example tests
dotnet test --filter "OrderProcessingExample"

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## Troubleshooting

### Workflow not initializing
- Check database connection
- Verify permissions
- Review logs for detailed errors

### Activities timing out
- Increase timeout values
- Check for long-running operations
- Monitor system resources

### Parallel execution not working
- Verify ExecutionMode is set to Parallel
- Check MaxConcurrentActivities setting
- Ensure activities are truly independent

### Custom activities failing
- Verify handler registration
- Check activity type name matches
- Review exception details in logs

## Further Reading

- [Getting Started Guide](../docs/getting-started.md)
- [Architecture Guide](../docs/architecture.md)
- [API Reference](../docs/api-reference.md)
- [Configuration Guide](../docs/configuration.md)
- [Troubleshooting Guide](../docs/troubleshooting.md)

## Contributing

Found an issue with the examples? Please [open an issue](https://github.com/Sarmkadan/dotnet-workflow-engine/issues) or submit a pull request.

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com)**
