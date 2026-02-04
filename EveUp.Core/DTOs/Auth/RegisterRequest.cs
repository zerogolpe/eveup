using System.ComponentModel.DataAnnotations;

namespace EveUp.Core.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MinLength(3)]
    public string Name { get; set; } = string.Empty;
}
