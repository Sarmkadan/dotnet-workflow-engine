# v2 Basic Usage Example

This example demonstrates the basic usage of dotnet-workflow-engine v2.0 with the new features and improvements.

## What This Example Shows

- Creating a simple workflow with visual layout information
- Registering the workflow in the database
- Executing the workflow with variables
- Displaying the execution results

## Prerequisites

- .NET 10 SDK
- SQLite (included with .NET)

## Running the Example

```bash
# Navigate to the example directory
cd examples/v2-basic-usage

# Restore dependencies
dotnet restore

# Run the example
dotnet run
```

## Key Features Demonstrated

1. **Visual Layout**: Activities include `Display` information with positions and colors
2. **New Activity Types**: Uses v2.0 activity types like `ValidatorActivity`, `ApprovalActivity`, `EmailActivity`
3. **Workflow Registration**: Shows how to save workflow definitions
4. **Execution**: Demonstrates workflow execution with context and variables
5. **Result Handling**: Shows how to process execution results

## Expected Output

```
✅ Workflow registered successfully!
🚀 Starting workflow execution...
Workflow ID: 12345678-1234-1234-1234-123456789abc
Instance ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx

✅ Workflow execution completed!
Status: Completed
Instance ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
Completed Activities: 5
Variables:
  - RequestId: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
  - Requester: john.doe@example.com
  - Amount: 1500.00
  - ApprovalRequired: True
```

## Next Steps

- Try modifying the workflow definition
- Add more activities and transitions
- Experiment with different activity types
- Check the database to see the workflow and instance records
