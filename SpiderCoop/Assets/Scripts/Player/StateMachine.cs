public class StateMachine
{
    public State CurrentState { get; private set; }


    public void Initialize(State startingState)
    {
        CurrentState = startingState;
        CurrentState.Enter();
    }


    public void ChangeState(State newState)
    {
        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }


    public void LogicUpdate() => CurrentState?.LogicUpdate();
    public void PhysicsUpdate() => CurrentState?.PhysicsUpdate();
}