using System.Security.Claims;
using EveUp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EveUp.Api.Controllers;

[ApiController]
[Route("api/applications")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _appService;

    public ApplicationsController(IApplicationService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// Worker se candidata a um job
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> Apply([FromBody] ApplyRequest request)
    {
        var workerId = GetUserId();
        var application = await _appService.ApplyAsync(request.JobId, workerId);
        return Created($"/api/applications/{application.Id}", MapApplication(application));
    }

    /// <summary>
    /// Worker retira sua candidatura
    /// </summary>
    [HttpPost("{id:guid}/withdraw")]
    public async Task<ActionResult> Withdraw(Guid id)
    {
        var workerId = GetUserId();
        var application = await _appService.WithdrawAsync(id, workerId);
        return Ok(MapApplication(application));
    }

    /// <summary>
    /// Company aprova uma candidatura
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult> Approve(Guid id)
    {
        var companyId = GetUserId();
        var application = await _appService.ApproveAsync(id, companyId);
        return Ok(MapApplication(application));
    }

    /// <summary>
    /// Company rejeita uma candidatura
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult> Reject(Guid id)
    {
        var companyId = GetUserId();
        var application = await _appService.RejectAsync(id, companyId);
        return Ok(MapApplication(application));
    }

    /// <summary>
    /// Lista candidaturas de um job (Company)
    /// </summary>
    [HttpGet("job/{jobId:guid}")]
    public async Task<ActionResult> GetByJob(Guid jobId)
    {
        var applications = await _appService.GetByJobAsync(jobId);
        return Ok(applications.Select(MapApplication));
    }

    /// <summary>
    /// Lista candidaturas do worker autenticado
    /// </summary>
    [HttpGet("mine")]
    public async Task<ActionResult> GetMyApplications([FromQuery] string? state = null)
    {
        var workerId = GetUserId();
        var applications = await _appService.GetByWorkerAsync(workerId, state);
        return Ok(applications.Select(MapApplication));
    }

    /// <summary>
    /// Retorna detalhes de uma candidatura
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(Guid id)
    {
        var application = await _appService.GetByIdAsync(id);
        return Ok(MapApplication(application));
    }

    /// <summary>
    /// Mapeia Application entity para response DTO consistente.
    /// Job é sempre null aqui — usamos campos flat (JobTitle, CompanyName)
    /// para evitar incompatibilidade de schema com o frontend.
    /// </summary>
    private static object MapApplication(EveUp.Core.Entities.Application a)
    {
        return new
        {
            a.Id,
            a.JobId,
            a.WorkerId,
            WorkerName = a.Worker?.Name,
            WorkerAvatarUrl = (string?)null,
            WorkerRating = a.Worker != null ? (double?)a.Worker.AverageRating : null,
            WorkerCompletedJobs = (int?)null,
            JobTitle = a.Job?.Title,
            CompanyName = a.Job?.Company?.Name,
            Job = (object?)null,
            a.State,
            a.CreatedAt,
            a.ApprovedAt,
            a.RejectedAt,
            a.WithdrawnAt,
            a.CompletedAt
        };
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim?.Value ?? throw new UnauthorizedAccessException());
    }
}

public class ApplyRequest
{
    public Guid JobId { get; set; }
}
