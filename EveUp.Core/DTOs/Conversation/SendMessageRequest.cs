using System.ComponentModel.DataAnnotations;

namespace EveUp.Core.DTOs.Conversation;

public class SendMessageRequest
{
    [MaxLength(2000, ErrorMessage = "Message cannot exceed 2000 characters")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// URL do arquivo anexado (retornado pelo upload endpoint)
    /// </summary>
    public string? AttachmentUrl { get; set; }

    /// <summary>
    /// Tipo da mensagem: text, image, audio, video, document
    /// </summary>
    public string MessageType { get; set; } = "text";
}
