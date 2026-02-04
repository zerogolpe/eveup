using EveUp.Core.Enums;
using EveUp.Core.Interfaces;
using EveUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EveUp.Infrastructure.Jobs;

/// <summary>
/// Background job that checks for expired/timed-out jobs and payments.
/// Runs periodically via Hangfire.
///
/// - Published jobs past EventDate → EXPIRED
/// - Payments stuck in PROCESSING for > 30min → FAILED
/// - Jobs in AWAITING_PAYMENT for > 24h → CANCELLED
/// </summary>
public class TimeoutCheckerJob
{
    private readonly EveUpDbContext _context;
    private readonly IAuditService _audit;
    private readonly ILogger<TimeoutCheckerJob> _logger;

    private static readonly TimeSpan PaymentProcessingTimeout = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan AwaitingPaymentTimeout = TimeSpan.FromHours(24);

    public TimeoutCheckerJob(EveUpDbContext context, IAuditService audit, ILogger<TimeoutCheckerJob> logger)
    {
        _context = context;
        _audit = audit;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("[TimeoutChecker] Starting timeout check...");

        await ExpirePublishedJobsAsync();
        await TimeoutStuckPaymentsAsync();
        await CancelUnpaidJobsAsync();

        _logger.LogInformation("[TimeoutChecker] Timeout check completed.");
    }

    /// <summary>
    /// Published jobs where EventDate has passed → EXPIRED
    /// </summary>
    private async Task ExpirePublishedJobsAsync()
    {
        var now = DateTime.UtcNow;
        var expiredJobs = await _context.Jobs
            .Where(j => j.State == JobState.PUBLISHED && j.EventDate < now)
            .ToListAsync();

        foreach (var job in expiredJobs)
        {
            var previousState = job.State;
            job.UpdateState(JobState.EXPIRED);

            await _audit.LogAsync("Job", job.Id,
                previousState.ToString(), JobState.EXPIRED.ToString(),
                "STATE_CHANGE", "Job expired: event date has passed.",
                null, null);

            _logger.LogInformation("[TimeoutChecker] Job {JobId} expired (EventDate: {EventDate})", job.Id, job.EventDate);
        }

        if (expiredJobs.Count > 0)
            await _context.SaveChangesAsync();

        _logger.LogInformation("[TimeoutChecker] Expired {Count} published jobs.", expiredJobs.Count);
    }

    /// <summary>
    /// Payments stuck in PROCESSING for too long → FAILED
    /// </summary>
    private async Task TimeoutStuckPaymentsAsync()
    {
        var cutoff = DateTime.UtcNow - PaymentProcessingTimeout;
        var stuckPayments = await _context.Payments
            .Where(p => p.State == PaymentState.PROCESSING && p.UpdatedAt < cutoff)
            .ToListAsync();

        foreach (var payment in stuckPayments)
        {
            var previousState = payment.State;
            payment.SetFailureReason("Payment processing timed out.");
            payment.IncrementRetry();

            var failState = payment.RetryCount >= 3
                ? PaymentState.FAILED_FINAL
                : PaymentState.FAILED;

            payment.UpdateState(failState);

            await _audit.LogAsync("Payment", payment.Id,
                previousState.ToString(), failState.ToString(),
                "STATE_CHANGE", $"Payment timed out after {PaymentProcessingTimeout.TotalMinutes} minutes.",
                null, null);

            _logger.LogWarning("[TimeoutChecker] Payment {PaymentId} timed out → {State}", payment.Id, failState);
        }

        if (stuckPayments.Count > 0)
            await _context.SaveChangesAsync();

        _logger.LogInformation("[TimeoutChecker] Timed out {Count} stuck payments.", stuckPayments.Count);
    }

    /// <summary>
    /// Jobs in AWAITING_PAYMENT for too long → CANCELLED
    /// </summary>
    private async Task CancelUnpaidJobsAsync()
    {
        var cutoff = DateTime.UtcNow - AwaitingPaymentTimeout;
        var unpaidJobs = await _context.Jobs
            .Where(j => j.State == JobState.AWAITING_PAYMENT && j.UpdatedAt < cutoff)
            .ToListAsync();

        foreach (var job in unpaidJobs)
        {
            var previousState = job.State;
            job.SetCancellationReason("Payment not received within 24 hours.");
            job.UpdateState(JobState.CANCELLED_AFTER_MATCH);

            await _audit.LogAsync("Job", job.Id,
                previousState.ToString(), JobState.CANCELLED_AFTER_MATCH.ToString(),
                "STATE_CHANGE", $"Job cancelled: payment not received within {AwaitingPaymentTimeout.TotalHours} hours.",
                null, null);

            _logger.LogWarning("[TimeoutChecker] Job {JobId} cancelled due to unpaid timeout.", job.Id);
        }

        if (unpaidJobs.Count > 0)
            await _context.SaveChangesAsync();

        _logger.LogInformation("[TimeoutChecker] Cancelled {Count} unpaid jobs.", unpaidJobs.Count);
    }
}
