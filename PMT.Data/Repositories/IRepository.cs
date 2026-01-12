namespace PMT.Data.Repositories;

public interface IRepository<TEntity> where TEntity : class {
    /// <summary>Adds a new entity.</summary>
    /// <returns>The added entity.</returns>
    public Task<TEntity> AddAsync(TEntity entity);

    /// <summary>Finds a single entity by id.</summary>
    /// <returns>The entity if found. Null otherwise.</returns>
    public Task<TEntity?> FindByIdAsync(int id);

    /// <summary>Retrieves all entities.</summary>
    /// <returns>All entities as IEnumerable.</returns>
    public Task<IEnumerable<TEntity>> FindAllAsync();

    /// <summary>Updates an existing entity.</summary>
    /// <returns>The updated entity. Null if the entity doesn't exist.</returns>
    public Task<TEntity?> UpdateAsync(TEntity entity);

    /// <summary>Deletes an entity by id.</summary>
    /// <returns>Returns true if deleted, false otherwise.</returns>
    public Task<bool> DeleteAsync(int id);
}