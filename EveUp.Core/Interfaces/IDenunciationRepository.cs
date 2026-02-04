using EveUp.Core.Entities;
using EveUp.Core.Enums;

namespace EveUp.Core.Interfaces;

public interface IDenunciationRepository
{
    Task<Denunciation?> GetByIdAsync(Guid id);
    Task<Denunciation?> GetByIdWithRelationsAsync(Guid id);
    Task<(List<Denunciation> items, int totalCount)> ListByUserIdAsync(Guid userId, int page, int pageSize);
    Task<(List<Denunciation> items, int totalCount)> ListByStatusAsync(DenunciationStatus status, int page, int pageSize);
    Task<List<Denunciation>> GetExpiredContestationDeadlinesAsync();
    Task AddAsync(Denunciation denunciation);
    Task UpdateAsync(Denunciation denunciation);
}
