using EveUp.Core.Entities;
using EveUp.Core.Enums;
using EveUp.Core.Exceptions;
using EveUp.Core.Interfaces;
using EveUp.Services.StateMachines;

namespace EveUp.Services;

public class ApplicationService : IApplicationService
{
    private readonly IApplicationRepository _appRepo;
    private readonly IJobRepository _jobRepo;
    private readonly IUserRepository _userRepo;
    private readonly IAuditService _audit;

    public ApplicationService(
        IApplicationRepository appRepo,
        IJobRepository jobRepo,
        IUserRepository userRepo,
        IAuditService audit)
    {
        _appRepo = appRepo;
        _jobRepo = jobRepo;
        _userRepo = userRepo;
        _audit = audit;
    }

    public async Task<Application> ApplyAsync(Guid jobId, Guid workerId)
    {
        var worker = await _userRepo.GetByIdAsync(workerId)
            ?? throw new BusinessRuleException("UserNotFound", "Worker not found.");

        if (worker.Type != UserType.WORKER)
            throw new BusinessRuleException("NotAWorker", "Only workers can apply to jobs.");

        if (worker.State != UserState.ACTIVE)
            throw new BusinessRuleException("UserNotActive", "Your account must be active to apply.");

        var job = await _jobRepo.GetByIdAsync(jobId)
            ?? throw new BusinessRuleException("JobNotFound", "Job not found.");

        if (job.State != JobState.PUBLISHED && job.State != JobState.MATCHING)
            throw new BusinessRuleException("JobNotAcceptingApplications", "This job is not accepting applications.");

        if (await _appRepo.ExistsAsync(jobId, workerId))
            throw new BusinessRuleException("AlreadyApplied", "You have already applied to this job.");

        var application = Application.Create(jobId, workerId);
        await _appRepo.AddAsync(application);

        await _audit.LogAsync("Application", application.Id,
            null, ApplicationState.PENDING.ToString(),
            "CREATED", $"Worker {workerId} applied to job {jobId}.",
            workerId, null);

        return application;
    }

    public async Task<Application> WithdrawAsync(Guid applicationId, Guid workerId)
    {
        var application = await _appRepo.GetByIdAsync(applicationId)
            ?? throw new BusinessRuleException("ApplicationNotFound", "Application not found.");

        if (application.WorkerId != workerId)
            throw new BusinessRuleException("Unauthorized", "You can only withdraw your own applications.");

        var previousState = application.State;
        ApplicationStateMachine.Validate(previousState, ApplicationState.WITHDRAWN);

        application.UpdateState(ApplicationState.WITHDRAWN);
        await _appRepo.UpdateAsync(application);

        await _audit.LogAsync("Application", application.Id,
            previousState.ToString(), ApplicationState.WITHDRAWN.ToString(),
            "STATE_CHANGE", "Application withdrawn by worker.",
            workerId, null);

        return application;
    }

    public async Task<Application> ApproveAsync(Guid applicationId, Guid companyId)
    {
        var application = await _appRepo.GetByIdWithJobAsync(applicationId)
            ?? throw new BusinessRuleException("ApplicationNotFound", "Application not found.");

        if (application.Job.CompanyId != companyId)
            throw new BusinessRuleException("Unauthorized", "You can only approve applications for your own jobs.");

        var previousState = application.State;
        ApplicationStateMachine.Validate(previousState, ApplicationState.APPROVED);

        // Usar transaction para garantir consistência entre application e job
        await _appRepo.BeginTransactionAsync();
        try
        {
            application.UpdateState(ApplicationState.APPROVED);
            await _appRepo.UpdateAsync(application);

            // Increment confirmed workers on the job
            var job = application.Job;
            job.IncrementConfirmedWorkers();

            // Se todas as vagas foram preenchidas, avançar vaga no fluxo
            // e notificar outros candidatos pendentes
            if (job.WorkersConfirmed >= job.WorkersNeeded)
            {
                // Transicionar via state machine: PUBLISHED/MATCHING -> CONFIRMED -> AWAITING_PAYMENT
                if (job.State == JobState.PUBLISHED || job.State == JobState.MATCHING)
                {
                    JobStateMachine.Validate(job.State, JobState.CONFIRMED);
                    job.UpdateState(JobState.CONFIRMED);
                }
                if (job.State == JobState.CONFIRMED)
                {
                    JobStateMachine.Validate(job.State, JobState.AWAITING_PAYMENT);
                    job.UpdateState(JobState.AWAITING_PAYMENT);
                }

                // Marcar outros candidatos pendentes como POSITION_FILLED
                var pendingApplications = await _appRepo.GetByJobAsync(job.Id);
                foreach (var pendingApp in pendingApplications)
                {
                    if (pendingApp.Id != applicationId && pendingApp.State == ApplicationState.PENDING)
                    {
                        ApplicationStateMachine.Validate(pendingApp.State, ApplicationState.POSITION_FILLED);
                        pendingApp.UpdateState(ApplicationState.POSITION_FILLED);
                        await _appRepo.UpdateAsync(pendingApp);

                        await _audit.LogAsync("Application", pendingApp.Id,
                            ApplicationState.PENDING.ToString(), ApplicationState.POSITION_FILLED.ToString(),
                            "STATE_CHANGE", $"Position filled - all vacancies taken for job {job.Id}.",
                            companyId, null);
                    }
                }
            }

            await _jobRepo.UpdateAsync(job);

            await _audit.LogAsync("Application", application.Id,
                previousState.ToString(), ApplicationState.APPROVED.ToString(),
                "STATE_CHANGE", $"Application approved by company {companyId}.",
                companyId, null);

            await _appRepo.CommitTransactionAsync();
        }
        catch
        {
            await _appRepo.RollbackTransactionAsync();
            throw;
        }

        return application;
    }

    public async Task<Application> RejectAsync(Guid applicationId, Guid companyId)
    {
        var application = await _appRepo.GetByIdWithJobAsync(applicationId)
            ?? throw new BusinessRuleException("ApplicationNotFound", "Application not found.");

        if (application.Job.CompanyId != companyId)
            throw new BusinessRuleException("Unauthorized", "You can only reject applications for your own jobs.");

        var previousState = application.State;
        ApplicationStateMachine.Validate(previousState, ApplicationState.REJECTED);

        application.UpdateState(ApplicationState.REJECTED);
        await _appRepo.UpdateAsync(application);

        await _audit.LogAsync("Application", application.Id,
            previousState.ToString(), ApplicationState.REJECTED.ToString(),
            "STATE_CHANGE", $"Application rejected by company {companyId}.",
            companyId, null);

        return application;
    }

    public async Task<List<Application>> GetByJobAsync(Guid jobId)
    {
        return await _appRepo.GetByJobAsync(jobId);
    }

    public async Task<List<Application>> GetByWorkerAsync(Guid workerId, string? state = null)
    {
        return await _appRepo.GetByWorkerAsync(workerId, state);
    }

    public async Task<Application> GetByIdAsync(Guid applicationId)
    {
        return await _appRepo.GetByIdAsync(applicationId)
            ?? throw new BusinessRuleException("ApplicationNotFound", "Application not found.");
    }
}
