using System.Security.Claims;
using EveUp.Core.DTOs.Attendance;
using EveUp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EveUp.Api.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    /// <summary>
    /// Profissional faz check-in no job (com geolocalização)
    /// </summary>
    [HttpPost("check-in")]
    public async Task<ActionResult<AttendanceResponse>> CheckIn([FromBody] CheckInRequest request)
    {
        var professionalId = GetUserId();
        var result = await _attendanceService.CheckInAsync(
            professionalId, request.JobId, request.Latitude, request.Longitude);
        return Ok(result);
    }

    /// <summary>
    /// Profissional faz check-out do job (com geolocalização)
    /// </summary>
    [HttpPost("check-out")]
    public async Task<ActionResult<AttendanceResponse>> CheckOut([FromBody] CheckOutRequest request)
    {
        var professionalId = GetUserId();
        var result = await _attendanceService.CheckOutAsync(
            professionalId, request.JobId, request.Latitude, request.Longitude);
        return Ok(result);
    }

    /// <summary>
    /// Empresa contesta presença (dentro de 24h após check-out)
    /// </summary>
    [HttpPost("{id:guid}/contest")]
    public async Task<ActionResult<AttendanceResponse>> Contest(Guid id)
    {
        var companyId = GetUserId();
        var result = await _attendanceService.ContestAsync(id, companyId);
        return Ok(result);
    }

    /// <summary>
    /// Empresa marca profissional como no-show
    /// </summary>
    [HttpPost("{id:guid}/no-show")]
    public async Task<ActionResult<AttendanceResponse>> MarkNoShow(Guid id)
    {
        var companyId = GetUserId();
        var result = await _attendanceService.MarkNoShowAsync(id, companyId);
        return Ok(result);
    }

    /// <summary>
    /// Lista presenças de um job (empresa ou profissionais aprovados)
    /// </summary>
    [HttpGet("job/{jobId:guid}")]
    public async Task<ActionResult<List<AttendanceResponse>>> GetByJob(Guid jobId)
    {
        var requesterId = GetUserId();
        var result = await _attendanceService.GetByJobAsync(jobId, requesterId);
        return Ok(result);
    }

    /// <summary>
    /// Lista minhas presenças (profissional)
    /// </summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<AttendanceResponse>>> GetMyAttendances()
    {
        var professionalId = GetUserId();
        var result = await _attendanceService.GetMyAttendancesAsync(professionalId);
        return Ok(result);
    }

    /// <summary>
    /// Retorna detalhes de uma presença
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AttendanceResponse>> GetById(Guid id)
    {
        var result = await _attendanceService.GetByIdAsync(id);
        return Ok(result);
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim?.Value ?? throw new UnauthorizedAccessException());
    }
}
