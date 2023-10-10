﻿using Prg.Window.ScriptableObjects;
using UnityEngine;

namespace Prg.Window
{
    /// <summary>
    /// Traps Escape key press and replaces current window with this window if Escape key is pressed.
    /// </summary>
    public class EscapeKeyRedirect : MonoBehaviour
    {
        [SerializeField] private WindowDef _redirectWindow;

        private void OnEnable()
        {
            WindowManager.Get().RegisterGoBackHandlerOnce(DoRedirect);
        }

        private void OnDisable()
        {
            WindowManager.Get().UnRegisterGoBackHandlerOnce(DoRedirect);
        }

        private WindowManager.GoBackAction DoRedirect()
        {
            var windowManager = WindowManager.Get();
            Debug.Log($"start {_redirectWindow} WindowCount {windowManager.WindowCount}");
            windowManager.PopCurrentWindow();
            windowManager.ShowWindow(_redirectWindow);
            Debug.Log($"done {_redirectWindow}");
            return WindowManager.GoBackAction.Abort;
        }
    }
}
