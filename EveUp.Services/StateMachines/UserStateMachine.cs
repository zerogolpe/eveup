using EveUp.Core.Enums;
using EveUp.Core.Exceptions;

namespace EveUp.Services.StateMachines;

public static class UserStateMachine
{
    private static readonly Dictionary<UserState, HashSet<UserState>> _transitions = new()
    {
        [UserState.CREATED] = [UserState.PENDING_CPF, UserState.DELETED],
        [UserState.PENDING_CPF] = [UserState.PENDING_PROFILE, UserState.DELETED],
        [UserState.PENDING_PROFILE] = [UserState.ACTIVE, UserState.DELETED],
        [UserState.ACTIVE] = [UserState.SUSPENDED, UserState.BANNED, UserState.DELETED],
        [UserState.SUSPENDED] = [UserState.ACTIVE, UserState.BANNED, UserState.DELETED],
        [UserState.BANNED] = [UserState.DELETED],
        [UserState.DELETED] = []
    };

    public static void Validate(UserState current, UserState next)
    {
        if (!_transitions.TryGetValue(current, out var allowed) || !allowed.Contains(next))
            throw new InvalidStateTransitionException("User", current.ToString(), next.ToString());
    }

    public static bool CanTransition(UserState current, UserState next)
    {
        return _transitions.TryGetValue(current, out var allowed) && allowed.Contains(next);
    }

    public static IReadOnlySet<UserState> GetAllowedTransitions(UserState current)
    {
        return _transitions.TryGetValue(current, out var allowed) ? allowed : new HashSet<UserState>();
    }
}
