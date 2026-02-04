using EveUp.Core.Enums;

namespace EveUp.Core.Entities;

public sealed class Job
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public User Company { get; private set; } = null!;

    public string Title { get; private set; } = string.Empty;
    public string EventName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string RequiredSkills { get; private set; } = string.Empty; // JSON array

    // Localização
    public string City { get; private set; } = string.Empty;
    public string? Region { get; private set; }
    public string Address { get; private set; } = string.Empty;
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }

    // Data e Horário
    public DateTime EventDate { get; private set; }
    public TimeSpan StartTime { get; private set; }
    public TimeSpan EndTime { get; private set; }
    public int EventDurationMinutes { get; private set; }
    public DateTime? EventEndTime { get; private set; }

    // Vagas e Pagamento
    public int WorkersNeeded { get; private set; }
    public int WorkersConfirmed { get; private set; }
    public decimal PaymentPerWorker { get; private set; }  // Mantido para compatibilidade
    public decimal GrossFee { get; private set; }  // Valor bruto (= PaymentPerWorker)
    public decimal EveUpFeePercent { get; private set; }
    public decimal EveUpFee { get; private set; }  // Persistido para imutabilidade financeira
    public decimal NetFee { get; private set; }  // Persistido para imutabilidade financeira
    public decimal TotalAmount { get; private set; }

    public JobState State { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }

    // Concurrency control
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    // Navigation
    public ICollection<Application> Applications { get; private set; } = new List<Application>();
    public ICollection<JobBreak> Breaks { get; private set; } = new List<JobBreak>();

    private Job() { }

    public static Job Create(
        Guid companyId,
        string title,
        string description,
        string eventType,
        string requiredSkills,
        string city,
        string address,
        DateTime eventDate,
        TimeSpan startTime,
        TimeSpan endTime,
        int workersNeeded,
        decimal grossFee,
        decimal eveUpFeePercent = 0.10m,
        string? eventName = null,
        string? region = null,
        decimal? latitude = null,
        decimal? longitude = null)
    {
        // SECURITY: Financial validation guards
        if (grossFee < 0)
            throw new ArgumentException("GrossFee cannot be negative", nameof(grossFee));

        if (eveUpFeePercent < 0 || eveUpFeePercent > 1.0m)
            throw new ArgumentException("EveUpFeePercent must be between 0.0 and 1.0 (0% to 100%)", nameof(eveUpFeePercent));

        if (workersNeeded <= 0)
            throw new ArgumentException("WorkersNeeded must be greater than zero", nameof(workersNeeded));

        var eventDurationMinutes = (int)(endTime - startTime).TotalMinutes;

        var eveUpFee = grossFee * eveUpFeePercent;
        var netFee = grossFee - eveUpFee;

        // SECURITY: Ensure NetFee is never negative
        if (netFee < 0)
            throw new InvalidOperationException($"Calculated NetFee cannot be negative. GrossFee: {grossFee}, EveUpFeePercent: {eveUpFeePercent}");

        return new Job
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Title = title,
            EventName = eventName ?? title,
            Description = description,
            EventType = eventType,
            RequiredSkills = requiredSkills,
            City = city,
            Region = region,
            Address = address,
            Latitude = latitude,
            Longitude = longitude,
            EventDate = eventDate,
            StartTime = startTime,
            EndTime = endTime,
            EventDurationMinutes = eventDurationMinutes,
            WorkersNeeded = workersNeeded,
            WorkersConfirmed = 0,
            GrossFee = grossFee,
            PaymentPerWorker = grossFee,  // Compatibilidade
            EveUpFeePercent = eveUpFeePercent,
            EveUpFee = eveUpFee,
            NetFee = netFee,
            TotalAmount = grossFee * workersNeeded,
            State = JobState.DRAFT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateState(JobState newState)
    {
        State = newState;
        UpdatedAt = DateTime.UtcNow;

        if (newState == JobState.PUBLISHED)
            PublishedAt = DateTime.UtcNow;
        else if (newState == JobState.CANCELLED || newState == JobState.CANCELLED_AFTER_MATCH)
            CancelledAt = DateTime.UtcNow;
    }

    public void Update(string title, string description, string eventType, string requiredSkills,
        string city, string address, DateTime eventDate, TimeSpan startTime, TimeSpan endTime,
        int workersNeeded, decimal grossFee, string? eventName = null, string? region = null,
        decimal? latitude = null, decimal? longitude = null)
    {
        // SECURITY: Financial validation guards
        if (grossFee < 0)
            throw new ArgumentException("GrossFee cannot be negative", nameof(grossFee));

        if (workersNeeded <= 0)
            throw new ArgumentException("WorkersNeeded must be greater than zero", nameof(workersNeeded));

        Title = title;
        EventName = eventName ?? title;
        Description = description;
        EventType = eventType;
        RequiredSkills = requiredSkills;
        City = city;
        Region = region;
        Address = address;
        Latitude = latitude;
        Longitude = longitude;
        EventDate = eventDate;
        StartTime = startTime;
        EndTime = endTime;
        EventDurationMinutes = (int)(endTime - startTime).TotalMinutes;
        WorkersNeeded = workersNeeded;
        GrossFee = grossFee;
        PaymentPerWorker = grossFee;  // Compatibilidade
        EveUpFee = grossFee * EveUpFeePercent;
        NetFee = grossFee - EveUpFee;

        // SECURITY: Ensure NetFee is never negative after update
        if (NetFee < 0)
            throw new InvalidOperationException($"Calculated NetFee cannot be negative. GrossFee: {grossFee}, EveUpFeePercent: {EveUpFeePercent}");

        TotalAmount = grossFee * workersNeeded;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementConfirmedWorkers()
    {
        WorkersConfirmed++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCancellationReason(string reason)
    {
        CancellationReason = reason;
    }

    public void AddBreak(JobBreak jobBreak)
    {
        if (Breaks.Count >= 2)
            throw new InvalidOperationException("A job cannot have more than 2 breaks");

        Breaks.Add(jobBreak);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveBreak(JobBreak jobBreak)
    {
        Breaks.Remove(jobBreak);
        UpdatedAt = DateTime.UtcNow;
    }
}
