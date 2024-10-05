using System.Collections;
using System.Text;
using Demo.Audio;
using Prg;
using Prg.Util;
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
            LogConfig.Create();
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
            var startupMessage = new StringBuilder()
                .Append(" Game ").Append(Application.productName)
                .Append(" Ver ").Append(Application.version)
                .Append(" Plat ").Append(AppPlatform.IsSimulator ? "Simulator" : AppPlatform.Name)
                .Append(" Screen ").Append(AppPlatform.Resolution())
                .ToString();
            Debug.Log(startupMessage);
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
