using Prg.EditorSupport;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Prg.Window.ScriptableObjects
{
    /// <summary>
    /// New Input System Package <c>InputActionReference</c> configuration for ESCAPE or BACK "key" action.
    /// </summary>
    /// <remarks>
    /// For example in "DefaultInputActions" asset, its path is "UI/Cancel" for this action.
    /// </remarks>
    [CreateAssetMenu(menuName = "Prg/Prg/EscapeInputAction", fileName = "EscapeInputAction")]
    public class EscapeInputAction : ScriptableObject
    {
        private const string Notes = "InputActionReference for Escape (Android 'back' key) handling.\r\n" +
                                     "\r\n" +
                                     "This is loaded using Resources.Load by EscapeKeyHandler.\r\n" +
                                     "We can and should use UNITY default 'UI/Cancel' Input Action for our purposes.\r\n" +
                                     "The reference for 'DefaultInputActions' can be found in:\r\n" +
                                     "Packages/InputSystem/InputSystem/Plugins/PlayerInput\r\n" +
                                     "\r\n" +
                                     "Note that in WebGL builds 'raw' ESC key handling is browser and platform dependent.";

        // ReSharper disable once NotAccessedField.Local
        [SerializeField, Header("Notes"), HelpBox(Notes)] private string _notes;

        [Header("Input Action Reference")] public InputActionReference _escapeInputAction;
    }
}
