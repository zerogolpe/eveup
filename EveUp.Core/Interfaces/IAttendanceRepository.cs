using EveUp.Core.Entities;

namespace EveUp.Core.Interfaces;

public interface IAttendanceRepository
{
    Task<Attendance?> GetByIdAsync(Guid id);
    Task<Attendance?> GetByJobAndProfessionalAsync(Guid jobId, Guid professionalId);
    Task<List<Attendance>> GetByJobIdAsync(Guid jobId);
    Task<List<Attendance>> GetPendingContestationAsync();
    Task AddAsync(Attendance attendance);
    Task UpdateAsync(Attendance attendance);
}
