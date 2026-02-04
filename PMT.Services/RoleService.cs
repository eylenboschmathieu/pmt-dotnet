using Humanizer;

using PMT.Data.Entities;
using PMT.Data.Repositories;

namespace PMT.Services;

public class RoleService(IRoleRepository _repo) {
    public async Task<Role?> Add(Role role) => await _repo.CreateAsync(role);
    public async Task<Role?> FindById(int id) => await _repo.GetAsync(id);
    public async Task<IEnumerable<Role>> FindAll() => await _repo.GetAllAsync();
    public async Task<bool> Update(Role role) => await _repo.UpdateAsync(role);
    public async Task<bool> Delete(int id) => await _repo.DeleteAsync(id);
    public async Task<IEnumerable<Role>> FindByUser(User user) => await _repo.FindByUser(user);
    public async Task<IEnumerable<Role>> FindByUser(int id) => await _repo.FindByUser(id);
    public async Task<IEnumerable<Role>> FindChildRoles(IEnumerable<int> ids) {
        if (ids.Contains(1))  // Quick hack to return all roles in case of admin
            return await _repo.GetAllAsync();

        List<Role> ret = [];

        foreach (Role role in await _repo.FindByIds(ids)) {
            IEnumerable<Role> children = [ role ];
            for (int depth = 0; depth < role.DelegationDepth; depth++) {
                children = await _repo.FindByParentIds(children.Select(e => e.Id));

                if (!children.Any())
                    break;

                ret.AddRange(children);
            }
        }

        return ret.Distinct();
    }
}
