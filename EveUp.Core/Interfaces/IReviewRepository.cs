using EveUp.Core.Entities;

namespace EveUp.Core.Interfaces;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(Guid id);
    Task<List<Review>> GetByUserAsync(Guid userId);
    Task<List<Review>> GetByJobAsync(Guid jobId);
    Task<bool> ExistsAsync(Guid jobId, Guid reviewerId);
    Task<(decimal averageRating, int totalReviews)> GetUserRatingStatsAsync(Guid userId);
    Task AddAsync(Review review);
}
