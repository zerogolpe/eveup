using EveUp.Core.Entities;

namespace EveUp.Core.Interfaces;

public interface IContestationRepository
{
    Task<Contestation?> GetByIdAsync(Guid id);
    Task<List<Contestation>> GetByDenunciationIdAsync(Guid denunciationId);
    Task AddAsync(Contestation contestation);
}
