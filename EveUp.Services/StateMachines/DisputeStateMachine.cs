using EveUp.Core.Enums;
using EveUp.Core.Exceptions;

namespace EveUp.Services.StateMachines;

public static class DisputeStateMachine
{
    private static readonly Dictionary<DisputeState, HashSet<DisputeState>> _transitions = new()
    {
        [DisputeState.OPENED] = [DisputeState.UNDER_REVIEW, DisputeState.CANCELLED],
        [DisputeState.UNDER_REVIEW] = [DisputeState.EVIDENCE_REQUESTED, DisputeState.FAVOR_COMPANY, DisputeState.FAVOR_WORKER, DisputeState.PARTIAL],
        [DisputeState.EVIDENCE_REQUESTED] = [DisputeState.EVIDENCE_RECEIVED, DisputeState.CANCELLED],
        [DisputeState.EVIDENCE_RECEIVED] = [DisputeState.UNDER_REVIEW],
        [DisputeState.FAVOR_COMPANY] = [],
        [DisputeState.FAVOR_WORKER] = [],
        [DisputeState.PARTIAL] = [],
        [DisputeState.CANCELLED] = []
    };

    public static void Validate(DisputeState current, DisputeState next)
    {
        if (!_transitions.TryGetValue(current, out var allowed) || !allowed.Contains(next))
            throw new InvalidStateTransitionException("Dispute", current.ToString(), next.ToString());
    }

    public static bool CanTransition(DisputeState current, DisputeState next)
    {
        return _transitions.TryGetValue(current, out var allowed) && allowed.Contains(next);
    }

    public static IReadOnlySet<DisputeState> GetAllowedTransitions(DisputeState current)
    {
        return _transitions.TryGetValue(current, out var allowed) ? allowed : new HashSet<DisputeState>();
    }
}
