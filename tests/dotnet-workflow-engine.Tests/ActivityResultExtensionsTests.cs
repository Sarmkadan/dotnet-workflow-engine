using DotNetWorkflowEngine.Models;
using DotNetWorkflowEngine.Enums;
using Xunit;

namespace DotNetWorkflowEngine.Tests;

public class ActivityResultExtensionsTests
{
    [Fact]
    public void Succeeded_ReturnsTrue_WhenStatusIsCompleted()
    {
        var result = new ActivityResult { Status = ActivityStatus.Completed };
        Assert.True(result.Succeeded());
    }

    [Fact]
    public void Succeeded_ReturnsFalse_WhenStatusIsNotCompleted()
    {
        var result = new ActivityResult { Status = ActivityStatus.Failed };
        Assert.False(result.Succeeded());
    }

    [Fact]
    public void FailureReason_ReturnsErrorMessage_WhenFailed()
    {
        var result = new ActivityResult { Status = ActivityStatus.Failed, ErrorMessage = "Error" };
        Assert.Equal("Error", result.FailureReason());
    }

    [Fact]
    public void Then_ExecutesNext_WhenSucceeded()
    {
        var result = new ActivityResult { Status = ActivityStatus.Completed };
        bool executed = false;
        result.Then(r => {
            executed = true;
            return r;
        });
        Assert.True(executed);
    }

    [Fact]
    public void Then_DoesNotExecuteNext_WhenFailed()
    {
        var result = new ActivityResult { Status = ActivityStatus.Failed };
        bool executed = false;
        result.Then(r => {
            executed = true;
            return r;
        });
        Assert.False(executed);
    }

    [Fact]
    public void Then_ReturnsNextResult_WhenSucceeded()
    {
        var result = new ActivityResult { Status = ActivityStatus.Completed };
        var nextResult = new ActivityResult { Status = ActivityStatus.Completed, ActivityId = "Next" };
        
        var finalResult = result.Then(r => nextResult);
        
        Assert.Same(nextResult, finalResult);
    }
}
