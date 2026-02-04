using EveUp.Core.Entities;
using EveUp.Core.Enums;

namespace EveUp.Core.Interfaces;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid id);
    Task<Job?> GetByIdWithCompanyAsync(Guid id);
    Task<(List<Job> items, int totalCount)> ListAsync(int page, int pageSize, string? city = null, string? eventType = null, string? skills = null);
    Task<(List<Job> items, int totalCount)> ListByCompanyAsync(Guid companyId, int page, int pageSize, string? state = null);
    Task AddAsync(Job job);
    Task UpdateAsync(Job job);
}
