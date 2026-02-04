using System.Security.Claims;
using EveUp.Core.DTOs.Denunciation;
using EveUp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EveUp.Api.Controllers;

[ApiController]
[Route("api/denunciations")]
[Authorize]
public class DenunciationsController : ControllerBase
{
    private readonly IDenunciationService _denunciationService;

    public DenunciationsController(IDenunciationService denunciationService)
    {
        _denunciationService = denunciationService;
    }

    /// <summary>
    /// Cria uma denúncia contra um usuário
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<DenunciationResponse>> Create(
        [FromBody] CreateDenunciationRequest request)
    {
        var initiatorId = GetUserId();
        var result = await _denunciationService.CreateAsync(
            initiatorId,
            request.TargetId,
            request.Description,
            request.JobId);

        return Created($"/api/denunciations/{result.Id}", result);
    }

    /// <summary>
    /// Lista denúncias do usuário autenticado
    /// </summary>
    [HttpGet("mine")]
    public async Task<ActionResult> GetMyDenunciations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        var (items, totalCount) = await _denunciationService.GetMyDenunciationsAsync(userId, page, pageSize);
        return Ok(new { items, totalCount, page, pageSize });
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim?.Value ?? throw new UnauthorizedAccessException());
    }
}
