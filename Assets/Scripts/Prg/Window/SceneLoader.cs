using Prg.Util;
using Prg.Window.ScriptableObjects;
using UnityEngine;
using UnityEngine.SceneManagement;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

namespace Prg.Window
{
    /// <summary>
    /// Simple scene loader for <c>WindowManager</c>.
    /// </summary>
    internal static class SceneLoader
    {
        public static bool NeedsSceneLoad(WindowDef windowDef)
        {
            return windowDef.HasScene && windowDef.SceneName != SceneManager.GetActiveScene().name;
        }

        public static void LoadScene(WindowDef windowDef)
        {
            if (windowDef.HasScene && windowDef.Scene.IsNetworkScene)
            {
                Debug.Log($"LOAD NETWORK {windowDef}", windowDef);
#if PHOTON_UNITY_NETWORKING
                PhotonNetwork.LoadLevel(scene.SceneName);
                return;
#else
                throw new UnityException("PHOTON_UNITY_NETWORKING not available");
#endif
            }
            Debug.Log($"LOAD LOCAL {windowDef}", windowDef);
            var sceneIndex = windowDef.IsSceneWindow
                ? windowDef.SceneIndex
                : FindFirstSceneIndex(windowDef.Scene.SceneName);
            SceneManager.LoadScene(sceneIndex);
            return;

            int FindFirstSceneIndex(string sceneName)
            {
                var sceneCount = SceneManager.sceneCountInBuildSettings;
                // scenePath = Assets/UiProto/Scenes/10-uiProto.unity
                var unitySceneName = $"/{sceneName}.unity";
                for (var index = 0; index < sceneCount; ++index)
                {
                    var scenePath = SceneUtility.GetScenePathByBuildIndex(index);
                    if (scenePath.EndsWith(unitySceneName))
                    {
                        return index;
                    }
                }
                throw new UnityException($"scene not found: {sceneName}");
            }
        }
    }
}
