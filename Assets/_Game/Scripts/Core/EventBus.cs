using System;
using System.Collections.Generic;

namespace BulletRoute.Core
{
    public interface IGameEvent { }

    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        public static void Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            var type = typeof(T);
            if (!_handlers.ContainsKey(type))
                _handlers[type] = new List<Delegate>();
            _handlers[type].Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            var type = typeof(T);
            if (_handlers.ContainsKey(type))
                _handlers[type].Remove(handler);
        }

        public static void Publish<T>(T gameEvent) where T : IGameEvent
        {
            var type = typeof(T);
            if (!_handlers.ContainsKey(type)) return;

            // Copy to avoid modification during iteration
            var handlers = new List<Delegate>(_handlers[type]);
            foreach (var handler in handlers)
            {
                ((Action<T>)handler)?.Invoke(gameEvent);
            }
        }

        public static void Clear()
        {
            _handlers.Clear();
        }
    }
}
