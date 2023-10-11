using System.Collections;
using Prg;
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
            Debug.Log($"Loader start {RichText.Yellow($"frame #{Time.frameCount}")}");
            // Audio needs to be started in Start()!
            Audio.AudioSettings.Initialize();
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
            loader.StartCoroutine(LoadServicesAsync());
        }
        private static IEnumerator LoadServicesAsync()
        {
            Debug.Log($"LoadServicesAsync start {RichText.Yellow($"frame #{Time.frameCount}")}");
            yield return null;
            Debug.Log("LoadServicesAsync exit");
        }
    }
}
