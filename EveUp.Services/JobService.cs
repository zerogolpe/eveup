using EveUp.Core.DTOs.Job;
using EveUp.Core.Entities;
using EveUp.Core.Enums;
using EveUp.Core.Exceptions;
using EveUp.Core.Interfaces;
using EveUp.Services.StateMachines;

namespace EveUp.Services;

public class JobService : IJobService
{
    private readonly IJobRepository _jobRepo;
    private readonly IUserRepository _userRepo;
    private readonly IAuditService _audit;

    public JobService(IJobRepository jobRepo, IUserRepository userRepo, IAuditService audit)
    {
        _jobRepo = jobRepo;
        _userRepo = userRepo;
        _audit = audit;
    }

    public async Task<JobListResponse> ListJobsAsync(
        int page, int pageSize, string? city = null, string? eventType = null, string? skills = null)
    {
        var (items, totalCount) = await _jobRepo.ListAsync(page, pageSize, city, eventType, skills);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new JobListResponse
        {
            Data = items.Select(JobResponse.FromEntity).ToList(),
            Meta = new PaginationMeta
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                TotalPages = totalPages
            }
        };
    }

    public async Task<JobListResponse> ListMyJobsAsync(Guid companyId, int page, int pageSize, string? state = null)
    {
        var (items, totalCount) = await _jobRepo.ListByCompanyAsync(companyId, page, pageSize, state);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new JobListResponse
        {
            Data = items.Select(JobResponse.FromEntity).ToList(),
            Meta = new PaginationMeta
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                TotalPages = totalPages
            }
        };
    }

    public async Task<JobResponse> GetByIdAsync(Guid jobId)
    {
        var job = await _jobRepo.GetByIdWithCompanyAsync(jobId)
            ?? throw new BusinessRuleException("JobNotFound", "Job not found.");
        return JobResponse.FromEntity(job);
    }

    public async Task<JobResponse> CreateAsync(Guid companyId, CreateJobRequest request)
    {
        var company = await _userRepo.GetByIdAsync(companyId)
            ?? throw new BusinessRuleException("UserNotFound", "Company not found.");

        if (company.Type != UserType.COMPANY)
            throw new BusinessRuleException("NotACompany", "Only companies can create jobs.");

        var eventDateUtc = request.EventDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(request.EventDate, DateTimeKind.Utc)
            : request.EventDate.ToUniversalTime();

        if (eventDateUtc <= DateTime.UtcNow)
            throw new BusinessRuleException("InvalidEventDate", "Event date must be in the future.");

        // Converter EventDurationMinutes para StartTime/EndTime
        var startTime = eventDateUtc.TimeOfDay;
        var endTime = startTime.Add(TimeSpan.FromMinutes(request.EventDurationMinutes));

        var job = Job.Create(
            companyId,
            request.Title,
            request.Description,
            request.EventType,
            request.RequiredSkills,
            request.City,
            request.Address,
            eventDateUtc.Date, // Apenas a data
            startTime,
            endTime,
            request.WorkersNeeded,
            request.PaymentPerWorker); // GrossFee = PaymentPerWorker para compatibilidade

        await _jobRepo.AddAsync(job);

        await _audit.LogAsync("Job", job.Id,
            null, JobState.DRAFT.ToString(),
            "CREATED", "Job created.",
            companyId, null);

        return JobResponse.FromEntity(job);
    }

    public async Task<JobResponse> UpdateAsync(Guid jobId, Guid companyId, CreateJobRequest request)
    {
        var job = await _jobRepo.GetByIdWithCompanyAsync(jobId)
            ?? throw new BusinessRuleException("JobNotFound", "Job not found.");

        if (job.CompanyId != companyId)
            throw new BusinessRuleException("Unauthorized", "You can only edit your own jobs.");

        if (job.State != JobState.DRAFT)
            throw new BusinessRuleException("JobNotEditable", "Only draft jobs can be edited.");

        var eventDateUtc = request.EventDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(request.EventDate, DateTimeKind.Utc)
            : request.EventDate.ToUniversalTime();

        // Converter EventDurationMinutes para StartTime/EndTime
        var startTime = eventDateUtc.TimeOfDay;
        var endTime = startTime.Add(TimeSpan.FromMinutes(request.EventDurationMinutes));

        job.Update(
            request.Title,
            request.Description,
            request.EventType,
            request.RequiredSkills,
            request.City,
            request.Address,
            eventDateUtc.Date, // Apenas a data
            startTime,
            endTime,
            request.WorkersNeeded,
            request.PaymentPerWorker); // GrossFee = PaymentPerWorker para compatibilidade

        await _jobRepo.UpdateAsync(job);

        await _audit.LogAsync("Job", job.Id,
            null, null,
            "UPDATED", "Job updated.",
            companyId, null);

        return JobResponse.FromEntity(job);
    }

    public async Task<JobResponse> PublishAsync(Guid jobId, Guid companyId)
    {
        var job = await _jobRepo.GetByIdWithCompanyAsync(jobId)
            ?? throw new BusinessRuleException("JobNotFound", "Job not found.");

        if (job.CompanyId != companyId)
            throw new BusinessRuleException("Unauthorized", "You can only publish your own jobs.");

        var previousState = job.State;
        JobStateMachine.Validate(previousState, JobState.PUBLISHED);

        job.UpdateState(JobState.PUBLISHED);
        await _jobRepo.UpdateAsync(job);

        await _audit.LogAsync("Job", job.Id,
            previousState.ToString(), JobState.PUBLISHED.ToString(),
            "STATE_CHANGE", "Job published.",
            companyId, null);

        return JobResponse.FromEntity(job);
    }

    public async Task<JobResponse> CancelAsync(Guid jobId, Guid companyId, string? reason = null)
    {
        var job = await _jobRepo.GetByIdWithCompanyAsync(jobId)
            ?? throw new BusinessRuleException("JobNotFound", "Job not found.");

        if (job.CompanyId != companyId)
            throw new BusinessRuleException("Unauthorized", "You can only cancel your own jobs.");

        var previousState = job.State;

        // Determine the target cancel state based on current state
        var targetState = previousState >= JobState.CONFIRMED
            ? JobState.CANCELLED_AFTER_MATCH
            : JobState.CANCELLED;

        JobStateMachine.Validate(previousState, targetState);

        if (!string.IsNullOrEmpty(reason))
            job.SetCancellationReason(reason);

        job.UpdateState(targetState);
        await _jobRepo.UpdateAsync(job);

        await _audit.LogAsync("Job", job.Id,
            previousState.ToString(), targetState.ToString(),
            "STATE_CHANGE", $"Job cancelled. Reason: {reason ?? "N/A"}",
            companyId, null);

        return JobResponse.FromEntity(job);
    }

    public async Task<JobResponse> ConfirmCompletionAsync(Guid jobId, Guid companyId)
    {
        var job = await _jobRepo.GetByIdWithCompanyAsync(jobId)
            ?? throw new BusinessRuleException("JobNotFound", "Job not found.");

        if (job.CompanyId != companyId)
            throw new BusinessRuleException("Unauthorized", "You can only confirm your own jobs.");

        var previousState = job.State;
        JobStateMachine.Validate(previousState, JobState.COMPLETED);

        job.UpdateState(JobState.COMPLETED);
        await _jobRepo.UpdateAsync(job);

        await _audit.LogAsync("Job", job.Id,
            previousState.ToString(), JobState.COMPLETED.ToString(),
            "STATE_CHANGE", "Job completed by company.",
            companyId, null);

        return JobResponse.FromEntity(job);
    }
}
