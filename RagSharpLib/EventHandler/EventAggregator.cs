using System;
using System.Collections.Generic;

namespace EventBroadcasting
{

    public static class EventAggregator
    {
        private static readonly Dictionary<Type, List<Delegate>> _subscriptions = new Dictionary<Type, List<Delegate>>();

        public static void Subscribe<TEvent>(Action<TEvent> handler)
        {
            var eventType = typeof(TEvent);

            if (!_subscriptions.ContainsKey(eventType))
            {
                _subscriptions[eventType] = new List<Delegate>();
            }

            _subscriptions[eventType].Add(handler);
        }

        internal static void Publish<TEvent>(TEvent eventToPublish)
        {
            var eventType = typeof(TEvent);

            if (_subscriptions.ContainsKey(eventType))
            {
                foreach (var handler in _subscriptions[eventType])
                {
                    ((Action<TEvent>)handler)(eventToPublish);
                }
            }
        }
    }
}
