namespace Core.FSM
{
    public interface IState<in T>
    {
        void Enter(T owner);
        void Update(T owner);
        void Exit(T owner);
    }
}
