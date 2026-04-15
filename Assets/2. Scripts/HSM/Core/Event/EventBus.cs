using System;
using System.Collections.Generic;
using System.Linq;
using Utilities;

namespace HSM.Core.Event
{
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<HandlerEntry>> _handlers = new();
        private readonly Queue<IEvent> _eventQueue = new();
        private readonly object _lock = new();

        private class HandlerEntry
        {
            public object Handler;
            public Func<IEvent, bool> CanHandle;
            public Action<IEvent> Handle;
        }

        public void Subscribe<T>(IEventHandler<T> handler) where T : IEvent
        {
            lock (_lock)
            {
                var eventType = typeof(T);
                if (!_handlers.ContainsKey(eventType))
                    _handlers[eventType] = new List<HandlerEntry>();

                var list = _handlers[eventType];
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Handler == (object)handler)
                        return;
                }

                list.Add(new HandlerEntry
                {
                    Handler = handler,
                    CanHandle = e => handler.CanHandle((T)e),
                    Handle = e => handler.HandleEvent((T)e)
                });
            }
        }

        public void Unsubscribe<T>(IEventHandler<T> handler) where T : IEvent
        {
            lock (_lock)
            {
                var eventType = typeof(T);
                if (!_handlers.ContainsKey(eventType))
                    return;

                var list = _handlers[eventType];
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i].Handler == (object)handler)
                    {
                        list.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public void Publish<T>(T eventData) where T : IEvent
        {
            lock (_lock)
            {
                _eventQueue.Enqueue(eventData);
            }
        }

        public void ProcessPendingEvents()
        {
            var eventsToProcess = new Queue<IEvent>();

            lock (_lock)
            {
                while (_eventQueue.Count > 0)
                    eventsToProcess.Enqueue(_eventQueue.Dequeue());
            }

            while (eventsToProcess.Count > 0)
            {
                var eventData = eventsToProcess.Dequeue();
                ProcessEvent(eventData);
            }
        }

        private void ProcessEvent(IEvent eventData)
        {
            var eventType = eventData.GetType();

            if (!_handlers.ContainsKey(eventType))
                return;

            var snapshot = _handlers[eventType].ToList();

            foreach (var entry in snapshot)
            {
                try
                {
                    if (entry.CanHandle(eventData))
                        entry.Handle(eventData);
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"Error processing event {eventType.Name}: {ex.Message}");
                }
            }
        }
    }
}