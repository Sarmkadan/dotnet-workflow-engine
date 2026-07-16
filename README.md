# dotnet-workflow-engine

[![Build Status](https://dev.azure.com/.../dotnet-workflow-engine/_apis/build/status/...)](https://dev.azure.com/.../dotnet-workflow-engine/_build/latest?definitionId=...)

The dotnet-workflow-engine is a lightweight, extensible workflow engine written in C#. It supports parallel execution, conditional branching, retry policies, and stateful activities. The engine is designed to be easily integrated into existing .NET applications and can be extended with custom activity handlers.

## AdvancedIntegrationTests

The `AdvancedIntegrationTests` class contains comprehensive integration tests for verifying advanced workflow engine scenarios and complex workflows. These tests cover a range of features, including parallel workflow execution, error handling with retry policies, state preservation across activities, and conditional routing.

Example usage:
```csharp
var advancedTests = new AdvancedIntegrationTests();
await advancedTests.ComplexWorkflow_WithParallelPaths_ExecutesSuccessfully();
await advancedTests.WorkflowWithErrorHandling_RecoverableError_CompletesSuccessfully();
await advancedTests.LongRunningWorkflow_PreservesStateAcrossActivities();
await advancedTests.WorkflowWithMultipleInstances_EachMaintainsOwnState();
await advancedTests.WorkflowWithConditionalRouting_SelectsCorrectPathBasedOnContext();
advancedTests.WorkflowLifecycle_FullCycle_StateTransitionsCorrectly();
await advancedTests.ActivityWithTimeout_CompletesWithinTimeLimit();
advancedTests.WorkflowBuilder_CreateSerialWorkflow_BuildsValidWorkflow();
advancedTests.WorkflowSerialization_RoundTrip_PreservesStructure();
```

## SerializationHelperTests

The `SerializationHelperTests` class demonstrates how to use the `SerializationHelper` utility to serialize and deserialize workflow engine objects, perform deep cloning, merge objects, and validate JSON. It covers typical scenarios such as converting activities to JSON, handling nulls, and ensuring data integrity across round‑trips.

Example usage:
```csharp
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Utilities;

// Create an activity instance
var activity = new Activity
{
    Id = "act-1",
    Name = "Sample Activity",
    TimeoutSeconds = 45,
    MaxRetries = 3
};

// Serialize to JSON
string json = SerializationHelper.ToJson(activity);
Console.WriteLine(json); // {"id":"act-1","name":"Sample Activity","timeoutSeconds":45,"maxRetries":3}

// Deserialize back to an object
var deserialized = SerializationHelper.FromJson<Activity>(json);
Console.WriteLine(deserialized?.Name); // Sample Activity

// Deep clone the activity
var clone = SerializationHelper.DeepClone(activity);
clone!.Name = "Cloned Activity";
Console.WriteLine(activity.Name); // Sample Activity (unchanged)

// Merge two activities (second overrides first)
var updated = new Activity { Id = "act-1", Name = "Updated Activity", MaxRetries = 5 };
var merged = SerializationHelper.Merge(activity, updated);
Console.WriteLine(merged.Name); // Updated Activity
Console.WriteLine(merged.MaxRetries); // 5
```

## WorkflowValidatorTests

`WorkflowValidatorTests` provides a suite of unit tests that verify the correctness of the workflow validation logic. Each public method exercises a specific validation rule, ensuring that workflows, activities, and transitions are checked for required fields, consistency, and logical correctness.

Typical usage involves creating an instance of the test class and invoking the desired test methods directly (for example, when debugging or running tests programmatically):

```csharp
using DotNetWorkflowEngine.Tests;

// Create the test class instance
var validatorTests = new WorkflowValidatorTests();

// Run a selection of validation scenarios
validatorTests.ValidateWorkflow_ValidWorkflow_ReturnsValid();
validatorTests.ValidateWorkflow_MissingId_ReturnsError();
validatorTests.ValidateWorkflow_MissingName_ReturnsError();
validatorTests.ValidateWorkflow_NoActivities_ReturnsError();
validatorTests.ValidateWorkflow_InvalidActivity_ReturnsError();
validatorTests.ValidateWorkflow_StartActivityNotFound_ReturnsError();
validatorTests.ValidateWorkflow_EndActivityNotFound_ReturnsError();
validatorTests.ValidateWorkflow_NoStartActivity_ReturnsWarning();
validatorTests.ValidateWorkflow_InvalidTransition_ReturnsError();

validatorTests.ValidateActivity_ValidActivity_ReturnsValid();
validatorTests.ValidateActivity_MissingId_ReturnsError();
validatorTests.ValidateActivity_MissingName_ReturnsError();
validatorTests.ValidateActivity_InvalidTimeout_ReturnsError();
validatorTests.ValidateActivity_NegativeRetries_ReturnsError();
validatorTests.ValidateActivity_RetriesWithoutPolicy_ReturnsWarning();

validatorTests.ValidateTransition_ValidTransition_ReturnsValid();
validatorTests.ValidateTransition_MissingFromActivity_ReturnsError();
validatorTests.ValidateTransition_MissingToActivity_ReturnsError();
validatorTests.ValidateTransition_FromActivityNotFound_ReturnsError();
validatorTests.ValidateTransition_ToActivityNotFound_ReturnsError();
```

These calls execute the underlying assertions (via FluentAssertions) and will throw if any validation rule fails, making them useful for ad‑hoc verification during development.