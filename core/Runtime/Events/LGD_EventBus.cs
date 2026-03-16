using System;
using System.Collections.Generic;

namespace LiveGameDev.Core
{
    /// <summary>
    /// Lightweight static generic event bus for decoupled cross-tool communication.
    /// Tools subscribe only to event types they care about.
    /// Each tool compiles independently — no hard cross-package dependencies.
    /// </summary>
    public static class LGD_EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _handlers = new();

        /// <summary>Subscribe a handler for events of type <typeparamref name="T"/>.</summary>
        public static void Subscribe<T>(Action<T> handler) where T : LGD_EventBase
        {
            var key = typeof(T);
            if (!_handlers.TryGetValue(key, out var list))
            {
                list = new List<Delegate>();
                _handlers[key] = list;
            }
            if (!list.Contains(handler))
                list.Add(handler);
        }

        /// <summary>Unsubscribe a previously registered handler.</summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : LGD_EventBase
        {
            if (_handlers.TryGetValue(typeof(T), out var list))
                list.Remove(handler);
        }

        /// <summary>Publish an event to all subscribers of type <typeparamref name="T"/>.</summary>
        public static void Publish<T>(T evt) where T : LGD_EventBase
        {
            if (!_handlers.TryGetValue(typeof(T), out var list)) return;
            // Iterate a copy to allow safe unsubscription inside a handler
            foreach (var handler in list.ToArray())
                (handler as Action<T>)?.Invoke(evt);
        }

        /// <summary>Remove all handlers for event type <typeparamref name="T"/>.</summary>
        public static void Clear<T>() where T : LGD_EventBase
            => _handlers.Remove(typeof(T));

        /// <summary>Remove all handlers for all event types. Call on domain reload.</summary>
        public static void ClearAll() => _handlers.Clear();
    }
}
