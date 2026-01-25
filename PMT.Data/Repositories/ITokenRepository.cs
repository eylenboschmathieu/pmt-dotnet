using PMT.Data.Entities;

namespace PMT.Data.Repositories;

public interface ITokenRepository : IRepository<RefreshToken> {
    /// <summary>
    /// Returns a refresh token, or null.
    /// </summary>
    /// <param name="refresh_cookie">The refresh cookie</param>
    /// <returns>A refresh token, or null if it doesn't exist.</returns>
    public Task<RefreshToken?> FindByCookieAsync(string refresh_cookie);

    /// <summary>
    /// Revoke all tokens of a user, on all devices.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>Nothing</returns>
    public Task RevokeUser(int userId);
}