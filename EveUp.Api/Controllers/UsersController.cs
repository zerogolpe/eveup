using System.Security.Claims;
using EveUp.Core.DTOs.User;
using EveUp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EveUp.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Retorna o perfil do usuário autenticado
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetMe()
    {
        var userId = GetUserId();
        var user = await _userService.GetByIdAsync(userId);
        return Ok(user);
    }

    /// <summary>
    /// Retorna o perfil de um usuário por ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        return Ok(user);
    }

    /// <summary>
    /// Atualiza o perfil do usuário autenticado
    /// </summary>
    [HttpPut("me")]
    public async Task<ActionResult<UserResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        var user = await _userService.UpdateProfileAsync(userId, request);
        return Ok(user);
    }

    /// <summary>
    /// Seleciona o tipo de conta (COMPANY ou WORKER)
    /// </summary>
    [HttpPost("me/role")]
    public async Task<ActionResult<UserResponse>> SelectRole([FromBody] SelectRoleRequest request)
    {
        var userId = GetUserId();
        var user = await _userService.SelectRoleAsync(userId, request.Type);
        return Ok(user);
    }

    /// <summary>
    /// Verifica o CPF do usuário
    /// </summary>
    [HttpPost("me/cpf")]
    public async Task<ActionResult> VerifyCpf([FromBody] VerifyCpfRequest request)
    {
        var userId = GetUserId();
        await _userService.VerifyCpfAsync(userId, request.Cpf);
        return Ok(new { message = "CPF verificado com sucesso." });
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim?.Value ?? throw new UnauthorizedAccessException());
    }
}
