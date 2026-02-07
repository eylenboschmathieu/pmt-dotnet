using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

using PMT.Data.Entities;

namespace PMT.Data.Repositories;

public class RoleRepository(ApplicationDbContext _dbContext) : IRoleRepository {
    public async Task<Role> CreateAsync(Role role) {
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync();
        return role;
    }

    public async Task<Role?> GetAsync(int id) => await _dbContext.Roles.FindAsync(id);

    public async Task<IEnumerable<Role>> GetAllAsync() => await _dbContext.Roles.ToListAsync();

    public async Task<bool> UpdateAsync(Role role) {
        bool exists = await _dbContext.Roles.AnyAsync(e => e.Id == role.Id);
        if (!exists)
            return false;

        _dbContext.Update(role);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id) {
        Role? role = await _dbContext.Roles.FindAsync(id);
        if (role is null)
            return false;

        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<Role>> FindByUser(User user) =>
        await _dbContext.Roles.Where(r => r.Users.Any(u => u.Id == user.Id)).ToListAsync();

    public async Task<IEnumerable<Role>> FindByUser(int id) =>
        await _dbContext.Roles.Where(r => r.Users.Any(u => u.Id == id)).ToListAsync();

    public async Task<Role?> FindByName(string name) =>
        await _dbContext.Roles.Where(e => e.Name == name).FirstOrDefaultAsync();

    public async Task<IEnumerable<Role>> FindByIds(IEnumerable<int> roleIds) =>
        await _dbContext.Roles.Where(e => roleIds.Contains(e.Id)).ToListAsync();

    public async Task<IEnumerable<Role>> FindByParentId(int id) =>
        await _dbContext.Roles.Where(e => e.ParentId.HasValue && e.ParentId.Value == id).ToListAsync();

    public async Task<IEnumerable<Role>> FindByParentIds(IEnumerable<int> ids) =>
        await _dbContext.Roles.Where(e => e.ParentId.HasValue && ids.Contains(e.ParentId.Value)).ToListAsync();
}
