using DotNetWorkflowEngine.Data.Repositories;
using DotNetWorkflowEngine.Exceptions;
using DotNetWorkflowEngine.Models;
using FluentAssertions;
using Xunit;

namespace DotNetWorkflowEngine.Tests;

/// <summary>
/// Tests verifying that <see cref="WorkflowInstanceRepository"/> enforces optimistic
/// concurrency on every mutation path instead of silently allowing last-write-wins.
/// </summary>
public class WorkflowInstanceRepositoryConcurrencyTests
{
    /// <summary>
    /// Loads the same instance twice (simulating two concurrent readers), mutates and saves
    /// both copies, and verifies the second save is rejected with a
    /// <see cref="WorkflowConcurrencyException"/> rather than silently overwriting the first.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenTwoLoadedCopiesBothSave_SecondSaveThrowsConcurrencyException()
    {
        // Arrange: seed a single instance.
        var repository = new WorkflowInstanceRepository();
        var instance = new WorkflowInstance("workflow-1");
        await repository.AddAsync(instance);

        // Simulate two independent readers loading the same row.
        var copyA = await repository.GetByIdAsync(instance.Id);
        var copyB = await repository.GetByIdAsync(instance.Id);
        copyA.Should().NotBeNull();
        copyB.Should().NotBeNull();

        copyA!.SetContextVariable("editedBy", "A");
        copyB!.SetContextVariable("editedBy", "B");

        // Act: the first save succeeds and advances the stored version.
        await repository.UpdateAsync(copyA);

        // Assert: the second save, still carrying the stale version it was loaded with,
        // must fail rather than clobbering copyA's write.
        var act = async () => await repository.UpdateAsync(copyB);
        await act.Should().ThrowAsync<WorkflowConcurrencyException>();

        var stored = await repository.GetByIdAsync(instance.Id);
        stored!.GetContextVariable("editedBy").Should().Be("A");
    }

    /// <summary>
    /// Verifies that a successful <see cref="WorkflowInstanceRepository.UpdateAsync"/> call
    /// increments the stored version, and a subsequent save with the refreshed version succeeds.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WithCurrentVersion_SucceedsAndIncrementsVersion()
    {
        // Arrange
        var repository = new WorkflowInstanceRepository();
        var instance = new WorkflowInstance("workflow-2");
        await repository.AddAsync(instance);

        var loaded = await repository.GetByIdAsync(instance.Id);
        loaded.Should().NotBeNull();
        var versionBeforeUpdate = loaded!.Version;

        // Act
        loaded.SetContextVariable("step", 1);
        await repository.UpdateAsync(loaded);

        // Assert
        loaded.Version.Should().Be(versionBeforeUpdate + 1);

        var reloaded = await repository.GetByIdAsync(instance.Id);
        reloaded!.Version.Should().Be(loaded.Version);

        reloaded.SetContextVariable("step", 2);
        var secondUpdate = async () => await repository.UpdateAsync(reloaded);
        await secondUpdate.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifies that <see cref="WorkflowInstanceRepository.UpdateAsync"/> throws a
    /// <see cref="WorkflowException"/> when no matching instance exists to update.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenInstanceDoesNotExist_ThrowsWorkflowException()
    {
        // Arrange
        var repository = new WorkflowInstanceRepository();
        var instance = new WorkflowInstance("workflow-3");

        // Act
        var act = async () => await repository.UpdateAsync(instance);

        // Assert
        await act.Should().ThrowAsync<WorkflowException>();
    }

    /// <summary>
    /// Verifies that <see cref="WorkflowInstanceRepository.DeleteWithConcurrencyCheckAsync"/>
    /// rejects a delete carrying a stale version instead of silently removing the current row.
    /// </summary>
    [Fact]
    public async Task DeleteWithConcurrencyCheckAsync_WithStaleVersion_ThrowsConcurrencyException()
    {
        // Arrange
        var repository = new WorkflowInstanceRepository();
        var instance = new WorkflowInstance("workflow-4");
        await repository.AddAsync(instance);

        var loaded = await repository.GetByIdAsync(instance.Id);
        loaded!.SetContextVariable("step", 1);
        await repository.UpdateAsync(loaded); // stored version is now ahead of instance.Version

        // Act
        var act = async () => await repository.DeleteWithConcurrencyCheckAsync(instance.Id, instance.Version);

        // Assert
        await act.Should().ThrowAsync<WorkflowConcurrencyException>();
        (await repository.ExistsAsync(instance.Id)).Should().BeTrue();
    }
}
