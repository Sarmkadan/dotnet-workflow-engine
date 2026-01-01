// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetWorkflowEngine.Data.Repositories;
using DotNetWorkflowEngine.Models;

namespace DotNetWorkflowEngine.Data.Context;

/// <summary>
/// Database context managing all repositories and connections.
/// </summary>
public class DatabaseContext : IAsyncDisposable
{
    private bool _isDisposed = false;
    private string? _connectionString;

    /// <summary>Gets the workflow repository.</summary>
    public WorkflowRepository Workflows { get; private set; }

    /// <summary>Gets the workflow instance repository.</summary>
    public WorkflowInstanceRepository Instances { get; private set; }

    /// <summary>Gets the audit log repository.</summary>
    public AuditRepository AuditLogs { get; private set; }

    /// <summary>
    /// Initializes the database context.
    /// </summary>
    public DatabaseContext(string? connectionString = null)
    {
        _connectionString = connectionString;
        Workflows = new WorkflowRepository();
        Instances = new WorkflowInstanceRepository();
        AuditLogs = new AuditRepository();
    }

    /// <summary>
    /// Initializes the database schema (if using real database).
    /// </summary>
    public async Task InitializeAsync()
    {
        // In-memory implementation - nothing to initialize
        // Real implementation would create tables, indexes, etc.
        await Task.CompletedTask;
    }

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    public async Task SaveChangesAsync()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(DatabaseContext));

        // Validate all data before saving
        await ValidateDataAsync();

        // In-memory implementation - nothing to save
        // Real implementation would commit transactions
        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates all data integrity.
    /// </summary>
    private async Task ValidateDataAsync()
    {
        // Validate workflow integrity
        var workflows = await Workflows.GetAllAsync();
        foreach (var workflow in workflows)
        {
            if (!workflow.Validate(out var errors))
            {
                throw new Exceptions.ValidationException($"Invalid workflow {workflow.Id}", errors);
            }
        }

        // Validate activity configurations
        foreach (var workflow in workflows)
        {
            foreach (var activity in workflow.Activities)
            {
                if (!activity.Validate(out var errors))
                {
                    throw new Exceptions.ValidationException($"Invalid activity {activity.Id}", errors);
                }
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Begins a new transaction.
    /// </summary>
    public async Task<IDbTransaction> BeginTransactionAsync()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(DatabaseContext));

        return new InMemoryTransaction(this);
    }

    /// <summary>
    /// Gets database statistics.
    /// </summary>
    public async Task<Dictionary<string, int>> GetStatisticsAsync()
    {
        var stats = new Dictionary<string, int>
        {
            ["Workflows"] = await Workflows.CountAsync(),
            ["Instances"] = await Instances.CountAsync(),
            ["AuditEntries"] = await AuditLogs.CountAsync()
        };

        return stats;
    }

    /// <summary>
    /// Clears all data (useful for testing).
    /// </summary>
    public async Task ClearAllAsync()
    {
        await Workflows.ClearAsync();
        await Instances.ClearAsync();
        await AuditLogs.ClearAsync();
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_isDisposed)
        {
            await SaveChangesAsync();
            _isDisposed = true;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Interface for database transactions.
    /// </summary>
    public interface IDbTransaction : IAsyncDisposable
    {
        Task CommitAsync();
        Task RollbackAsync();
    }

    /// <summary>
    /// In-memory transaction implementation.
    /// </summary>
    private class InMemoryTransaction : IDbTransaction
    {
        private readonly DatabaseContext _context;
        private bool _isCommitted = false;

        public InMemoryTransaction(DatabaseContext context)
        {
            _context = context;
        }

        public async Task CommitAsync()
        {
            if (!_isCommitted)
            {
                await _context.SaveChangesAsync();
                _isCommitted = true;
            }
        }

        public Task RollbackAsync()
        {
            // In-memory implementation - no rollback needed
            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_isCommitted)
            {
                await RollbackAsync();
            }

            GC.SuppressFinalize(this);
        }
    }
}
