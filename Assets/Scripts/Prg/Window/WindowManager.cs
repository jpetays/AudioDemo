using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Prg.Window.ScriptableObjects;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Prg.Window
{
    /// <summary>
    /// Runtime class to hold <c>WindowDef</c> and its related 'window' instance (aka <c>GameObject</c>).
    /// </summary>
    public class MyWindow
    {
        private readonly WindowDef _windowDef;
        private GameObject _windowInst;
        private string _debugState;

        public GameObject WindowInst => _windowInst;
        public WindowDef WindowDef => _windowDef;
        public bool IsValid => _windowInst != null;

        public MyWindow(WindowDef windowDef, GameObject window)
        {
            _windowDef = windowDef;
            SetWindow(window);
        }

        public void SetWindow(GameObject window)
        {
            _windowInst = window;
            _debugState = window != null
                ? $"{Mathf.Abs(window.GetInstanceID()):x}:{_windowDef}"
                : $"0:{_windowDef}";
            Debug.Log(_debugState);
        }

        public void Invalidate()
        {
            if (_debugState.StartsWith("X"))
            {
                return;
            }
            _windowInst = null;
            _debugState = $"X:{_debugState}";
        }

        public override string ToString()
        {
            return _debugState;
        }
    }

    /// <summary>
    /// Internal implementation to handle window showing and hiding so that <c>WindowManager</c> does not need to know anything about this.
    /// </summary>
    /// <remarks>
    /// This also makes it possible to debug lod window management in better granularity.
    /// </remarks>
    internal static class WindowActivator
    {
        public static void Show(MyWindow window)
        {
            Debug.Log($"Show {window.WindowDef}", window.WindowInst);
            window.WindowInst.SetActive(true);
        }

        public static void Hide(MyWindow window)
        {
            Debug.Log($"Hide {window.WindowDef}", window.WindowInst);
            if (window.IsValid)
            {
                window.WindowInst.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Simple <c>WindowManager</c> with managed window bread crumbs list.
    /// </summary>
    public class WindowManager : MonoBehaviour, IWindowManager
    {
        public enum GoBackAction
        {
            Continue,
            Abort
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void SubsystemRegistration()
        {
            // Manual reset if UNITY Domain Reloading is disabled.
            _windowManager = null;
            _isApplicationQuitting = false;
        }

        public static IWindowManager Get()
        {
            if (_isApplicationQuitting)
            {
                // Can throw some nasty looking errors :-(
                return null;
            }
            return _windowManager ??= UnitySingleton.CreateStaticSingleton<WindowManager>();
        }

        public static bool TryGet(out IWindowManager windowManager)
        {
            if (_isApplicationQuitting)
            {
                windowManager = null;
                return false;
            }
            windowManager = _windowManager;
            return windowManager != null;
        }

        public static bool IsCreated() => _windowManager != null && !_isApplicationQuitting;

        private static IWindowManager _windowManager;
        private static bool _isApplicationQuitting;

        private List<MyWindow> _currentWindows;
        private List<MyWindow> _knownWindows;

        [SerializeField] private List<string> _currentWindowsList;
        [SerializeField] private List<string> _knownWindowsList;

        private GameObject _windowsParent;
        private WindowDef _pendingWindow;
        private bool _hasPendingWindow;

        private List<Func<GoBackAction>> _goBackOnceHandler;
        private int _executionLevel;

        private void Awake()
        {
            Debug.Log("Awake");
            _currentWindows = new List<MyWindow>();
            _knownWindows = new List<MyWindow>();
            _currentWindowsList = new List<string>();
            _knownWindowsList = new List<string>();

            SceneManager.sceneLoaded += SceneLoaded;
            SceneManager.sceneUnloaded += SceneUnloaded;
            var handler = gameObject.AddComponent<EscapeKeyHandler>();
            handler.SetCallback(EscapeKeyPressed);
            Application.quitting += () => _isApplicationQuitting = true;
            ResetState();
        }

        private void ResetState()
        {
            _currentWindows.Clear();
            _knownWindows.Clear();
            _currentWindowsList.Clear();
            _knownWindowsList.Clear();
            _pendingWindow = null;
            _hasPendingWindow = false;
            _goBackOnceHandler = null;
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_hasPendingWindow)
            {
                return;
            }
            var pendingWindow = _pendingWindow;
            _pendingWindow = null;
            _hasPendingWindow = false;
            // ReSharper disable once Unity.UnknownTag
            var instance = GameObject.FindWithTag(WindowLoader.TagName);
            if (instance != null)
            {
                var windowLoader = instance.GetComponent<WindowLoader>();
                if (windowLoader != null && windowLoader.WillYouLoadThis(pendingWindow))
                {
                    Debug.Log($"sceneLoaded {scene.GetFullName()} pending {_pendingWindow} SKIP");
                    return;
                }
            }
            Debug.Log($"sceneLoaded {scene.GetFullName()} pending {_pendingWindow}");
            ((IWindowManager)this).ShowWindow(pendingWindow);
        }

        private void SceneUnloaded(Scene scene)
        {
            Debug.Log(
                $"sceneUnloaded {scene.GetFullName()} knownWindows {_knownWindows.Count} pending {_pendingWindow}");
            _knownWindows.Clear();
            _knownWindowsList.Clear();
            _windowsParent = null;
            _goBackOnceHandler = null;
        }

        void IWindowManager.SetWindowsParent([AllowNull] GameObject windowsParent)
        {
            _windowsParent = windowsParent;
        }

        private void EscapeKeyPressed()
        {
            ((IWindowManager)this).GoBack();
        }

        void IWindowManager.RegisterGoBackHandlerOnce(Func<GoBackAction> handler)
        {
            _goBackOnceHandler ??= new List<Func<GoBackAction>>();
            if (!_goBackOnceHandler.Contains(handler))
            {
                _goBackOnceHandler.Add(handler);
            }
        }

        void IWindowManager.UnRegisterGoBackHandlerOnce(Func<GoBackAction> handler)
        {
            _goBackOnceHandler?.Remove(handler);
        }

        WindowDef IWindowManager.CurrentWindow
        {
            get
            {
                if (_currentWindows.Count == 0)
                {
                    return null;
                }
                var currentWindow = _currentWindows[0];
                return currentWindow.WindowDef;
            }
        }

        int IWindowManager.WindowCount => _currentWindows.Count;

        [MaybeNull] List<MyWindow> IWindowManager.WindowStack => _currentWindows;

        int IWindowManager.FindIndex(WindowDef windowDef)
        {
            return _currentWindows.FindIndex(x => x.WindowDef == windowDef);
        }

        void IWindowManager.GoBack()
        {
            Debug.Log($"GoBack count {_currentWindows.Count} handler {_goBackOnceHandler?.Count ?? -1}");
            if (_goBackOnceHandler != null)
            {
                // Callbacks can register again!
                var tempHandler = _goBackOnceHandler;
                _goBackOnceHandler = null;
                var goBackResult = InvokeCallbacks(tempHandler);
                if (goBackResult == GoBackAction.Abort)
                {
                    Debug.Log($"GoBack interrupted by handler");
                    return;
                }
            }
            if (_currentWindows.Count <= 1)
            {
                AppPlatform.ExitGracefully();
                return;
            }
            PopAndHide();
            if (_currentWindows.Count == 0)
            {
                Debug.Log($"NOTE! GoBack has no windows to show");
                return;
            }
            var currentWindow = _currentWindows[0];
            if (currentWindow.IsValid)
            {
                WindowActivator.Show(currentWindow);
                return;
            }
            // Re-create the window
            _currentWindows.RemoveAt(0);
            _currentWindowsList.RemoveAt(0);
            if (currentWindow.WindowDef.IsSceneWindow)
            {
                // Reset totally: typically used for debugging without window def and prefab.
                _currentWindows.Clear();
                _currentWindowsList.Clear();
                SceneLoader.LoadScene(currentWindow.WindowDef);
                return;
            }
            ((IWindowManager)this).ShowWindow(currentWindow.WindowDef);
        }

        void IWindowManager.Unwind(WindowDef unwindWindowDef)
        {
            if (unwindWindowDef != null)
            {
                SafeExecution(DoUnwind);
                return;
            }
            _currentWindows.Clear();
            _currentWindowsList.Clear();
            return;

            void DoUnwind()
            {
                while (_currentWindows.Count > 1)
                {
                    var stackWindow = _currentWindows[1];
                    if (stackWindow.WindowDef.Equals(unwindWindowDef))
                    {
                        break;
                    }
                    Debug.Log($"Unwind RemoveAt {stackWindow} count {_currentWindows.Count}");
                    _currentWindows.RemoveAt(1);
                    _currentWindowsList.RemoveAt(1);
                }
                // Add if required - note that window prefab will not be instantiated now!
                var insertionIndex = 0;
                if (_currentWindows.Count == 1)
                {
                    var stackWindow = _currentWindows[0];
                    insertionIndex = stackWindow.WindowDef.Equals(unwindWindowDef) ? -1 : 1;
                }
                else if (_currentWindows.Count > 1)
                {
                    var stackWindow = _currentWindows[1];
                    insertionIndex = stackWindow.WindowDef.Equals(unwindWindowDef) ? -1 : 1;
                }
                if (insertionIndex >= 0)
                {
                    var currentWindow = new MyWindow(unwindWindowDef, null);
                    Debug.Log($"Unwind Insert {currentWindow} count {_currentWindows.Count} index {insertionIndex}");
                    _currentWindows.Insert(insertionIndex, currentWindow);
                    _currentWindowsList.Insert(insertionIndex, currentWindow.ToString());
                }
            }
        }

        void IWindowManager.UnwindNaviHelper(WindowDef naviTarget)
        {
            // Check if navigation target window is already in window stack and we are actually going back to it.
            var windowCount = ((IWindowManager)this).WindowCount;
            if (windowCount <= 1)
            {
                return;
            }
            Debug.Log($"UNWIND 1 {naviTarget}");
            var targetIndex = ((IWindowManager)this).FindIndex(naviTarget);
            switch (targetIndex)
            {
                case < 1:
                    return;
                case 1:
                    ((IWindowManager)this).GoBack();
                    return;
                default:
                    ((IWindowManager)this).Unwind(naviTarget);
                    ((IWindowManager)this).GoBack();
                    break;
            }
        }

        void IWindowManager.ShowWindow(WindowDef windowDef)
        {
            {
                Debug.Log($"SHOW 1 {windowDef}");
                foreach (var known in _knownWindows)
                {
                    Debug.Log($"KNOW 1 {known}");
                }
                foreach (var current in _currentWindows)
                {
                    Debug.Log($"CURR 1 {current}");
                }
            }
            SafeExecution(DoShowWindow);
            {
                Debug.Log($"SHOW 2 {windowDef}");
                foreach (var known in _knownWindows)
                {
                    Debug.Log($"KNOW 2 {known}");
                }
                foreach (var current in _currentWindows)
                {
                    Debug.Log($"CURR 2 {current}");
                }
            }
            return;

            void DoShowWindow()
            {
                Assert.IsNotNull(windowDef, "windowDef != null");
                if (SceneLoader.NeedsSceneLoad(windowDef))
                {
                    _pendingWindow = windowDef;
                    _hasPendingWindow = true;
                    InvalidateWindowsForSceneUnload(_currentWindows, _currentWindowsList);
                    SceneLoader.LoadScene(windowDef);
                    Debug.Log($"LoadWindow {windowDef} pendingWindow exit");
                    return;
                }
                if (_hasPendingWindow && !_pendingWindow.Equals(windowDef))
                {
                    Debug.Log($"LoadWindow IGNORE {windowDef} PENDING {_pendingWindow}");
                    return;
                }
                if (IsVisible(windowDef))
                {
                    Debug.Log($"LoadWindow ALREADY IsVisible {windowDef}");
                    return;
                }
                var currentWindow =
                    _knownWindows.FirstOrDefault(x => windowDef.Equals(x.WindowDef))
                    ?? CreateWindow(windowDef);
                if (_currentWindows.Count > 0)
                {
                    var previousWindow = _currentWindows[0];
                    // It seems that currentWindow can be previousWindow due to some misconfiguration or missing configuration
                    if (currentWindow.WindowDef.Equals(previousWindow.WindowDef))
                    {
                        // We must accept this fact - for now - and can not do anything about it (but remove it).
                        Debug.Log(
                            $"ShowWindow {windowDef} is already in window stack ({_currentWindows.Count}) - when it possibly should not be");
                        PopAndHide();
                    }
                    else if (previousWindow.WindowDef.IsPopOutWindow)
                    {
                        PopAndHide();
                    }
                    else
                    {
                        if (currentWindow.WindowDef.IsSceneWindow &&
                            currentWindow.WindowDef.SceneName == previousWindow.WindowDef.SceneName)
                        {
                            // Window prefab has already been loaded and
                            // now WindowLoader is trying to load same window in the scene.
                            // We need to remove this completely because they are 'duplicates'.
                            // Note that SceneLoaded _hasPendingWindow should prevent that this happens!
                            PopAndHide();
                        }
                        else
                        {
                            WindowActivator.Hide(previousWindow);
                        }
                    }
                }
                if (!currentWindow.IsValid)
                {
                    Debug.Log($"CreateWindowPrefab {windowDef}");
                    currentWindow.SetWindow(CreateWindowPrefab(currentWindow.WindowDef));
                }
                _currentWindows.Insert(0, currentWindow);
                _currentWindowsList.Insert(0, currentWindow.ToString());
                WindowActivator.Show(currentWindow);
            }
        }

        void IWindowManager.PopCurrentWindow()
        {
            if (_currentWindows.Count == 0)
            {
                return;
            }
            SafeExecution(PopAndHide);
        }

        private void SafeExecution(Action action)
        {
            // Window manager operations can not be interleaved but must be sequential.
            Assert.IsTrue(_executionLevel == 0, "_executionLevel == 0");
            _executionLevel += 1;
            try
            {
                action();
            }
            catch (Exception)
            {
                _executionLevel = 0;
                throw;
            }
            _executionLevel -= 1;
            Assert.IsTrue(_executionLevel == 0, "_executionLevel == 0");
        }

        private MyWindow CreateWindow(WindowDef windowDef)
        {
            Assert.IsTrue(windowDef.HasPrefab, $"windowDef prefab has been destroyed: {windowDef}");
            Debug.Log($"CreateWindow {windowDef} count {_currentWindows.Count}");
            var prefab = CreateWindowPrefab(windowDef);
            var currentWindow = new MyWindow(windowDef, prefab);
            _knownWindows.Add(currentWindow);
            _knownWindowsList.Add(currentWindow.ToString());
            CheckWindowPolicy(currentWindow);
            return currentWindow;
        }

        [Conditional("UNITY_EDITOR")]
        private static void CheckWindowPolicy(MyWindow window)
        {
            if (!window.IsValid)
            {
                return;
            }
            window.WindowInst.AddComponent<WindowPolicyChecker>();
        }

        private GameObject CreateWindowPrefab(WindowDef windowDef)
        {
            var prefab = windowDef.WindowPrefab;
            var isSceneObject = prefab.scene.handle != 0;
            if (isSceneObject)
            {
                return prefab;
            }
            prefab = _windowsParent == null
                ? Instantiate(prefab)
                : Instantiate(prefab, _windowsParent.transform);
            prefab.name = prefab.name.Replace("(Clone)", string.Empty);
            return prefab;
        }

        private void PopAndHide()
        {
            Assert.IsTrue(_currentWindows.Count > 0, "_currentWindows.Count > 0");
            var firstWindow = _currentWindows[0];
            _currentWindows.RemoveAt(0);
            _currentWindowsList.RemoveAt(0);
            WindowActivator.Hide(firstWindow);
        }

        private bool IsVisible(WindowDef windowDef)
        {
            if (_currentWindows.Count == 0)
            {
                return false;
            }
            var firstWindow = _currentWindows[0];
            var isVisible = windowDef.Equals(firstWindow.WindowDef) && firstWindow.IsValid;
            Debug.Log($"IsVisible new {windowDef} first {firstWindow} : {isVisible}");
            return isVisible;
        }

        private static void InvalidateWindowsForSceneUnload(List<MyWindow> windows, List<string> windowsList)
        {
            windowsList.Clear();
            foreach (var window in windows)
            {
                window.Invalidate();
                windowsList.Add(window.ToString());
            }
        }

        private static GoBackAction InvokeCallbacks(List<Func<GoBackAction>> callbackList)
        {
            var goBackResult = GoBackAction.Continue;
            foreach (var func in callbackList)
            {
                var result = func();
                Debug.Log($"invokeResult {func} = {result}");
                if (result == GoBackAction.Abort)
                {
                    goBackResult = GoBackAction.Abort;
                }
            }
            Debug.Log($"InvokeCallbacks : {goBackResult}");
            return goBackResult;
        }
    }
}
