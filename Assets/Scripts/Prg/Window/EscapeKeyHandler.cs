using System;
using Prg.Window.ScriptableObjects;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace Prg.Window
{
    /// <summary>
    /// Tracks Escape key press (and similar functionality on other devices) on behalf of <c>WindowManager</c>
    /// using <c>UnityEngine.InputSystem</c> to listen given <c>InputAction</c> for Navigation/Back action.
    /// </summary>
    /// Actual <c>InputAction</c> is configured in Editor with separate <c>ScriptableObject</c>.
    /// <remarks>
    /// </remarks>
    public class EscapeKeyHandler : MonoBehaviour
    {
        [SerializeField] private InputActionReference _escapeInputActionRef;

        private Action _callback;

        private void Awake()
        {
            var escapeInputAction = Resources.Load<EscapeInputAction>(nameof(EscapeInputAction));
            Assert.IsNotNull(escapeInputAction, "escapeInputAction != null, Create it in Resources folder");
            _escapeInputActionRef = escapeInputAction._escapeInputAction;
            Assert.IsNotNull(_escapeInputActionRef,
                "_escapeInputActionRef != null, Create 'EscapeInputAction' in Resources folder");
            Debug.Log($"escapeInputActionRef {_escapeInputActionRef}", _escapeInputActionRef);
        }

        private void OnEnable()
        {
            _escapeInputActionRef.action.performed += OnEscapeActionPerformed;
        }

        private void OnDisable()
        {
            _escapeInputActionRef.action.performed -= OnEscapeActionPerformed;
        }

        private void OnDestroy()
        {
            _escapeInputActionRef.action.performed -= OnEscapeActionPerformed;
        }

        private void OnEscapeActionPerformed(InputAction.CallbackContext ctx)
        {
            Debug.Log($"{_callback?.Method} {ctx.action.name}", _escapeInputActionRef);
            _callback?.Invoke();
        }

        public void SetCallback(Action callback)
        {
            Debug.Log($"{_callback?.Method} <- {callback.Method}");
            _callback = callback;
        }
    }
}
