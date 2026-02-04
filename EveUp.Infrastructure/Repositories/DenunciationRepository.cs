using EveUp.Core.Entities;
using EveUp.Core.Enums;
using EveUp.Core.Interfaces;
using EveUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EveUp.Infrastructure.Repositories;

public class DenunciationRepository : IDenunciationRepository
{
    private readonly EveUpDbContext _context;

    public DenunciationRepository(EveUpDbContext context) => _context = context;

    public async Task<Denunciation?> GetByIdAsync(Guid id) =>
        await _context.Denunciations.FindAsync(id);

    public async Task<Denunciation?> GetByIdWithRelationsAsync(Guid id) =>
        await _context.Denunciations
            .Include(d => d.Target)
            .Include(d => d.Job)
            .Include(d => d.Contestations)
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task<(List<Denunciation> items, int totalCount)> ListByUserIdAsync(Guid userId, int page, int pageSize)
    {
        var query = _context.Denunciations
            .Include(d => d.Target)
            .Include(d => d.Job)
            .Where(d => d.InitiatorId == userId || d.TargetId == userId);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<Denunciation> items, int totalCount)> ListByStatusAsync(DenunciationStatus status, int page, int pageSize)
    {
        // SECURITY: Never include Initiator to prevent identity leak
        var query = _context.Denunciations
            .Include(d => d.Target)
            .Include(d => d.Job)
            .Where(d => d.Status == status);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<List<Denunciation>> GetExpiredContestationDeadlinesAsync() =>
        await _context.Denunciations
            .Where(d => d.Status == DenunciationStatus.Open &&
                       d.ContestationDeadline < DateTime.UtcNow)
            .Take(1000)  // Limite de segurança para background job
            .ToListAsync();

    public async Task AddAsync(Denunciation denunciation)
    {
        await _context.Denunciations.AddAsync(denunciation);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Denunciation denunciation)
    {
        _context.Denunciations.Update(denunciation);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException(
                "The denunciation was modified by another process. Please reload and try again.",
                ex);
        }
    }
}
