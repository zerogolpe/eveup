using EveUp.Core.Enums;

namespace EveUp.Core.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Cpf { get; private set; }
    public string? Cnpj { get; private set; }
    public string? Phone { get; private set; }
    public UserType? Type { get; private set; }
    public UserState State { get; private set; }

    // Para WORKER
    public string? Skills { get; private set; }       // JSON array
    public string? City { get; private set; }
    public string? Availability { get; private set; } // JSON array

    // Para COMPANY
    public string? CompanyName { get; private set; }

    // Reputação
    public int ReputationScore { get; private set; }
    public int TotalReviews { get; private set; }
    public decimal AverageRating { get; private set; }
    public bool HasPendingReview { get; private set; }
    public int MissedReviewCount { get; private set; }

    // Controle e Moderação
    public int Strikes { get; private set; }
    public int WarningCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? SuspendedAt { get; private set; }
    public DateTime? BannedAt { get; private set; }
    public string? BanReason { get; private set; }
    public DateTime? BanExpires { get; private set; }

    // Financeiro
    public string? PayeeId { get; private set; }
    public bool HasPendingPayment { get; private set; }

    // Anti-abuso (interno, nunca expor em API)
    public int DenunciationsMade { get; private set; }
    public int DenunciationsReceived { get; private set; }
    public int DisputesOpened { get; private set; }

    // KYC
    public bool EmailVerified { get; private set; }
    public bool CpfVerified { get; private set; }
    public bool DocumentVerified { get; private set; }
    public bool SelfieVerified { get; private set; }

    // Concurrency control
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    // Navigation
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    private User() { }

    public static User Create(string email, string passwordHash, string name)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            Name = name,
            State = UserState.CREATED,
            ReputationScore = 500,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void VerifyEmail()
    {
        EmailVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCpf(string cpf, bool verified)
    {
        Cpf = cpf;
        CpfVerified = verified;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetType(UserType type)
    {
        Type = type;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateState(UserState newState)
    {
        State = newState;
        UpdatedAt = DateTime.UtcNow;

        if (newState == UserState.SUSPENDED)
            SuspendedAt = DateTime.UtcNow;
        else if (newState == UserState.BANNED)
            BannedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string? phone, string? city, string? skills, string? availability, string? companyName = null, string? cnpj = null)
    {
        Phone = phone ?? Phone;
        City = city ?? City;
        Skills = skills ?? Skills;
        Availability = availability ?? Availability;
        CompanyName = companyName ?? CompanyName;
        Cnpj = cnpj ?? Cnpj;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRating(decimal averageRating, int totalReviews)
    {
        AverageRating = averageRating;
        TotalReviews = totalReviews;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddStrike()
    {
        Strikes++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddWarning()
    {
        WarningCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPendingReview(bool hasPending)
    {
        HasPendingReview = hasPending;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementMissedReview()
    {
        MissedReviewCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Ban(string reason, DateTime? expiresAt = null)
    {
        State = UserState.BANNED;
        BannedAt = DateTime.UtcNow;
        BanReason = reason;
        BanExpires = expiresAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Unban()
    {
        if (State == UserState.BANNED)
        {
            State = UserState.ACTIVE;
            BanReason = null;
            BanExpires = null;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void SetPayeeId(string payeeId)
    {
        PayeeId = payeeId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPendingPayment(bool hasPending)
    {
        HasPendingPayment = hasPending;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementDenunciationsMade()
    {
        DenunciationsMade++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementDenunciationsReceived()
    {
        DenunciationsReceived++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementDisputesOpened()
    {
        DisputesOpened++;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsBanned() => State == UserState.BANNED && (BanExpires == null || BanExpires > DateTime.UtcNow);

    public bool CanCreateOrAcceptJob() => !HasPendingReview && !HasPendingPayment && !IsBanned();
}
