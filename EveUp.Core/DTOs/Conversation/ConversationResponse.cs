using EveUp.Core.Entities;

namespace EveUp.Core.DTOs.Conversation;

public class ConversationResponse
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string? JobTitle { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public Guid ProfessionalId { get; set; }
    public string? ProfessionalName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public static ConversationResponse FromEntity(Core.Entities.Conversation conversation)
    {
        return new ConversationResponse
        {
            Id = conversation.Id,
            JobId = conversation.JobId,
            JobTitle = conversation.Job?.Title,
            CompanyId = conversation.CompanyId,
            CompanyName = conversation.Company?.Name,
            ProfessionalId = conversation.ProfessionalId,
            ProfessionalName = conversation.Professional?.Name,
            IsActive = conversation.IsActive,
            CreatedAt = conversation.CreatedAt
        };
    }
}
