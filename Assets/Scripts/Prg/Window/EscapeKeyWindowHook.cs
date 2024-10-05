using Prg.Window.ScriptableObjects;
using UnityEngine;

namespace Prg.Window
{
    /// <summary>
    /// Traps Escape key press and opens this window instead of previous window when Escape key is pressed.
    /// </summary>
    public class EscapeKeyWindowHook : MonoBehaviour
    {
        private const string Tp = "This window will open on top of current (aka previous) window";

        [SerializeField, Tooltip(Tp)] private WindowDef _hookedWindow;

        private void OnEnable()
        {
            WindowManager.Get().RegisterGoBackHandlerOnce(ShowWindow);
        }

        private void OnDisable()
        {
            if (WindowManager.TryGet(out var windowManager))
            {
                windowManager.UnRegisterGoBackHandlerOnce(ShowWindow);
            }
        }

        private WindowManager.GoBackAction ShowWindow()
        {
            var windowManager = WindowManager.Get();
            Debug.Log($"start {_hookedWindow} WindowCount {windowManager.WindowCount}");
            windowManager.ShowWindow(_hookedWindow);
            Debug.Log($"done {_hookedWindow}");
            return WindowManager.GoBackAction.Abort;
        }
    }
}
