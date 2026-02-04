using EveUp.Core.DTOs.Denunciation;
using EveUp.Core.Entities;
using EveUp.Core.Exceptions;
using EveUp.Core.Interfaces;

namespace EveUp.Services;

public class DenunciationService : IDenunciationService
{
    private readonly IDenunciationRepository _denunciationRepo;
    private readonly IUserRepository _userRepo;
    private readonly IAuditService _audit;

    public DenunciationService(
        IDenunciationRepository denunciationRepo,
        IUserRepository userRepo,
        IAuditService audit)
    {
        _denunciationRepo = denunciationRepo;
        _userRepo = userRepo;
        _audit = audit;
    }

    public async Task<DenunciationResponse> CreateAsync(
        Guid initiatorId, Guid targetId, string description, Guid? jobId = null)
    {
        if (initiatorId == targetId)
            throw new BusinessRuleException("InvalidTarget", "Você não pode denunciar a si mesmo.");

        // Verificar que o alvo existe
        var target = await _userRepo.GetByIdAsync(targetId)
            ?? throw new BusinessRuleException("UserNotFound", "Usuário não encontrado.");

        // Criar denúncia
        var denunciation = Denunciation.Create(initiatorId, targetId, description, jobId);
        await _denunciationRepo.AddAsync(denunciation);

        // Incrementar contadores
        var initiator = await _userRepo.GetByIdAsync(initiatorId);
        initiator?.IncrementDenunciationsMade();
        if (initiator != null) await _userRepo.UpdateAsync(initiator);

        target.IncrementDenunciationsReceived();
        await _userRepo.UpdateAsync(target);

        // Auditoria
        await _audit.LogAsync("Denunciation", denunciation.Id,
            null, "Open",
            "CREATED", "Denunciation created.",
            initiatorId, null);

        return new DenunciationResponse
        {
            Id = denunciation.Id,
            TargetId = denunciation.TargetId,
            TargetName = target.Name,
            JobId = denunciation.JobId,
            Description = denunciation.Description,
            Status = denunciation.Status,
            CreatedAt = denunciation.CreatedAt,
            ContestationDeadline = denunciation.ContestationDeadline,
        };
    }

    public async Task<(List<DenunciationResponse> items, int totalCount)> GetMyDenunciationsAsync(
        Guid userId, int page = 1, int pageSize = 20)
    {
        var (items, totalCount) = await _denunciationRepo.ListByUserIdAsync(userId, page, pageSize);

        var responses = items.Select(d => DenunciationResponse.FromEntity(d, userId)).ToList();
        return (responses, totalCount);
    }
}
