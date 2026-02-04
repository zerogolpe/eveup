namespace EveUp.Core.Entities;

public sealed class Conversation
{
    public Guid Id { get; private set; }
    public Guid JobId { get; private set; }
    public Job Job { get; private set; } = null!;
    public Guid CompanyId { get; private set; }
    public User Company { get; private set; } = null!;
    public Guid ProfessionalId { get; private set; }
    public User Professional { get; private set; } = null!;

    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public ICollection<ChatMessage> Messages { get; private set; } = new List<ChatMessage>();

    private Conversation() { }

    public static Conversation Create(Guid jobId, Guid companyId, Guid professionalId)
    {
        return new Conversation
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            CompanyId = companyId,
            ProfessionalId = professionalId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Reactivate()
    {
        IsActive = true;
    }

    public bool IsParticipant(Guid userId)
    {
        return CompanyId == userId || ProfessionalId == userId;
    }
}
