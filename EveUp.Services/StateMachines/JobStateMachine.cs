using EveUp.Core.Enums;
using EveUp.Core.Exceptions;

namespace EveUp.Services.StateMachines;

public static class JobStateMachine
{
    private static readonly Dictionary<JobState, HashSet<JobState>> _transitions = new()
    {
        [JobState.DRAFT] = [JobState.PUBLISHED, JobState.CANCELLED],
        [JobState.PUBLISHED] = [JobState.MATCHING, JobState.CONFIRMED, JobState.EXPIRED, JobState.CANCELLED],
        [JobState.MATCHING] = [JobState.CONFIRMED, JobState.NO_MATCH, JobState.CANCELLED],
        [JobState.CONFIRMED] = [JobState.AWAITING_PAYMENT, JobState.CANCELLED_AFTER_MATCH],
        [JobState.AWAITING_PAYMENT] = [JobState.PAID, JobState.CANCELLED_AFTER_MATCH],
        [JobState.PAID] = [JobState.IN_PROGRESS],
        [JobState.IN_PROGRESS] = [JobState.COMPLETED, JobState.DISPUTED],
        [JobState.COMPLETED] = [JobState.SETTLED, JobState.DISPUTED],
        [JobState.DISPUTED] = [JobState.RESOLVED],
        [JobState.SETTLED] = [],
        [JobState.RESOLVED] = [JobState.SETTLED],
        [JobState.CANCELLED] = [],
        [JobState.EXPIRED] = [],
        [JobState.NO_MATCH] = [],
        [JobState.CANCELLED_AFTER_MATCH] = []
    };

    public static void Validate(JobState current, JobState next)
    {
        if (!_transitions.TryGetValue(current, out var allowed) || !allowed.Contains(next))
            throw new InvalidStateTransitionException("Job", current.ToString(), next.ToString());
    }

    public static bool CanTransition(JobState current, JobState next)
    {
        return _transitions.TryGetValue(current, out var allowed) && allowed.Contains(next);
    }

    public static IReadOnlySet<JobState> GetAllowedTransitions(JobState current)
    {
        return _transitions.TryGetValue(current, out var allowed) ? allowed : new HashSet<JobState>();
    }
}
