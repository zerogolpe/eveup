using EveUp.Core.Entities;

namespace EveUp.Core.Interfaces;

public interface IDisputeRepository
{
    Task<Dispute?> GetByIdAsync(Guid id);
    Task<List<Dispute>> GetByJobAsync(Guid jobId);
    Task<List<Dispute>> GetByUserAsync(Guid userId);
    Task AddAsync(Dispute dispute);
    Task UpdateAsync(Dispute dispute);
}
