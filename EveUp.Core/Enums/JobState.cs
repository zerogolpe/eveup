namespace EveUp.Core.Enums;

public enum JobState
{
    DRAFT = 0,
    PUBLISHED = 1,
    MATCHING = 2,
    CONFIRMED = 3,
    AWAITING_PAYMENT = 4,
    PAID = 5,
    IN_PROGRESS = 6,
    COMPLETED = 7,
    DISPUTED = 8,
    SETTLED = 9,
    RESOLVED = 10,
    CANCELLED = 11,
    EXPIRED = 12,
    NO_MATCH = 13,
    CANCELLED_AFTER_MATCH = 14
}
