using EveUp.Core.DTOs.Conversation;
using EveUp.Core.Entities;
using EveUp.Core.Exceptions;
using EveUp.Core.Interfaces;

namespace EveUp.Services;

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversationRepo;
    private readonly IJobRepository _jobRepo;
    private readonly IUserRepository _userRepo;
    private readonly IAuditService _audit;

    public ConversationService(
        IConversationRepository conversationRepo,
        IJobRepository jobRepo,
        IUserRepository userRepo,
        IAuditService audit)
    {
        _conversationRepo = conversationRepo;
        _jobRepo = jobRepo;
        _userRepo = userRepo;
        _audit = audit;
    }

    public async Task<ConversationResponse> GetOrCreateAsync(
        Guid jobId, Guid callerId, Guid professionalId)
    {
        // Verificar se job existe
        var job = await _jobRepo.GetByIdAsync(jobId)
            ?? throw new BusinessRuleException("JobNotFound", "Job not found.");

        // Determinar se o chamador é a empresa ou o profissional
        Guid companyId;
        Guid actualProfessionalId;

        if (job.CompanyId == callerId)
        {
            // Chamador é a empresa
            companyId = callerId;
            actualProfessionalId = professionalId;
        }
        else if (callerId == professionalId)
        {
            // Chamador é o próprio profissional — só pode buscar existente
            companyId = job.CompanyId;
            actualProfessionalId = callerId;
        }
        else
        {
            throw new BusinessRuleException("Unauthorized", "You are not authorized for this conversation.");
        }

        // Buscar conversa existente
        var existingConversation = await _conversationRepo.GetByJobAndParticipantsAsync(
            jobId, companyId, actualProfessionalId);

        if (existingConversation != null)
            return ConversationResponse.FromEntity(existingConversation);

        // Apenas a empresa pode criar novas conversas
        if (job.CompanyId != callerId)
            throw new BusinessRuleException("Unauthorized", "Only the company can start a new conversation.");

        // Criar nova conversa
        var conversation = Conversation.Create(jobId, companyId, actualProfessionalId);
        await _conversationRepo.AddAsync(conversation);

        await _audit.LogAsync("Conversation", conversation.Id,
            null, "ACTIVE",
            "CREATED", "Conversation created.",
            companyId, null);

        // Recarregar com navigation properties para resposta completa
        var created = await _conversationRepo.GetByIdAsync(conversation.Id);
        return ConversationResponse.FromEntity(created ?? conversation);
    }

    public async Task<ConversationResponse> GetByIdAsync(Guid conversationId, Guid requesterId)
    {
        var conversation = await _conversationRepo.GetByIdAsync(conversationId)
            ?? throw new BusinessRuleException("ConversationNotFound", "Conversation not found.");

        // Verificar que o usuário é participante
        if (!conversation.IsParticipant(requesterId))
            throw new BusinessRuleException("Unauthorized", "You are not a participant of this conversation.");

        return ConversationResponse.FromEntity(conversation);
    }

    public async Task<List<ConversationResponse>> GetMyConversationsAsync(Guid userId)
    {
        var conversations = await _conversationRepo.GetByUserAsync(userId);
        return conversations.Select(ConversationResponse.FromEntity).ToList();
    }

    public async Task<MessageResponse> SendMessageAsync(
        Guid conversationId, Guid senderId, string content, string? attachmentUrl = null)
    {
        // Validar conversa
        var conversation = await _conversationRepo.GetByIdAsync(conversationId)
            ?? throw new BusinessRuleException("ConversationNotFound", "Conversation not found.");

        // Verificar que o usuário é participante
        if (!conversation.IsParticipant(senderId))
            throw new BusinessRuleException("Unauthorized", "You are not a participant of this conversation.");

        // Verificar que a conversa está ativa
        if (!conversation.IsActive)
            throw new BusinessRuleException("ConversationInactive", "This conversation is no longer active.");

        // Criar mensagem
        var message = ChatMessage.Create(conversationId, senderId, content, attachmentUrl);
        await _conversationRepo.AddMessageAsync(message);

        await _audit.LogAsync("ChatMessage", message.Id,
            null, null,
            "MESSAGE_SENT", $"Message sent in conversation {conversationId}.",
            senderId, null);

        return MessageResponse.FromEntity(message);
    }

    public async Task<List<MessageResponse>> GetMessagesAsync(
        Guid conversationId, Guid requesterId, int limit = 100)
    {
        // Verificar acesso
        var conversation = await _conversationRepo.GetByIdAsync(conversationId)
            ?? throw new BusinessRuleException("ConversationNotFound", "Conversation not found.");

        if (!conversation.IsParticipant(requesterId))
            throw new BusinessRuleException("Unauthorized", "You are not a participant of this conversation.");

        // Buscar mensagens
        var messages = await _conversationRepo.GetMessagesAsync(conversationId, limit);
        return messages.Select(MessageResponse.FromEntity).ToList();
    }
}
