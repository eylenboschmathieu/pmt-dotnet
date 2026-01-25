using PMT.Data.Entities;

namespace PMT.Data.Repositories;

public record UserDTO(string? Name, string? Email, IEnumerable<string> Roles);

public interface IUserRepository : IRepository<User> {
    /// <summary>
    /// Find user by google id.
    /// </summary>
    /// <param name="googleId"></param>
    /// <returns>A user, or null if the google id doesn't exist.</returns>
    public Task<User?> FindByGoogleId(string googleId);

    /// <summary>
    /// Find user by email.
    /// </summary>
    /// <param name="email"></param>
    /// <returns>A user, or null if the email doesn't exist.</returns>
    public Task<User?> FindByEmail(string email);

    /// <summary>
    /// Set a user to active/inactive.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="active"></param>
    /// <returns>True if successfull, false if not.</returns>
    public Task<bool> SetActive(int userId, bool active);

    /// <summary>
    /// Returns list of users.
    /// </summary>
    /// <returns>An IEnumerable of user objects.</returns>
    public Task<IEnumerable<User>> FindSelect();

    /// <summary>
    /// Returns all information about a user.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>A user, or null if the user isn't found.</returns>
    public Task<User?> FindUserData(int userId);  // User data for editing a user, ie. id, name, email, roles, isActive, createdBy

    /// <summary>
    /// Returns basic information about a user, including roles.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>A user, or null if the user isn't found.</returns>
    public Task<User?> FindByIdWithRoles(int userId);
}
