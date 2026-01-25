
using Microsoft.EntityFrameworkCore;

using PMT.Data.Entities;

namespace PMT.Data.Repositories;

public class UserRepository : IUserRepository {
    private readonly ApplicationDbContext _dbContext = null!;

    public UserRepository(ApplicationDbContext dbContext) {
        _dbContext = dbContext;
    }

    public async Task<User> CreateAsync(User entity) {
        _dbContext.Users.Add(entity);
        await _dbContext.SaveChangesAsync();

        return entity;
    }

    public async Task<User?> GetAsync(int id) => await _dbContext.Users.FindAsync(id);

    public async Task<IEnumerable<User>> GetAllAsync() => await _dbContext.Users.ToListAsync();

    public async Task<bool> UpdateAsync(User user) {
        bool exists = await _dbContext.Users.AnyAsync(e => e.Id == user.Id);
        if (!exists)
            return false;

        _dbContext.Update(user);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id) {
        User? user = await _dbContext.Users.FindAsync(id);

        if (user is null)
            return false;

        _dbContext.Users.Remove(user!);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<User?> FindByIdWithRoles(int id) => await _dbContext.Users
        .Include(e => e.Roles)
        .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<User?> FindByGoogleId(string googleId) => await _dbContext.Users.FirstOrDefaultAsync(e => e.GoogleId == googleId);

    public async Task<User?> FindByEmail(string email) => await _dbContext.Users
        .Include(e => e.Roles)
        .FirstOrDefaultAsync(u => u.Email.Equals(email));

    public async Task<User?> FindUserData(int userId) => await _dbContext.Users
        .Include(e => e.Roles)
        .Include(e => e.CreatedBy)
        .AsNoTracking()
        .FirstOrDefaultAsync(e => e.Id == userId);

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

    public async Task<IEnumerable<User>> FindSelect() => await _dbContext.Users
        .Include(e => e.Roles)
        .Where(e => !string.IsNullOrEmpty(e.Name) && e.Active)
        .AsNoTracking()
        .ToListAsync();
}
