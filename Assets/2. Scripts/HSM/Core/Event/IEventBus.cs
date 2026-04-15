namespace HSM.Core.Event
{
    public interface IEventBus
    {
        void Subscribe<T>(IEventHandler<T> handler) where T : IEvent;
        void Unsubscribe<T>(IEventHandler<T> handler) where T : IEvent;
        void Publish<T>(T eventData) where T : IEvent;
        void ProcessPendingEvents();
    }
}