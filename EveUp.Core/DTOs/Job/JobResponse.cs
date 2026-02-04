using EveUp.Core.Enums;

namespace EveUp.Core.DTOs.Job;

public class JobResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string RequiredSkills { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }
    public int EventDurationMinutes { get; set; }
    public int WorkersNeeded { get; set; }
    public int WorkersConfirmed { get; set; }
    public decimal PaymentPerWorker { get; set; }
    public decimal TotalAmount { get; set; }

    public JobState State { get; set; }
    public DateTime CreatedAt { get; set; }

    public static JobResponse FromEntity(Entities.Job job)
    {
        return new JobResponse
        {
            Id = job.Id,
            CompanyId = job.CompanyId,
            CompanyName = job.Company?.Name ?? string.Empty,
            Title = job.Title,
            Description = job.Description,
            EventType = job.EventType,
            RequiredSkills = job.RequiredSkills,
            City = job.City,
            Address = job.Address,
            EventDate = job.EventDate,
            EventDurationMinutes = job.EventDurationMinutes,
            WorkersNeeded = job.WorkersNeeded,
            WorkersConfirmed = job.WorkersConfirmed,
            PaymentPerWorker = job.PaymentPerWorker,
            TotalAmount = job.TotalAmount,
            State = job.State,
            CreatedAt = job.CreatedAt
        };
    }
}
