namespace EveUp.Core.Entities;

public sealed class AuditLog
{
    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }

    public string? PreviousState { get; private set; }
    public string? NewState { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? Details { get; private set; }

    public Guid? PerformedByUserId { get; private set; }
    public string? IpAddress { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string entityType,
        Guid entityId,
        string? previousState,
        string? newState,
        string action,
        string? details,
        Guid? performedByUserId,
        string? ipAddress)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            PreviousState = previousState,
            NewState = newState,
            Action = action,
            Details = details,
            PerformedByUserId = performedByUserId,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };
    }
}
