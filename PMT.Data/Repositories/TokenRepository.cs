
using Microsoft.EntityFrameworkCore;

using PMT.Data.Entities;

namespace PMT.Data.Repositories;

public class TokenRepository(ApplicationDbContext _dbContext) : ITokenRepository {
    public async Task<RefreshToken> AddAsync(RefreshToken entity) {
        _dbContext.RefreshTokens.Add(entity);
        await _dbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> DeleteAsync(int id) {
        RefreshToken? token = await _dbContext.RefreshTokens.FindAsync(id);
        if (token is null)
            return false;

        _dbContext.RefreshTokens.Remove(token);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public Task<IEnumerable<RefreshToken>> FindAllAsync() {
        throw new NotImplementedException();
    }

    public Task<RefreshToken?> FindByIdAsync(int id) {
        throw new NotImplementedException();
    }

    public async Task<RefreshToken?> UpdateAsync(RefreshToken entity) {
        RefreshToken? token = await _dbContext.RefreshTokens.FindAsync(entity.Id);
        if (token is null) return null;

        token.Token = entity.Token;
        token.IpAddress = entity.IpAddress;
        token.Created = entity.Created;
        token.Expires = entity.Expires;
        token.ReplacedByToken = entity.ReplacedByToken;
        token.User = entity.User;
        token.Revoked = entity.Revoked;
        _dbContext.RefreshTokens.Update(token);

        await _dbContext.SaveChangesAsync();

        return token;
    }

    public async Task<RefreshToken?> FindByTokenAsync(string refresh_token) {
        return await _dbContext.RefreshTokens.Include(e => e.User).Where(e => e.Token.Equals(refresh_token)).FirstOrDefaultAsync();
    }
}
