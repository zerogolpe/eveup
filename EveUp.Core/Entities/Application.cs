using EveUp.Core.Enums;

namespace EveUp.Core.Entities;

public sealed class Application
{
    public Guid Id { get; private set; }
    public Guid JobId { get; private set; }
    public Job Job { get; private set; } = null!;
    public Guid WorkerId { get; private set; }
    public User Worker { get; private set; } = null!;

    public ApplicationState State { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public DateTime? WithdrawnAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private Application() { }

    public static Application Create(Guid jobId, Guid workerId)
    {
        return new Application
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            WorkerId = workerId,
            State = ApplicationState.PENDING,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateState(ApplicationState newState)
    {
        State = newState;
        UpdatedAt = DateTime.UtcNow;

        switch (newState)
        {
            case ApplicationState.APPROVED:
                ApprovedAt = DateTime.UtcNow;
                break;
            case ApplicationState.REJECTED:
                RejectedAt = DateTime.UtcNow;
                break;
            case ApplicationState.WITHDRAWN:
                WithdrawnAt = DateTime.UtcNow;
                break;
            case ApplicationState.COMPLETED:
                CompletedAt = DateTime.UtcNow;
                break;
        }
    }
}
