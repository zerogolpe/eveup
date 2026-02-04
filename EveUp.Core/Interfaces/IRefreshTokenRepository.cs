using EveUp.Core.Entities;

namespace EveUp.Core.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    Task UpdateAsync(RefreshToken token);
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
    Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId);
    Task<List<RefreshToken>> GetByFamilyAsync(string family);
    Task DeleteExpiredAsync(DateTime before);
}
