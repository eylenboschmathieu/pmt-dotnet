using PMT.Data.Entities;

namespace PMT.Data.Repositories;

public record UserDTO(string? Name, string? Email, IEnumerable<string> Roles);

public interface IUserRepository : IRepository<User> {
    public Task<User?> FindByGoogleId(string googleId);
    public Task<User?> FindByEmail(string email);
    public Task<IEnumerable<User>> FindAllActive();
    public Task<bool> SetActive(int userId, bool active);
    public Task<IEnumerable<User>> FindSelect();  // Find all users to add to drop down lists
    public Task<User?> FindUserData(int userId);  // User data for editing a user, ie. id, name, email, roles, isActive, createdBy
    public Task<User?> FindWithRolesById(int userId);
}
