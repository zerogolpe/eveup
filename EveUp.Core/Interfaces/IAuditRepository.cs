using EveUp.Core.Entities;

namespace EveUp.Core.Interfaces;

public interface IAuditRepository
{
    Task AddAsync(AuditLog log);
    Task<List<AuditLog>> GetByEntityAsync(string entityType, Guid entityId);
}
