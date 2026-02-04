using EveUp.Core.Entities;

namespace EveUp.Core.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    Task<(string token, string family)> GenerateRefreshTokenAsync(User user, string? existingFamily = null);
    Task<RefreshToken?> ValidateRefreshTokenAsync(string rawToken);
    Task RevokeRefreshTokenAsync(string rawToken, string reason);
    Task RevokeAllUserTokensAsync(Guid userId, string reason);
}
