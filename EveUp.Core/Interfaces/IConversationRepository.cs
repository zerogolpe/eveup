using EveUp.Core.Entities;

namespace EveUp.Core.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id);
    Task<Conversation?> GetByJobIdAsync(Guid jobId);
    Task<Conversation?> GetByJobAndParticipantsAsync(Guid jobId, Guid companyId, Guid professionalId);
    Task<List<Conversation>> GetByUserAsync(Guid userId);
    Task<List<ChatMessage>> GetMessagesAsync(Guid conversationId, int limit = 100);
    Task AddAsync(Conversation conversation);
    Task AddMessageAsync(ChatMessage message);
    Task UpdateAsync(Conversation conversation);
}
