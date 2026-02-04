using EveUp.Core.Entities;
using EveUp.Core.Interfaces;
using EveUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EveUp.Infrastructure.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly EveUpDbContext _context;

    public ConversationRepository(EveUpDbContext context) => _context = context;

    public async Task<Conversation?> GetByIdAsync(Guid id) =>
        await _context.Conversations
            .Include(c => c.Company)
            .Include(c => c.Professional)
            .Include(c => c.Job)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Conversation?> GetByJobIdAsync(Guid jobId) =>
        await _context.Conversations
            .Include(c => c.Company)
            .Include(c => c.Professional)
            .FirstOrDefaultAsync(c => c.JobId == jobId);

    public async Task<Conversation?> GetByJobAndParticipantsAsync(
        Guid jobId, Guid companyId, Guid professionalId) =>
        await _context.Conversations
            .Include(c => c.Company)
            .Include(c => c.Professional)
            .Include(c => c.Job)
            .FirstOrDefaultAsync(c => c.JobId == jobId &&
                                     c.CompanyId == companyId &&
                                     c.ProfessionalId == professionalId);

    public async Task<List<Conversation>> GetByUserAsync(Guid userId) =>
        await _context.Conversations
            .Include(c => c.Job)
            .Include(c => c.Company)
            .Include(c => c.Professional)
            .Where(c => c.CompanyId == userId || c.ProfessionalId == userId)
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .Take(1000)  // Limite de segurança
            .ToListAsync();

    public async Task<List<ChatMessage>> GetMessagesAsync(Guid conversationId, int limit = 100) =>
        await _context.ChatMessages
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .ToListAsync();

    public async Task AddAsync(Conversation conversation)
    {
        await _context.Conversations.AddAsync(conversation);
        await _context.SaveChangesAsync();
    }

    public async Task AddMessageAsync(ChatMessage message)
    {
        await _context.ChatMessages.AddAsync(message);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Conversation conversation)
    {
        _context.Conversations.Update(conversation);
        await _context.SaveChangesAsync();
    }
}
