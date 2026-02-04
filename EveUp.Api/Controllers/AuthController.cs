using System.Security.Claims;
using EveUp.Core.DTOs.Auth;
using EveUp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EveUp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuditService _audit;

    public AuthController(IAuthService authService, IAuditService audit)
    {
        _authService = authService;
        _audit = audit;
    }

    /// <summary>
    /// Login - retorna access token e refresh token
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var ipAddress = GetIpAddress();

        var result = await _authService.LoginAsync(request.Email, request.Password, ipAddress);

        if (result == null)
        {
            await _audit.LogAsync("Auth", Guid.Empty, null, null, "LOGIN_FAILED",
                $"Failed login attempt for {request.Email}", null, ipAddress);

            return Unauthorized(new { message = "Email ou senha inválidos" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Registro de novo usuário
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
    {
        var ipAddress = GetIpAddress();

        var result = await _authService.RegisterAsync(
            request.Email,
            request.Password,
            request.Name,
            ipAddress);

        if (result == null)
        {
            return BadRequest(new { message = "Email já cadastrado" });
        }

        return Created("", result);
    }

    /// <summary>
    /// Refresh token - renova access token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<RefreshTokenResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var ipAddress = GetIpAddress();

        var result = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);

        if (result == null)
        {
            return Unauthorized(new { message = "Token inválido ou expirado" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Logout - revoga refresh token
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var userId = GetUserId();
        var ipAddress = GetIpAddress();

        await _authService.LogoutAsync(request.RefreshToken, userId, ipAddress);

        return NoContent();
    }

    /// <summary>
    /// Logout de todos os dispositivos
    /// </summary>
    [HttpPost("logout-all")]
    [Authorize]
    public async Task<ActionResult> LogoutAll()
    {
        var userId = GetUserId();
        var ipAddress = GetIpAddress();

        await _authService.LogoutAllAsync(userId, ipAddress);

        return NoContent();
    }

    /// <summary>
    /// Esqueci a senha
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        // Sempre retorna OK para não expor se email existe
        await _authService.SendPasswordResetAsync(request.Email);
        return Ok(new { message = "Se o email existir, você receberá instruções" });
    }

    private string GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim?.Value ?? throw new UnauthorizedAccessException());
    }
}
