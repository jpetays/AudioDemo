using UnityEngine;
using UnityEngine.SceneManagement;

namespace Prg.Window.ScriptableObjects
{
    /// <summary>
    /// Window definition for <c>WindowManager</c>.<br />
    /// It consists of window prefab (or runtime scene object) and
    /// optional scene  definition (or runtime scene build index).
    /// </summary>
    [CreateAssetMenu(menuName = "Prg/Prg/WindowDef", fileName = "window NAME")]
    public class WindowDef : ScriptableObject
    {
        private const string Tooltip = "Pop out and hide this window before showing any other window";

        [SerializeField] private GameObject _windowPrefab;
        [Tooltip(Tooltip), SerializeField] private bool _isPopOutWindow;
        [SerializeField] private SceneDef _scene;

        public bool HasScene => _scene != null;
        public bool HasPrefab => _windowPrefab != null;
        public bool IsPopOutWindow => _isPopOutWindow;
        public GameObject WindowPrefab => _windowPrefab;
        public string WindowName => HasPrefab ? _windowPrefab.name : string.Empty;
        public string SceneName => HasScene ? _scene.SceneName : string.Empty;
        public SceneDef Scene => _scene;
        public bool IsSceneWindow { get; private set; }
        public int SceneIndex { get; private set; } = -1;

        public void SetWindowPrefab(GameObject sceneWindow)
        {
            // This WindowDef was created on-the-fly just for this scene.
            _windowPrefab = sceneWindow;
            IsSceneWindow = true;
            var scene = SceneManager.GetActiveScene();
            _scene = CreateInstance<SceneDef>();
            _scene.SetSceneName(scene.name);
            SceneIndex = SceneManager.GetActiveScene().buildIndex;
        }

        public override string ToString()
        {
            return
                $"{WindowName}" +
                $"[{(HasScene ? _scene.SceneName : "*")}:{(IsSceneWindow ? $"{SceneIndex}" : "*")}]" +
                $"{(IsPopOutWindow ? ":PopOut" : "")}";
        }
    }
}
