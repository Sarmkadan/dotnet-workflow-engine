// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# REST API Reference

Complete reference for all REST API endpoints.

## Base URL

```
http://localhost:5000/api
```

## Authentication

All endpoints require Bearer token authentication (except health):

```
Authorization: Bearer <jwt-token>
```

## Response Format

All responses use JSON format:

```json
{
  "success": true,
  "data": {...},
  "errors": null,
  "timestamp": "2026-01-15T10:30:00Z"
}
```

## Workflows Endpoints

### List Workflows

```http
GET /workflows
```

**Query Parameters:**
- `status` (optional): Active, Inactive, Archived
- `pageSize` (default: 10): Items per page
- `pageNumber` (default: 1): Page number
- `search` (optional): Search by name

**Response:**
```json
{
  "success": true,
  "data": {
    "workflows": [
      {
        "id": "550e8400-e29b-41d4-a716-446655440000",
        "name": "OrderProcessing",
        "version": 1,
        "status": "Active",
        "description": "Process customer orders",
        "createdAt": "2026-01-15T10:30:00Z",
        "publishedAt": "2026-01-15T10:35:00Z",
        "activitiesCount": 5,
        "instancesCount": 42
      }
    ],
    "total": 1,
    "pageSize": 10,
    "pageNumber": 1
  }
}
```

### Get Workflow Details

```http
GET /workflows/{id}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "OrderProcessing",
    "version": 1,
    "status": "Active",
    "description": "Process customer orders",
    "activities": [
      {
        "id": "validate_order",
        "name": "Validate Order",
        "type": "ValidationActivity",
        "timeout": "00:00:30",
        "retryPolicy": "None",
        "maxRetries": 0
      }
    ],
    "transitions": [
      {
        "id": "t1",
        "sourceActivityId": "validate_order",
        "targetActivityId": "process_payment",
        "condition": null
      }
    ],
    "createdAt": "2026-01-15T10:30:00Z",
    "publishedAt": "2026-01-15T10:35:00Z"
  }
}
```

### Create Workflow

```http
POST /workflows
Content-Type: application/json
```

**Request Body:**
```json
{
  "name": "OrderProcessing",
  "description": "Process customer orders",
  "activities": [
    {
      "id": "validate",
      "name": "Validate Order",
      "type": "ValidationActivity",
      "timeout": "00:00:30"
    }
  ],
  "transitions": [
    {
      "id": "t1",
      "sourceActivityId": "validate",
      "targetActivityId": "payment"
    }
  ]
}
```

**Response:** `201 Created`
```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "OrderProcessing",
    "version": 1
  }
}
```

### Update Workflow

```http
PUT /workflows/{id}
Content-Type: application/json
```

**Response:** `200 OK`

### Delete Workflow

```http
DELETE /workflows/{id}
```

**Response:** `204 No Content`

### Publish Workflow

```http
POST /workflows/{id}/publish
```

**Response:** `200 OK`

## Workflow Instances Endpoints

### Execute Workflow

```http
POST /workflows/{workflowId}/execute
Content-Type: application/json
```

**Request Body:**
```json
{
  "variables": {
    "orderId": "ORD-12345",
    "customerId": "CUST-67890",
    "amount": 99.99
  },
  "executionMode": "Sequential"
}
```

**Response:** `201 Created`
```json
{
  "success": true,
  "data": {
    "id": "650e8400-e29b-41d4-a716-446655440001",
    "workflowId": "550e8400-e29b-41d4-a716-446655440000",
    "status": "Running",
    "startedAt": "2026-01-15T10:35:00Z"
  }
}
```

### Get Instance Status

```http
GET /instances/{instanceId}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "650e8400-e29b-41d4-a716-446655440001",
    "workflowId": "550e8400-e29b-41d4-a716-446655440000",
    "status": "Running",
    "currentActivityId": "process_payment",
    "variables": {
      "orderId": "ORD-12345",
      "paymentStatus": "Processing"
    },
    "startedAt": "2026-01-15T10:35:00Z",
    "completedAt": null
  }
}
```

### List Instances

```http
GET /instances
```

**Query Parameters:**
- `workflowId` (optional): Filter by workflow
- `status` (optional): Running, Completed, Failed
- `pageSize` (default: 10): Items per page
- `pageNumber` (default: 1): Page number

**Response:**
```json
{
  "success": true,
  "data": {
    "instances": [
      {
        "id": "650e8400-e29b-41d4-a716-446655440001",
        "workflowId": "550e8400-e29b-41d4-a716-446655440000",
        "status": "Running",
        "startedAt": "2026-01-15T10:35:00Z"
      }
    ],
    "total": 42,
    "pageSize": 10,
    "pageNumber": 1
  }
}
```

### Get Instance Activities

