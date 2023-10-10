using TMPro;
using UnityEngine;

namespace Prg.Window.ScriptableObjects
{
    [CreateAssetMenu(menuName = "Prg/AllowedFonts", fileName = nameof(AllowedFonts))]
    public class AllowedFonts : ScriptableObject
    {
        [Header("TextMesh Pro")] public TMP_FontAsset[] _tmpFonts;
        [Header("Legacy")] public Font[] _fonts;
    }
}
