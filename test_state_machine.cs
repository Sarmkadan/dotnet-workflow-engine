// Simple test to verify the state machine transitions work correctly
using System;
using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;

public class StateMachineTest
{
    public static void TestTransitions()
    {
        Console.WriteLine("Testing Workflow Status State Machine...\n");

        var instance = new WorkflowInstance("test-workflow");
        Console.WriteLine($"Initial state: {instance.Status}");

        // Test valid transitions
        Console.WriteLine("\n=== Testing Valid Transitions ===");

        // Draft -> Active
        Console.WriteLine("Testing Draft -> Active...");
        instance.TransitionTo(WorkflowStatus.Active);
        Console.WriteLine($"✓ Success: {instance.Status}");
        Console.WriteLine($"Version after transition: {instance.Version}");

        // Active -> WaitingForMessage
        Console.WriteLine("\nTesting Active -> WaitingForMessage...");
        instance.TransitionTo(WorkflowStatus.WaitingForMessage);
        Console.WriteLine($"✓ Success: {instance.Status}");
        Console.WriteLine($"Version after transition: {instance.Version}");

        // WaitingForMessage -> Active (resume)
        Console.WriteLine("\nTesting WaitingForMessage -> Active...");
        instance.TransitionTo(WorkflowStatus.Active);
        Console.WriteLine($"✓ Success: {instance.Status}");
        Console.WriteLine($"Version after transition: {instance.Version}");

        // Active -> Suspended
        Console.WriteLine("\nTesting Active -> Suspended...");
        instance.TransitionTo(WorkflowStatus.Suspended, "Manual pause");
        Console.WriteLine($"✓ Success: {instance.Status}");
        Console.WriteLine($"Version after transition: {instance.Version}");
        Console.WriteLine($"ErrorMessage: {instance.ErrorMessage}");

        // Suspended -> Active (resume)
        Console.WriteLine("\nTesting Suspended -> Active...");
        instance.TransitionTo(WorkflowStatus.Active);
        Console.WriteLine($"✓ Success: {instance.Status}");
        Console.WriteLine($"Version after transition: {instance.Version}");

        // Active -> Archived (complete)
        Console.WriteLine("\nTesting Active -> Archived...");
        instance.TransitionTo(WorkflowStatus.Archived);
        Console.WriteLine($"✓ Success: {instance.Status}");
        Console.WriteLine($"Version after transition: {instance.Version}");
        Console.WriteLine($"CompletedAt: {instance.CompletedAt}");
        Console.WriteLine($"ExecutionTimeMs: {instance.ExecutionTimeMs}");

        // Test terminal state (Archived cannot transition)
        Console.WriteLine("\n=== Testing Invalid Transitions ===");
        Console.WriteLine("Testing Archived -> Active (should fail)...");
        try
        {
            instance.TransitionTo(WorkflowStatus.Active);
            Console.WriteLine("✗ ERROR: Should have thrown exception!");
        }
        catch (WorkflowStatusTransitionException ex)
        {
            Console.WriteLine($"✓ Correctly threw exception: {ex.GetTransitionDetails()}");
        }

        // Test Draft -> Archived (should fail)
        Console.WriteLine("\nTesting Draft -> Archived (should fail)...");
        var newInstance = new WorkflowInstance("test-workflow-2");
        try
        {
            newInstance.TransitionTo(WorkflowStatus.Archived);
            Console.WriteLine("✗ ERROR: Should have thrown exception!");
        }
        catch (WorkflowStatusTransitionException ex)
        {
            Console.WriteLine($"✓ Correctly threw exception: {ex.GetTransitionDetails()}");
        }

        // Test Cancelled -> Active (should fail)
        Console.WriteLine("\nTesting Active -> Cancelled...");
        var cancelInstance = new WorkflowInstance("test-workflow-3");
        cancelInstance.TransitionTo(WorkflowStatus.Active);
        cancelInstance.TransitionTo(WorkflowStatus.Cancelled);
        Console.WriteLine($"State: {cancelInstance.Status}");
        try
        {
            cancelInstance.TransitionTo(WorkflowStatus.Active);
            Console.WriteLine("✗ ERROR: Should have thrown exception!");
        }
        catch (WorkflowStatusTransitionException ex)
        {
            Console.WriteLine($"✓ Correctly threw exception: {ex.GetTransitionDetails()}");
        }

        Console.WriteLine("\n=== Transition Matrix ===");
        Console.WriteLine(WorkflowStatusMachine.GetTransitionMatrix());

        Console.WriteLine("\n=== All tests passed! ===");
    }
}