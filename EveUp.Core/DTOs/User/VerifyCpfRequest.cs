using System.ComponentModel.DataAnnotations;

namespace EveUp.Core.DTOs.User;

public class VerifyCpfRequest
{
    [Required]
    [StringLength(11, MinimumLength = 11)]
    public string Cpf { get; set; } = string.Empty;
}
