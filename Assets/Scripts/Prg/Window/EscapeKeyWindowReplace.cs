using Prg.Window.ScriptableObjects;
using UnityEngine;

namespace Prg.Window
{
    /// <summary>
    /// Traps Escape key press and replaces current window with this window when Escape key is pressed.
    /// </summary>
    public class EscapeKeyWindowReplace : MonoBehaviour
    {
        private const string Tp = "This window will replace current (aka previous) window";

        [SerializeField, Tooltip(Tp)] private WindowDef _replacedWindow;

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
            Debug.Log($"start {_replacedWindow} WindowCount {windowManager.WindowCount}");
            windowManager.PopCurrentWindow();
            windowManager.ShowWindow(_replacedWindow);
            Debug.Log($"done {_replacedWindow}");
            return WindowManager.GoBackAction.Abort;
        }
    }
}
