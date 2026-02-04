using EveUp.Core.Enums;

namespace EveUp.Core.Entities;

public sealed class Payment
{
    public Guid Id { get; private set; }
    public Guid JobId { get; private set; }
    public Job Job { get; private set; } = null!;
    public Guid CompanyId { get; private set; }
    public User Company { get; private set; } = null!;

    public decimal GrossAmount { get; private set; }
    public decimal PlatformFee { get; private set; }
    public decimal NetAmount { get; private set; }

    public string? PaymentMethod { get; private set; }
    public string? TransactionId { get; private set; }  // ID externo do PSP
    public string? PspResponse { get; private set; }    // JSON da resposta do PSP

    public PaymentState State { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? HeldAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }
    public DateTime? FailedAt { get; private set; }

    public int RetryCount { get; private set; }
    public string? FailureReason { get; private set; }

    // Idempotency
    public string? IdempotencyKey { get; private set; }

    // Concurrency control
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private Payment() { }

    public static Payment Create(Guid jobId, Guid companyId, decimal grossAmount, decimal platformFeePercent, string? idempotencyKey = null)
    {
        // SECURITY: Financial validation guards
        if (grossAmount < 0)
            throw new ArgumentException("GrossAmount cannot be negative", nameof(grossAmount));

        if (platformFeePercent < 0 || platformFeePercent > 100)
            throw new ArgumentException("PlatformFeePercent must be between 0 and 100", nameof(platformFeePercent));

        var fee = grossAmount * (platformFeePercent / 100);
        var netAmount = grossAmount - fee;

        // SECURITY: Ensure NetAmount is never negative
        if (netAmount < 0)
            throw new InvalidOperationException($"Calculated NetAmount cannot be negative. GrossAmount: {grossAmount}, PlatformFeePercent: {platformFeePercent}");

        return new Payment
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            CompanyId = companyId,
            GrossAmount = grossAmount,
            PlatformFee = fee,
            NetAmount = netAmount,
            State = PaymentState.CREATED,
            IdempotencyKey = idempotencyKey,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateState(PaymentState newState)
    {
        State = newState;
        UpdatedAt = DateTime.UtcNow;

        switch (newState)
        {
            case PaymentState.HELD:
                HeldAt = DateTime.UtcNow;
                break;
            case PaymentState.RELEASED:
            case PaymentState.PARTIALLY_RELEASED:
                ReleasedAt = DateTime.UtcNow;
                break;
            case PaymentState.REFUNDED:
                RefundedAt = DateTime.UtcNow;
                break;
            case PaymentState.FAILED:
            case PaymentState.FAILED_FINAL:
                FailedAt = DateTime.UtcNow;
                break;
        }
    }

    public void SetTransactionId(string transactionId)
    {
        TransactionId = transactionId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPaymentMethod(string method)
    {
        PaymentMethod = method;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPspResponse(string response)
    {
        PspResponse = response;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetFailureReason(string reason)
    {
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementRetry()
    {
        RetryCount++;
        UpdatedAt = DateTime.UtcNow;
    }
}
