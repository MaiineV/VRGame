namespace HSM.Core.Event
{
    public interface IEventHandler<in T> where T : IEvent
    {
        void HandleEvent(T eventData);
        bool CanHandle(T eventData);
    }
}