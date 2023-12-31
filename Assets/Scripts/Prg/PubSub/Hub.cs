#if PUBSUB_THREADS
#else
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace Prg.PubSub
{
    /// <summary>
    /// Simple Publish Subscribe Pattern implementation using C# <c>WeakReference</c>s and/or <c>UnityEngine.Object</c>s.
    /// </summary>
    /// <remarks>
    /// This implementation can be configured with #define <c>PUBSUB_THREADS</c> to be multi-threaded safe!<br />
    /// Normal UNITY application can do well with single threaded model.
    /// </remarks>
    public class Hub
    {
        /// <summary>
        /// Wrapper for actual Subscriber object.<br />
        /// <c>WeakReference</c> and <c>UnityEngine.Object</c> requires a bit different handling to detect if Subscriber is still alive.
        /// </summary>
        private class Subscriber
        {
            public readonly bool IsWeakRef;
            private readonly WeakReference _cSubscriber;
            private readonly Object _uSubscriber;

            public bool IsAlive => IsWeakRef ? _cSubscriber.IsAlive : _uSubscriber != null;

            public object Target => IsWeakRef ? _cSubscriber.Target : _uSubscriber;

            public Subscriber(object subscriber)
            {
                if (subscriber is Object unityObject)
                {
                    IsWeakRef = false;
                    _uSubscriber = unityObject;
                }
                else
                {
                    IsWeakRef = true;
                    _cSubscriber = new WeakReference(subscriber);
                }
            }

            public override string ToString()
            {
                return IsWeakRef
                    ? _cSubscriber.IsAlive ? $"{_cSubscriber}" : "_Garbage_"
                    : $"{_uSubscriber}";
            }
        }

        /// <summary>
        /// The message Handler aka (client) Subscriber.
        /// </summary>
        /// <remarks>
        /// Names here are not best possible, this is very old implementation :-(
        /// </remarks>
        private class Handler
        {
            public readonly Delegate Action;
            public readonly Subscriber Subscriber;
            public readonly Type MessageType;
            private readonly object _selectorWrapper;

            public bool Select<T>(T message)
            {
                if (_selectorWrapper == null)
                {
                    return true;
                }
                if (_selectorWrapper is SelectorWrapper<T> wrapper)
                {
                    return wrapper.Selector(message);
                }
                return false;
            }

            public Handler(Delegate action, Subscriber subscriber, Type messageType, object selectorWrapper)
            {
                Action = action;
                Subscriber = subscriber;
                MessageType = messageType;
                _selectorWrapper = selectorWrapper;
            }

            public override string ToString()
            {
                return
                    $"Action={Action.Method.Name} Subscriber={Subscriber} Type={MessageType.Name}";
            }

            public class SelectorWrapper<T>
            {
                public readonly Predicate<T> Selector;

                public SelectorWrapper(Predicate<T> selector)
                {
                    Selector = selector;
                }
            }
        }
#if PUBSUB_THREADS
        private readonly object _locker = new();
#else
        public static void SetMainThreadId(int threadId) => _mainThreadId = threadId;

        private static int _mainThreadId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void SubsystemRegistration()
        {
            // Manual reset if UNITY Domain Reloading is disabled.
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }
#endif

        private readonly List<Handler> _handlers = new();

        public void DumpHandlerCount()
        {
            int handlerCount;
#if PUBSUB_THREADS
            lock (_locker)
#else
            Assert.AreEqual(_mainThreadId, Thread.CurrentThread.ManagedThreadId);
#endif
            {
                handlerCount = _handlers.Count;
                if (handlerCount == 0)
                {
                    return;
                }
                foreach (var handler in _handlers)
                {
                    Debug.Log($"handler {handler}");
                }
            }
            Debug.LogWarning($"handlerCount is {handlerCount}");
        }

        public void Publish<T>(T data = default)
        {
            var handlersToCall = new List<Handler>();
            var handlersToRemoveList = new List<Handler>();

#if PUBSUB_THREADS
            lock (_locker)
#else
            Assert.AreEqual(_mainThreadId, Thread.CurrentThread.ManagedThreadId);
#endif
            {
                foreach (var handler in _handlers)
                {
                    if (!handler.Subscriber.IsAlive)
                    {
                        handlersToRemoveList.Add(handler);
                        continue;
                    }
                    if (handler.MessageType.IsAssignableFrom(typeof(T)))
                    {
                        handlersToCall.Add(handler);
                    }
                }

                foreach (var l in handlersToRemoveList)
                {
                    //-Debug.Log($"remove {l}");
                    _handlers.Remove(l);
                }
            }

            foreach (var handler in handlersToCall)
            {
                if (!handler.Select(data))
                {
                    continue;
                }
                var subscriber = handler.Subscriber;
                if (subscriber.IsWeakRef)
                {
                    // Get reference to subscriber's target 1) to find that is is alive now and 2) to keep it alive during the callback.
                    var target = subscriber.Target;
                    if (target == null)
                    {
                        continue;
                    }
                    ((Action<T>)handler.Action)(data);

                    // References the specified object, which makes it ineligible for garbage collection
                    // from the start of the current routine to the point where this method is called.
                    GC.KeepAlive(target);
                }
                else
                {
                    // Just check that target exists in UNITY
                    var target = subscriber.Target;
                    if (target == null)
                    {
                        continue;
                    }
                    ((Action<T>)handler.Action)(data);
                }
            }
        }

        public void Subscribe<T>(object subscriber, Action<T> messageHandler, Predicate<T> messageSelector)
        {
            var selectorWrapper = messageSelector != null ? new Handler.SelectorWrapper<T>(messageSelector) : null;
            var item = new Handler(messageHandler, new Subscriber(subscriber), typeof(T), selectorWrapper);
#if PUBSUB_THREADS
            lock (_locker)
#else
            Assert.AreEqual(_mainThreadId, Thread.CurrentThread.ManagedThreadId);
#endif
            {
                //-Debug.Log($"subscribe {item}");
                _handlers.Add(item);
            }
        }

        public void Unsubscribe(object subscriber)
        {
#if PUBSUB_THREADS
            lock (_locker)
#else
            Assert.AreEqual(_mainThreadId, Thread.CurrentThread.ManagedThreadId);
#endif
            {
                var query = _handlers
                    .Where(handler => !handler.Subscriber.IsAlive ||
                                      Equals(handler.Subscriber.Target, subscriber));

                foreach (var h in query.ToList())
                {
                    //-Debug.Log($"unsubscribe {h}");
                    _handlers.Remove(h);
                }
            }
        }

        public void Unsubscribe<T>(object subscriber, Action<T> handlerToRemove = null)
        {
#if PUBSUB_THREADS
            lock (_locker)
#else
            Assert.AreEqual(_mainThreadId, Thread.CurrentThread.ManagedThreadId);
#endif
            {
                var query = _handlers
                    .Where(handler => !handler.Subscriber.IsAlive ||
                                      (handler.MessageType == typeof(T) &&
                                       Equals(handler.Subscriber.Target, subscriber)));

                if (handlerToRemove != null)
                {
                    query = query.Where(handler => !handler.Subscriber.IsAlive ||
                                                   handler.Action.Equals(handlerToRemove));
                }

                foreach (var h in query.ToList())
                {
                    //-Debug.Log($"unsubscribe {h}");
                    _handlers.Remove(h);
                }
            }
        }
    }
}
