namespace EveUp.Core.DTOs.Payment;

public class PspWebhookRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public decimal? Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime Timestamp { get; set; }
}
