using EveUp.Core.Enums;
using EveUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EveUp.Infrastructure.Jobs;

/// <summary>
/// Background job que confirma automaticamente presenças após 24h sem contestação
/// </summary>
public class AutoConfirmAttendanceJob
{
    private readonly EveUpDbContext _context;
    private readonly ILogger<AutoConfirmAttendanceJob> _logger;

    public AutoConfirmAttendanceJob(EveUpDbContext context, ILogger<AutoConfirmAttendanceJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting auto-confirmation of attendances...");

        try
        {
            // Buscar attendances que fizeram check-out há mais de 24h e ainda não foram confirmadas/contestadas
            var attendancesToConfirm = await _context.Attendances
                .Include(a => a.Job)
                .Include(a => a.Professional)
                .Where(a => a.Status == AttendanceStatus.CheckedOut &&
                           a.CheckOutAt != null &&
                           a.CheckOutAt.Value.AddHours(24) <= DateTime.UtcNow)
                .Take(1000)  // Limite de segurança
                .ToListAsync();

            if (!attendancesToConfirm.Any())
            {
                _logger.LogInformation("No attendances to auto-confirm.");
                return;
            }

            foreach (var attendance in attendancesToConfirm)
            {
                attendance.Confirm();
                _logger.LogInformation(
                    "Auto-confirmed attendance {AttendanceId} for job {JobId} and professional {ProfessionalId}",
                    attendance.Id, attendance.JobId, attendance.ProfessionalId);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Auto-confirmed {Count} attendances.", attendancesToConfirm.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-confirming attendances");
            throw;
        }
    }
}