```http
GET /instances/{instanceId}/activities
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "validate_order",
      "name": "Validate Order",
      "status": "Completed",
      "startedAt": "2026-01-15T10:35:00Z",
      "completedAt": "2026-01-15T10:35:05Z",
      "duration": "00:00:05"
    },
    {
      "id": "process_payment",
      "name": "Process Payment",
      "status": "Running",
      "startedAt": "2026-01-15T10:35:05Z",
      "completedAt": null,
      "duration": "00:00:10"
    }
  ]
}
```

### Cancel Instance

```http
POST /instances/{instanceId}/cancel
```

**Response:** `200 OK`

### Retry Activity

```http
POST /instances/{instanceId}/retry
Content-Type: application/json
```

**Request Body:**
```json
{
  "activityId": "process_payment"
}
```

**Response:** `200 OK`

## Audit Trail Endpoints

### List Audit Logs

```http
GET /audit
```

**Query Parameters:**
- `workflowId` (optional): Filter by workflow
- `instanceId` (optional): Filter by instance
- `activityId` (optional): Filter by activity
- `action` (optional): Created, Started, Completed, Failed
- `startDate` (optional): ISO 8601 date
- `endDate` (optional): ISO 8601 date
- `pageSize` (default: 50): Items per page
- `pageNumber` (default: 1): Page number

**Response:**
```json
{
  "success": true,
  "data": {
    "logs": [
      {
        "id": "audit-001",
        "timestamp": "2026-01-15T10:35:00Z",
        "workflowId": "550e8400-e29b-41d4-a716-446655440000",
        "instanceId": "650e8400-e29b-41d4-a716-446655440001",
        "activityId": "validate_order",
        "action": "Completed",
        "changes": {
          "status": "Completed",
          "result": "Valid"
        },
        "userId": "user-123",
        "ipAddress": "192.168.1.1"
      }
    ],
    "total": 150,
    "pageSize": 50,
    "pageNumber": 1
  }
}
```

### Get Instance Audit Log

```http
GET /audit/instances/{instanceId}
```

### Export Audit Trail

```http
GET /audit/export
```

**Query Parameters:**
- `format` (default: csv): csv, json, xml
- `workflowId` (optional): Filter by workflow
- `startDate` (optional): ISO 8601 date
- `endDate` (optional): ISO 8601 date

**Response:** File download (Content-Type: text/csv, application/json, application/xml)

## Health & Monitoring Endpoints

### Health Check

```http
GET /health
```

**Response:** `200 OK`
```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "cache": "Healthy",
    "hangfire": "Healthy"
  }
}
```

### Metrics

```http
GET /metrics
```

**Response:**
```json
{
  "success": true,
  "data": {
    "workflowsTotal": 12,
    "instancesRunning": 8,
    "instancesCompleted": 342,
    "instancesFailed": 5,
    "averageExecutionTime": "00:05:30",
    "totalActivitiesExecuted": 2847
  }
}
```

## Error Responses

### 400 Bad Request
```json
{
  "success": false,
  "errors": ["Invalid workflow definition: Activity 'X' not found"]
}
```

### 401 Unauthorized
```json
{
  "success": false,
  "errors": ["Invalid or missing authorization token"]
}
```

### 403 Forbidden
```json
{
  "success": false,
  "errors": ["User does not have permission to access this workflow"]
}
```

### 404 Not Found
```json
{
  "success": false,
  "errors": ["Workflow not found"]
}
```

### 500 Internal Server Error
```json
{
  "success": false,
  "errors": ["An unexpected error occurred"]
}
```

## Pagination

All list endpoints support pagination:

**Query Parameters:**
- `pageSize` (default: 10, max: 100): Items per page
- `pageNumber` (default: 1): Page number (1-based)

**Response includes:**
```json
{
  "data": [...],
  "total": 150,
  "pageSize": 10,
  "pageNumber": 1,
  "totalPages": 15
}
```

## Rate Limiting

API endpoints are rate-limited:
- 100 requests per minute per user
- 1000 requests per minute per IP

Response headers:
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 2026-01-15T10:31:00Z
```

## API Examples

### Example 1: Execute Order Processing Workflow

```bash
# Get the workflow ID
curl -X GET http://localhost:5000/api/workflows \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json"

# Execute the workflow
curl -X POST http://localhost:5000/api/workflows/550e8400-e29b-41d4-a716-446655440000/execute \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "variables": {
      "orderId": "ORD-12345",
      "customerId": "CUST-67890",
      "amount": 99.99
    }
  }'

# Check instance status
curl -X GET http://localhost:5000/api/instances/650e8400-e29b-41d4-a716-446655440001 \
  -H "Authorization: Bearer <token>"
```

### Example 2: Export Audit Trail

```bash
curl -X GET "http://localhost:5000/api/audit/export?format=csv&startDate=2026-01-01" \
  -H "Authorization: Bearer <token>" \
  -o audit-report.csv
```
