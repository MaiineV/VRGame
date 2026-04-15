using System;
using System.Collections.Generic;
using HSM.Core.Event;
using Utilities;

namespace HSM.Core.State
{
    public class HierarchicalStateMachine : IStateMachine
    {
        public string CurrentStateId => _currentState?.StateId ?? string.Empty;
        public IState CurrentState => _currentState;
        public IStateContext Context { get; private set; }
        
        private IEventBus EventBus => Context.EventBus;

        private readonly Dictionary<string, IState> _states = new();
        private readonly Stack<IState> _stateStack = new();
        private IState _currentState;
        private bool _isInitialized;

        public event Action<string, string> OnStateChanged;
        public event Action<string> OnStatePushed;
        public event Action<string> OnStatePopped;

        public HierarchicalStateMachine(IStateContext context)
        {
            Context = context;
        }

        public void Initialize()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;
            EventBus.ProcessPendingEvents();
        }

        public void Update()
        {
            if (!_isInitialized)
                return;

            EventBus.ProcessPendingEvents();
            _currentState?.Update(Context);
        }

        public void Shutdown()
        {
            while (_stateStack.Count > 0)
                PopState();

            _currentState?.Exit(Context);
            _currentState = null;
            _isInitialized = false;
        }

        public void TransitionTo(string stateId)
        {
            if (!_states.TryGetValue(stateId, out var newState))
            {
                MyLogger.LogError($"State '{stateId}' not found");
                return;
            }

            if (_currentState != null)
            {
                if (!_currentState.CanTransitionTo(stateId, Context))
                {
                    MyLogger.LogWarning($"Transition from '{_currentState.StateId}' to '{stateId}' not allowed");
                    return;
                }
            }

            var previousStateId = _currentState?.StateId ?? string.Empty;
            
            _currentState?.Exit(Context);
            _currentState = newState;
            _currentState.Enter(Context);

            OnStateChanged?.Invoke(previousStateId, stateId);
        }

        public void PushState(string stateId)
        {
            if (!_states.TryGetValue(stateId, out var newState))
            {
                MyLogger.LogError($"State '{stateId}' not found");
                return;
            }

            if (_currentState != null)
                _stateStack.Push(_currentState);

            _currentState = newState;
            _currentState.Enter(Context);

            OnStatePushed?.Invoke(stateId);
        }

        public void PopState()
        {
            if (_stateStack.Count == 0)
            {
                MyLogger.LogWarning("No states to pop");
                return;
            }

            var poppedStateId = _currentState?.StateId ?? string.Empty;
            
            _currentState?.Exit(Context);
            _currentState = _stateStack.Pop();

            OnStatePopped?.Invoke(poppedStateId);
        }

        public void PopToState(string stateId)
        {
            while (_stateStack.Count > 0)
            {
                var topState = _stateStack.Peek();
                if (topState.StateId == stateId)
                {
                    PopState();
                    return;
                }
                PopState();
            }

            MyLogger.LogWarning($"State '{stateId}' not found in stack");
        }

        public void AddState(IState state)
        {
            if (state == null) return;
            _states[state.StateId] = state;
        }

        public void RemoveState(string stateId)
        {
            if (_currentState?.StateId == stateId)
            {
                MyLogger.LogError($"Cannot remove active state '{stateId}'");
                return;
            }

            _states.Remove(stateId);
        }

        public IState GetState(string stateId)
        {
            _states.TryGetValue(stateId, out var state);
            return state;
        }

        public bool IsInState(string stateId)
        {
            return _currentState?.StateId == stateId;
        }

        public bool IsInStateHierarchy(string stateId)
        {
            return _currentState?.IsInStateHierarchy(stateId) == true;
        }
    }
}