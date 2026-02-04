using System.ComponentModel.DataAnnotations;

namespace EveUp.Core.DTOs.Payment;

public class CreatePaymentRequest
{
    [Required]
    public Guid JobId { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = string.Empty;

    public string? IdempotencyKey { get; set; }
}
