using EveUp.Core.Entities;
using EveUp.Core.Enums;

namespace EveUp.Core.DTOs.Attendance;

public class AttendanceResponse
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string? JobTitle { get; set; }
    public Guid ProfessionalId { get; set; }
    public string? ProfessionalName { get; set; }

    public DateTime? CheckInAt { get; set; }
    public decimal? CheckInLatitude { get; set; }
    public decimal? CheckInLongitude { get; set; }

    public DateTime? CheckOutAt { get; set; }
    public decimal? CheckOutLatitude { get; set; }
    public decimal? CheckOutLongitude { get; set; }

    public AttendanceStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static AttendanceResponse FromEntity(Core.Entities.Attendance attendance)
    {
        return new AttendanceResponse
        {
            Id = attendance.Id,
            JobId = attendance.JobId,
            JobTitle = attendance.Job?.Title,
            ProfessionalId = attendance.ProfessionalId,
            ProfessionalName = attendance.Professional?.Name,
            CheckInAt = attendance.CheckInAt,
            CheckInLatitude = attendance.CheckInLatitude,
            CheckInLongitude = attendance.CheckInLongitude,
            CheckOutAt = attendance.CheckOutAt,
            CheckOutLatitude = attendance.CheckOutLatitude,
            CheckOutLongitude = attendance.CheckOutLongitude,
            Status = attendance.Status,
            CreatedAt = attendance.CreatedAt,
            UpdatedAt = attendance.UpdatedAt
        };
    }
}
