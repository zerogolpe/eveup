namespace EveUp.Core.Entities;

public sealed class Review
{
    public Guid Id { get; private set; }
    public Guid JobId { get; private set; }
    public Job Job { get; private set; } = null!;
    public Guid ReviewerId { get; private set; }
    public User Reviewer { get; private set; } = null!;
    public Guid ReviewedUserId { get; private set; }
    public User ReviewedUser { get; private set; } = null!;

    public int Rating { get; private set; } // 1-5
    public string? Comment { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private Review() { }

    public static Review Create(Guid jobId, Guid reviewerId, Guid reviewedUserId, int rating, string? comment)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5");

        return new Review
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            ReviewerId = reviewerId,
            ReviewedUserId = reviewedUserId,
            Rating = rating,
            Comment = comment,
            CreatedAt = DateTime.UtcNow
        };
    }
}
