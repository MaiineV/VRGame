using System;
using HSM.Core.Event;

namespace HSM.Core.State
{
    public class StateEvent : IEvent
    {
        public string EventType { get; }
        public DateTime Timestamp { get; }
        public object Payload { get; }

        public StateEvent(string eventType, object payload = null)
        {
            EventType = eventType;
            Payload = payload;
            Timestamp = DateTime.UtcNow;
        }

        public T GetPayload<T>() where T : class
        {
            return Payload as T;
        }
    }
}