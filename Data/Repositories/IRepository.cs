// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetWorkflowEngine.Data.Repositories;

/// <summary>
/// Generic repository interface for data access operations.
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Gets an entity by ID.
    /// </summary>
    Task<T?> GetByIdAsync(string id);

    /// <summary>
    /// Gets all entities.
    /// </summary>
    Task<List<T>> GetAllAsync();

    /// <summary>
    /// Adds a new entity.
    /// </summary>
    Task AddAsync(T entity);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    Task UpdateAsync(T entity);

    /// <summary>
    /// Deletes an entity.
    /// </summary>
    Task DeleteAsync(string id);

    /// <summary>
    /// Checks if an entity exists.
    /// </summary>
    Task<bool> ExistsAsync(string id);

    /// <summary>
    /// Gets the count of entities.
    /// </summary>
    Task<int> CountAsync();

    /// <summary>
    /// Gets entities with pagination.
    /// </summary>
    Task<(List<T> Items, int Total)> GetPagedAsync(int pageNumber, int pageSize);
}
