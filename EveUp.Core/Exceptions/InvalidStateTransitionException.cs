namespace EveUp.Core.Exceptions;

public class InvalidStateTransitionException : Exception
{
    public string EntityType { get; }
    public string CurrentState { get; }
    public string AttemptedState { get; }

    public InvalidStateTransitionException(string entityType, string currentState, string attemptedState)
        : base($"Invalid state transition for {entityType}: {currentState} → {attemptedState}")
    {
        EntityType = entityType;
        CurrentState = currentState;
        AttemptedState = attemptedState;
    }
}
