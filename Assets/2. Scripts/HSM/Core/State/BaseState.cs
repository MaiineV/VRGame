using System.Collections.Generic;
using HSM.Core.Event;

namespace HSM.Core.State
{
    public abstract class BaseState : IState
    {
        public abstract string StateId { get; }
        public IState Parent { get; private set; }
        public bool IsActive { get; private set; }
        
        protected readonly Dictionary<string, IState> _childStates = new();
        protected IState _currentChildState;
        protected IStateContext _context;
        
        protected IEventBus EventBus => _context?.EventBus;

        public virtual void Enter(IStateContext context)
        {
            _context = context;
            IsActive = true;
            OnEnter(context);
            
            var defaultChild = GetDefaultChildState();
            if (defaultChild != null)
                TransitionToChild(defaultChild.StateId);
        }

        public virtual void Exit(IStateContext context)
        {
            _currentChildState?.Exit(context);
            _currentChildState = null;
            OnExit(context);
            IsActive = false;
        }

        public virtual void Update(IStateContext context)
        {
            if (!IsActive) return;
            
            OnUpdate(context);
            _currentChildState?.Update(context);
        }

        public virtual bool CanTransitionTo(string targetStateId, IStateContext context)
        {
            return OnCanTransitionTo(targetStateId, context);
        }

        public virtual IState GetChildState(string stateId)
        {
            _childStates.TryGetValue(stateId, out var state);
            return state;
        }

        public virtual void AddChildState(IState childState)
        {
            if (childState == null) return;
            
            _childStates[childState.StateId] = childState;
            SetParent(childState, this);
        }

        public virtual void RemoveChildState(string stateId)
        {
            if (_childStates.TryGetValue(stateId, out var state))
            {
                if (_currentChildState == state)
                {
                    _currentChildState.Exit(_context);
                    _currentChildState = null;
                }
                
                _childStates.Remove(stateId);
                SetParent(state, null);
            }
        }

        protected virtual void TransitionToChild(string childStateId)
        {
            if (!_childStates.TryGetValue(childStateId, out var newChild))
                return;

            if (_currentChildState != null)
            {
                if (_currentChildState.StateId == childStateId)
                    return;
                
                _currentChildState.Exit(_context);
            }

            _currentChildState = newChild;
            _currentChildState.Enter(_context);
        }

        protected virtual IState GetDefaultChildState()
        {
            return null;
        }

        protected virtual void OnEnter(IStateContext context) { }
        protected virtual void OnUpdate(IStateContext context) { }
        protected virtual void OnExit(IStateContext context) { }
        protected virtual bool OnCanTransitionTo(string targetStateId, IStateContext context) { return true; }

        protected T GetService<T>() where T : class
        {
            return _context?.GetService<T>();
        }

        protected void PublishEvent<T>(T eventData) where T : IEvent
        {
            EventBus?.Publish(eventData);
        }

        private static void SetParent(IState child, IState parent)
        {
            if (child is BaseState baseChild)
            {
                baseChild.Parent = parent;
            }
        }

        public bool IsInStateHierarchy(string stateId)
        {
            if (StateId == stateId) return true;
            return _currentChildState?.IsInStateHierarchy(stateId) == true;
        }
    }
}