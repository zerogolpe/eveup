using EveUp.Core.DTOs.Conversation;

namespace EveUp.Core.Interfaces;

public interface IConversationService
{
    Task<ConversationResponse> GetOrCreateAsync(Guid jobId, Guid callerId, Guid professionalId);
    Task<ConversationResponse> GetByIdAsync(Guid conversationId, Guid requesterId);
    Task<List<ConversationResponse>> GetMyConversationsAsync(Guid userId);
    Task<MessageResponse> SendMessageAsync(Guid conversationId, Guid senderId, string content, string? attachmentUrl = null);
    Task<List<MessageResponse>> GetMessagesAsync(Guid conversationId, Guid requesterId, int limit = 100);
}
