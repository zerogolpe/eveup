using System.Security.Claims;
using EveUp.Core.DTOs.Payment;
using EveUp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EveUp.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// Cria um pagamento para um job (Company)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PaymentResponse>> Create([FromBody] CreatePaymentRequest request)
    {
        var companyId = GetUserId();
        var result = await _paymentService.CreatePaymentAsync(companyId, request);
        return Created($"/api/payments/{result.Id}", result);
    }

    /// <summary>
    /// Processa um pagamento (inicia cobrança no PSP)
    /// </summary>
    [HttpPost("{id:guid}/process")]
    public async Task<ActionResult<PaymentResponse>> Process(Guid id)
    {
        var companyId = GetUserId();
        var payment = await _paymentService.GetByIdAsync(id);
        if (payment.CompanyId != companyId)
            return Forbid();

        var result = await _paymentService.ProcessPaymentAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Retorna detalhes de um pagamento por ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentResponse>> GetById(Guid id)
    {
        var result = await _paymentService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Retorna pagamento de um job
    /// </summary>
    [HttpGet("job/{jobId:guid}")]
    public async Task<ActionResult<PaymentResponse>> GetByJob(Guid jobId)
    {
        var result = await _paymentService.GetByJobIdAsync(jobId);
        return Ok(result);
    }

    /// <summary>
    /// Lista pagamentos da empresa autenticada
    /// </summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<PaymentResponse>>> ListMine()
    {
        var companyId = GetUserId();
        var result = await _paymentService.ListByCompanyAsync(companyId);
        return Ok(result);
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim?.Value ?? throw new UnauthorizedAccessException());
    }
}
