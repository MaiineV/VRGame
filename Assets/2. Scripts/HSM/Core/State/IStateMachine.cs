using System;

namespace HSM.Core.State
{
    public interface IStateMachine
    {
        string CurrentStateId { get; }
        IState CurrentState { get; }
        IStateContext Context { get; }
        
        void Initialize();
        void Update();
        void Shutdown();
        
        void TransitionTo(string stateId);
        void PushState(string stateId);
        void PopState();
        void PopToState(string stateId);
        
        void AddState(IState state);
        void RemoveState(string stateId);
        IState GetState(string stateId);
        
        bool IsInState(string stateId);
        bool IsInStateHierarchy(string stateId);
        
        event Action<string, string> OnStateChanged;
        event Action<string> OnStatePushed;
        event Action<string> OnStatePopped;
    }
}