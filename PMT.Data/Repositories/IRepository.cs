namespace PMT.Data.Repositories;

public interface IRepository<TEntity> where TEntity : class {
    /// <summary>Adds a new entity.</summary>
    /// <returns>The added entity.</returns>
    public Task<TEntity> CreateAsync(TEntity entity);

    /// <summary>Finds a single entity by id.</summary>
    /// <returns>The entity. Null if the entity can't be found.</returns>
    public Task<TEntity?> GetAsync(int id);

    /// <summary>Retrieves all entities.</summary>
    /// <returns>All entities as IEnumerable.</returns>
    public Task<IEnumerable<TEntity>> GetAllAsync();

    /// <summary>Updates an existing entity.</summary>
    /// <returns>The updated entity. Null if the entity doesn't exist.</returns>
    public Task<bool> UpdateAsync(TEntity entity);

    /// <summary>Deletes an entity by id.</summary>
    /// <returns>True if deleted, false otherwise.</returns>
    public Task<bool> DeleteAsync(int id);
}