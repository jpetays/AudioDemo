using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Prg.PubSub
{
    /// <summary>
    /// Simple Publish Subscribe Pattern using Extension Methods to delegate work to actual implementation.
    /// </summary>
    /// <remarks>
    /// This implementation supports UNITY <c>Object</c>s in addition to normal C# <c>object</c>s - but not at the same time!
    /// </remarks>
    public static class PubSubExtensions
    {
        private static readonly Hub Hub = new();
        private static bool _isApplicationQuitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeSceneLoad()
        {
            SetEditorStatus();
        }

        [Conditional("UNITY_EDITOR")]
        private static void SetEditorStatus()
        {
            _isApplicationQuitting = false;
            Application.quitting += () => _isApplicationQuitting = true;
            SceneManager.sceneUnloaded += _ => CheckHandlerCount();
            return;

            void CheckHandlerCount()
            {
                if (_isApplicationQuitting)
                {
                    return;
                }
                // We assume that all handlers have Unsubscribed when level ends.
                Hub.DumpHandlerCount();
            }
        }

        public static void SetMainThreadId(int threadId)
        {
#if PUBSUB_THREADS
#else
            Hub.SetMainThreadId(threadId);
#endif
        }

        /// <summary>
        /// Gets default hub for this subscriber.
        /// </summary>
        /// <param name="subscriber">The subscriber for this hub</param>
        /// <returns>The default hub instance appropriate for this object type.</returns>
        public static Hub GetHub(this object subscriber)
        {
            return Hub;
        }

        /// <summary>
        /// Publish a message to all subscribers.
        /// </summary>
        /// <param name="_">Sending object is not used</param>
        /// <param name="data">The message to send</param>
        /// <typeparam name="T">Type of the message</typeparam>
        public static void Publish<T>(this object _, T data)
        {
            Hub.Publish(data);
        }

        /// <summary>
        /// Subscribes to a message.
        /// </summary>
        /// <param name="subscriber">The subscriber</param>
        /// <param name="messageHandler">Callback to receive the message</param>
        /// <param name="messageSelector">Predicate to filter messages</param>
        /// <typeparam name="T">Type of the message</typeparam>
        public static void Subscribe<T>(this object subscriber, Action<T> messageHandler,
            Predicate<T> messageSelector = null)
        {
            Hub.Subscribe(subscriber, messageHandler, messageSelector);
        }

        /// <summary>
        /// Unsubscribes to all messages.
        /// </summary>
        /// <param name="subscriber">The subscriber</param>
        public static void Unsubscribe(this object subscriber)
        {
            Hub.Unsubscribe(subscriber);
        }

        /// <summary>
        /// Unsubscribes to messages of type <c>T</c>.
        /// </summary>
        /// <param name="subscriber">The subscriber</param>
        /// <typeparam name="T">Type of the message</typeparam>
        public static void Unsubscribe<T>(this object subscriber)
        {
            Hub.Unsubscribe(subscriber, (Action<T>)null);
        }

        /// <summary>
        /// Unsubscribes to messages for given message handler callback.
        /// </summary>
        /// <param name="subscriber">The subscriber</param>
        /// <param name="messageHandler">Message handler callback subscribed to</param>
        /// <typeparam name="T">Type of the message</typeparam>
        public static void Unsubscribe<T>(this object subscriber, Action<T> messageHandler)
        {
            Hub.Unsubscribe(subscriber, messageHandler);
        }
    }
}
