using EveUp.Core.Entities;

namespace EveUp.Core.Interfaces;

public interface IApplicationService
{
    Task<Application> ApplyAsync(Guid jobId, Guid workerId);
    Task<Application> WithdrawAsync(Guid applicationId, Guid workerId);
    Task<Application> ApproveAsync(Guid applicationId, Guid companyId);
    Task<Application> RejectAsync(Guid applicationId, Guid companyId);
    Task<List<Application>> GetByJobAsync(Guid jobId);
    Task<List<Application>> GetByWorkerAsync(Guid workerId, string? state = null);
    Task<Application> GetByIdAsync(Guid applicationId);
}
