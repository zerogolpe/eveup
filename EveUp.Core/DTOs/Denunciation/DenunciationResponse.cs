using EveUp.Core.Enums;

namespace EveUp.Core.DTOs.Denunciation;

public class DenunciationResponse
{
    public Guid Id { get; set; }
    public Guid TargetId { get; set; }
    public string? TargetName { get; set; }
    public Guid? JobId { get; set; }
    public string? JobTitle { get; set; }
    public string Description { get; set; } = string.Empty;
    public DenunciationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ContestationDeadline { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? Resolution { get; set; }

    public static DenunciationResponse FromEntity(Core.Entities.Denunciation d, Guid requesterId)
    {
        return new DenunciationResponse
        {
            Id = d.Id,
            TargetId = d.TargetId,
            TargetName = d.Target?.Name,
            JobId = d.JobId,
            JobTitle = d.Job?.Title,
            Description = d.Description,
            Status = d.Status,
            CreatedAt = d.CreatedAt,
            ContestationDeadline = d.ContestationDeadline,
            ResolvedAt = d.ResolvedAt,
            Resolution = d.Resolution
        };
    }
}

public class CreateDenunciationRequest
{
    public Guid TargetId { get; set; }
    public Guid? JobId { get; set; }
    public string Description { get; set; } = string.Empty;
}
