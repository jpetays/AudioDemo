using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

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
        private static readonly UnityHub UnityHub = new();
        private static bool _isApplicationQuitting;

        /// <summary>
        /// This extension supports two separate hubs and they are easy to mix
        /// but messages can not go from one hub to an other so we guard it like this for now.
        /// </summary>
        /// <remarks>
        /// <c>UnityHub</c> is specially designed for messaging between single threaded UNITY <c>Object</c>s.<br />
        /// Standard <c>object</c> <c>Hub</c> uses <c>WeakReference</c>s that does not work (at all) with UNITY <c>Object</c>s
        /// because actual reference is not managed by C# side.
        /// </remarks>
        [SuppressMessage("ReSharper", "ConvertToConstant.Global")]
        public static readonly bool IsAllowMixedBubs = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeSceneLoad()
        {
            SetEditorStatus();
        }

        [Conditional("UNITY_EDITOR")]
        private static void SetEditorStatus()
        {
            void CheckHandlerCount()
            {
                if (_isApplicationQuitting)
                {
                    return;
                }
                Hub.CheckHandlerCount(isLogging: true);
                UnityHub.CheckHandlerCount(isLogging: true);
            }

            _isApplicationQuitting = false;
            Application.quitting += () => _isApplicationQuitting = true;
            SceneManager.sceneUnloaded += _ => CheckHandlerCount();
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
        /// Gets default hub for this subscriber.
        /// </summary>
        /// <param name="subscriber">The subscriber for this hub</param>
        /// <returns>The default hub instance appropriate for this object type.</returns>
        public static UnityHub GetHub(this Object subscriber)
        {
            return UnityHub;
        }

        /// <summary>
        /// Publish a message to all subscribers.
        /// </summary>
        /// <param name="_">Sending object is not used</param>
        /// <param name="data">The message to send</param>
        /// <typeparam name="T">Type of the message</typeparam>
        public static void Publish<T>(this object _, T data)
        {
            Assert.IsTrue(IsAllowMixedBubs, "IsAllowMixedBubs must be set explicitly");
            Hub.Publish(data);
        }

        public static void Publish<T>(this Object _, T data)
        {
            UnityHub.Publish(data);
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

        public static void Subscribe<T>(this Object subscriber, Action<T> messageHandler,
            Predicate<T> messageSelector = null)
        {
            UnityHub.Subscribe(subscriber, messageHandler, messageSelector);
        }

        /// <summary>
        /// Unsubscribes to all messages. 
        /// </summary>
        /// <param name="subscriber">The subscriber</param>
        public static void Unsubscribe(this object subscriber)
        {
            Hub.Unsubscribe(subscriber);
        }

        public static void Unsubscribe(this Object subscriber)
        {
            UnityHub.Unsubscribe(subscriber);
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

        public static void Unsubscribe<T>(this Object subscriber)
        {
            UnityHub.Unsubscribe(subscriber, (Action<T>)null);
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

        public static void Unsubscribe<T>(this Object subscriber, Action<T> messageHandler)
        {
            UnityHub.Unsubscribe(subscriber, messageHandler);
        }
    }
}
