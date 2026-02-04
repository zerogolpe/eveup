namespace EveUp.Core.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        string entityType,
        Guid entityId,
        string? previousState,
        string? newState,
        string action,
        string? details,
        Guid? performedByUserId,
        string? ipAddress);
}
