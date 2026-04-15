using HSM.Core;
using HSM.Core.State;
using Utilities;

namespace HSM.Utilities
{

    public class StateLogger
    {
        private readonly IStateMachine _stateMachine;
        private readonly bool _logTransitions;
        private readonly bool _logEvents;

        public StateLogger(IStateMachine stateMachine, bool logTransitions = true, bool logEvents = false)
        {
            _stateMachine = stateMachine;
            _logTransitions = logTransitions;
            _logEvents = logEvents;

            if (_logTransitions)
            {
                _stateMachine.OnStateChanged += OnStateChanged;
                _stateMachine.OnStatePushed += OnStatePushed;
                _stateMachine.OnStatePopped += OnStatePopped;
            }
        }

        private void OnStateChanged(string fromState, string toState)
        {
            MyLogger.LogInfo($"[StateMachine] Transition: {fromState} → {toState}");
        }

        private void OnStatePushed(string stateId)
        {
            MyLogger.LogInfo($"[StateMachine] Pushed: {stateId}");
        }

        private void OnStatePopped(string stateId)
        {
            MyLogger.LogInfo($"[StateMachine] Popped: {stateId}");
        }

        public void Dispose()
        {
            if (_logTransitions)
            {
                _stateMachine.OnStateChanged -= OnStateChanged;
                _stateMachine.OnStatePushed -= OnStatePushed;
                _stateMachine.OnStatePopped -= OnStatePopped;
            }
        }
    }
}