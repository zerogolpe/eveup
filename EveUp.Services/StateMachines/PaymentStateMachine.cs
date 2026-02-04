using EveUp.Core.Enums;
using EveUp.Core.Exceptions;

namespace EveUp.Services.StateMachines;

/// <summary>
/// Payment state machine. CRITICAL: HELD → RELEASED direct transition is FORBIDDEN.
/// Must always go HELD → RELEASING → RELEASED.
/// </summary>
public static class PaymentStateMachine
{
    private static readonly Dictionary<PaymentState, HashSet<PaymentState>> _transitions = new()
    {
        [PaymentState.CREATED] = [PaymentState.PROCESSING, PaymentState.CANCELLED],
        [PaymentState.PROCESSING] = [PaymentState.HELD, PaymentState.FAILED],
        [PaymentState.HELD] = [PaymentState.RELEASING, PaymentState.FROZEN, PaymentState.REFUNDED],
        // NOTE: HELD → RELEASED is intentionally NOT included. Must go through RELEASING.
        [PaymentState.FROZEN] = [PaymentState.RELEASING, PaymentState.REFUNDED],
        [PaymentState.RELEASING] = [PaymentState.RELEASED, PaymentState.PARTIALLY_RELEASED, PaymentState.FAILED],
        [PaymentState.RELEASED] = [],
        [PaymentState.PARTIALLY_RELEASED] = [PaymentState.RELEASING, PaymentState.REFUNDED],
        [PaymentState.REFUNDED] = [],
        [PaymentState.FAILED] = [PaymentState.PROCESSING, PaymentState.FAILED_FINAL],
        [PaymentState.FAILED_FINAL] = [],
        [PaymentState.CANCELLED] = []
    };

    public static void Validate(PaymentState current, PaymentState next)
    {
        if (!_transitions.TryGetValue(current, out var allowed) || !allowed.Contains(next))
            throw new InvalidStateTransitionException("Payment", current.ToString(), next.ToString());
    }

    public static bool CanTransition(PaymentState current, PaymentState next)
    {
        return _transitions.TryGetValue(current, out var allowed) && allowed.Contains(next);
    }

    public static IReadOnlySet<PaymentState> GetAllowedTransitions(PaymentState current)
    {
        return _transitions.TryGetValue(current, out var allowed) ? allowed : new HashSet<PaymentState>();
    }
}
