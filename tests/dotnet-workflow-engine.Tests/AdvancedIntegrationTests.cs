// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Enums;
using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNetWorkflowEngine.Tests;

public class AdvancedIntegrationTests
{
    private (WorkflowExecutionService, ActivityService, WorkflowDefinitionService, AuditService) CreateServices()
    {
        var auditRepoMock = new Mock<IAuditRepository>();
        auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLogEntry>())).Returns(Task.CompletedTask);

        var auditService = new AuditService(auditRepoMock.Object);
        var definitionService = new WorkflowDefinitionService();
        var retryPolicyService = new RetryPolicyService();
        var activityService = new ActivityService(retryPolicyService);
        var executionService = new WorkflowExecutionService(definitionService, auditService, activityService);

        return (executionService, activityService, definitionService, auditService);
    }

    [Fact]
    public async Task ComplexWorkflow_WithParallelPaths_ExecutesSuccessfully()
    {
        var (executionService, activityService, _, _) = CreateServices();
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<ExecutionContext>()))
            .ReturnsAsync(new Dictionary<string, object?>());
        activityService.RegisterHandler("default", mockHandler.Object);

        var workflow = new Workflow
        {
            Id = "parallel-workflow",
            Name = "Parallel Workflow",
            StartActivityId = "split",
            EndActivityId = "merge",
            Activities = new List<Activity>
            {
                new Activity { Id = "split", Name = "Parallel Split", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" },
                new Activity { Id = "path-a", Name = "Path A", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" },
                new Activity { Id = "path-b", Name = "Path B", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" },
                new Activity { Id = "merge", Name = "Parallel Merge", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" }
            },
            Transitions = new List<Transition>
            {
                new Transition { Id = "t1", FromActivityId = "split", ToActivityId = "path-a" },
                new Transition { Id = "t2", FromActivityId = "split", ToActivityId = "path-b" },
                new Transition { Id = "t3", FromActivityId = "path-a", ToActivityId = "merge" },
                new Transition { Id = "t4", FromActivityId = "path-b", ToActivityId = "merge" }
            }
        };
        workflow.Publish();

        var instance = executionService.CreateInstance(workflow.Id);
        instance.Start();

        var result = await executionService.StartAsync(instance.Id);

        result.Should().NotBeNull();
        mockHandler.Verify(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<ExecutionContext>()), Times.AtLeast(4));
    }

    [Fact]
    public async Task WorkflowWithErrorHandling_RecoverableError_CompletesSuccessfully()
    {
        var (executionService, activityService, _, _) = CreateServices();
        var callCount = 0;

        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(
                It.Is<Activity>(a => a.Id == "flaky-activity"),
                It.IsAny<ExecutionContext>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount < 2)
                    return Task.FromException<Dictionary<string, object?>>(new TimeoutException("Temporary failure"));
                return Task.FromResult(new Dictionary<string, object?>());
            });
        mockHandler.Setup(h => h.ExecuteAsync(
                It.Is<Activity>(a => a.Id != "flaky-activity"),
                It.IsAny<ExecutionContext>()))
            .ReturnsAsync(new Dictionary<string, object?>());

        activityService.RegisterHandler("default", mockHandler.Object);

        var workflow = new Workflow
        {
            Id = "resilient-workflow",
            Name = "Resilient Workflow",
            StartActivityId = "start",
            EndActivityId = "end",
            Activities = new List<Activity>
            {
                new Activity { Id = "start", Name = "Start", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" },
                new Activity
                {
                    Id = "flaky-activity",
                    Name = "Flaky Activity",
                    TimeoutSeconds = 30,
                    MaxRetries = 3,
                    RetryPolicy = RetryPolicy.ExponentialBackoff,
                    HandlerType = "default"
                },
                new Activity { Id = "end", Name = "End", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" }
            },
            Transitions = new List<Transition>
            {
                new Transition { Id = "t1", FromActivityId = "start", ToActivityId = "flaky-activity" },
                new Transition { Id = "t2", FromActivityId = "flaky-activity", ToActivityId = "end" }
            }
        };
        workflow.Publish();

        var instance = executionService.CreateInstance(workflow.Id);
        instance.Start();

        var result = await executionService.StartAsync(instance.Id);

        result.Should().NotBeNull();
        callCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task LongRunningWorkflow_PreservesStateAcrossActivities()
    {
        var (executionService, activityService, _, _) = CreateServices();
        var executedSteps = new List<string>();

        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<ExecutionContext>()))
            .Callback<Activity, ExecutionContext>((activity, ctx) =>
            {
                executedSteps.Add(activity.Id);
                ctx.SetVariable($"{activity.Id}_executed", true);
            })
            .ReturnsAsync(new Dictionary<string, object?>());

        activityService.RegisterHandler("default", mockHandler.Object);

        var workflow = new Workflow
        {
            Id = "state-preserving-workflow",
            Name = "State Preserving",
            StartActivityId = "step1",
            EndActivityId = "step5",
            Activities = new List<Activity>
            {
                new Activity { Id = "step1", Name = "Step 1", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" },
                new Activity { Id = "step2", Name = "Step 2", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" },
                new Activity { Id = "step3", Name = "Step 3", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" },
                new Activity { Id = "step4", Name = "Step 4", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" },
                new Activity { Id = "step5", Name = "Step 5", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" }
            },
            Transitions = new List<Transition>
            {
                new Transition { Id = "t1", FromActivityId = "step1", ToActivityId = "step2" },
                new Transition { Id = "t2", FromActivityId = "step2", ToActivityId = "step3" },
                new Transition { Id = "t3", FromActivityId = "step3", ToActivityId = "step4" },
                new Transition { Id = "t4", FromActivityId = "step4", ToActivityId = "step5" }
            }
        };
        workflow.Publish();

        var instance = executionService.CreateInstance(workflow.Id);
        instance.Start();

        await executionService.StartAsync(instance.Id);

        executedSteps.Should().HaveCount(5);
        executedSteps.Should().Equal(new[] { "step1", "step2", "step3", "step4", "step5" });
    }

    [Fact]
    public async Task WorkflowWithMultipleInstances_EachMaintainsOwnState()
    {
        var (executionService, activityService, _, _) = CreateServices();
        var executionTraces = new Dictionary<string, List<string>>();

        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<ExecutionContext>()))
            .Callback<Activity, ExecutionContext>((activity, ctx) =>
            {
                var instanceId = ctx.WorkflowInstanceId;
                if (!executionTraces.ContainsKey(instanceId))
                    executionTraces[instanceId] = new List<string>();
                executionTraces[instanceId].Add(activity.Id);
            })
            .ReturnsAsync(new Dictionary<string, object?>());

        activityService.RegisterHandler("default", mockHandler.Object);

        var workflow = new Workflow
        {
            Id = "multi-instance-workflow",
            Name = "Multi Instance",
            StartActivityId = "start",
            EndActivityId = "end",
            Activities = new List<Activity>
            {
                new Activity { Id = "start", Name = "Start", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" },
                new Activity { Id = "end", Name = "End", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" }
            },
            Transitions = new List<Transition>
            {
                new Transition { Id = "t1", FromActivityId = "start", ToActivityId = "end" }
            }
        };
        workflow.Publish();

        var instances = new List<WorkflowInstance>();
        for (int i = 0; i < 3; i++)
        {
            var instance = executionService.CreateInstance(workflow.Id);
            instance.Start();
            instances.Add(instance);
        }

        await Task.WhenAll(instances.Select(inst => executionService.StartAsync(inst.Id)));

        executionTraces.Should().HaveCount(3);
        foreach (var trace in executionTraces.Values)
        {
            trace.Should().Equal(new[] { "start", "end" });
        }
    }

    [Fact]
    public async Task WorkflowWithConditionalRouting_SelectsCorrectPathBasedOnContext()
    {
        var (executionService, activityService, _, _) = CreateServices();
        var executedPaths = new List<string>();

        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<ExecutionContext>()))
            .Callback<Activity, ExecutionContext>((activity, ctx) =>
            {
                if (activity.Id == "decision")
                {
                    ctx.SetVariable("userType", "premium");
                }
                executedPaths.Add(activity.Id);
            })
            .ReturnsAsync(new Dictionary<string, object?>());

        activityService.RegisterHandler("default", mockHandler.Object);

        var workflow = new Workflow
        {
            Id = "conditional-workflow",
            Name = "Conditional Routing",
            StartActivityId = "decision",
            EndActivityId = "complete",
            Activities = new List<Activity>
            {
                new Activity { Id = "decision", Name = "Decision", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" },
                new Activity { Id = "premium-path", Name = "Premium Path", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" },
                new Activity { Id = "standard-path", Name = "Standard Path", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" },
                new Activity { Id = "complete", Name = "Complete", TimeoutSeconds = 30, MaxRetries = 0, HandlerType = "default" }
            },
            Transitions = new List<Transition>
            {
                new Transition { Id = "t1", FromActivityId = "decision", ToActivityId = "premium-path", ConditionExpression = "${userType} == \"premium\"" },
                new Transition { Id = "t2", FromActivityId = "decision", ToActivityId = "standard-path", ConditionExpression = "${userType} != \"premium\"" },
                new Transition { Id = "t3", FromActivityId = "premium-path", ToActivityId = "complete" },
                new Transition { Id = "t4", FromActivityId = "standard-path", ToActivityId = "complete" }
            }
        };
        workflow.Publish();

        var instance = executionService.CreateInstance(workflow.Id);
        instance.Start();

        await executionService.StartAsync(instance.Id);

        executedPaths.Should().Contain(new[] { "decision", "premium-path", "complete" });
        executedPaths.Should().NotContain("standard-path");
    }

    [Fact]
    public void WorkflowLifecycle_FullCycle_StateTransitionsCorrectly()
    {
        var (executionService, _, definitionService, _) = CreateServices();

        // Create
        var workflow = definitionService.CreateWorkflow("full-cycle", "Full Lifecycle");
        workflow.Should().NotBeNull();

        // Configure
        definitionService.AddActivity("full-cycle", new Activity { Id = "act1", Name = "Activity 1", TimeoutSeconds = 30 });
        definitionService.SetStartActivity("full-cycle", "act1");
        definitionService.SetEndActivity("full-cycle", "act1");

        // Validate
        var isValid = definitionService.ValidateWorkflow("full-cycle", out var errors);
        isValid.Should().BeTrue();

        // Publish
        definitionService.PublishWorkflow("full-cycle");
        var publishedWorkflow = definitionService.GetWorkflow("full-cycle");
        publishedWorkflow!.IsPublished.Should().BeTrue();

        // Execute
        var instance = executionService.CreateInstance("full-cycle");
        instance.Status.Should().Be(WorkflowStatus.Draft);

        instance.Start();
        instance.Status.Should().Be(WorkflowStatus.Active);

        // Cleanup
        var deleted = definitionService.DeleteWorkflow("full-cycle");
        deleted.Should().BeTrue();
        definitionService.GetWorkflow("full-cycle").Should().BeNull();
    }

    [Fact]
    public async Task ActivityWithTimeout_CompletesWithinTimeLimit()
    {
        var (executionService, activityService, _, _) = CreateServices();
        var mockHandler = new Mock<ActivityService.IActivityHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<Activity>(), It.IsAny<ExecutionContext>()))
            .ReturnsAsync(new Dictionary<string, object?>());

        activityService.RegisterHandler("default", mockHandler.Object);

        var workflow = new Workflow
        {
            Id = "timeout-workflow",
            Name = "Timeout Test",
            StartActivityId = "quick-task",
            EndActivityId = "quick-task",
            Activities = new List<Activity>
            {
                new Activity { Id = "quick-task", Name = "Quick Task", TimeoutSeconds = 60, MaxRetries = 0, HandlerType = "default" }
            }
        };
        workflow.Publish();

        var instance = executionService.CreateInstance(workflow.Id);
        instance.Start();

        var startTime = DateTime.UtcNow;
        await executionService.StartAsync(instance.Id);
        var duration = DateTime.UtcNow - startTime;

        duration.Should().BeLessThan(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void WorkflowBuilder_CreateSerialWorkflow_BuildsValidWorkflow()
    {
        var definitionService = new WorkflowDefinitionService();

        var workflow = Utilities.WorkflowBuilder
            .CreateSerial("serial-wf", "Serial Process", definitionService, "Init", "Process", "Finalize")
            .Build();

        workflow.Activities.Should().HaveCount(3);
        workflow.Activities.Select(a => a.Id).Should().Equal(new[] { "init", "process", "finalize" });
    }

    [Fact]
    public void WorkflowSerialization_RoundTrip_PreservesStructure()
    {
        var workflow = new Workflow
        {
            Id = "serialize-test",
            Name = "Serialization Test",
            StartActivityId = "start",
            Activities = new List<Activity>
            {
                new Activity { Id = "start", Name = "Start", TimeoutSeconds = 30, MaxRetries = 0 }
            }
        };

        var json = Utilities.SerializationHelper.ToJson(workflow);
        var deserialized = Utilities.SerializationHelper.FromJson<Workflow>(json);

        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be("serialize-test");
        deserialized.Activities.Should().HaveCount(1);
    }
}
