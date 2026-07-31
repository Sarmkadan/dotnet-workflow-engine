using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using DotNetWorkflowEngine.Monitoring;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DotNetWorkflowEngine.Tests;

public class WorkflowMetricsTests
{
    private readonly Mock<ILogger<WorkflowMetrics>> _loggerMock = new();
    private readonly WorkflowMetrics _metrics;

    public WorkflowMetricsTests()
    {
        _metrics = new WorkflowMetrics(_loggerMock.Object);
    }

    [Fact]
    public async Task TestMetricsRecording()
    {
        _metrics.RecordWorkflowExecution("wf1", 100, true);
        _metrics.RecordWorkflowExecution("wf2", 200, false);
        _metrics.RecordActivityExecution("act1", 50, true);
        _metrics.RecordActivityExecution("act2", 150, false);
        _metrics.RecordError("error1", "details1");
        _metrics.RecordError("error1", "details2");

        var snapshot = await _metrics.GetMetricsAsync();

        Assert.Equal(2, snapshot.TotalWorkflowsExecuted);
        Assert.Equal(1, snapshot.SuccessfulWorkflows);
        Assert.Equal(1, snapshot.FailedWorkflows);
        Assert.Equal(150, snapshot.AverageWorkflowDurationMs);
        Assert.Equal(2, snapshot.TotalActivitiesExecuted);
        Assert.Equal(1, snapshot.SuccessfulActivities);
        Assert.Equal(1, snapshot.FailedActivities);
        Assert.Equal(100, snapshot.AverageActivityDurationMs);
        Assert.Equal(2, snapshot.ErrorCount["error1"]);
    }
}
