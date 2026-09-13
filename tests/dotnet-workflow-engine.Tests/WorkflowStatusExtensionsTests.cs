using DotNetWorkflowEngine.Enums;
using Xunit;

namespace DotNetWorkflowEngine.Tests;

public class WorkflowStatusExtensionsTests
{
    [Theory]
    [InlineData(WorkflowStatus.Archived)]
    [InlineData(WorkflowStatus.Cancelled)]
    public void IsTerminal_TerminalStatus_ReturnsTrue(WorkflowStatus status)
    {
        Assert.True(status.IsTerminal());
    }

    [Theory]
    [InlineData(WorkflowStatus.Draft)]
    [InlineData(WorkflowStatus.Active)]
    [InlineData(WorkflowStatus.Deprecated)]
    [InlineData(WorkflowStatus.Suspended)]
    [InlineData(WorkflowStatus.WaitingForMessage)]
    public void IsTerminal_NonTerminalStatus_ReturnsFalse(WorkflowStatus status)
    {
        Assert.False(status.IsTerminal());
    }
}
