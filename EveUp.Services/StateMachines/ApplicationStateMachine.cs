using EveUp.Core.Enums;
using EveUp.Core.Exceptions;

namespace EveUp.Services.StateMachines;

public static class ApplicationStateMachine
{
    private static readonly Dictionary<ApplicationState, HashSet<ApplicationState>> _transitions = new()
    {
        [ApplicationState.PENDING] = [ApplicationState.APPROVED, ApplicationState.REJECTED, ApplicationState.WITHDRAWN, ApplicationState.POSITION_FILLED],
        [ApplicationState.APPROVED] = [ApplicationState.ACTIVE, ApplicationState.WITHDRAWN],
        [ApplicationState.REJECTED] = [],
        [ApplicationState.WITHDRAWN] = [],
        [ApplicationState.ACTIVE] = [ApplicationState.COMPLETED, ApplicationState.NO_SHOW],
        [ApplicationState.COMPLETED] = [ApplicationState.RATED],
        [ApplicationState.RATED] = [],
        [ApplicationState.NO_SHOW] = [],
        [ApplicationState.POSITION_FILLED] = []
    };

    public static void Validate(ApplicationState current, ApplicationState next)
    {
        if (!_transitions.TryGetValue(current, out var allowed) || !allowed.Contains(next))
            throw new InvalidStateTransitionException("Application", current.ToString(), next.ToString());
    }

    public static bool CanTransition(ApplicationState current, ApplicationState next)
    {
        return _transitions.TryGetValue(current, out var allowed) && allowed.Contains(next);
    }

    public static IReadOnlySet<ApplicationState> GetAllowedTransitions(ApplicationState current)
    {
        return _transitions.TryGetValue(current, out var allowed) ? allowed : new HashSet<ApplicationState>();
    }
}
