using EveUp.Core.Entities;

namespace EveUp.Core.Interfaces;

public interface IReviewService
{
    Task<Review> CreateAsync(Guid jobId, Guid reviewerId, Guid reviewedUserId, int rating, string? comment);
    Task<List<Review>> GetByUserAsync(Guid userId);
    Task<List<Review>> GetByJobAsync(Guid jobId);
}
