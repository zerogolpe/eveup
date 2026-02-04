using EveUp.Core.Entities;
using EveUp.Core.Enums;

namespace EveUp.Core.Interfaces;

public interface IDisputeService
{
    Task<Dispute> OpenAsync(Guid jobId, Guid userId, DisputeType type, string description);
    Task<Dispute> GetByIdAsync(Guid disputeId);
    Task<List<Dispute>> GetByJobAsync(Guid jobId);
    Task<List<Dispute>> GetByUserAsync(Guid userId);
    Task<Dispute> AddEvidenceAsync(Guid disputeId, Guid userId, string evidenceJson);
    Task<Dispute> ResolveAsync(Guid disputeId, DisputeState resolution, string details, decimal? refundAmount = null, decimal? workerPayout = null);
}
