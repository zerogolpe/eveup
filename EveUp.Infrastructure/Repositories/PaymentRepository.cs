using EveUp.Core.Entities;
using EveUp.Core.Interfaces;
using EveUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EveUp.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly EveUpDbContext _context;

    public PaymentRepository(EveUpDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(Guid id)
    {
        return await _context.Payments
            .Include(p => p.Job)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Payment?> GetByJobIdAsync(Guid jobId)
    {
        return await _context.Payments
            .Include(p => p.Job)
            .FirstOrDefaultAsync(p => p.JobId == jobId);
    }

    public async Task<Payment?> GetByTransactionIdAsync(string transactionId)
    {
        return await _context.Payments
            .Include(p => p.Job)
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId);
    }

    public async Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey);
    }

    public async Task<List<Payment>> ListByCompanyAsync(Guid companyId)
    {
        return await _context.Payments
            .Include(p => p.Job)
            .Where(p => p.CompanyId == companyId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Payment payment)
    {
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Payment payment)
    {
        _context.Payments.Update(payment);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException(
                "The payment was modified by another process. Please reload and try again.",
                ex);
        }
    }
}
