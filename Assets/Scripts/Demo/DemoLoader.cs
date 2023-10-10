using System.Collections;
using UnityEngine;
using Debug = Prg.Debug;

namespace Demo
{
    /// <summary>
    /// Helper for StartCoroutine().
    /// </summary>
    internal class Loader : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log("Awake");
        }

        private void Start()
        {
            Debug.Log("Start");
        }
    }

    internal static class DemoLoader
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void SubsystemRegistration()
        {
            // Manual reset if UNITY Domain Reloading is disabled.
            Debug.Log("SubsystemRegistration");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeSceneLoad()
        {
            Debug.Log("BeforeSceneLoad");
            var parent = new GameObject(nameof(DemoLoader));
            var loader = parent.AddComponent<Loader>();
            loader.StartCoroutine(LoadServicesCoroutine());
        }
        private static IEnumerator LoadServicesCoroutine()
        {
            Debug.Log("LoadServicesCoroutine start");
            yield return null;
            Debug.Log("LoadServicesCoroutine exit");
        }
    }
}
