namespace EveUp.Core.Interfaces;

public interface IPspProvider
{
    Task<PspResult> CreateChargeAsync(decimal amount, string paymentMethod, string idempotencyKey);
    Task<PspResult> RefundAsync(string transactionId, decimal amount);
    Task<PspResult> GetStatusAsync(string transactionId);
}

public class PspResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? Status { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RawResponse { get; set; }
}
