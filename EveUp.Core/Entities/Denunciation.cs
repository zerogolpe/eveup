using EveUp.Core.Enums;

namespace EveUp.Core.Entities;

public sealed class Denunciation
{
    public Guid Id { get; private set; }
    public Guid InitiatorId { get; private set; }  // INTERNAL ONLY - NUNCA expor em API pública
    public Guid TargetId { get; private set; }  // Quem está sendo denunciado
    public User Target { get; private set; } = null!;
    public Guid? JobId { get; private set; }  // Vaga relacionada (opcional)
    public Job? Job { get; private set; }

    public string Description { get; private set; } = string.Empty;
    public string? AttachmentUrls { get; private set; }  // JSON array de URLs (máx 10)

    public DenunciationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime ContestationDeadline { get; private set; }  // CreatedAt + 48h
    public DateTime? ResolvedAt { get; private set; }
    public string? Resolution { get; private set; }
    public Guid? ResolvedByAdminId { get; private set; }

    // Concurrency control
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    // Navigation
    public ICollection<Contestation> Contestations { get; private set; } = new List<Contestation>();

    private Denunciation() { }

    public static Denunciation Create(Guid initiatorId, Guid targetId, string description,
        Guid? jobId = null, string? attachmentUrls = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required", nameof(description));

        if (description.Length > 500)
            throw new ArgumentException("Description must be 500 characters or less", nameof(description));

        if (initiatorId == targetId)
            throw new ArgumentException("Cannot denounce yourself");

        return new Denunciation
        {
            Id = Guid.NewGuid(),
            InitiatorId = initiatorId,
            TargetId = targetId,
            JobId = jobId,
            Description = description,
            AttachmentUrls = attachmentUrls,
            Status = DenunciationStatus.Open,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ContestationDeadline = DateTime.UtcNow.AddHours(48)
        };
    }

    public void UpdateStatus(DenunciationStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;

        if (newStatus == DenunciationStatus.ResolvedProInitiator ||
            newStatus == DenunciationStatus.ResolvedProTarget ||
            newStatus == DenunciationStatus.Dismissed)
        {
            ResolvedAt = DateTime.UtcNow;
        }
    }

    public void Resolve(DenunciationStatus resolution, string resolutionText, Guid adminId)
    {
        if (resolution != DenunciationStatus.ResolvedProInitiator &&
            resolution != DenunciationStatus.ResolvedProTarget &&
            resolution != DenunciationStatus.Dismissed)
        {
            throw new ArgumentException("Invalid resolution status");
        }

        Status = resolution;
        Resolution = resolutionText;
        ResolvedByAdminId = adminId;
        ResolvedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanBeContested()
    {
        return Status == DenunciationStatus.Open && DateTime.UtcNow <= ContestationDeadline;
    }

    public bool IsContestationDeadlinePassed()
    {
        return DateTime.UtcNow > ContestationDeadline;
    }

    /// <summary>
    /// Verifica se o usuário é participante da denúncia (pode ver, mas InitiatorId continua oculto)
    /// </summary>
    public bool IsParticipant(Guid userId)
    {
        return TargetId == userId || InitiatorId == userId;
    }

    /// <summary>
    /// Verifica se o usuário é o denunciado (para autorização de contestação)
    /// </summary>
    public bool IsTarget(Guid userId)
    {
        return TargetId == userId;
    }
}
