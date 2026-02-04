using EveUp.Core.Enums;

namespace EveUp.Core.DTOs.Payment;

public class PaymentResponse
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid CompanyId { get; set; }

    public decimal GrossAmount { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal NetAmount { get; set; }

    public string? PaymentMethod { get; set; }
    public PaymentState State { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? HeldAt { get; set; }
    public DateTime? ReleasedAt { get; set; }

    public static PaymentResponse FromEntity(Entities.Payment payment)
    {
        return new PaymentResponse
        {
            Id = payment.Id,
            JobId = payment.JobId,
            CompanyId = payment.CompanyId,
            GrossAmount = payment.GrossAmount,
            PlatformFee = payment.PlatformFee,
            NetAmount = payment.NetAmount,
            PaymentMethod = payment.PaymentMethod,
            State = payment.State,
            CreatedAt = payment.CreatedAt,
            HeldAt = payment.HeldAt,
            ReleasedAt = payment.ReleasedAt
        };
    }
}
