using UnityEngine;
using UnityEngine.SceneManagement;

namespace Prg.Util
{
    public static class CanvasUtil
    {
        public static Canvas FindLastActiveTopLevelCanvas()
        {
            var currentScene = SceneManager.GetActiveScene();
            var rootGameObjects = currentScene.GetRootGameObjects();
            var index = rootGameObjects.Length;
            while (--index >= 0)
            {
                var child = rootGameObjects[index];
                var foundCanvas = child.GetComponentInChildren<Canvas>();
                if (foundCanvas == null || !foundCanvas.isActiveAndEnabled)
                {
                    continue;
                }
                return foundCanvas;
            }
            return null;
        }
    }
}
