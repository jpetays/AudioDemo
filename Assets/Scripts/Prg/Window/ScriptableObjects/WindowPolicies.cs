using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using NaughtyAttributes;
using TMPro;
using UnityEngine;

namespace Prg.Window.ScriptableObjects
{
    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class AllowedFonts2
    {
        private const string I1 = "Allowed TextMesh Pro Fonts list";
        private const string I2 = "This list should be empty!";

        [Header("Log Settings")] public bool LogAsError;
        public bool ShowFullPath;

        [Header("TextMesh Pro"), InfoBox(I1)] public TMP_FontAsset[] TextMeshProFonts;

        [Header("Legacy Fonts"), InfoBox(I2)] public Font[] LegacyFonts;
    }

    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class ScrollRect2
    {
        private const string I1 = "The sensitivity to scroll wheel and track pad scroll events.";
        private const string I2 = "Fix Scroll Sensitivity if it different than default value";

        [InfoBox(I1)] public float DefaultScrollSensitivity = 1.0f;
        [InfoBox(I2)] public bool FixScrollSensitivity;
    }

    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Debugging
    {
        public bool LogAllCheckedCanvases;
        public bool AddButtonClickLogger;
    }

    [CreateAssetMenu(menuName = "Prg/Prg/" + nameof(WindowPolicies), fileName = nameof(WindowPolicies))]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class WindowPolicies : ScriptableObject
    {
        [SerializeField, Header("Fonts")] public AllowedFonts2 Fonts;

        [SerializeField, Header("Scroll Rect")] public ScrollRect2 ScrollRect;

        [SerializeField, Header("Debugging")] public Debugging Debug;
    }
}
