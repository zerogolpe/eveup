using System.Security.Claims;
using EveUp.Core.DTOs.Job;
using EveUp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EveUp.Api.Controllers;

[ApiController]
[Route("api/jobs")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(IJobService jobService, ILogger<JobsController> logger)
    {
        _jobService = jobService;
        _logger = logger;
    }

    /// <summary>
    /// Lista jobs publicados (com filtros opcionais)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<JobListResponse>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? city = null,
        [FromQuery] string? eventType = null,
        [FromQuery] string? skills = null)
    {
        var result = await _jobService.ListJobsAsync(page, pageSize, city, eventType, skills);
        return Ok(result);
    }

    /// <summary>
    /// Lista jobs da empresa autenticada
    /// </summary>
    [HttpGet("mine")]
    public async Task<ActionResult<JobListResponse>> ListMyJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? state = null)
    {
        var companyId = GetUserId();
        var result = await _jobService.ListMyJobsAsync(companyId, page, pageSize, state);
        return Ok(result);
    }

    /// <summary>
    /// Retorna detalhes de um job
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<JobResponse>> GetById(Guid id)
    {
        var result = await _jobService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Cria um novo job (COMPANY only)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<JobResponse>> Create([FromBody] CreateJobRequest request)
    {
        var companyId = GetUserId();
        var result = await _jobService.CreateAsync(companyId, request);
        return Created($"/api/jobs/{result.Id}", result);
    }

    /// <summary>
    /// Atualiza um job (apenas DRAFT)
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JobResponse>> Update(Guid id, [FromBody] CreateJobRequest request)
    {
        var companyId = GetUserId();
        var result = await _jobService.UpdateAsync(id, companyId, request);
        return Ok(result);
    }

    /// <summary>
    /// Publica um job (DRAFT → PUBLISHED)
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<JobResponse>> Publish(Guid id)
    {
        var companyId = GetUserId();
        var result = await _jobService.PublishAsync(id, companyId);
        return Ok(result);
    }

    /// <summary>
    /// Cancela um job
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<JobResponse>> Cancel(Guid id, [FromBody] CancelJobRequest? request = null)
    {
        var companyId = GetUserId();
        var result = await _jobService.CancelAsync(id, companyId, request?.Reason);
        return Ok(result);
    }

    /// <summary>
    /// Confirma a conclusão de um job (IN_PROGRESS → COMPLETED)
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<JobResponse>> Complete(Guid id)
    {
        var companyId = GetUserId();
        var result = await _jobService.ConfirmCompletionAsync(id, companyId);
        return Ok(result);
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim?.Value ?? throw new UnauthorizedAccessException());
    }
}

public class CancelJobRequest
{
    public string? Reason { get; set; }
}
