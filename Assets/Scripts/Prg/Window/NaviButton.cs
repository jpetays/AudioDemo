using Prg.Window.ScriptableObjects;
using UnityEngine;
using UnityEngine.UI;

namespace Prg.Window
{
    /// <summary>
    /// Default navigation button for <c>WindowManager</c>.
    /// </summary>
    /// <remarks>
    /// <c>Button</c> initial <c>interactable</c> state can be set in Editor and later by code.
    /// </remarks>
    [RequireComponent(typeof(Button))]
    public class NaviButton : MonoBehaviour
    {
        private const string Tooltip = "Pop out and hide current window before showing target window";

        [Header("Settings"), SerializeField] private WindowDef _naviTarget;
        [Tooltip(Tooltip), SerializeField] private bool _isCurrentPopOutWindow;

        private void Start()
        {
            Debug.Log($"{name}", this);
            var button = GetComponent<Button>();
            if (_naviTarget == null)
            {
                button.interactable = false;
                return;
            }
            var windowManager = WindowManager.Get();
            var isCurrentWindow = windowManager.FindIndex(_naviTarget) == 0;
            if (isCurrentWindow)
            {
                button.interactable = false;
                return;
            }
            button.onClick.AddListener(OnNaviButtonClick);
        }

        private void OnNaviButtonClick()
        {
            Debug.Log($"naviTarget {_naviTarget} isCurrentPopOutWindow {_isCurrentPopOutWindow}", _naviTarget);
            var windowManager = WindowManager.Get();
            if (_isCurrentPopOutWindow)
            {
                windowManager.PopCurrentWindow();
            }
            windowManager.UnwindNaviHelper(_naviTarget);
            windowManager.ShowWindow(_naviTarget);
        }
    }
}
