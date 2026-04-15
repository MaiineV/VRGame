using System;
using System.Collections.Generic;
using HSM.Core.Event;

namespace HSM.Core.State
{
    public class StateContext : IStateContext
    {
        public IEventBus EventBus { get; private set; }
        public IStateMachine StateMachine { get; private set; }
        
        private readonly Dictionary<Type, object> _services = new();
        private readonly Dictionary<string, object> _data = new();

        public StateContext(IEventBus eventBus)
        {
            EventBus = eventBus;
        }

        public void SetStateMachine(IStateMachine stateMachine)
        {
            StateMachine = stateMachine;
        }

        public T GetService<T>() where T : class
        {
            _services.TryGetValue(typeof(T), out var service);
            return service as T;
        }

        public void RegisterService<T>(T service) where T : class
        {
            _services[typeof(T)] = service;
        }

        public void SetData<T>(string key, T value)
        {
            _data[key] = value;
        }

        public T GetData<T>(string key)
        {
            if (_data.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return default(T);
        }

        public bool HasData(string key)
        {
            return _data.ContainsKey(key);
        }
    }
}