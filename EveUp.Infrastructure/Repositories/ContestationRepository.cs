using EveUp.Core.Entities;
using EveUp.Core.Interfaces;
using EveUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EveUp.Infrastructure.Repositories;

public class ContestationRepository : IContestationRepository
{
    private readonly EveUpDbContext _context;

    public ContestationRepository(EveUpDbContext context) => _context = context;

    public async Task<Contestation?> GetByIdAsync(Guid id) =>
        await _context.Contestations
            .Include(c => c.Contestant)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<List<Contestation>> GetByDenunciationIdAsync(Guid denunciationId) =>
        await _context.Contestations
            .Include(c => c.Contestant)
            .Where(c => c.DenunciationId == denunciationId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(Contestation contestation)
    {
        await _context.Contestations.AddAsync(contestation);
        await _context.SaveChangesAsync();
    }
}
