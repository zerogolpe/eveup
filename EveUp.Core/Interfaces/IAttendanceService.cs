using EveUp.Core.DTOs.Attendance;

namespace EveUp.Core.Interfaces;

public interface IAttendanceService
{
    Task<AttendanceResponse> CheckInAsync(Guid professionalId, Guid jobId, decimal? latitude, decimal? longitude);
    Task<AttendanceResponse> CheckOutAsync(Guid professionalId, Guid jobId, decimal? latitude, decimal? longitude);
    Task<AttendanceResponse> ContestAsync(Guid attendanceId, Guid companyId);
    Task<AttendanceResponse> MarkNoShowAsync(Guid attendanceId, Guid companyId);
    Task<AttendanceResponse> GetByIdAsync(Guid attendanceId);
    Task<List<AttendanceResponse>> GetByJobAsync(Guid jobId, Guid requesterId);
    Task<List<AttendanceResponse>> GetMyAttendancesAsync(Guid professionalId);
}
