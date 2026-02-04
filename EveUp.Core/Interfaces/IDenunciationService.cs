using EveUp.Core.DTOs.Denunciation;

namespace EveUp.Core.Interfaces;

public interface IDenunciationService
{
    Task<DenunciationResponse> CreateAsync(Guid initiatorId, Guid targetId, string description, Guid? jobId = null);
    Task<(List<DenunciationResponse> items, int totalCount)> GetMyDenunciationsAsync(Guid userId, int page = 1, int pageSize = 20);
}
