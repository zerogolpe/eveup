using EveUp.Core.DTOs.Job;

namespace EveUp.Core.Interfaces;

public interface IJobService
{
    Task<JobListResponse> ListJobsAsync(int page, int pageSize, string? city = null, string? eventType = null, string? skills = null);
    Task<JobListResponse> ListMyJobsAsync(Guid companyId, int page, int pageSize, string? state = null);
    Task<JobResponse> GetByIdAsync(Guid jobId);
    Task<JobResponse> CreateAsync(Guid companyId, CreateJobRequest request);
    Task<JobResponse> UpdateAsync(Guid jobId, Guid companyId, CreateJobRequest request);
    Task<JobResponse> PublishAsync(Guid jobId, Guid companyId);
    Task<JobResponse> CancelAsync(Guid jobId, Guid companyId, string? reason = null);
    Task<JobResponse> ConfirmCompletionAsync(Guid jobId, Guid companyId);
}
