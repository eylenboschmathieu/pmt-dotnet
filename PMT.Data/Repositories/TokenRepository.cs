
using Microsoft.EntityFrameworkCore;

using PMT.Data.Entities;

namespace PMT.Data.Repositories;

public class TokenRepository(ApplicationDbContext _dbContext) : ITokenRepository {
    public async Task<RefreshToken> CreateAsync(RefreshToken entity) {
        _dbContext.RefreshTokens.Add(entity);
        await _dbContext.SaveChangesAsync();
        
        return entity;
    }

    public Task<RefreshToken?> GetAsync(int id) => throw new NotImplementedException();

    public Task<IEnumerable<RefreshToken>> GetAllAsync() => throw new NotImplementedException();

    public async Task<bool> UpdateAsync(RefreshToken token) {
        bool exists = await _dbContext.RefreshTokens.AnyAsync(e => e.Id == token.Id);
        if (!exists)
            return false;

        _dbContext.RefreshTokens.Update(token);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id) {
        RefreshToken? token = await _dbContext.RefreshTokens.FindAsync(id);
        if (token is null)
            return false;

        _dbContext.RefreshTokens.Remove(token);
        await _dbContext.SaveChangesAsync();
        
        return true;
    }

    public async Task<RefreshToken?> FindByCookieAsync(string refresh_cookie) => await _dbContext.RefreshTokens
        .Include(e => e.User)
        .FirstOrDefaultAsync(e => e.Token.Equals(refresh_cookie));

    public async Task RevokeUser(int userId) {
        DateTime now = DateTime.UtcNow;
        await _dbContext.RefreshTokens
            .Where(e => e.UserId == userId)
            .ForEachAsync(e => e.Revoked = now);

        await _dbContext.SaveChangesAsync();
    }
}
