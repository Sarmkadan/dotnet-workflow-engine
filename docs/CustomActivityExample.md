# CustomActivityExample
The `CustomActivityExample` type is a central component in the `dotnet-workflow-engine` project, designed to demonstrate the integration and execution of custom activities within a workflow. It encapsulates various custom activities such as SMS sending, image processing, and report generation, providing a unified interface for their execution and management.

## API
- **Constructors**: 
  - `public CustomActivityExample`: Initializes a new instance of the `CustomActivityExample` class.
- **Properties**:
  - `public string PhoneNumber`: Gets or sets the phone number for SMS activities.
  - `public string Message`: Gets or sets the message content for SMS activities.
  - `public string ImageUrl`: Gets or sets the URL of the image for image processing activities.
  - `public string ProcessingType`: Gets or sets the type of image processing to apply.
  - `public string ReportType`: Gets or sets the type of report to generate.
  - `public string Format`: Gets or sets the format of the report.
- **Methods**:
  - `public async Task<Dictionary<string, object?>> ExecuteAsync`: Executes the custom activity asynchronously, returning a dictionary with execution results.
  - `public async Task<ActionResult> InitializeWorkflow`: Initializes a workflow, preparing it for execution.
  - `public async Task<ActionResult> ExecuteWithCustomActivities`: Executes a workflow with custom activities.
  - `public async Task<ActionResult> GetExecutionResults`: Retrieves the results of a workflow execution.

## Usage
The following examples illustrate how to utilize the `CustomActivityExample` class for executing custom activities and managing workflows:
```csharp
// Example 1: Executing a custom SMS activity
var customActivity = new CustomActivityExample();
customActivity.PhoneNumber = "+1234567890";
customActivity.Message = "Hello, this is a test message.";
var executionResults = await customActivity.ExecuteAsync();
Console.WriteLine($"SMS sent to {customActivity.PhoneNumber}: {customActivity.Message}");

// Example 2: Initializing and executing a workflow with custom activities
var workflowInitializer = new CustomActivityExample();
var initializationResult = await workflowInitializer.InitializeWorkflow();
if (initializationResult.IsSuccess)
{
    var executionResult = await workflowInitializer.ExecuteWithCustomActivities();
    var results = await workflowInitializer.GetExecutionResults();
    Console.WriteLine("Workflow executed successfully.");
}
```

## Notes
- **Thread Safety**: The `CustomActivityExample` class is designed to support concurrent access, but it is the responsibility of the caller to ensure that properties are accessed and modified in a thread-safe manner.
- **Edge Cases**: When executing custom activities, be aware of potential exceptions related to network connectivity (for SMS and image processing activities) and data formatting issues (for report generation). Always handle exceptions appropriately to maintain the integrity of the workflow.
- **Inheritance and Polymorphism**: This class may serve as a base for deriving more specialized activity examples, leveraging polymorphism to extend its functionality while maintaining a common interface for workflow execution and management.
