using EveUp.Core.DTOs.Payment;
using EveUp.Core.Entities;
using EveUp.Core.Enums;
using EveUp.Core.Exceptions;
using EveUp.Core.Interfaces;
using EveUp.Services.StateMachines;

namespace EveUp.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepo;
    private readonly IJobRepository _jobRepo;
    private readonly IPspProvider _psp;
    private readonly IAuditService _audit;

    private const decimal PlatformFeePercent = 15m;
    private const int MaxRetries = 3;

    public PaymentService(
        IPaymentRepository paymentRepo,
        IJobRepository jobRepo,
        IPspProvider psp,
        IAuditService audit)
    {
        _paymentRepo = paymentRepo;
        _jobRepo = jobRepo;
        _psp = psp;
        _audit = audit;
    }

    public async Task<PaymentResponse> CreatePaymentAsync(Guid companyId, CreatePaymentRequest request)
    {
        var job = await _jobRepo.GetByIdAsync(request.JobId)
            ?? throw new BusinessRuleException("JobNotFound", "Job not found.");

        if (job.CompanyId != companyId)
            throw new BusinessRuleException("Unauthorized", "You can only pay for your own jobs.");

        if (job.State != JobState.AWAITING_PAYMENT)
            throw new BusinessRuleException("JobNotAwaitingPayment", "Job is not awaiting payment.");

        // Idempotency check
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var existing = await _paymentRepo.GetByIdempotencyKeyAsync(request.IdempotencyKey);
            if (existing != null)
                return PaymentResponse.FromEntity(existing);
        }

        var payment = Payment.Create(job.Id, companyId, job.TotalAmount, PlatformFeePercent, request.IdempotencyKey);
        payment.SetPaymentMethod(request.PaymentMethod);

        await _paymentRepo.AddAsync(payment);

        await _audit.LogAsync("Payment", payment.Id,
            null, PaymentState.CREATED.ToString(),
            "CREATED", $"Payment created for job {job.Id}. Amount: {payment.GrossAmount}",
            companyId, null);

        return PaymentResponse.FromEntity(payment);
    }

    public async Task<PaymentResponse> ProcessPaymentAsync(Guid paymentId)
    {
        var payment = await _paymentRepo.GetByIdAsync(paymentId)
            ?? throw new BusinessRuleException("PaymentNotFound", "Payment not found.");

        var previousState = payment.State;
        PaymentStateMachine.Validate(previousState, PaymentState.PROCESSING);

        payment.UpdateState(PaymentState.PROCESSING);
        await _paymentRepo.UpdateAsync(payment);

        await _audit.LogAsync("Payment", payment.Id,
            previousState.ToString(), PaymentState.PROCESSING.ToString(),
            "STATE_CHANGE", "Payment processing started.",
            null, null);

        // Call PSP
        var result = await _psp.CreateChargeAsync(
            payment.GrossAmount,
            payment.PaymentMethod ?? "credit_card",
            payment.IdempotencyKey ?? payment.Id.ToString());

        if (result.Success)
        {
            payment.SetTransactionId(result.TransactionId!);
            payment.SetPspResponse(result.RawResponse ?? "{}");

            // PROCESSING → HELD (money held in escrow)
            PaymentStateMachine.Validate(PaymentState.PROCESSING, PaymentState.HELD);
            payment.UpdateState(PaymentState.HELD);

            await _paymentRepo.UpdateAsync(payment);

            await _audit.LogAsync("Payment", payment.Id,
                PaymentState.PROCESSING.ToString(), PaymentState.HELD.ToString(),
                "STATE_CHANGE", $"Payment held. TransactionId: {result.TransactionId}",
                null, null);

            // Update job state to PAID
            var job = payment.Job ?? await _jobRepo.GetByIdAsync(payment.JobId);
            if (job != null && job.State == JobState.AWAITING_PAYMENT)
            {
                var jobPrev = job.State;
                job.UpdateState(JobState.PAID);
                await _jobRepo.UpdateAsync(job);

                await _audit.LogAsync("Job", job.Id,
                    jobPrev.ToString(), JobState.PAID.ToString(),
                    "STATE_CHANGE", "Job paid via payment processing.",
                    null, null);
            }
        }
        else
        {
            payment.SetPspResponse(result.RawResponse ?? "{}");
            payment.SetFailureReason(result.ErrorMessage ?? "Unknown error");
            payment.IncrementRetry();

            var failState = payment.RetryCount >= MaxRetries
                ? PaymentState.FAILED_FINAL
                : PaymentState.FAILED;

            PaymentStateMachine.Validate(PaymentState.PROCESSING, failState);
            payment.UpdateState(failState);
            await _paymentRepo.UpdateAsync(payment);

            await _audit.LogAsync("Payment", payment.Id,
                PaymentState.PROCESSING.ToString(), failState.ToString(),
                "STATE_CHANGE", $"Payment failed: {result.ErrorMessage}. Retry: {payment.RetryCount}/{MaxRetries}",
                null, null);
        }

        return PaymentResponse.FromEntity(payment);
    }

    public async Task<PaymentResponse> GetByIdAsync(Guid paymentId)
    {
        var payment = await _paymentRepo.GetByIdAsync(paymentId)
            ?? throw new BusinessRuleException("PaymentNotFound", "Payment not found.");
        return PaymentResponse.FromEntity(payment);
    }

    public async Task<PaymentResponse> GetByJobIdAsync(Guid jobId)
    {
        var payment = await _paymentRepo.GetByJobIdAsync(jobId)
            ?? throw new BusinessRuleException("PaymentNotFound", "Payment not found for this job.");
        return PaymentResponse.FromEntity(payment);
    }

    public async Task<List<PaymentResponse>> ListByCompanyAsync(Guid companyId)
    {
        var payments = await _paymentRepo.ListByCompanyAsync(companyId);
        return payments.Select(PaymentResponse.FromEntity).ToList();
    }

    /// <summary>
    /// Called by PSP webhook when payment is confirmed (charge captured).
    /// </summary>
    public async Task ConfirmPaymentAsync(string transactionId)
    {
        var payment = await _paymentRepo.GetByTransactionIdAsync(transactionId)
            ?? throw new BusinessRuleException("PaymentNotFound", "Payment not found for transaction.");

        // If already held, nothing to do
        if (payment.State == PaymentState.HELD)
            return;

        var previousState = payment.State;
        PaymentStateMachine.Validate(previousState, PaymentState.HELD);

        payment.UpdateState(PaymentState.HELD);
        await _paymentRepo.UpdateAsync(payment);

        await _audit.LogAsync("Payment", payment.Id,
            previousState.ToString(), PaymentState.HELD.ToString(),
            "STATE_CHANGE", $"Payment confirmed via webhook. TransactionId: {transactionId}",
            null, null);
    }

    /// <summary>
    /// Called by PSP webhook when payment fails.
    /// </summary>
    public async Task FailPaymentAsync(string transactionId, string? reason)
    {
        var payment = await _paymentRepo.GetByTransactionIdAsync(transactionId)
            ?? throw new BusinessRuleException("PaymentNotFound", "Payment not found for transaction.");

        var previousState = payment.State;
        payment.IncrementRetry();

        var failState = payment.RetryCount >= MaxRetries
            ? PaymentState.FAILED_FINAL
            : PaymentState.FAILED;

        PaymentStateMachine.Validate(previousState, failState);

        if (!string.IsNullOrEmpty(reason))
            payment.SetFailureReason(reason);

        payment.UpdateState(failState);
        await _paymentRepo.UpdateAsync(payment);

        await _audit.LogAsync("Payment", payment.Id,
            previousState.ToString(), failState.ToString(),
            "STATE_CHANGE", $"Payment failed via webhook: {reason}. Retry: {payment.RetryCount}/{MaxRetries}",
            null, null);
    }

    /// <summary>
    /// Called by PSP webhook when refund is confirmed.
    /// </summary>
    public async Task ConfirmRefundAsync(string transactionId)
    {
        var payment = await _paymentRepo.GetByTransactionIdAsync(transactionId)
            ?? throw new BusinessRuleException("PaymentNotFound", "Payment not found for transaction.");

        var previousState = payment.State;
        PaymentStateMachine.Validate(previousState, PaymentState.REFUNDED);

        payment.UpdateState(PaymentState.REFUNDED);
        await _paymentRepo.UpdateAsync(payment);

        await _audit.LogAsync("Payment", payment.Id,
            previousState.ToString(), PaymentState.REFUNDED.ToString(),
            "STATE_CHANGE", $"Refund confirmed via webhook. TransactionId: {transactionId}",
            null, null);
    }

    /// <summary>
    /// Called by PSP webhook on chargeback — freeze the payment.
    /// </summary>
    public async Task HandleChargebackAsync(string transactionId)
    {
        var payment = await _paymentRepo.GetByTransactionIdAsync(transactionId)
            ?? throw new BusinessRuleException("PaymentNotFound", "Payment not found for transaction.");

        var previousState = payment.State;
        PaymentStateMachine.Validate(previousState, PaymentState.FROZEN);

        payment.UpdateState(PaymentState.FROZEN);
        await _paymentRepo.UpdateAsync(payment);

        await _audit.LogAsync("Payment", payment.Id,
            previousState.ToString(), PaymentState.FROZEN.ToString(),
            "STATE_CHANGE", $"Payment frozen due to chargeback. TransactionId: {transactionId}",
            null, null);
    }
}
