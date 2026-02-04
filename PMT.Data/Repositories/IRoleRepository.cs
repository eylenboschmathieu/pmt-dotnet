using PMT.Data.Entities;

namespace PMT.Data.Repositories;

public interface IRoleRepository : IRepository<Role> {
    /// <summary>
    /// Returns a list of roles that this user belongs to.
    /// </summary>
    /// <param name="user"></param>
    /// <returns>An IEnumerable containing Role objects.</returns>
    public Task<IEnumerable<Role>> FindByUser(User user);
    public Task<IEnumerable<Role>> FindByUser(int id);

    /// <summary>
    /// Find a role by name.
    /// </summary>
    /// <param name="name">Name of the role</param>
    /// <returns>Returns a role object, or null if no role with this name exists.</returns>
    public Task<Role?> FindByName(string name);

    /// <summary>
    /// Returns list of roles.
    /// </summary>
    /// <param name="roleIds">List of role id's</param>
    /// <returns>An IEnumerable containing role objects.</returns>
    public Task<IEnumerable<Role>> FindByIds(IEnumerable<int> roleIds);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Task<IEnumerable<Role>> FindByParentId(int id);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    public Task<IEnumerable<Role>> FindByParentIds(IEnumerable<int> ids);
}
