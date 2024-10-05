using NaughtyAttributes;
using Prg.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

namespace Prg.Localization
{
    public enum LocalizedComponentType
    {
        Unknown,
        Text,
        Button,
        Toggle,
    }

    /// <summary>
    /// Localized component to hold localization key and default value for text (if key not found).
    /// </summary>
    public class Localized : MonoBehaviour
    {
        public const string NoLocalize = nameof(NoLocalize);
        public const string EmptyMarkerValue = "EMPTY_TEXT";

        public const char KeySeparator = '.';

        public static string KeyFrom(Component component)
        {
            return component.GetFullPath().Replace('/', KeySeparator);
        }

        [Header("Settings")] public bool _isManualUpdate;
        public string _key;
        [ReadOnly, AllowNesting] public string _defaultText;

        [Header("Text Component"), ReadOnly, AllowNesting] public LocalizedComponentType _componentType;
        [ReadOnly, AllowNesting] public TMP_Text _textMeshPro;

        public string SafeText => _textMeshPro != null ? _textMeshPro.text : EmptyMarkerValue;

        private bool _hasTextComponent;

        private void Awake()
        {
            _hasTextComponent = _textMeshPro != null;
            if (!_hasTextComponent)
            {
                Debug.LogError($"textMeshPro is NULL for {_componentType} [{_key}]: {this.GetFullPath()}", this);
                return;
            }
            if (_componentType == LocalizedComponentType.Button)
            {
                // Force button text to be one liner!
                _textMeshPro.enableWordWrapping = false;
                _textMeshPro.overflowMode = TextOverflowModes.Overflow;
            }
            Debug.Log($"[{_key}]: {_textMeshPro.text}");
        }

        public void SetText(string text)
        {
            if (!_hasTextComponent)
            {
                return;
            }
            Debug.Log($"[{_key}]: {_textMeshPro.text} <- {text}");
            _textMeshPro.text = text;
        }

        public void SetColor(Color color)
        {
            if (!_hasTextComponent)
            {
                return;
            }
            _textMeshPro.color = color;
        }

        private void OnEnable()
        {
            MyAssert.AreNotEqual(LocalizedComponentType.Unknown, _componentType, "valid componentType is required",
                this);
            this.Localize();
#if UNITY_EDITOR
            if (gameObject.CompareTag(NoLocalize))
            {
                SetColor(Localizer.MissingKeyColor);
                Debug.LogError($"NoLocalize Tag in: {this.GetFullPath()}", this);
            }
#endif
        }

        public override string ToString()
        {
            return $"key={_key}, text={SanitizeText(_defaultText)}";
        }

        public static string SanitizeText(string text)
        {
            text = RegexUtil.RemoveAllTags(text);
            text = RegexUtil.RemoveAllEmptyLines(text);
            text = RegexUtil.ReplaceCrLf(text, ". ");
            if (text.Length == 0)
            {
                text = Localized.EmptyMarkerValue;
            }
            return text.Length > 50 ? text[..50] : text;
        }
    }
}
