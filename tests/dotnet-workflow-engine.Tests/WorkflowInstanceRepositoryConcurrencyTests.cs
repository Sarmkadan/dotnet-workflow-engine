using DotNetWorkflowEngine.Data.Repositories;
using DotNetWorkflowEngine.Exceptions;
using DotNetWorkflowEngine.Models;
using FluentAssertions;
using Xunit;

namespace DotNetWorkflowEngine.Tests;

/// <summary>
/// Tests verifying that <see cref="WorkflowInstance"/> optimistic concurrency via version field
/// is properly enforced by <see cref="WorkflowInstanceRepository"/>.
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
    /// Verifies that creating a brand-new <see cref="WorkflowInstance"/> initializes the version
    /// field to 0, ensuring callers get the documented starting value.
    /// </summary>
    [Fact]
    public void WorkflowInstance_WhenNewInstanceCreated_VersionInitializedToZero()
    {
        // Arrange & Act
        var instance = new WorkflowInstance("workflow-new");

        // Assert
        instance.Version.Should().Be(0, "A new WorkflowInstance should initialize Version to 0 for optimistic concurrency");
    }

    /// <summary>
    /// Verifies that creating a brand-new <see cref="WorkflowInstance"/> via constructor with
    /// explicit parameters initializes the version field to 0.
    /// </summary>
    [Fact]
    public void WorkflowInstance_WhenCreatedWithParameters_VersionInitializedToZero()
    {
        // Arrange & Act
        var instance = new WorkflowInstance("workflow-params", "correlation-123", 2);

        // Assert
        instance.Version.Should().Be(0, "A new WorkflowInstance should initialize Version to 0 regardless of constructor parameters");
        instance.WorkflowId.Should().Be("workflow-params");
        instance.CorrelationId.Should().Be("correlation-123");
        instance.DefinitionVersion.Should().Be(2);
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
    /// Verifies that <see cref="WorkflowInstanceRepository.AddAsync"/> initializes the version
    /// field to 0 when adding a new instance.
    /// </summary>
    [Fact]
    public async Task AddAsync_WhenNewInstanceAdded_VersionInitializedToZero()
    {
        // Arrange
        var repository = new WorkflowInstanceRepository();
        var instance = new WorkflowInstance("workflow-add");

        // Act
        await repository.AddAsync(instance);

        // Assert
        instance.Version.Should().Be(0, "AddAsync should initialize Version to 0");

        // Verify it's persisted correctly
        var loaded = await repository.GetByIdAsync(instance.Id);
        loaded!.Version.Should().Be(0);
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
    /// Verifies that each successful <see cref="WorkflowInstanceRepository.UpdateAsync"/> call
    /// increments the version field by exactly 1, ensuring predictable version progression.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenCalledMultipleTimes_VersionIncrementsByOneEachTime()
    {
        // Arrange
        var repository = new WorkflowInstanceRepository();
        var instance = new WorkflowInstance("workflow-multi");
        await repository.AddAsync(instance);

        var loaded = await repository.GetByIdAsync(instance.Id);
        loaded.Should().NotBeNull();

        // Act & Assert - Verify version increments by exactly 1 each time
        loaded!.SetContextVariable("step", 1);
        await repository.UpdateAsync(loaded);
        loaded.Version.Should().Be(1);

        loaded.SetContextVariable("step", 2);
        await repository.UpdateAsync(loaded);
        loaded.Version.Should().Be(2);

        loaded.SetContextVariable("step", 3);
        await repository.UpdateAsync(loaded);
        loaded.Version.Should().Be(3);

        // Verify persisted version matches
        var reloaded = await repository.GetByIdAsync(instance.Id);
        reloaded!.Version.Should().Be(3);
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
