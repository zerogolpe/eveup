using EveUp.Core.Entities;
using EveUp.Core.Enums;
using EveUp.Core.Interfaces;
using EveUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EveUp.Infrastructure.Repositories;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly EveUpDbContext _context;

    public AttendanceRepository(EveUpDbContext context) => _context = context;

    public async Task<Attendance?> GetByIdAsync(Guid id) =>
        await _context.Attendances
            .Include(a => a.Job)
            .Include(a => a.Professional)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Attendance?> GetByJobAndProfessionalAsync(Guid jobId, Guid professionalId) =>
        await _context.Attendances
            .Include(a => a.Job)
            .Include(a => a.Professional)
            .FirstOrDefaultAsync(a => a.JobId == jobId && a.ProfessionalId == professionalId);

    public async Task<List<Attendance>> GetByJobIdAsync(Guid jobId) =>
        await _context.Attendances
            .Include(a => a.Professional)
            .Where(a => a.JobId == jobId)
            .Take(1000)  // Limite de segurança
            .ToListAsync();

    public async Task<List<Attendance>> GetPendingContestationAsync() =>
        await _context.Attendances
            .Include(a => a.Job)
            .ThenInclude(j => j.Company)
            .Include(a => a.Professional)
            .Where(a => a.Status == AttendanceStatus.CheckedOut &&
                       a.CheckOutAt != null &&
                       a.CheckOutAt.Value.AddHours(24) <= DateTime.UtcNow)
            .Take(1000)  // Limite de segurança para background job
            .ToListAsync();

    public async Task AddAsync(Attendance attendance)
    {
        await _context.Attendances.AddAsync(attendance);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Attendance attendance)
    {
        _context.Attendances.Update(attendance);
        await _context.SaveChangesAsync();
    }
}
