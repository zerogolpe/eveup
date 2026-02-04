using EveUp.Core.DTOs.User;
using EveUp.Core.Enums;
using EveUp.Core.Exceptions;
using EveUp.Core.Interfaces;
using EveUp.Services.StateMachines;

namespace EveUp.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IAuditService _audit;

    public UserService(IUserRepository userRepo, IAuditService audit)
    {
        _userRepo = userRepo;
        _audit = audit;
    }

    public async Task<UserResponse> GetByIdAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new BusinessRuleException("UserNotFound", "User not found.");
        return UserResponse.FromEntity(user);
    }

    public async Task<UserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new BusinessRuleException("UserNotFound", "User not found.");

        user.UpdateProfile(
            request.Phone,
            request.City,
            request.Skills,
            request.Availability,
            request.CompanyName,
            request.Cnpj);

        // If user is PENDING_PROFILE and fills required fields, transition to ACTIVE
        if (user.State == UserState.PENDING_PROFILE)
        {
            var previousState = user.State;
            UserStateMachine.Validate(previousState, UserState.ACTIVE);
            user.UpdateState(UserState.ACTIVE);

            await _audit.LogAsync("User", user.Id,
                previousState.ToString(), UserState.ACTIVE.ToString(),
                "STATE_CHANGE", "Profile completed, user activated.",
                userId, null);
        }

        await _userRepo.UpdateAsync(user);

        await _audit.LogAsync("User", user.Id,
            null, null,
            "PROFILE_UPDATED", "Profile updated.",
            userId, null);

        return UserResponse.FromEntity(user);
    }

    public async Task<UserResponse> SelectRoleAsync(Guid userId, UserType type)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new BusinessRuleException("UserNotFound", "User not found.");

        if (user.Type != null)
            throw new BusinessRuleException("RoleAlreadySelected", "User role has already been selected.");

        user.SetType(type);

        await _userRepo.UpdateAsync(user);

        await _audit.LogAsync("User", user.Id,
            null, null,
            "ROLE_SELECTED", $"User selected role: {type}.",
            userId, null);

        return UserResponse.FromEntity(user);
    }

    public async Task VerifyCpfAsync(Guid userId, string cpf)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new BusinessRuleException("UserNotFound", "User not found.");

        if (user.CpfVerified)
            throw new BusinessRuleException("CpfAlreadyVerified", "CPF has already been verified.");

        // Validate CPF format (11 digits)
        var cleanCpf = cpf.Replace(".", "").Replace("-", "");
        if (cleanCpf.Length != 11 || !cleanCpf.All(char.IsDigit))
            throw new BusinessRuleException("InvalidCpf", "CPF must have 11 digits.");

        user.SetCpf(cleanCpf, true);

        // Transition PENDING_CPF → PENDING_PROFILE
        if (user.State == UserState.PENDING_CPF)
        {
            var previousState = user.State;
            UserStateMachine.Validate(previousState, UserState.PENDING_PROFILE);
            user.UpdateState(UserState.PENDING_PROFILE);

            await _audit.LogAsync("User", user.Id,
                previousState.ToString(), UserState.PENDING_PROFILE.ToString(),
                "STATE_CHANGE", "CPF verified, moved to pending profile.",
                userId, null);
        }

        await _userRepo.UpdateAsync(user);

        await _audit.LogAsync("User", user.Id,
            null, null,
            "CPF_VERIFIED", "CPF verified successfully.",
            userId, null);
    }
}
