using EveUp.Core.Entities;
using EveUp.Core.Interfaces;
using EveUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EveUp.Infrastructure.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly EveUpDbContext _context;

    public ReviewRepository(EveUpDbContext context)
    {
        _context = context;
    }

    public async Task<Review?> GetByIdAsync(Guid id)
    {
        return await _context.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.ReviewedUser)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<Review>> GetByUserAsync(Guid userId)
    {
        return await _context.Reviews
            .Include(r => r.Reviewer)
            .Where(r => r.ReviewedUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Review>> GetByJobAsync(Guid jobId)
    {
        return await _context.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.ReviewedUser)
            .Where(r => r.JobId == jobId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid jobId, Guid reviewerId)
    {
        return await _context.Reviews
            .AnyAsync(r => r.JobId == jobId && r.ReviewerId == reviewerId);
    }

    public async Task<(decimal averageRating, int totalReviews)> GetUserRatingStatsAsync(Guid userId)
    {
        var reviews = await _context.Reviews
            .Where(r => r.ReviewedUserId == userId)
            .ToListAsync();

        if (reviews.Count == 0)
            return (0, 0);

        return ((decimal)reviews.Average(r => r.Rating), reviews.Count);
    }

    public async Task AddAsync(Review review)
    {
        await _context.Reviews.AddAsync(review);
        await _context.SaveChangesAsync();
    }
}
