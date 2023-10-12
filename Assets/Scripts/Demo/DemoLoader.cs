using System.Collections;
using Demo.Audio;
using Prg;
using UnityEngine;
using Debug = Prg.Debug;

namespace Demo
{
    internal class DemoLoader : MonoBehaviour
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
            var loader = parent.AddComponent<DemoLoader>();
            // Start async services ASAP.
            loader.StartCoroutine(LoadServicesAsync());
        }

        private void Awake()
        {
            Debug.Log("Awake");
        }

        private void Start()
        {
            Debug.Log($"Start() {RichText.Yellow($"frame #{Time.frameCount}")} start");
            // UNITY Audio Mixer needs to be started in Start()!
            AudioConfig.Initialize();
            Debug.Log($"Start() {RichText.Yellow($"frame #{Time.frameCount}")} done");
        }

        private static IEnumerator LoadServicesAsync()
        {
            Debug.Log($"LoadServicesAsync {RichText.Yellow($"frame #{Time.frameCount}")} start");
            yield return null;
            if (AppPlatform.IsMobile)
            {
                MobileAudio.Initialize();
            }
            Debug.Log($"LoadServicesAsync {RichText.Yellow($"frame #{Time.frameCount}")} done");
        }
    }
}
