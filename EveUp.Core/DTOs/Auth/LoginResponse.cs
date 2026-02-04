using EveUp.Core.DTOs.User;

namespace EveUp.Core.DTOs.Auth;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; } // Segundos
    public UserResponse User { get; set; } = null!;
}
