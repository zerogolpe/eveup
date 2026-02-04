using EveUp.Core.Enums;

namespace EveUp.Core.Entities;

public sealed class Dispute
{
    public Guid Id { get; private set; }
    public Guid JobId { get; private set; }
    public Job Job { get; private set; } = null!;
    public Guid OpenedByUserId { get; private set; }
    public User OpenedByUser { get; private set; } = null!;

    public DisputeType Type { get; private set; }
    public DisputeState State { get; private set; }

    public string Description { get; private set; } = string.Empty;
    public string? Resolution { get; private set; }
    public string? Evidence { get; private set; } // JSON array of evidence items

    public decimal? RefundAmount { get; private set; }
    public decimal? WorkerPayout { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    private Dispute() { }

    public static Dispute Create(Guid jobId, Guid openedByUserId, DisputeType type, string description)
    {
        return new Dispute
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            OpenedByUserId = openedByUserId,
            Type = type,
            State = DisputeState.OPENED,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateState(DisputeState newState)
    {
        State = newState;
        UpdatedAt = DateTime.UtcNow;

        if (newState == DisputeState.FAVOR_COMPANY ||
            newState == DisputeState.FAVOR_WORKER ||
            newState == DisputeState.PARTIAL)
        {
            ResolvedAt = DateTime.UtcNow;
        }
    }

    public void SetResolution(string resolution, decimal? refundAmount = null, decimal? workerPayout = null)
    {
        Resolution = resolution;
        RefundAmount = refundAmount;
        WorkerPayout = workerPayout;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddEvidence(string evidenceJson)
    {
        Evidence = evidenceJson;
        UpdatedAt = DateTime.UtcNow;
    }
}
