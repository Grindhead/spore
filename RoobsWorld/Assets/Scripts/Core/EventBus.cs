using System;
using System.Collections.Generic;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Marker interface for events that can be published through the global <see cref="EventBus"/>.
    /// </summary>
    public interface IGameEvent { }

    /// <summary>
    /// Lightweight typed publish/subscribe event bus used to decouple game systems via <see cref="IGameEvent"/> messages.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var existing))
                _handlers[type] = Delegate.Combine(existing, handler);
            else
                _handlers[type] = handler;
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var existing))
            {
                var result = Delegate.Remove(existing, handler);
                if (result == null)
                    _handlers.Remove(type);
                else
                    _handlers[type] = result;
            }
        }

        public static void Publish<T>(T gameEvent) where T : IGameEvent
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var existing))
                ((Action<T>)existing)?.Invoke(gameEvent);
        }

        public static void Clear()
        {
            _handlers.Clear();
        }

        public static bool HasSubscribers<T>() where T : IGameEvent
        {
            return _handlers.ContainsKey(typeof(T));
        }
    }
}
