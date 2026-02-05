using System.ComponentModel.DataAnnotations;

namespace EveUp.Core.DTOs.Auth;

public class ResendVerificationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
