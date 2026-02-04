using EveUp.Core.Entities;

namespace EveUp.Core.Interfaces;

public interface IChatMessageRepository
{
    Task<ChatMessage?> GetByIdAsync(Guid id);
    Task<(List<ChatMessage> items, int totalCount)> GetByConversationIdAsync(Guid conversationId, int page, int pageSize);
    Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId);
    Task AddAsync(ChatMessage message);
    Task UpdateAsync(ChatMessage message);
    Task MarkAsReadAsync(Guid conversationId, Guid userId);
}
