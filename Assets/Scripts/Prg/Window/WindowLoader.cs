using System.Collections;
using NaughtyAttributes;
using Prg.Window.ScriptableObjects;
using UnityEngine;
using UnityEngine.Assertions;

namespace Prg.Window
{
    /// <summary>
    /// Initial window loader for any level that uses <c>WindowManager</c>.
    /// </summary>
    public class WindowLoader : MonoBehaviour
    {
        /// <summary>
        /// <c>WindowManager</c> uses this tag to find <c>WindowLoader</c> from scene.
        /// </summary>
        public const string TagName = nameof(WindowLoader);

        private const string Notes =
            "Loads given window first time when this level is loaded using Window Manager.\r\n" +
            "Window can be a Prefab or GameObject or both.\r\n" +
            "This object is set as root for all window objects.";

        private const string Tp1 = "The window prefab to show";
        private const string Tp2 = "Reset Window Stack (e.g. for maina menu)";
        private const string Tp3 = "Optional Scene Window to show (when scene loads first time)";

        [InfoBox(Notes)]
        [SerializeField, Tooltip(Tp1), Header("Window to Show")] private WindowDef _windowPrefab;

        [SerializeField, Tooltip(Tp2)] private bool _resetWindowsStack;
        [SerializeField, Tooltip(Tp3)] private GameObject _sceneWindow;

        private static int _windowCounter;

        public static bool TryGet(out WindowLoader windowLoader)
        {
            windowLoader = FindFirstObjectByType<WindowLoader>();
            return windowLoader != null;
        }

        public bool WillYouLoadThis(WindowDef windowDef)
        {
            return _sceneWindow != null && _sceneWindow.name == windowDef.WindowName;
        }

        public bool IsWindowVisible { get; private set; }

        private void Awake()
        {
            // ReSharper disable once Unity.UnknownTag
            Assert.IsTrue(CompareTag(TagName));
        }

        private IEnumerator Start()
        {
            if (_sceneWindow == null)
            {
                yield return FirstTimeGameDelay();
            }
            var windowManager = WindowManager.Get();
            windowManager.SetWindowsParent(gameObject);
            var windowDef = GetWindowToLoad();
            if (_resetWindowsStack)
            {
                // Brute force, remove everything!
                while (windowManager.WindowCount > 0)
                {
                    windowManager.PopCurrentWindow();
                }
            }
            else
            {
                windowManager.UnwindNaviHelper(windowDef);
            }
            Debug.Log($"{this}", windowDef);
            windowManager.ShowWindow(windowDef);
            yield return null;
            IsWindowVisible = true;
            Debug.Log($"{this}", windowDef);
            if (Time.frameCount < 10)
            {
                Debug.Log($"{RichText.Yellow($"frame #{Time.frameCount}")} done");
            }
        }

        private WindowDef GetWindowToLoad()
        {
            if (_sceneWindow != null)
            {
                return LoadSceneObjectAsWindow();
            }
            Assert.IsNotNull(_windowPrefab, "_windowPrefab != null");
            Assert.IsNotNull(_windowPrefab.WindowPrefab, "_windowPrefab.WindowPrefab != null");
            return _windowPrefab;

            WindowDef LoadSceneObjectAsWindow()
            {
                var canvas = GetComponentInChildren<Canvas>();
                Assert.IsNotNull(canvas, "Canvas not found and Window is null, Canvas must be child of WindowLoader");
                var windowDef = ScriptableObject.CreateInstance<WindowDef>();
                windowDef.name = $"noname[{++_windowCounter}]";
                windowDef.SetWindowPrefab(_sceneWindow);
                return windowDef;
            }
        }

        private static IEnumerator FirstTimeGameDelay()
        {
            const int framesToWait = 3;
            if (Time.frameCount > framesToWait)
            {
                yield break;
            }
            // Let game startup continue few frames before we start loading our first window.
            while (Time.frameCount <= framesToWait)
            {
                yield return null;
            }
            Debug.Log($"WindowLoader {RichText.Yellow($"frame #{Time.frameCount}")} start");
        }

        public override string ToString()
        {
            return $"{nameof(_windowPrefab)}: {_windowPrefab}, {nameof(_sceneWindow)}: {_sceneWindow}" +
                   $", {nameof(IsWindowVisible)}: {IsWindowVisible}";
        }
    }
}
