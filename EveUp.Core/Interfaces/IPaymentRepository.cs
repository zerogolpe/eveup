using EveUp.Core.Entities;

namespace EveUp.Core.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id);
    Task<Payment?> GetByJobIdAsync(Guid jobId);
    Task<Payment?> GetByTransactionIdAsync(string transactionId);
    Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey);
    Task<List<Payment>> ListByCompanyAsync(Guid companyId);
    Task AddAsync(Payment payment);
    Task UpdateAsync(Payment payment);
}
