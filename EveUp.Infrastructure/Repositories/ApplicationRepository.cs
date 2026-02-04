using EveUp.Core.Entities;
using EveUp.Core.Enums;
using EveUp.Core.Interfaces;
using EveUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EveUp.Infrastructure.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly EveUpDbContext _context;

    public ApplicationRepository(EveUpDbContext context)
    {
        _context = context;
    }

    public async Task<Application?> GetByIdAsync(Guid id)
    {
        return await _context.Applications
            .Include(a => a.Worker)
            .Include(a => a.Job)
            .ThenInclude(j => j.Company)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Application?> GetByIdWithJobAsync(Guid id)
    {
        return await _context.Applications
            .Include(a => a.Job)
            .ThenInclude(j => j.Company)
            .Include(a => a.Worker)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Application?> GetByJobAndWorkerAsync(Guid jobId, Guid workerId)
    {
        return await _context.Applications
            .Include(a => a.Worker)
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.JobId == jobId && a.WorkerId == workerId);
    }

    public async Task<List<Application>> GetByJobAsync(Guid jobId)
    {
        return await _context.Applications
            .Include(a => a.Worker)
            .Where(a => a.JobId == jobId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Application>> GetByWorkerAsync(Guid workerId, string? state = null)
    {
        var query = _context.Applications
            .Include(a => a.Job)
            .ThenInclude(j => j.Company)
            .Where(a => a.WorkerId == workerId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(state) && Enum.TryParse<ApplicationState>(state, true, out var appState))
            query = query.Where(a => a.State == appState);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid jobId, Guid workerId)
    {
        return await _context.Applications
            .AnyAsync(a => a.JobId == jobId && a.WorkerId == workerId);
    }

    public async Task AddAsync(Application application)
    {
        await _context.Applications.AddAsync(application);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Application application)
    {
        _context.Applications.Update(application);
        await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        var transaction = _context.Database.CurrentTransaction;
        if (transaction != null)
            await transaction.CommitAsync();
    }

    public async Task RollbackTransactionAsync()
    {
        var transaction = _context.Database.CurrentTransaction;
        if (transaction != null)
            await transaction.RollbackAsync();
    }
}
