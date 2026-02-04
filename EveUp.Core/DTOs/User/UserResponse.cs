using EveUp.Core.Enums;

namespace EveUp.Core.DTOs.User;

public class UserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public string? Cnpj { get; set; }
    public string? Phone { get; set; }
    public UserType? Type { get; set; }
    public UserState State { get; set; }

    public string? Skills { get; set; }
    public string? City { get; set; }
    public string? Availability { get; set; }
    public string? CompanyName { get; set; }

    public int ReputationScore { get; set; }
    public int TotalReviews { get; set; }
    public decimal AverageRating { get; set; }

    public bool EmailVerified { get; set; }
    public bool CpfVerified { get; set; }
    public bool DocumentVerified { get; set; }

    public DateTime CreatedAt { get; set; }

    public static UserResponse FromEntity(Entities.User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Cpf = user.CpfVerified ? MaskCpf(user.Cpf) : null,
            Cnpj = user.Cnpj,
            Phone = user.Phone,
            Type = user.Type,
            State = user.State,
            Skills = user.Skills,
            City = user.City,
            Availability = user.Availability,
            CompanyName = user.CompanyName,
            ReputationScore = user.ReputationScore,
            TotalReviews = user.TotalReviews,
            AverageRating = user.AverageRating,
            EmailVerified = user.EmailVerified,
            CpfVerified = user.CpfVerified,
            DocumentVerified = user.DocumentVerified,
            CreatedAt = user.CreatedAt
        };
    }

    private static string? MaskCpf(string? cpf)
    {
        if (string.IsNullOrEmpty(cpf) || cpf.Length < 11) return null;
        return $"{cpf[..3]}.***.***.{cpf[9..]}";
    }
}
