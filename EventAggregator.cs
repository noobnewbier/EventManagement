using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using NLog;

namespace Engine.Support.EventAggregator
{
    // Directly copied from caliburn's EventAggregator: https://github.com/Caliburn-Micro/Caliburn.Micro/blob/316042591f3a70346e1bd92daa900565eafdb50d/src/Caliburn.Micro/EventAggregator.cs
    // The only change made was to remove the thread marshaller.
    public class EventAggregator : IEventAggregator
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        private readonly List<Handler> _handlers = new List<Handler>();
        
        /// <summary>
        /// Searches the subscribed handlers to check if we have a handler for
        /// the message type supplied.
        /// </summary>
        /// <param name="messageType">The message type to check with</param>
        /// <returns>True if any handler is found, false if not.</returns>
        public bool HandlerExistsFor(Type messageType)
        {
            return _handlers.Any(handler => handler.Handles(messageType) & !handler.IsDead);
        }

        /// <summary>
        /// Subscribes an instance to all events declared through implementations of IHandle
        /// </summary>
        /// <param name="subscriber">The instance to subscribe for event publication.</param>
        public virtual void Subscribe(object subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));
            List<Handler> handlers = _handlers;
            bool lockTaken = false;
            try
            {
                Monitor.Enter(handlers, ref lockTaken);
                if (_handlers.Any(x => x.Matches(subscriber)))
                    return;
                _handlers.Add(new Handler(subscriber));
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(handlers);
            }
        }

        /// <summary>Unsubscribes the instance from all events.</summary>
        /// <param name="subscriber">The instance to unsubscribe.</param>
        public virtual void Unsubscribe(object subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));
            List<Handler> handlers = _handlers;
            bool lockTaken = false;
            try
            {
                Monitor.Enter(handlers, ref lockTaken);
                Handler handler = _handlers.FirstOrDefault(x => x.Matches(subscriber));
                if (handler == null)
                    return;
                _handlers.Remove(handler);
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(handlers);
            }
        }

        /// <summary>Publishes a message.</summary>
        /// <param name="message">The message instance.</param>
        public virtual void Publish(object message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            
            var messageType = message.GetType();

            if (_handlers.All(handler => !handler.Handles(messageType)))
            {
                Logger.Warn($"{messageType} has no handler.");
                return;
            }
            
            var handlersToRemove = _handlers
                .Where(handler => !handler.Handle(messageType, message))
                .ToList();
            
            if (!handlersToRemove.Any())
            {
                return;
            }
            
            foreach (var handler in handlersToRemove)
            {
                _handlers.Remove(handler);
            }
        }

        private class Handler 
        {
            private readonly WeakReference _reference;
            private readonly Dictionary<Type, MethodInfo> _supportedHandlers = new Dictionary<Type, MethodInfo>();

            public bool IsDead => _reference.Target == null;

            public Handler(object handler) 
            {
                _reference = new WeakReference(handler);

                var interfaces = handler.GetType().GetInterfaces().Where(x => typeof(IHandle).IsAssignableFrom(x) && x.IsGenericType);

                foreach(var @interface in interfaces)
                {
                    var type = @interface.GetGenericArguments()[0];
                    var method = @interface.GetMethod("Handle", new[] { type });

                    if (method != null) 
                    {
                        _supportedHandlers[type] = method;
                    }
                }
            }

            public bool Matches(object instance) 
            {
                return _reference.Target == instance;
            }

            public bool Handle(Type messageType, object message) 
            {
                var target = _reference.Target;
                if (target == null) 
                {
                    return false;
                }

                foreach(var pair in _supportedHandlers) 
                {
                    if(pair.Key.IsAssignableFrom(messageType))
                    {
                        pair.Value.Invoke(target, new[] {message});
                    }
                }
                
                return true;
            }

            public bool Handles(Type messageType) 
            {
                return _supportedHandlers.Any(pair => pair.Key.IsAssignableFrom(messageType));
            }
        }
    }
}