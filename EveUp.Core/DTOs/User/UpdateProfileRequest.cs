namespace EveUp.Core.DTOs.User;

public class UpdateProfileRequest
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Skills { get; set; }       // JSON array
    public string? Availability { get; set; } // JSON array
    public string? CompanyName { get; set; }
    public string? Cnpj { get; set; }
}
