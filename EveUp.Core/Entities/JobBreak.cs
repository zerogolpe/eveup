namespace EveUp.Core.Entities;

public sealed class JobBreak
{
    public Guid Id { get; private set; }
    public Guid JobId { get; private set; }
    public Job Job { get; private set; } = null!;

    public TimeSpan StartTime { get; private set; }
    public int DurationMinutes { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private JobBreak() { }

    public static JobBreak Create(Guid jobId, TimeSpan startTime, int durationMinutes)
    {
        if (durationMinutes <= 0)
            throw new ArgumentException("Duration must be greater than zero", nameof(durationMinutes));

        return new JobBreak
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            StartTime = startTime,
            DurationMinutes = durationMinutes,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(TimeSpan startTime, int durationMinutes)
    {
        if (durationMinutes <= 0)
            throw new ArgumentException("Duration must be greater than zero", nameof(durationMinutes));

        StartTime = startTime;
        DurationMinutes = durationMinutes;
    }
}
