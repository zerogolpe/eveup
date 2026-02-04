using EveUp.Core.Entities;

namespace EveUp.Core.Interfaces;

public interface IApplicationRepository
{
    Task<Application?> GetByIdAsync(Guid id);
    Task<Application?> GetByIdWithJobAsync(Guid id);
    Task<Application?> GetByJobAndWorkerAsync(Guid jobId, Guid workerId);
    Task<List<Application>> GetByJobAsync(Guid jobId);
    Task<List<Application>> GetByWorkerAsync(Guid workerId, string? state = null);
    Task<bool> ExistsAsync(Guid jobId, Guid workerId);
    Task AddAsync(Application application);
    Task UpdateAsync(Application application);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
