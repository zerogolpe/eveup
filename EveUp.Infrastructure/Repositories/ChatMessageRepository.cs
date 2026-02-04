using EveUp.Core.Entities;
using EveUp.Core.Interfaces;
using EveUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EveUp.Infrastructure.Repositories;

public class ChatMessageRepository : IChatMessageRepository
{
    private readonly EveUpDbContext _context;

    public ChatMessageRepository(EveUpDbContext context) => _context = context;

    public async Task<ChatMessage?> GetByIdAsync(Guid id) =>
        await _context.ChatMessages
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == id);

    public async Task<(List<ChatMessage> items, int totalCount)> GetByConversationIdAsync(Guid conversationId, int page, int pageSize)
    {
        var query = _context.ChatMessages
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId)
    {
        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation == null) return 0;

        return await _context.ChatMessages
            .Where(m => m.ConversationId == conversationId &&
                       m.SenderId != userId &&
                       !m.IsRead)
            .CountAsync();
    }

    public async Task AddAsync(ChatMessage message)
    {
        await _context.ChatMessages.AddAsync(message);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ChatMessage message)
    {
        _context.ChatMessages.Update(message);
        await _context.SaveChangesAsync();
    }

    public async Task MarkAsReadAsync(Guid conversationId, Guid userId)
    {
        var messages = await _context.ChatMessages
            .Where(m => m.ConversationId == conversationId &&
                       m.SenderId != userId &&
                       !m.IsRead)
            .ToListAsync();

        foreach (var msg in messages)
            msg.MarkAsRead();

        await _context.SaveChangesAsync();
    }
}
