#if PUBSUB_THREADS
#else
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;
#endif

using System;
using System.Collections.Generic;
using System.Linq;

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
            public readonly object UnsubscribeHandle;
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

            public Handler(Delegate action, Subscriber subscriber, Type messageType, object selectorWrapper,
                object unsubscribeHandle = null)
            {
                Action = action;
                Subscriber = subscriber;
                MessageType = messageType;
                _selectorWrapper = selectorWrapper;
                UnsubscribeHandle = unsubscribeHandle ?? this;
            }

            public override string ToString()
            {
                var method = Action.Method;
                return
                    $"Action={method.DeclaringType}/{method.Name} Message={MessageType.Name} Subscriber={Subscriber}";
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
        private readonly object _locker = new object();
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
            // lock (_locker)
            {
                handlerCount = _handlers.Count;
                if (handlerCount == 0)
                {
                    return;
                }
#if !PUBSUB_THREADS
                foreach (var handler in _handlers)
                {
                    Debug.LogWarning($"handler {handler}");
                }
#endif
            }
#if !PUBSUB_THREADS
            Debug.LogWarning($"handlerCount is {handlerCount}");
#endif
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
            // lock (_locker)
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

            try
            {
                foreach (var handler in handlersToCall)
                {
                    if (!handler.Select(data))
                    {
                        continue;
                    }
                    var subscriber = handler.Subscriber;
                    if (subscriber.IsWeakRef)
                    {
                        // Get reference to subscriber's target
                        // 1) to find that is is alive now and
                        // 2) to keep it alive during the callback.
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
                        // Just check that target exists in UNITY side
                        var target = subscriber.Target;
                        if (target == null)
                        {
                            continue;
                        }
                        ((Action<T>)handler.Action)(data);
                    }
                }
            }
            catch (Exception x)
            {
#if !PUBSUB_THREADS
                // It seems that Cysharp.Threading.Tasks.EnumeratorAsyncExtensions/EnumeratorPromise:MoveNext
                // or similar might swallow or defer callback exceptions.
                // We log then here to indicate that something went wrong.
                Debug.LogError($"handler failed: {x}");
#endif
                throw;
            }
        }

        public object Subscribe<T>(object subscriber, Action<T> messageHandler, Predicate<T> messageSelector,
            object unsubscribeHandle)
        {
            var selectorWrapper = messageSelector != null ? new Handler.SelectorWrapper<T>(messageSelector) : null;
            var item = new Handler(messageHandler, new Subscriber(subscriber), typeof(T), selectorWrapper,
                unsubscribeHandle);
#if PUBSUB_THREADS
            lock (_locker)
#else
            Assert.AreEqual(_mainThreadId, Thread.CurrentThread.ManagedThreadId);
#endif
            // lock (_locker)
            {
                //-Debug.Log($"subscribe {item}");
                _handlers.Add(item);
            }
            return item.UnsubscribeHandle;
        }

        public void Unsubscribe(object subscriber)
        {
#if PUBSUB_THREADS
            lock (_locker)
#else
            Assert.AreEqual(_mainThreadId, Thread.CurrentThread.ManagedThreadId);
#endif
            // lock (_locker)
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
            // lock (_locker)
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

        public void UnsubscribeListener(object unsubscribeHandle)
        {
#if PUBSUB_THREADS
            lock (_locker)
#else
            Assert.AreEqual(_mainThreadId, Thread.CurrentThread.ManagedThreadId);
#endif
            // lock (_locker)
            {
                var query = _handlers
                    .Where(handler => !handler.Subscriber.IsAlive ||
                                      ReferenceEquals(handler.UnsubscribeHandle, unsubscribeHandle));

                foreach (var h in query.ToList())
                {
                    //-Debug.Log($"unsubscribe {h}");
                    _handlers.Remove(h);
                }
            }
        }
    }
}
