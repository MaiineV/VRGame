using System;

namespace HSM.Core.Event
{
    public interface IEvent
    {
        string EventType { get; }
        DateTime Timestamp { get; }
        object Payload { get; }
        T GetPayload<T>() where T : class;
    }
}