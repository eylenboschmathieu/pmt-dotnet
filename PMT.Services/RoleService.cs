using PMT.Data.Entities;
using PMT.Data.Repositories;

namespace PMT.Services;

public class RoleService(IRoleRepository _repo) {
    public async Task<Role?> Add(Role role) => await _repo.AddAsync(role);
    public async Task<Role?> FindById(int id) => await _repo.FindByIdAsync(id);
    public async Task<IEnumerable<Role>> FindAll() => await _repo.FindAllAsync();
    public async Task<Role?> Update(Role role) => await _repo.UpdateAsync(role);
    public async Task<bool> Delete(int id) => await _repo.DeleteAsync(id);
    public async Task<IEnumerable<Role>> FindByUser(User user) => await _repo.FindByUser(user);
}
