using System;
using System.Collections.Generic;
using Utilities;

namespace Core.FSM
{
    public sealed class StateMachine<TKey, TOwner> where TKey : Enum
    {
        public TKey CurrentKey { get; private set; }
        public bool IsRunning { get; private set; }

        private readonly Dictionary<TKey, IState<TOwner>> _states = new();
        private readonly TOwner _owner;
        private IState<TOwner> _current;

        public StateMachine(TOwner owner)
        {
            _owner = owner;
        }

        public void AddState(TKey key, IState<TOwner> state)
        {
            _states[key] = state;
        }

        public void Start(TKey initialKey)
        {
            if (!_states.TryGetValue(initialKey, out var state))
            {
                MyLogger.LogError($"[FSM] State '{initialKey}' not registered.");
                return;
            }

            CurrentKey = initialKey;
            _current = state;
            IsRunning = true;
            _current.Enter(_owner);
        }

        public void TransitionTo(TKey key)
        {
            if (!_states.TryGetValue(key, out var next))
            {
                MyLogger.LogError($"[FSM] State '{key}' not registered.");
                return;
            }

            _current?.Exit(_owner);
            CurrentKey = key;
            _current = next;
            _current.Enter(_owner);
        }

        public void Update()
        {
            if (IsRunning)
                _current?.Update(_owner);
        }

        public void Shutdown()
        {
            if (!IsRunning) return;
            _current?.Exit(_owner);
            _current = null;
            IsRunning = false;
        }
    }
}
