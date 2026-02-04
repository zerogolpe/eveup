using System.Security.Claims;
using EveUp.Core.Enums;
using EveUp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EveUp.Api.Controllers;

[ApiController]
[Route("api/disputes")]
[Authorize]
public class DisputesController : ControllerBase
{
    private readonly IDisputeService _disputeService;

    public DisputesController(IDisputeService disputeService)
    {
        _disputeService = disputeService;
    }

    /// <summary>
    /// Abre uma disputa para um job
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> Open([FromBody] OpenDisputeRequest request)
    {
        var userId = GetUserId();
        var dispute = await _disputeService.OpenAsync(request.JobId, userId, request.Type, request.Description);
        return Created($"/api/disputes/{dispute.Id}", new
        {
            dispute.Id,
            dispute.JobId,
            dispute.State,
            dispute.Type,
            dispute.CreatedAt
        });
    }

    /// <summary>
    /// Lista disputas do usuário autenticado (abriu ou é dono do job)
    /// </summary>
    [HttpGet("mine")]
    public async Task<ActionResult> GetMine()
    {
        var userId = GetUserId();
        var disputes = await _disputeService.GetByUserAsync(userId);
        return Ok(disputes.Select(d => new
        {
            d.Id,
            d.JobId,
            d.OpenedByUserId,
            OpenedByName = d.OpenedByUser?.Name,
            d.Type,
            d.State,
            d.Description,
            d.CreatedAt,
            d.ResolvedAt
        }));
    }

    /// <summary>
    /// Retorna detalhes de uma disputa (apenas participantes)
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(Guid id)
    {
        var userId = GetUserId();
        var dispute = await _disputeService.GetByIdAsync(id);

        // Verificar se o usuário é participante da disputa
        if (dispute.OpenedByUserId != userId && dispute.Job?.CompanyId != userId)
            return Forbid();

        return Ok(new
        {
            dispute.Id,
            dispute.JobId,
            dispute.OpenedByUserId,
            OpenedByName = dispute.OpenedByUser?.Name,
            dispute.Type,
            dispute.State,
            dispute.Description,
            dispute.Resolution,
            dispute.Evidence,
            dispute.RefundAmount,
            dispute.WorkerPayout,
            dispute.CreatedAt,
            dispute.ResolvedAt
        });
    }

    /// <summary>
    /// Lista disputas de um job
    /// </summary>
    [HttpGet("job/{jobId:guid}")]
    public async Task<ActionResult> GetByJob(Guid jobId)
    {
        var disputes = await _disputeService.GetByJobAsync(jobId);
        return Ok(disputes.Select(d => new
        {
            d.Id,
            d.JobId,
            d.OpenedByUserId,
            d.Type,
            d.State,
            d.Description,
            d.CreatedAt,
            d.ResolvedAt
        }));
    }

    /// <summary>
    /// Adiciona evidência a uma disputa
    /// </summary>
    [HttpPost("{id:guid}/evidence")]
    public async Task<ActionResult> AddEvidence(Guid id, [FromBody] AddEvidenceRequest request)
    {
        var userId = GetUserId();
        var dispute = await _disputeService.AddEvidenceAsync(id, userId, request.Evidence);
        return Ok(new { dispute.Id, dispute.State, dispute.Evidence });
    }

    /// <summary>
    /// Resolve uma disputa (admin/sistema)
    /// </summary>
    [HttpPost("{id:guid}/resolve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Resolve(Guid id, [FromBody] ResolveDisputeRequest request)
    {
        var dispute = await _disputeService.ResolveAsync(
            id, request.Resolution, request.Details, request.RefundAmount, request.WorkerPayout);
        return Ok(new
        {
            dispute.Id,
            dispute.State,
            dispute.Resolution,
            dispute.RefundAmount,
            dispute.WorkerPayout,
            dispute.ResolvedAt
        });
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim?.Value ?? throw new UnauthorizedAccessException());
    }
}

public class OpenDisputeRequest
{
    public Guid JobId { get; set; }
    public DisputeType Type { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class AddEvidenceRequest
{
    public string Evidence { get; set; } = string.Empty;
}

public class ResolveDisputeRequest
{
    public DisputeState Resolution { get; set; }
    public string Details { get; set; } = string.Empty;
    public decimal? RefundAmount { get; set; }
    public decimal? WorkerPayout { get; set; }
}
