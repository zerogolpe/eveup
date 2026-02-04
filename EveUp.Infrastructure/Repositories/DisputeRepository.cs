using EveUp.Core.Entities;
using EveUp.Core.Interfaces;
using EveUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EveUp.Infrastructure.Repositories;

public class DisputeRepository : IDisputeRepository
{
    private readonly EveUpDbContext _context;

    public DisputeRepository(EveUpDbContext context)
    {
        _context = context;
    }

    public async Task<Dispute?> GetByIdAsync(Guid id)
    {
        return await _context.Disputes
            .Include(d => d.Job)
            .Include(d => d.OpenedByUser)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<List<Dispute>> GetByJobAsync(Guid jobId)
    {
        return await _context.Disputes
            .Include(d => d.OpenedByUser)
            .Where(d => d.JobId == jobId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Dispute>> GetByUserAsync(Guid userId)
    {
        return await _context.Disputes
            .Include(d => d.Job)
            .Include(d => d.OpenedByUser)
            .Where(d => d.OpenedByUserId == userId || d.Job.CompanyId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Dispute dispute)
    {
        await _context.Disputes.AddAsync(dispute);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Dispute dispute)
    {
        _context.Disputes.Update(dispute);
        await _context.SaveChangesAsync();
    }
}
