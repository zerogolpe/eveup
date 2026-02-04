using EveUp.Core.DTOs.Payment;

namespace EveUp.Core.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponse> CreatePaymentAsync(Guid companyId, CreatePaymentRequest request);
    Task<PaymentResponse> ProcessPaymentAsync(Guid paymentId);
    Task<PaymentResponse> GetByIdAsync(Guid paymentId);
    Task<PaymentResponse> GetByJobIdAsync(Guid jobId);
    Task<List<PaymentResponse>> ListByCompanyAsync(Guid companyId);
    Task ConfirmPaymentAsync(string transactionId);
    Task FailPaymentAsync(string transactionId, string? reason);
    Task ConfirmRefundAsync(string transactionId);
    Task HandleChargebackAsync(string transactionId);
}
