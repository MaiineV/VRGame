namespace HSM.Core.State
{
    public interface IState
    {
        string StateId { get; }
        IState Parent { get; }
        bool IsActive { get; }
        
        void Enter(IStateContext context);
        void Exit(IStateContext context);
        void Update(IStateContext context);
        
        bool CanTransitionTo(string targetStateId, IStateContext context);
        IState GetChildState(string stateId);
        void AddChildState(IState childState);
        void RemoveChildState(string stateId);
        
        bool IsInStateHierarchy(string stateId);
    }
}