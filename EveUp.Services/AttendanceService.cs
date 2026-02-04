using EveUp.Core.DTOs.Attendance;
using EveUp.Core.Entities;
using EveUp.Core.Enums;
using EveUp.Core.Exceptions;
using EveUp.Core.Interfaces;

namespace EveUp.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly IJobRepository _jobRepo;
    private readonly IApplicationRepository _applicationRepo;
    private readonly IAuditService _audit;

    public AttendanceService(
        IAttendanceRepository attendanceRepo,
        IJobRepository jobRepo,
        IApplicationRepository applicationRepo,
        IAuditService audit)
    {
        _attendanceRepo = attendanceRepo;
        _jobRepo = jobRepo;
        _applicationRepo = applicationRepo;
        _audit = audit;
    }

    public async Task<AttendanceResponse> CheckInAsync(
        Guid professionalId, Guid jobId, decimal? latitude, decimal? longitude)
    {
        // 1. Validar que o job existe
        var job = await _jobRepo.GetByIdWithCompanyAsync(jobId)
            ?? throw new BusinessRuleException("JobNotFound", "Job not found.");

        // 2. Validar que o profissional foi aprovado
        var application = await _applicationRepo.GetByJobAndWorkerAsync(jobId, professionalId)
            ?? throw new BusinessRuleException("ApplicationNotFound", "You did not apply for this job.");

        if (application.State != ApplicationState.APPROVED)
            throw new BusinessRuleException("NotApproved", "You must be approved to check in.");

        // 3. Validar que o job está em estado válido para check-in
        if (job.State != JobState.CONFIRMED && job.State != JobState.IN_PROGRESS)
            throw new BusinessRuleException("InvalidJobState", "Job must be CONFIRMED or IN_PROGRESS for check-in.");

        // 4. Verificar se já existe attendance para este profissional neste job
        var existingAttendance = await _attendanceRepo.GetByJobAndProfessionalAsync(jobId, professionalId);

        if (existingAttendance == null)
        {
            // Criar nova attendance (com proteção contra race condition)
            existingAttendance = Attendance.Create(jobId, professionalId);
            try
            {
                await _attendanceRepo.AddAsync(existingAttendance);
            }
            catch (Exception)
            {
                // Unique constraint violation = outro request criou primeiro
                existingAttendance = await _attendanceRepo.GetByJobAndProfessionalAsync(jobId, professionalId)
                    ?? throw new BusinessRuleException("CheckInFailed", "Failed to create attendance record.");
            }
        }

        // 5. Realizar check-in
        existingAttendance.CheckIn(latitude, longitude);
        await _attendanceRepo.UpdateAsync(existingAttendance);

        // 6. Atualizar estado do job para IN_PROGRESS se necessário
        if (job.State == JobState.CONFIRMED)
        {
            job.UpdateState(JobState.IN_PROGRESS);
            await _jobRepo.UpdateAsync(job);
        }

        // 7. Audit log
        await _audit.LogAsync("Attendance", existingAttendance.Id,
            null, AttendanceStatus.CheckedIn.ToString(),
            "CHECK_IN", "Professional checked in.",
            professionalId, null);

        return AttendanceResponse.FromEntity(existingAttendance);
    }

    public async Task<AttendanceResponse> CheckOutAsync(
        Guid professionalId, Guid jobId, decimal? latitude, decimal? longitude)
    {
        // 1. Buscar attendance
        var attendance = await _attendanceRepo.GetByJobAndProfessionalAsync(jobId, professionalId)
            ?? throw new BusinessRuleException("AttendanceNotFound", "Attendance record not found.");

        // 2. Realizar check-out
        attendance.CheckOut(latitude, longitude);
        await _attendanceRepo.UpdateAsync(attendance);

        // 3. Audit log
        await _audit.LogAsync("Attendance", attendance.Id,
            AttendanceStatus.CheckedIn.ToString(), AttendanceStatus.CheckedOut.ToString(),
            "CHECK_OUT", "Professional checked out.",
            professionalId, null);

        return AttendanceResponse.FromEntity(attendance);
    }

    public async Task<AttendanceResponse> ContestAsync(Guid attendanceId, Guid companyId)
    {
        // 1. Buscar attendance
        var attendance = await _attendanceRepo.GetByIdAsync(attendanceId)
            ?? throw new BusinessRuleException("AttendanceNotFound", "Attendance not found.");

        // 2. Verificar ownership (job pertence à company)
        var job = await _jobRepo.GetByIdWithCompanyAsync(attendance.JobId)
            ?? throw new BusinessRuleException("JobNotFound", "Job not found.");

        if (job.CompanyId != companyId)
            throw new BusinessRuleException("Unauthorized", "You can only contest attendances for your own jobs.");

        // 3. Validar que pode contestar (dentro de 24h após check-out)
        if (!attendance.CanBeContested())
            throw new BusinessRuleException("ContestationExpired", "Contestation period has expired (24h after check-out).");

        // 4. Contestar
        var previousStatus = attendance.Status;
        attendance.Contest();
        await _attendanceRepo.UpdateAsync(attendance);

        // 5. Audit log
        await _audit.LogAsync("Attendance", attendance.Id,
            previousStatus.ToString(), AttendanceStatus.Contested.ToString(),
            "CONTESTED", "Attendance contested by company.",
            companyId, null);

        return AttendanceResponse.FromEntity(attendance);
    }

    public async Task<AttendanceResponse> MarkNoShowAsync(Guid attendanceId, Guid companyId)
    {
        // 1. Buscar attendance
        var attendance = await _attendanceRepo.GetByIdAsync(attendanceId)
            ?? throw new BusinessRuleException("AttendanceNotFound", "Attendance not found.");

        // 2. Verificar ownership
        var job = await _jobRepo.GetByIdWithCompanyAsync(attendance.JobId)
            ?? throw new BusinessRuleException("JobNotFound", "Job not found.");

        if (job.CompanyId != companyId)
            throw new BusinessRuleException("Unauthorized", "You can only mark no-show for your own jobs.");

        // 3. Marcar como no-show
        var previousStatus = attendance.Status;
        attendance.MarkAsNoShow();
        await _attendanceRepo.UpdateAsync(attendance);

        // 4. Audit log
        await _audit.LogAsync("Attendance", attendance.Id,
            previousStatus.ToString(), AttendanceStatus.NoShow.ToString(),
            "NO_SHOW", "Professional marked as no-show by company.",
            companyId, null);

        return AttendanceResponse.FromEntity(attendance);
    }

    public async Task<AttendanceResponse> GetByIdAsync(Guid attendanceId)
    {
        var attendance = await _attendanceRepo.GetByIdAsync(attendanceId)
            ?? throw new BusinessRuleException("AttendanceNotFound", "Attendance not found.");

        return AttendanceResponse.FromEntity(attendance);
    }

    public async Task<List<AttendanceResponse>> GetByJobAsync(Guid jobId, Guid requesterId)
    {
        // Verificar se o requester tem acesso (company owner ou professional approved)
        var job = await _jobRepo.GetByIdWithCompanyAsync(jobId)
            ?? throw new BusinessRuleException("JobNotFound", "Job not found.");

        var isCompany = job.CompanyId == requesterId;
        var isProfessional = await _applicationRepo.GetByJobAndWorkerAsync(jobId, requesterId) != null;

        if (!isCompany && !isProfessional)
            throw new BusinessRuleException("Unauthorized", "You don't have access to this job's attendances.");

        var attendances = await _attendanceRepo.GetByJobIdAsync(jobId);
        return attendances.Select(AttendanceResponse.FromEntity).ToList();
    }

    public async Task<List<AttendanceResponse>> GetMyAttendancesAsync(Guid professionalId)
    {
        // Buscar todas as applications do profissional
        var applications = await _applicationRepo.GetByWorkerAsync(professionalId);
        var jobIds = applications.Select(a => a.JobId).ToList();

        var allAttendances = new List<Attendance>();
        foreach (var jobId in jobIds)
        {
            var attendance = await _attendanceRepo.GetByJobAndProfessionalAsync(jobId, professionalId);
            if (attendance != null)
                allAttendances.Add(attendance);
        }

        return allAttendances.Select(AttendanceResponse.FromEntity).ToList();
    }
}
