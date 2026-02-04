using System.Security.Cryptography;
using System.Text;
using EveUp.Core.DTOs.Payment;
using EveUp.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EveUp.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IAuditService _audit;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IPaymentService paymentService,
        IAuditService audit,
        IConfiguration configuration,
        ILogger<WebhooksController> logger)
    {
        _paymentService = paymentService;
        _audit = audit;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Webhook do PSP para notificações de pagamento
    /// </summary>
    [HttpPost("psp")]
    public async Task<ActionResult> PspWebhook(
        [FromBody] PspWebhookRequest request,
        [FromHeader(Name = "X-Webhook-Signature")] string? signature = null)
    {
        // Verify webhook signature
        if (!VerifyWebhookSignature(request, signature))
        {
            _logger.LogWarning("[Webhook] Invalid signature for transaction {TransactionId}", request.TransactionId);
            return Unauthorized(new { message = "Invalid webhook signature." });
        }

        _logger.LogInformation(
            "[Webhook] PSP event: transactionId={TransactionId}, status={Status}",
            request.TransactionId, request.Status);

        await _audit.LogAsync("Webhook", Guid.Empty,
            null, null,
            "PSP_WEBHOOK", $"PSP webhook: {request.Status} for {request.TransactionId}",
            null, GetIpAddress());

        switch (request.Status.ToLowerInvariant())
        {
            case "captured":
            case "approved":
            case "confirmed":
                await _paymentService.ConfirmPaymentAsync(request.TransactionId);
                break;

            case "failed":
            case "declined":
            case "error":
                await _paymentService.FailPaymentAsync(request.TransactionId, request.FailureReason);
                break;

            case "refunded":
            case "reversed":
                await _paymentService.ConfirmRefundAsync(request.TransactionId);
                break;

            case "chargeback":
            case "dispute":
                await _paymentService.HandleChargebackAsync(request.TransactionId);
                break;

            default:
                _logger.LogWarning("[Webhook] Unknown PSP status: {Status}", request.Status);
                break;
        }

        return Ok(new { received = true });
    }

    private bool VerifyWebhookSignature(PspWebhookRequest request, string? signature)
    {
        var secret = _configuration["Psp:WebhookSecret"];

        // SEGURANÇA: Se o secret não está configurado, rejeitar por padrão
        if (string.IsNullOrEmpty(secret))
        {
            _logger.LogError("[Webhook] Psp:WebhookSecret not configured. Rejecting webhook.");
            return false;
        }

        if (string.IsNullOrEmpty(signature))
            return false;

        // HMAC-SHA256 verification
        var payload = $"{request.TransactionId}:{request.Status}:{request.Timestamp:O}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computedHash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

        var signatureBytes = Encoding.UTF8.GetBytes(signature.ToLowerInvariant());
        var computedBytes = Encoding.UTF8.GetBytes(computedHash);
        return CryptographicOperations.FixedTimeEquals(computedBytes, signatureBytes);
    }

    private string GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
