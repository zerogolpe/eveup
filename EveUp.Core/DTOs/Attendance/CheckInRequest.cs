using System.ComponentModel.DataAnnotations;

namespace EveUp.Core.DTOs.Attendance;

public class CheckInRequest
{
    [Required(ErrorMessage = "JobId is required")]
    public Guid JobId { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
