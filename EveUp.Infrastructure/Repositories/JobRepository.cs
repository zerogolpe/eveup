using EveUp.Core.Entities;
using EveUp.Core.Enums;
using EveUp.Core.Interfaces;
using EveUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EveUp.Infrastructure.Repositories;

public class JobRepository : IJobRepository
{
    private readonly EveUpDbContext _context;

    public JobRepository(EveUpDbContext context)
    {
        _context = context;
    }

    public async Task<Job?> GetByIdAsync(Guid id)
    {
        return await _context.Jobs.FindAsync(id);
    }

    public async Task<Job?> GetByIdWithCompanyAsync(Guid id)
    {
        return await _context.Jobs
            .Include(j => j.Company)
            .FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task<(List<Job> items, int totalCount)> ListAsync(
        int page, int pageSize, string? city = null, string? eventType = null, string? skills = null)
    {
        var query = _context.Jobs
            .Include(j => j.Company)
            .Where(j => j.State == JobState.PUBLISHED)
            .AsQueryable();

        if (!string.IsNullOrEmpty(city))
            query = query.Where(j => j.City.ToLower().Contains(city.ToLower()));

        if (!string.IsNullOrEmpty(eventType))
            query = query.Where(j => j.EventType.ToLower() == eventType.ToLower());

        if (!string.IsNullOrEmpty(skills))
            query = query.Where(j => j.RequiredSkills.ToLower().Contains(skills.ToLower()));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<Job> items, int totalCount)> ListByCompanyAsync(
        Guid companyId, int page, int pageSize, string? state = null)
    {
        var query = _context.Jobs
            .Include(j => j.Company)
            .Where(j => j.CompanyId == companyId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(state))
        {
            if (state.Equals("active", StringComparison.OrdinalIgnoreCase))
            {
                var activeStates = new[]
                {
                    JobState.PUBLISHED, JobState.MATCHING, JobState.CONFIRMED,
                    JobState.AWAITING_PAYMENT, JobState.PAID, JobState.IN_PROGRESS
                };
                query = query.Where(j => activeStates.Contains(j.State));
            }
            else if (state.Equals("finished", StringComparison.OrdinalIgnoreCase))
            {
                var finishedStates = new[] { JobState.COMPLETED, JobState.SETTLED, JobState.RESOLVED };
                query = query.Where(j => finishedStates.Contains(j.State));
            }
            else if (Enum.TryParse<JobState>(state, true, out var jobState))
            {
                query = query.Where(j => j.State == jobState);
            }
        }

        var totalCount = await query.CountAsync();

        // Ordenação: Ativas/Publicadas primeiro, depois em andamento, concluídas, canceladas por último
        // Dentro de cada grupo, mais recentes primeiro
        var items = await query
            .OrderBy(j => j.State == JobState.CANCELLED || j.State == JobState.CANCELLED_AFTER_MATCH ? 4 :
                          j.State == JobState.COMPLETED || j.State == JobState.SETTLED || j.State == JobState.RESOLVED ? 3 :
                          j.State == JobState.IN_PROGRESS || j.State == JobState.AWAITING_PAYMENT || j.State == JobState.PAID ? 2 :
                          j.State == JobState.PUBLISHED || j.State == JobState.MATCHING || j.State == JobState.CONFIRMED ? 1 : 5)
            .ThenByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(Job job)
    {
        await _context.Jobs.AddAsync(job);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Job job)
    {
        _context.Jobs.Update(job);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException(
                "The job was modified by another process. Please reload and try again.",
                ex);
        }
    }
}
