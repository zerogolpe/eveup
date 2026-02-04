namespace EveUp.Core.Entities;

public sealed class Contestation
{
    public Guid Id { get; private set; }
    public Guid DenunciationId { get; private set; }
    public Denunciation Denunciation { get; private set; } = null!;
    public Guid ContestantId { get; private set; }  // Quem contesta (o denunciado)
    public User Contestant { get; private set; } = null!;

    public string Description { get; private set; } = string.Empty;
    public string? AttachmentUrls { get; private set; }  // JSON array de URLs (máx 10)

    public DateTime CreatedAt { get; private set; }

    private Contestation() { }

    public static Contestation Create(Guid denunciationId, Guid contestantId, string description, string? attachmentUrls = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required", nameof(description));

        if (description.Length > 500)
            throw new ArgumentException("Description must be 500 characters or less", nameof(description));

        return new Contestation
        {
            Id = Guid.NewGuid(),
            DenunciationId = denunciationId,
            ContestantId = contestantId,
            Description = description,
            AttachmentUrls = attachmentUrls,
            CreatedAt = DateTime.UtcNow
        };
    }
}
