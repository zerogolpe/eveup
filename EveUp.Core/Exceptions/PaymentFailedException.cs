namespace EveUp.Core.Exceptions;

public class PaymentFailedException : Exception
{
    public string? TransactionId { get; }
    public string? FailureCode { get; }

    public PaymentFailedException(string message, string? transactionId = null, string? failureCode = null)
        : base(message)
    {
        TransactionId = transactionId;
        FailureCode = failureCode;
    }
}
