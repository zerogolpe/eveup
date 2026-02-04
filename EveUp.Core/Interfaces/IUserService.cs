using EveUp.Core.DTOs.User;
using EveUp.Core.Enums;

namespace EveUp.Core.Interfaces;

public interface IUserService
{
    Task<UserResponse> GetByIdAsync(Guid userId);
    Task<UserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<UserResponse> SelectRoleAsync(Guid userId, UserType type);
    Task VerifyCpfAsync(Guid userId, string cpf);
}
