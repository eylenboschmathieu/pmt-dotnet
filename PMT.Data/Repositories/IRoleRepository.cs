using PMT.Data.Entities;

namespace PMT.Data.Repositories;

public interface IRoleRepository : IRepository<Role> {
    public Task<IEnumerable<Role>> FindByUser(User user);
    public Task<Role?> FindByName(string name);
    public Task<IEnumerable<Role>> FindByIds(IEnumerable<int> roleIds);
}
