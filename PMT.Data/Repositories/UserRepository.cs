
using Microsoft.EntityFrameworkCore;

using PMT.Data.Entities;

namespace PMT.Data.Repositories;

public class UserRepository : IUserRepository {
    private readonly ApplicationDbContext _dbContext = null!;

    public UserRepository(ApplicationDbContext dbContext) {
        _dbContext = dbContext;
    }

    public async Task<User> AddAsync(User entity) {
        _dbContext.Users.Add(entity);
        await _dbContext.SaveChangesAsync();

        return entity;
    }

    public async Task<bool> DeleteAsync(int id) {
        User? user = await _dbContext.Users.FindAsync(id);

        if (user is null)
            return false;

        _dbContext.Users.Remove(user!);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<User>> FindAllAsync() {
        return await _dbContext.Users.Include(u => u.CreatedBy).ToListAsync();
    }

    public async Task<User?> FindByIdAsync(int id) {
        return await _dbContext.Users.FindAsync(id);
    }

    public async Task<User?> FindWithRolesById(int id) {
        return await _dbContext.Users.Include(e => e.Roles).FirstAsync(e => e.Id == id);
    }

    public async Task<User?> FindByGoogleId(string googleId) {
        return await _dbContext.Users.Include(e => e.Roles).FirstOrDefaultAsync(u => u.GoogleId == googleId);
    }

    public async Task<User?> FindByEmail(string email) {
        return await _dbContext.Users.Include(e => e.Roles).FirstOrDefaultAsync(u => u.Email.Equals(email));
    }

    public async Task<User?> UpdateAsync(User entity) {
        var user = await _dbContext.Users.FindAsync(entity.Id);
        if (user is null)
            return null;

        user.Name = entity.Name;
        user.GoogleId = entity.GoogleId;
        user.Active = entity.Active;
        user.Roles = entity.Roles;
        await _dbContext.SaveChangesAsync();

        return user;
    }

    public async Task<User?> FindUserData(int userId) {
        return await _dbContext.Users.Include(e => e.Roles).Include(e => e.CreatedBy).FirstOrDefaultAsync(e => e.Id == userId);
    }

    public async Task<IEnumerable<User>> FindAllActive() {
        return await _dbContext.Users.Include(e => e.Roles).Where(e => e.Active).ToListAsync();
    }

    public async Task<bool> SetActive(int userId, bool active) {
        User? user = await _dbContext.Users.FindAsync(userId);

        if (user is not null) {
            user.Active = active;
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<IEnumerable<User>> FindSelect() {
        return await _dbContext.Users.Include(e => e.Roles).Where(e => !string.IsNullOrEmpty(e.Name) && e.Active).ToListAsync();
    }
}
