using PMT.Data.Entities;

namespace PMT.Data.Repositories;

public interface ITokenRepository : IRepository<RefreshToken> {
    public Task<RefreshToken?> FindByTokenAsync(string refresh_token);
}