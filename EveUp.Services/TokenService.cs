using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EveUp.Core.Entities;
using EveUp.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EveUp.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IAuditService _audit;

    public TokenService(
        IConfiguration config,
        IRefreshTokenRepository refreshTokenRepo,
        IAuditService audit)
    {
        _config = config;
        _refreshTokenRepo = refreshTokenRepo;
        _audit = audit;
    }

    public string GenerateAccessToken(User user)
    {
        var jwtSettings = _config.GetSection("Jwt");
        var secretKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Type?.ToString() ?? "NONE"),
            new Claim("state", user.State.ToString())
        };

        var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);
        var expiration = DateTime.UtcNow.AddMinutes(
            int.Parse(jwtSettings["AccessTokenExpirationMinutes"]!));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<(string token, string family)> GenerateRefreshTokenAsync(User user, string? existingFamily = null)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var tokenHash = HashToken(rawToken);
        var family = existingFamily ?? Guid.NewGuid().ToString();

        var expirationDays = int.Parse(_config["Jwt:RefreshTokenExpirationDays"]!);
        var refreshToken = RefreshToken.Create(user.Id, tokenHash, family, expirationDays);

        await _refreshTokenRepo.AddAsync(refreshToken);

        await _audit.LogAsync("RefreshToken", refreshToken.Id, null, "CREATED",
            "TOKEN_GENERATED", "Refresh token generated", user.Id, null);

        return (rawToken, family);
    }

    public async Task<RefreshToken?> ValidateRefreshTokenAsync(string rawToken)
    {
        var tokenHash = HashToken(rawToken);
        var refreshToken = await _refreshTokenRepo.GetByTokenHashAsync(tokenHash);

        if (refreshToken == null)
            return null;

        if (!refreshToken.IsActive)
        {
            // Token inativo usado = possível roubo
            // Revogar toda a família de tokens
            if (refreshToken.IsRevoked)
            {
                await RevokeTokenFamilyAsync(refreshToken.TokenFamily, "Reuse of revoked token detected");
            }
            return null;
        }

        return refreshToken;
    }

    public async Task RevokeRefreshTokenAsync(string rawToken, string reason)
    {
        var tokenHash = HashToken(rawToken);
        var refreshToken = await _refreshTokenRepo.GetByTokenHashAsync(tokenHash);

        if (refreshToken != null && refreshToken.IsActive)
        {
            refreshToken.Revoke(reason);
            await _refreshTokenRepo.UpdateAsync(refreshToken);

            await _audit.LogAsync("RefreshToken", refreshToken.Id, "ACTIVE", "REVOKED",
                reason, null, refreshToken.UserId, null);
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, string reason)
    {
        var tokens = await _refreshTokenRepo.GetActiveByUserIdAsync(userId);

        foreach (var token in tokens)
        {
            token.Revoke(reason);
            await _refreshTokenRepo.UpdateAsync(token);
        }

        await _audit.LogAsync("User", userId, null, null, "ALL_TOKENS_REVOKED",
            reason, userId, null);
    }

    public int GetAccessTokenExpirationSeconds()
    {
        var minutes = int.Parse(_config["Jwt:AccessTokenExpirationMinutes"]!);
        return minutes * 60;
    }

    private async Task RevokeTokenFamilyAsync(string family, string reason)
    {
        var tokens = await _refreshTokenRepo.GetByFamilyAsync(family);

        foreach (var token in tokens.Where(t => t.IsActive))
        {
            token.Revoke(reason);
            await _refreshTokenRepo.UpdateAsync(token);
        }
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
