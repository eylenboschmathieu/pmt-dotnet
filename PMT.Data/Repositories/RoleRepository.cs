using Microsoft.EntityFrameworkCore;

using PMT.Data.Entities;

namespace PMT.Data.Repositories;

public class RoleRepository(ApplicationDbContext _dbContext) : IRoleRepository {
    public async Task<Role> AddAsync(Role role) {
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync();
        return role;
    }

    public async Task<bool> DeleteAsync(int id) {
        var role = await _dbContext.Roles.FindAsync(id);
        if (role is null)
            return false;

        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Role>> FindAllAsync() {
        return await _dbContext.Roles.ToListAsync();
    }

    public async Task<Role?> FindByIdAsync(int id) {
        return await _dbContext.Roles.FindAsync(id);
    }

    public async Task<Role?> UpdateAsync(Role role) {
        var dbRole = await _dbContext.Roles.FindAsync(role.Id);
        if (dbRole is null)
            return null;

        // Update fields
        dbRole.Name = role.Name;

        await _dbContext.SaveChangesAsync();
        return dbRole;
    }

    public async Task<IEnumerable<Role>> FindByUser(User user) {
        return await _dbContext.Roles.Where(r => r.Users.Any(u => u.Id == user.Id)).ToListAsync();
    }

    public async Task<Role?> FindByName(string name) {
        return await _dbContext.Roles.Where(e => e.Name == name).FirstAsync();
    }

    public async Task<IEnumerable<Role>> FindByIds(IEnumerable<int> roleIds) {
        return await _dbContext.Roles.Where(e => roleIds.Contains(e.Id)).ToListAsync();
    }
}
