using EveUp.Core.Entities;
using EveUp.Core.Enums;
using EveUp.Core.Exceptions;
using EveUp.Core.Interfaces;
using EveUp.Services.StateMachines;

namespace EveUp.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepo;
    private readonly IJobRepository _jobRepo;
    private readonly IApplicationRepository _appRepo;
    private readonly IUserRepository _userRepo;
    private readonly IAuditService _audit;

    public ReviewService(
        IReviewRepository reviewRepo,
        IJobRepository jobRepo,
        IApplicationRepository appRepo,
        IUserRepository userRepo,
        IAuditService audit)
    {
        _reviewRepo = reviewRepo;
        _jobRepo = jobRepo;
        _appRepo = appRepo;
        _userRepo = userRepo;
        _audit = audit;
    }

    public async Task<Review> CreateAsync(Guid jobId, Guid reviewerId, Guid reviewedUserId, int rating, string? comment)
    {
        var job = await _jobRepo.GetByIdAsync(jobId)
            ?? throw new BusinessRuleException("JobNotFound", "Job not found.");

        if (job.State != JobState.COMPLETED && job.State != JobState.SETTLED && job.State != JobState.RESOLVED)
            throw new BusinessRuleException("JobNotCompleted", "Reviews can only be submitted after job completion.");

        var reviewer = await _userRepo.GetByIdAsync(reviewerId)
            ?? throw new BusinessRuleException("UserNotFound", "Reviewer not found.");

        var reviewedUser = await _userRepo.GetByIdAsync(reviewedUserId)
            ?? throw new BusinessRuleException("UserNotFound", "Reviewed user not found.");

        if (reviewerId == reviewedUserId)
            throw new BusinessRuleException("SelfReview", "You cannot review yourself.");

        if (await _reviewRepo.ExistsAsync(jobId, reviewerId))
            throw new BusinessRuleException("AlreadyReviewed", "You have already reviewed for this job.");

        var review = Review.Create(jobId, reviewerId, reviewedUserId, rating, comment);
        await _reviewRepo.AddAsync(review);

        // Update the reviewed user's rating stats
        var (avgRating, totalReviews) = await _reviewRepo.GetUserRatingStatsAsync(reviewedUserId);
        reviewedUser.UpdateRating(avgRating, totalReviews);
        await _userRepo.UpdateAsync(reviewedUser);

        // Try to transition the reviewer's application to RATED (for workers)
        var applications = await _appRepo.GetByJobAsync(jobId);
        var reviewerApp = applications.FirstOrDefault(a => a.WorkerId == reviewerId && a.State == ApplicationState.COMPLETED);
        if (reviewerApp != null)
        {
            var prevState = reviewerApp.State;
            if (ApplicationStateMachine.CanTransition(prevState, ApplicationState.RATED))
            {
                reviewerApp.UpdateState(ApplicationState.RATED);
                await _appRepo.UpdateAsync(reviewerApp);

                await _audit.LogAsync("Application", reviewerApp.Id,
                    prevState.ToString(), ApplicationState.RATED.ToString(),
                    "STATE_CHANGE", "Application marked as rated after review.",
                    reviewerId, null);
            }
        }

        await _audit.LogAsync("Review", review.Id,
            null, null,
            "CREATED", $"Review created: {rating}/5 for user {reviewedUserId} on job {jobId}.",
            reviewerId, null);

        return review;
    }

    public async Task<List<Review>> GetByUserAsync(Guid userId)
    {
        return await _reviewRepo.GetByUserAsync(userId);
    }

    public async Task<List<Review>> GetByJobAsync(Guid jobId)
    {
        return await _reviewRepo.GetByJobAsync(jobId);
    }
}
