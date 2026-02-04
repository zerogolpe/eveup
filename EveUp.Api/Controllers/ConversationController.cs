using System.Security.Claims;
using EveUp.Core.DTOs.Conversation;
using EveUp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EveUp.Api.Controllers;

[ApiController]
[Route("api/conversations")]
[Authorize]
public class ConversationController : ControllerBase
{
    private readonly IConversationService _conversationService;

    public ConversationController(IConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    /// <summary>
    /// Cria ou busca conversa existente entre empresa e profissional para um job
    /// </summary>
    [HttpPost("job/{jobId:guid}/professional/{professionalId:guid}")]
    public async Task<ActionResult<ConversationResponse>> GetOrCreate(
        Guid jobId, Guid professionalId)
    {
        var companyId = GetUserId();
        var result = await _conversationService.GetOrCreateAsync(jobId, companyId, professionalId);
        return Ok(result);
    }

    /// <summary>
    /// Lista minhas conversas
    /// </summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<ConversationResponse>>> GetMyConversations()
    {
        var userId = GetUserId();
        var result = await _conversationService.GetMyConversationsAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// Retorna detalhes de uma conversa
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConversationResponse>> GetById(Guid id)
    {
        var userId = GetUserId();
        var result = await _conversationService.GetByIdAsync(id, userId);
        return Ok(result);
    }

    /// <summary>
    /// Envia mensagem em uma conversa
    /// </summary>
    [HttpPost("{id:guid}/messages")]
    public async Task<ActionResult<MessageResponse>> SendMessage(
        Guid id, [FromBody] SendMessageRequest request)
    {
        var senderId = GetUserId();
        var result = await _conversationService.SendMessageAsync(id, senderId, request.Content, request.AttachmentUrl);
        return Created($"/api/conversations/{id}/messages/{result.Id}", result);
    }

    /// <summary>
    /// Lista mensagens de uma conversa
    /// </summary>
    [HttpGet("{id:guid}/messages")]
    public async Task<ActionResult<List<MessageResponse>>> GetMessages(
        Guid id, [FromQuery] int limit = 100)
    {
        var userId = GetUserId();
        var result = await _conversationService.GetMessagesAsync(id, userId, limit);
        return Ok(result);
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim?.Value ?? throw new UnauthorizedAccessException());
    }
}
