using EveUp.Core.Enums;

namespace EveUp.Core.Entities;

public sealed class Attendance
{
    public Guid Id { get; private set; }
    public Guid JobId { get; private set; }
    public Job Job { get; private set; } = null!;
    public Guid ProfessionalId { get; private set; }
    public User Professional { get; private set; } = null!;

    // Check-in
    public DateTime? CheckInAt { get; private set; }
    public decimal? CheckInLatitude { get; private set; }
    public decimal? CheckInLongitude { get; private set; }

    // Check-out
    public DateTime? CheckOutAt { get; private set; }
    public decimal? CheckOutLatitude { get; private set; }
    public decimal? CheckOutLongitude { get; private set; }

    public AttendanceStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Attendance() { }

    public static Attendance Create(Guid jobId, Guid professionalId)
    {
        return new Attendance
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            ProfessionalId = professionalId,
            Status = AttendanceStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void CheckIn(decimal? latitude = null, decimal? longitude = null)
    {
        if (CheckInAt.HasValue)
            throw new InvalidOperationException("Already checked in");

        CheckInAt = DateTime.UtcNow;
        CheckInLatitude = latitude;
        CheckInLongitude = longitude;
        Status = AttendanceStatus.CheckedIn;
        UpdatedAt = DateTime.UtcNow;
    }

    public void CheckOut(decimal? latitude = null, decimal? longitude = null)
    {
        if (!CheckInAt.HasValue)
            throw new InvalidOperationException("Must check in before checking out");

        if (CheckOutAt.HasValue)
            throw new InvalidOperationException("Already checked out");

        CheckOutAt = DateTime.UtcNow;
        CheckOutLatitude = latitude;
        CheckOutLongitude = longitude;
        Status = AttendanceStatus.CheckedOut;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsNoShow()
    {
        Status = AttendanceStatus.NoShow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Contest()
    {
        if (!CheckOutAt.HasValue)
            throw new InvalidOperationException("Can only contest after check-out");

        Status = AttendanceStatus.Contested;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Confirm()
    {
        if (!CheckOutAt.HasValue)
            throw new InvalidOperationException("Can only confirm after check-out");

        Status = AttendanceStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanBeContested()
    {
        if (!CheckOutAt.HasValue)
            return false;

        var contestationDeadline = CheckOutAt.Value.AddHours(24);
        return DateTime.UtcNow <= contestationDeadline;
    }
}
