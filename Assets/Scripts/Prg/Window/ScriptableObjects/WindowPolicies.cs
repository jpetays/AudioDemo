using System;
using System.Diagnostics.CodeAnalysis;
using TMPro;
using UnityEngine;

namespace Prg.Window.ScriptableObjects
{
    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class AllowedFonts2
    {
        [Header("Log Settings")] public bool LogAsError;
        public bool ShowFullPath;
        [Header("TextMesh Pro")] public TMP_FontAsset[] TextMeshProFonts;
        [Header("Legacy")] public Font[] LegacyFonts;
    }

    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Debugging
    {
        public bool AddButtonClickLogger;
    }

    [CreateAssetMenu(menuName = "Prg/Prg/" + nameof(WindowPolicies), fileName = nameof(WindowPolicies))]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class WindowPolicies : ScriptableObject
    {
        [SerializeField, Header("Fonts")] public AllowedFonts2 Fonts;

        [SerializeField, Header("Debugging")] public Debugging Debug;
    }
}
