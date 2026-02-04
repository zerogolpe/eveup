using System.ComponentModel.DataAnnotations;
using EveUp.Core.Enums;

namespace EveUp.Core.DTOs.User;

public class SelectRoleRequest
{
    [Required]
    public UserType Type { get; set; }
}
