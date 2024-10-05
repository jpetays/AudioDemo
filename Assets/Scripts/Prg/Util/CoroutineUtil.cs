using System;
using System.Collections;
using UnityEngine;

namespace Prg.Util
{
    public class CoroutineHost : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void SubsystemRegistration()
        {
            // Manual reset if UNITY Domain Reloading is disabled.
            _hasCoroutineHost = false;
        }

        private static bool _hasCoroutineHost;
        private static CoroutineHost _coroutineHost;

        public static CoroutineHost Instance
        {
            get
            {
                if (!_hasCoroutineHost)
                {
                    _coroutineHost = UnitySingleton.CreateStaticSingleton<CoroutineHost>();
                }
                return _coroutineHost;
            }
        }
    }

    public static class CoroutineUtil
    {
        #region Coroutine Extension Methods

        /// <summary>
        /// Execute an action once as coroutine on next frame.
        /// </summary>
        public static void ExecuteOnNextFrame(this MonoBehaviour component, Action action)
        {
            ExecuteAsCoroutine(component, action);
        }

        /// <summary>
        /// Execute an action once as coroutine.
        /// </summary>
        public static void ExecuteAsCoroutine(this MonoBehaviour component, Action action, YieldInstruction wait = null)
        {
            component.StartCoroutine(ExecuteAfterDelay());
            return;

            IEnumerator ExecuteAfterDelay()
            {
                yield return wait;
                action();
            }
        }

        /// <summary>
        /// Execute a bool function as coroutine until it returns false.
        /// </summary>
        public static void ExecuteAsCoroutine(this MonoBehaviour component, Func<bool> function,
            YieldInstruction wait = null)
        {
            component.StartCoroutine(ExecuteWhile());
            return;

            IEnumerator ExecuteWhile()
            {
                for (;;)
                {
                    yield return wait;
                    if (!function())
                    {
                        yield break;
                    }
                }
            }
        }

        #endregion
    }
}
