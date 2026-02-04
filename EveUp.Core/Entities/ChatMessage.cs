namespace EveUp.Core.Entities;

public sealed class ChatMessage
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public Conversation Conversation { get; private set; } = null!;
    public Guid SenderId { get; private set; }
    public User Sender { get; private set; } = null!;

    public string Content { get; private set; } = string.Empty;
    public string? AttachmentUrls { get; private set; }  // JSON array de URLs
    public bool IsRead { get; private set; }
    public DateTime SentAt { get; private set; }
    public DateTime? ReadAt { get; private set; }

    private ChatMessage() { }

    public static ChatMessage Create(Guid conversationId, Guid senderId, string content, string? attachmentUrls = null)
    {
        if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(attachmentUrls))
            throw new ArgumentException("Message must have content or attachments");

        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content ?? string.Empty,
            AttachmentUrls = attachmentUrls,
            IsRead = false,
            SentAt = DateTime.UtcNow
        };
    }

    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }
    }
}
