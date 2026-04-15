using HSM.Core;
using HSM.Core.State;

namespace HSM.Utilities
{
    public class StateValidator
    {
        public static bool ValidateStateMachine(IStateMachine stateMachine)
        {
            // Add validation logic here
            return true;
        }

        public static bool ValidateStateTransition(IState fromState, IState toState, IStateContext context)
        {
            if (fromState == null || toState == null) return false;
            return fromState.CanTransitionTo(toState.StateId, context);
        }
    }
}