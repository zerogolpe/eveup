using EveUp.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EveUp.Infrastructure.Services;

/// <summary>
/// Mock PSP Provider for MVP. Simulates payment processing.
/// Replace with real PSP integration (Stripe, PagSeguro, etc.) in production.
/// </summary>
public class MockPspProvider : IPspProvider
{
    private readonly ILogger<MockPspProvider> _logger;

    public MockPspProvider(ILogger<MockPspProvider> logger)
    {
        _logger = logger;
    }

    public async Task<PspResult> CreateChargeAsync(decimal amount, string paymentMethod, string idempotencyKey)
    {
        _logger.LogInformation(
            "[MockPSP] CreateCharge: amount={Amount}, method={Method}, idempotencyKey={Key}",
            amount, paymentMethod, idempotencyKey);

        // Simulate async processing
        await Task.Delay(100);

        // Simulate 95% success rate
        var success = Random.Shared.Next(100) < 95;

        if (success)
        {
            var transactionId = $"psp_{Guid.NewGuid():N}";
            _logger.LogInformation("[MockPSP] Charge succeeded: transactionId={TransactionId}", transactionId);

            return new PspResult
            {
                Success = true,
                TransactionId = transactionId,
                Status = "captured",
                RawResponse = $"{{\"id\":\"{transactionId}\",\"status\":\"captured\",\"amount\":{amount}}}"
            };
        }

        _logger.LogWarning("[MockPSP] Charge failed: insufficient_funds");
        return new PspResult
        {
            Success = false,
            ErrorCode = "insufficient_funds",
            ErrorMessage = "The card has insufficient funds.",
            RawResponse = "{\"error\":{\"code\":\"insufficient_funds\",\"message\":\"The card has insufficient funds.\"}}"
        };
    }

    public async Task<PspResult> RefundAsync(string transactionId, decimal amount)
    {
        _logger.LogInformation(
            "[MockPSP] Refund: transactionId={TransactionId}, amount={Amount}",
            transactionId, amount);

        await Task.Delay(50);

        return new PspResult
        {
            Success = true,
            TransactionId = transactionId,
            Status = "refunded",
            RawResponse = $"{{\"id\":\"{transactionId}\",\"status\":\"refunded\",\"refund_amount\":{amount}}}"
        };
    }

    public async Task<PspResult> GetStatusAsync(string transactionId)
    {
        _logger.LogInformation("[MockPSP] GetStatus: transactionId={TransactionId}", transactionId);

        await Task.Delay(50);

        return new PspResult
        {
            Success = true,
            TransactionId = transactionId,
            Status = "captured",
            RawResponse = $"{{\"id\":\"{transactionId}\",\"status\":\"captured\"}}"
        };
    }
}
