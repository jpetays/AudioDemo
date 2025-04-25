using System;
using NaughtyAttributes;
using Prg.Util;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

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
        /// <summary>
        /// NoLocalizeTag is used to skip localization and must be set externally to take effect.
        /// </summary>
        public static string NoLocalizeTag;

        public static Func<string, string> VariableReplacer;

        private const string EmptyMarkerValue = "EMPTY_TEXT";
        private const char KeySeparator = '.';
        private const string PrefabCanvasEnvironment = "Canvas (Environment)/";

        public static string KeyFrom(Component component)
        {
            var path = component.GetFullPath();
            if (path.StartsWith(PrefabCanvasEnvironment))
            {
                path = path.Remove(0, PrefabCanvasEnvironment.Length);
            }
            return path.Replace('/', KeySeparator);
        }

        [Header("Settings")] public bool _isManualUpdate;
        public bool _useVariableReplacement;
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
            MyAssert.AreNotEqual(LocalizedComponentType.Unknown, _componentType,
                "valid componentType is required", this);
            if (_useVariableReplacement)
            {
                this.Localize(VariableReplacer);
            }
            else
            {
                this.Localize();
            }
#if UNITY_EDITOR
            Assert.AreNotEqual("", NoLocalizeTag);
            if (gameObject.CompareTag(NoLocalizeTag))
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
                text = EmptyMarkerValue;
            }
            return text.Length > 50 ? text[..50] : text;
        }
    }
}
