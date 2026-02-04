using EveUp.Core.Entities;
using EveUp.Core.Interfaces;

namespace EveUp.Services;

public class AuditService : IAuditService
{
    private readonly IAuditRepository _auditRepo;

    public AuditService(IAuditRepository auditRepo)
    {
        _auditRepo = auditRepo;
    }

    public async Task LogAsync(
        string entityType,
        Guid entityId,
        string? previousState,
        string? newState,
        string action,
        string? details,
        Guid? performedByUserId,
        string? ipAddress)
    {
        var log = AuditLog.Create(
            entityType,
            entityId,
            previousState,
            newState,
            action,
            details,
            performedByUserId,
            ipAddress);

        await _auditRepo.AddAsync(log);
    }
}
