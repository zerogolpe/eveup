using EveUp.Core.Entities;
using EveUp.Core.Enums;
using EveUp.Core.Exceptions;
using EveUp.Core.Interfaces;
using EveUp.Services.StateMachines;

namespace EveUp.Services;

public class DisputeService : IDisputeService
{
    private readonly IDisputeRepository _disputeRepo;
    private readonly IJobRepository _jobRepo;
    private readonly IAuditService _audit;

    public DisputeService(IDisputeRepository disputeRepo, IJobRepository jobRepo, IAuditService audit)
    {
        _disputeRepo = disputeRepo;
        _jobRepo = jobRepo;
        _audit = audit;
    }

    public async Task<Dispute> OpenAsync(Guid jobId, Guid userId, DisputeType type, string description)
    {
        var job = await _jobRepo.GetByIdAsync(jobId)
            ?? throw new BusinessRuleException("JobNotFound", "Job not found.");

        if (job.State != JobState.IN_PROGRESS && job.State != JobState.COMPLETED)
            throw new BusinessRuleException("JobNotDisputable", "Job cannot be disputed in its current state.");

        var dispute = Dispute.Create(jobId, userId, type, description);
        await _disputeRepo.AddAsync(dispute);

        // Transition job to DISPUTED
        if (job.State != JobState.DISPUTED)
        {
            var jobPrev = job.State;
            JobStateMachine.Validate(jobPrev, JobState.DISPUTED);
            job.UpdateState(JobState.DISPUTED);
            await _jobRepo.UpdateAsync(job);

            await _audit.LogAsync("Job", job.Id,
                jobPrev.ToString(), JobState.DISPUTED.ToString(),
                "STATE_CHANGE", $"Job disputed by user {userId}.",
                userId, null);
        }

        await _audit.LogAsync("Dispute", dispute.Id,
            null, DisputeState.OPENED.ToString(),
            "CREATED", $"Dispute opened: {type}. {description}",
            userId, null);

        return dispute;
    }

    public async Task<Dispute> GetByIdAsync(Guid disputeId)
    {
        return await _disputeRepo.GetByIdAsync(disputeId)
            ?? throw new BusinessRuleException("DisputeNotFound", "Dispute not found.");
    }

    public async Task<List<Dispute>> GetByJobAsync(Guid jobId)
    {
        return await _disputeRepo.GetByJobAsync(jobId);
    }

    public async Task<List<Dispute>> GetByUserAsync(Guid userId)
    {
        return await _disputeRepo.GetByUserAsync(userId);
    }

    public async Task<Dispute> AddEvidenceAsync(Guid disputeId, Guid userId, string evidenceJson)
    {
        var dispute = await _disputeRepo.GetByIdAsync(disputeId)
            ?? throw new BusinessRuleException("DisputeNotFound", "Dispute not found.");

        // Verificar se o usuário é participante da disputa
        if (dispute.OpenedByUserId != userId && dispute.Job?.CompanyId != userId)
            throw new BusinessRuleException("Unauthorized", "You are not a participant in this dispute.");

        if (dispute.State != DisputeState.EVIDENCE_REQUESTED && dispute.State != DisputeState.OPENED)
            throw new BusinessRuleException("EvidenceNotAccepted", "Evidence cannot be submitted in the current state.");

        dispute.AddEvidence(evidenceJson);

        // If evidence was requested, transition to EVIDENCE_RECEIVED
        if (dispute.State == DisputeState.EVIDENCE_REQUESTED)
        {
            var previousState = dispute.State;
            DisputeStateMachine.Validate(previousState, DisputeState.EVIDENCE_RECEIVED);
            dispute.UpdateState(DisputeState.EVIDENCE_RECEIVED);

            await _audit.LogAsync("Dispute", dispute.Id,
                previousState.ToString(), DisputeState.EVIDENCE_RECEIVED.ToString(),
                "STATE_CHANGE", "Evidence received.",
                userId, null);
        }

        await _disputeRepo.UpdateAsync(dispute);

        await _audit.LogAsync("Dispute", dispute.Id,
            null, null,
            "EVIDENCE_ADDED", "Evidence submitted.",
            userId, null);

        return dispute;
    }

    public async Task<Dispute> ResolveAsync(
        Guid disputeId,
        DisputeState resolution,
        string details,
        decimal? refundAmount = null,
        decimal? workerPayout = null)
    {
        var dispute = await _disputeRepo.GetByIdAsync(disputeId)
            ?? throw new BusinessRuleException("DisputeNotFound", "Dispute not found.");

        // Must be in UNDER_REVIEW to resolve
        if (dispute.State != DisputeState.UNDER_REVIEW)
        {
            // Try to move to UNDER_REVIEW first if in a valid state
            if (dispute.State == DisputeState.EVIDENCE_RECEIVED)
            {
                DisputeStateMachine.Validate(dispute.State, DisputeState.UNDER_REVIEW);
                dispute.UpdateState(DisputeState.UNDER_REVIEW);
            }
            else if (dispute.State == DisputeState.OPENED)
            {
                DisputeStateMachine.Validate(dispute.State, DisputeState.UNDER_REVIEW);
                dispute.UpdateState(DisputeState.UNDER_REVIEW);
            }
            else
            {
                throw new BusinessRuleException("DisputeNotResolvable", "Dispute cannot be resolved in its current state.");
            }
        }

        var previousState = dispute.State;
        DisputeStateMachine.Validate(previousState, resolution);

        dispute.SetResolution(details, refundAmount, workerPayout);
        dispute.UpdateState(resolution);
        await _disputeRepo.UpdateAsync(dispute);

        await _audit.LogAsync("Dispute", dispute.Id,
            previousState.ToString(), resolution.ToString(),
            "STATE_CHANGE", $"Dispute resolved: {resolution}. {details}",
            null, null);

        // Transition job to RESOLVED
        var job = await _jobRepo.GetByIdAsync(dispute.JobId);
        if (job != null && job.State == JobState.DISPUTED)
        {
            var jobPrev = job.State;
            JobStateMachine.Validate(jobPrev, JobState.RESOLVED);
            job.UpdateState(JobState.RESOLVED);
            await _jobRepo.UpdateAsync(job);

            await _audit.LogAsync("Job", job.Id,
                jobPrev.ToString(), JobState.RESOLVED.ToString(),
                "STATE_CHANGE", $"Job resolved after dispute {disputeId}.",
                null, null);
        }

        return dispute;
    }
}
