using EveUp.Core.Entities;

namespace EveUp.Core.DTOs.Conversation;

public class MessageResponse
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string? SenderName { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public string MessageType { get; set; } = "text";
    public DateTime SentAt { get; set; }

    public static MessageResponse FromEntity(ChatMessage message)
    {
        // Inferir tipo da mensagem a partir das URLs de anexo
        var attachmentUrl = message.AttachmentUrls;
        var messageType = "text";

        if (!string.IsNullOrEmpty(attachmentUrl))
        {
            var lower = attachmentUrl.ToLowerInvariant();
            if (lower.Contains(".jpg") || lower.Contains(".jpeg") || lower.Contains(".png") || lower.Contains(".gif") || lower.Contains(".webp"))
                messageType = "image";
            else if (lower.Contains(".mp4") || lower.Contains(".mov"))
                messageType = "video";
            else if (lower.Contains(".mp3") || lower.Contains(".m4a") || lower.Contains(".wav") || lower.Contains(".ogg"))
                messageType = "audio";
            else if (lower.Contains(".pdf") || lower.Contains(".doc") || lower.Contains(".docx"))
                messageType = "document";
            else
                messageType = "file";
        }

        return new MessageResponse
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderName = message.Sender?.Name,
            Content = message.Content,
            AttachmentUrl = message.AttachmentUrls,
            MessageType = messageType,
            SentAt = message.SentAt
        };
    }
}
