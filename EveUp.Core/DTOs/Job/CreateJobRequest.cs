using System.ComponentModel.DataAnnotations;

namespace EveUp.Core.DTOs.Job;

public class CreateJobRequest
{
    [Required]
    [MinLength(5)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MinLength(10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string EventType { get; set; } = string.Empty;

    [Required]
    public string RequiredSkills { get; set; } = string.Empty; // JSON array

    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    public DateTime EventDate { get; set; }

    [Required]
    [Range(30, 1440)]
    public int EventDurationMinutes { get; set; }

    [Required]
    [Range(1, 100)]
    public int WorkersNeeded { get; set; }

    [Required]
    [Range(50, 5000)]
    public decimal PaymentPerWorker { get; set; }
}
