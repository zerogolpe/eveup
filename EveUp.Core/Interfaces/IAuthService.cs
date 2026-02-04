using EveUp.Core.DTOs.Auth;

namespace EveUp.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(string email, string password, string ipAddress);
    Task<LoginResponse?> RegisterAsync(string email, string password, string name, string ipAddress);
    Task<RefreshTokenResponse?> RefreshTokenAsync(string refreshToken, string ipAddress);
    Task LogoutAsync(string refreshToken, Guid userId, string ipAddress);
    Task LogoutAllAsync(Guid userId, string ipAddress);
    Task SendPasswordResetAsync(string email);
}
