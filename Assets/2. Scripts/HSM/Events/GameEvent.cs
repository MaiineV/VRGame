using System;
using HSM.Core.Event;

namespace HSM.Events
{
    public abstract class GameEvent : IEvent
    {
        public string EventType => GetType().Name;
        public DateTime Timestamp { get; }
        public object Payload => this;

        protected GameEvent()
        {
            Timestamp = DateTime.UtcNow;
        }

        public T GetPayload<T>() where T : class
        {
            return this as T;
        }
    }
}
