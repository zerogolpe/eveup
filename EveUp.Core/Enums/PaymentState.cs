namespace EveUp.Core.Enums;

public enum PaymentState
{
    CREATED = 0,
    PROCESSING = 1,
    HELD = 2,
    FROZEN = 3,
    RELEASING = 4,
    RELEASED = 5,
    PARTIALLY_RELEASED = 6,
    REFUNDED = 7,
    FAILED = 8,
    FAILED_FINAL = 9,
    CANCELLED = 10
}
