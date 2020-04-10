# Activity Handler Implementation Guide

Activity handlers are the execution units behind each workflow step. Any activity with a
`HandlerType` set requires a registered `IActivityHandler` implementation to run.

---

## Table of Contents

1. [The IActivityHandler Interface](#the-iactivityhandler-interface)
2. [Creating a Custom Handler](#creating-a-custom-handler)
3. [Registering Handlers with DI](#registering-handlers-with-di)
4. [Input Parameter Mapping](#input-parameter-mapping)
5. [Output Mapping](#output-mapping)
6. [Error Handling Best Practices](#error-handling-best-practices)
7. [Example Handlers](#example-handlers)
   - [HTTP Call Handler](#http-call-handler)
   - [Database Query Handler](#database-query-handler)
   - [File Processing Handler](#file-processing-handler)

---

## The IActivityHandler Interface

The interface lives inside `ActivityService` and has a single method:

```csharp
public interface IActivityHandler
{
    Task<Dictionary<string, object?>> ExecuteAsync(Activity activity, ExecutionContext context);
}
```

| Parameter   | Description                                                   |
|-------------|---------------------------------------------------------------|
| `activity`  | The activity definition containing configuration and metadata |
| `context`   | The live execution context with inputs, variables, and state  |

The return value is a dictionary of output values. Keys in that dictionary are matched against
the activity's `OutputMapping` to write results back into the workflow instance context.

---

## Creating a Custom Handler

1. Create a class that implements `IActivityHandler`.
2. Read inputs via `activity.GetInputParameter(key)` or `context.GetActivityInput(key)`.
3. Perform the work (I/O, computation, integration, etc.).
4. Return a `Dictionary<string, object?>` containing the output values.

Minimal skeleton:

```csharp
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using ExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

public class SendEmailHandler : ActivityService.IActivityHandler
{
    private readonly IEmailClient _emailClient;

    public SendEmailHandler(IEmailClient emailClient)
    {
        _emailClient = emailClient;
    }

    public async Task<Dictionary<string, object?>> ExecuteAsync(
        Activity activity,
        ExecutionContext context)
    {
        var to      = activity.GetInputParameter("to")?.ToString()
                      ?? throw new ArgumentException("Missing 'to' parameter");
        var subject = activity.GetInputParameter("subject")?.ToString() ?? string.Empty;
        var body    = activity.GetInputParameter("body")?.ToString() ?? string.Empty;

        var messageId = await _emailClient.SendAsync(to, subject, body);

        return new Dictionary<string, object?>
        {
            ["messageId"] = messageId,
            ["sentAt"]    = DateTime.UtcNow
        };
    }
}
```

---

## Registering Handlers with DI

Handlers must be registered before the `ActivityService` is used.  Register them during
application startup, typically in `Program.cs` or a dedicated extension method:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWorkflowEngine(builder.Configuration);

// Register your handlers as singletons or scoped services
builder.Services.AddSingleton<SendEmailHandler>();
builder.Services.AddSingleton<IEmailClient, SmtpEmailClient>();

var app = builder.Build();

// Wire handlers into ActivityService after the container is built
var activityService = app.Services.GetRequiredService<ActivityService>();
activityService.RegisterHandler("SendEmail", app.Services.GetRequiredService<SendEmailHandler>());

app.Run();
```

> **Tip:** Use a named constant (e.g. `"SendEmail"`) that matches the `HandlerType` value set
> on the `Activity` definition. This string is the lookup key.

---

## Input Parameter Mapping

Input parameters are defined on the `Activity` object and are available in two ways inside a
handler:

### From the activity definition

```csharp
// Set when building the workflow
activity.SetInputParameter("to", "customer@example.com");

// Read inside the handler
var to = activity.GetInputParameter("to")?.ToString();
```

### From the live execution context

When a prior activity writes a value to the instance context via `OutputMapping`, it is
accessible through the context variables:

```csharp
// Read a value that was produced by an earlier activity
var orderId = context.GetVariable<string>("orderId");
```

Combine both approaches to build dynamic, data-driven workflows:

```csharp
// Static configuration from activity definition
var templateId = activity.GetInputParameter("templateId")?.ToString();

// Dynamic data from earlier activities
var customerId = context.GetVariable<string>("customerId");
```

---

## Output Mapping

Return values from `ExecuteAsync` are written back to the workflow instance context according to
the activity's `OutputMapping` dictionary.

```csharp
// Workflow definition (key = handler output key, value = context variable name)
activity.AddOutputMapping("messageId", "lastEmailMessageId");

// Handler returns:
return new Dictionary<string, object?>
{
    ["messageId"] = "msg-42a3b"   // ← mapped to context variable "lastEmailMessageId"
};
```

A subsequent activity can then read `context.GetVariable<string>("lastEmailMessageId")`.

---

## Error Handling Best Practices

### Let exceptions propagate naturally

`ActivityService` wraps every handler call with retry logic. Throw exceptions freely — the
retry policy configured on the activity (fixed delay, exponential back-off, etc.) will handle
transient failures automatically.

```csharp
// Good: throw so the retry policy can decide what to do
var response = await _httpClient.PostAsync(url, content);
response.EnsureSuccessStatusCode();   // throws HttpRequestException on failure
```

### Use typed, descriptive exceptions

Wrapping in an `ActivityException` (or a domain-specific exception) gives the audit trail a
meaningful error code:

```csharp
using DotNetWorkflowEngine.Exceptions;

if (result.StatusCode == 404)
    throw new ActivityException(
        $"Resource not found at {url}",
        activity.Id);
```

### Handle non-retriable errors explicitly

If an error condition should not trigger a retry (e.g., a business validation failure), mark
the activity as optional or use a short-circuit pattern:

```csharp
if (!isValid)
{
    // Return a failure signal in the output instead of throwing
    return new Dictionary<string, object?> { ["isValid"] = false };
}
```

### Honour CancellationToken where possible

Long-running handlers should accept and respect cancellation:

```csharp
public async Task<Dictionary<string, object?>> ExecuteAsync(
    Activity activity,
    ExecutionContext context,
    CancellationToken cancellationToken = default)
{
    var response = await _httpClient.GetAsync(url, cancellationToken);
    // ...
}
```

---

## Example Handlers

### HTTP Call Handler

Calls an external HTTP endpoint, maps the response body to a context variable.

```csharp
using System.Net.Http.Json;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using ExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

public class HttpCallHandler : ActivityService.IActivityHandler
{
    private readonly HttpClient _http;

    public HttpCallHandler(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("workflow");
    }

    public async Task<Dictionary<string, object?>> ExecuteAsync(
        Activity activity,
        ExecutionContext context)
    {
        var url    = activity.GetInputParameter("url")?.ToString()
                     ?? throw new ArgumentException("Missing 'url' parameter");
        var method = activity.GetInputParameter("method")?.ToString() ?? "GET";

        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        // Forward a bearer token from context if present
        if (context.GetVariable<string>("bearerToken") is { } token)
            request.Headers.Authorization = new("Bearer", token);

        using var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var body       = await response.Content.ReadAsStringAsync();
        var statusCode = (int)response.StatusCode;

        return new Dictionary<string, object?>
        {
            ["statusCode"]   = statusCode,
            ["responseBody"] = body
        };
    }
}
```

Registration:

```csharp
builder.Services.AddHttpClient("workflow");
builder.Services.AddSingleton<HttpCallHandler>();
// after app.Build():
activityService.RegisterHandler("HttpCall", app.Services.GetRequiredService<HttpCallHandler>());
```

Activity definition:

```csharp
new Activity
{
    Id          = "call-payment-api",
    Name        = "Call Payment API",
    HandlerType = "HttpCall",
    InputParameters = new()
    {
        ["url"]    = "https://payments.example.com/charge",
        ["method"] = "POST"
    },
    OutputMapping = new()
    {
        ["statusCode"]   = "paymentStatusCode",
        ["responseBody"] = "paymentResponse"
    },
    RetryPolicy = RetryPolicy.ExponentialBackoff,
    MaxRetries  = 3
}
```

---

### Database Query Handler

Runs a parameterised SQL query and returns the first row as a dictionary.

```csharp
using Microsoft.Data.SqlClient;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using ExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

public class DatabaseQueryHandler : ActivityService.IActivityHandler
{
    private readonly string _connectionString;

    public DatabaseQueryHandler(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException("Connection string not configured");
    }

    public async Task<Dictionary<string, object?>> ExecuteAsync(
        Activity activity,
        ExecutionContext context)
    {
        var sql = activity.GetInputParameter("sql")?.ToString()
                  ?? throw new ArgumentException("Missing 'sql' parameter");

        var results = new Dictionary<string, object?>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);

        // Bind parameters from activity inputs (prefix "param:")
        foreach (var (key, value) in activity.InputParameters)
        {
            if (key.StartsWith("param:"))
                command.Parameters.AddWithValue(key[6..], value ?? DBNull.Value);
        }

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            for (var i = 0; i < reader.FieldCount; i++)
                results[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        }

        results["rowsRead"] = reader.RecordsAffected;
        return results;
    }
}
```

Registration:

```csharp
builder.Services.AddSingleton<DatabaseQueryHandler>();
// after app.Build():
activityService.RegisterHandler("DatabaseQuery", app.Services.GetRequiredService<DatabaseQueryHandler>());
```

---

### File Processing Handler

Reads a file, applies a transformation, and writes the result.

```csharp
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using ExecutionContext = DotNetWorkflowEngine.Models.ExecutionContext;

public class FileProcessingHandler : ActivityService.IActivityHandler
{
    public async Task<Dictionary<string, object?>> ExecuteAsync(
        Activity activity,
        ExecutionContext context)
    {
        var inputPath  = activity.GetInputParameter("inputPath")?.ToString()
                         ?? throw new ArgumentException("Missing 'inputPath' parameter");
        var outputPath = activity.GetInputParameter("outputPath")?.ToString()
                         ?? throw new ArgumentException("Missing 'outputPath' parameter");

        if (!File.Exists(inputPath))
            throw new FileNotFoundException($"Input file not found: {inputPath}");

        var content     = await File.ReadAllTextAsync(inputPath);
        var transformed = content.ToUpperInvariant(); // replace with real logic

        await File.WriteAllTextAsync(outputPath, transformed);

        return new Dictionary<string, object?>
        {
            ["bytesWritten"] = transformed.Length,
            ["outputPath"]   = outputPath
        };
    }
}
```

Registration:

```csharp
builder.Services.AddSingleton<FileProcessingHandler>();
// after app.Build():
activityService.RegisterHandler("FileProcessing", app.Services.GetRequiredService<FileProcessingHandler>());
```

---

## See Also

- [Workflow Patterns](workflow-patterns.md) — parallel execution, error paths, and branching
- [Configuration Guide](configuration.md) — global retry defaults and timeouts
- [API Reference](api-reference.md) — REST endpoints for managing workflow definitions
