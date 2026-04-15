using HSM.Core;
using HSM.Core.State;

namespace HSM.Extensions
{
    public static class StateMachineExtensions
    {
        public static void RegisterService<T>(this IStateContext context, T service) where T : class
        {
            if (context is StateContext stateContext)
                stateContext.RegisterService(service);
        }

        public static IStateMachine WithState<T>(this IStateMachine stateMachine) where T : IState, new()
        {
            var state = new T();
            stateMachine.AddState(state);
            return stateMachine;
        }

        public static IStateMachine WithState(this IStateMachine stateMachine, IState state)
        {
            stateMachine.AddState(state);
            return stateMachine;
        }

        public static IStateMachine StartWith(this IStateMachine stateMachine, string stateId)
        {
            stateMachine.Initialize();
            stateMachine.TransitionTo(stateId);
            return stateMachine;
        }
    }
}