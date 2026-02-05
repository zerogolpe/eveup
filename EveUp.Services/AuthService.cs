using EveUp.Core.DTOs.Auth;
using EveUp.Core.DTOs.User;
using EveUp.Core.Entities;
using EveUp.Core.Exceptions;
using EveUp.Core.Interfaces;

namespace EveUp.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordService _passwordService;
    private readonly TokenService _tokenService;
    private readonly IAuditService _audit;
    private readonly INotificationService _notification;

    public AuthService(
        IUserRepository userRepo,
        IPasswordService passwordService,
        TokenService tokenService,
        IAuditService audit,
        INotificationService notification)
    {
        _userRepo = userRepo;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _audit = audit;
        _notification = notification;
    }

    public async Task<LoginResponse?> LoginAsync(string email, string password, string ipAddress)
    {
        var user = await _userRepo.GetByEmailAsync(email.ToLowerInvariant());

        if (user == null)
            return null;

        if (!_passwordService.VerifyPassword(password, user.PasswordHash))
            return null;

        // Verificar se conta está bloqueada
        if (user.State == Core.Enums.UserState.BANNED)
        {
            await _audit.LogAsync("Auth", user.Id, null, null, "LOGIN_BLOCKED",
                "Banned user attempted login", user.Id, ipAddress);
            return null;
        }

        if (user.State == Core.Enums.UserState.DELETED)
            return null;

        var accessToken = _tokenService.GenerateAccessToken(user);
        var (refreshToken, _) = await _tokenService.GenerateRefreshTokenAsync(user);

        await _audit.LogAsync("Auth", user.Id, null, null, "LOGIN_SUCCESS",
            "User logged in", user.Id, ipAddress);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _tokenService.GetAccessTokenExpirationSeconds(),
            User = UserResponse.FromEntity(user)
        };
    }

    public async Task<LoginResponse?> RegisterAsync(string email, string password, string name, string ipAddress)
    {
        var normalizedEmail = email.ToLowerInvariant();

        // Verificar se email já existe
        if (await _userRepo.ExistsByEmailAsync(normalizedEmail))
            return null;

        // Validar força da senha
        if (!_passwordService.IsPasswordStrong(password))
            throw new BusinessRuleException("WEAK_PASSWORD", "A senha deve ter no mínimo 8 caracteres, incluindo maiúscula, minúscula e número");

        var passwordHash = _passwordService.HashPassword(password);
        var user = User.Create(normalizedEmail, passwordHash, name);

        await _userRepo.AddAsync(user);

        // Verificação de email desabilitada temporariamente - marcar como verificado
        user.VerifyEmail();
        await _userRepo.UpdateAsync(user);
        // TODO: Reativar quando Resend estiver configurado
        // try { await GenerateAndSendVerificationCodeAsync(user); }
        // catch { /* Email failure must not block registration */ }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var (refreshToken, _) = await _tokenService.GenerateRefreshTokenAsync(user);

        await _audit.LogAsync("User", user.Id, null, "CREATED", "REGISTER",
            "New user registered", user.Id, ipAddress);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _tokenService.GetAccessTokenExpirationSeconds(),
            User = UserResponse.FromEntity(user)
        };
    }

    public async Task<RefreshTokenResponse?> RefreshTokenAsync(string refreshToken, string ipAddress)
    {
        var storedToken = await _tokenService.ValidateRefreshTokenAsync(refreshToken);

        if (storedToken == null)
            return null;

        var user = await _userRepo.GetByIdAsync(storedToken.UserId);
        if (user == null)
            return null;

        // Verificar se conta ainda está ativa
        if (user.State == Core.Enums.UserState.BANNED || user.State == Core.Enums.UserState.DELETED)
        {
            await _tokenService.RevokeAllUserTokensAsync(user.Id, "Account banned/deleted");
            return null;
        }

        // Revogar token antigo e gerar novo na mesma família
        await _tokenService.RevokeRefreshTokenAsync(refreshToken, "Replaced by new token");

        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var (newRefreshToken, _) = await _tokenService.GenerateRefreshTokenAsync(user, storedToken.TokenFamily);

        await _audit.LogAsync("Auth", user.Id, null, null, "TOKEN_REFRESHED",
            "Token refreshed", user.Id, ipAddress);

        return new RefreshTokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = _tokenService.GetAccessTokenExpirationSeconds()
        };
    }

    public async Task LogoutAsync(string refreshToken, Guid userId, string ipAddress)
    {
        await _tokenService.RevokeRefreshTokenAsync(refreshToken, "User logout");

        await _audit.LogAsync("Auth", userId, null, null, "LOGOUT",
            "User logged out", userId, ipAddress);
    }

    public async Task LogoutAllAsync(Guid userId, string ipAddress)
    {
        await _tokenService.RevokeAllUserTokensAsync(userId, "User logout all devices");

        await _audit.LogAsync("Auth", userId, null, null, "LOGOUT_ALL",
            "User logged out from all devices", userId, ipAddress);
    }

    public async Task SendPasswordResetAsync(string email)
    {
        var user = await _userRepo.GetByEmailAsync(email.ToLowerInvariant());

        // Sempre retorna sem erro para não expor se email existe
        if (user == null)
            return;

        // TODO: Gerar token de reset e enviar por email
        await _notification.SendEmailAsync(
            user.Email,
            "Recuperação de senha - EveUp",
            "Use o link para redefinir sua senha: [LINK]");

        await _audit.LogAsync("Auth", user.Id, null, null, "PASSWORD_RESET_REQUESTED",
            "Password reset email sent", user.Id, null);
    }

    public async Task SendVerificationCodeAsync(string email)
    {
        var user = await _userRepo.GetByEmailAsync(email.ToLowerInvariant());
        if (user == null)
            return;

        if (user.EmailVerified)
            return;

        await GenerateAndSendVerificationCodeAsync(user);
    }

    public async Task<bool> VerifyEmailAsync(string email, string code)
    {
        var user = await _userRepo.GetByEmailAsync(email.ToLowerInvariant());
        if (user == null)
            return false;

        if (user.EmailVerified)
            return true;

        if (user.EmailVerificationCode != code)
            return false;

        if (user.EmailVerificationCodeExpiresAt == null || user.EmailVerificationCodeExpiresAt < DateTime.UtcNow)
            return false;

        user.VerifyEmail();
        await _userRepo.UpdateAsync(user);

        await _audit.LogAsync("User", user.Id, null, null, "EMAIL_VERIFIED",
            "Email verified successfully", user.Id, null);

        return true;
    }

    private async Task GenerateAndSendVerificationCodeAsync(User user)
    {
        var code = Random.Shared.Next(100000, 999999).ToString();
        user.SetEmailVerificationCode(code, DateTime.UtcNow.AddMinutes(30));
        await _userRepo.UpdateAsync(user);

        var htmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 480px; margin: 0 auto;'>
                <h2 style='color: #6C63FF;'>EveUp - Verificação de Email</h2>
                <p>Olá, <strong>{user.Name}</strong>!</p>
                <p>Seu código de verificação é:</p>
                <div style='background: #f4f4f4; padding: 20px; text-align: center; border-radius: 8px; margin: 20px 0;'>
                    <span style='font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #333;'>{code}</span>
                </div>
                <p>Este código expira em <strong>30 minutos</strong>.</p>
                <p style='color: #888; font-size: 12px;'>Se você não criou uma conta no EveUp, ignore este email.</p>
            </div>";

        await _notification.SendEmailAsync(
            user.Email,
            "EveUp - Código de Verificação",
            htmlBody);
    }
}
