using System.Collections;
using System.Collections.Generic;
using System.Text;
using Prg.Util;
using Prg.Window.ScriptableObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prg.Window
{
    public class WindowPolicyChecker : MonoBehaviour
    {
        private static WindowPolicies _windowPolicies;
        private static bool _isWarning;
        private static bool _isShowFullPath;

        private IEnumerator Start()
        {
            // Wait two frames (to let things get going) before checking "windows policies".
            yield return null;
            yield return null;
            LogConfig.ForceLogging(GetType());
            _windowPolicies = null;
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                CheckCanvas(canvas);
            }
            enabled = false;
        }

        private static void CheckCanvas(Canvas canvas)
        {
            if (canvas == null)
            {
                return;
            }
            if (_windowPolicies == null)
            {
                _windowPolicies = Resources.Load<WindowPolicies>(nameof(WindowPolicies));
                if (_windowPolicies == null)
                {
                    return;
                }
                _isWarning = !_windowPolicies.Fonts.LogAsError;
                _isShowFullPath = _windowPolicies.Fonts.ShowFullPath;
            }
            if (_windowPolicies.Debug.LogAllCheckedCanvases)
            {
                Debug.Log($"{RichText.Yellow(canvas.name)}", canvas);
            }
            if (_windowPolicies.Debug.AddButtonClickLogger)
            {
                // Button hack.
                foreach (var button in canvas.GetComponentsInChildren<Button>(includeInactive: true))
                {
                    button.onClick.AddListener(() =>
                        Debug.LogWarning($"{button.gameObject.GetFullPath()}", button));
                }
            }
            var allowedFonts = _windowPolicies.Fonts;
            if (allowedFonts == null)
            {
                return;
            }
            var knownFontNames = new List<string>();
            if (allowedFonts.TextMeshProFonts != null)
            {
                foreach (var font in allowedFonts.TextMeshProFonts)
                {
                    knownFontNames.Add(font.name);
                }
            }
            if (allowedFonts.LegacyFonts != null)
            {
                foreach (var font in allowedFonts.LegacyFonts)
                {
                    knownFontNames.Add(font.name);
                }
            }
            var components = new HashSet<Component>();
            foreach (var text in canvas.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true))
            {
                if (text.font == null)
                {
                    Debug.LogError($"Text font is missing from {text.gameObject.GetFullPath()}", text);
                    continue;
                }
                CheckFontName(components, text, knownFontNames, text.font.name);
            }
            foreach (var text in canvas.GetComponentsInChildren<Text>(includeInactive: true))
            {
                CheckFontName(components, text, knownFontNames, text.font.name);
            }
            // This selects TextMeshProUGUI instances as well because they inherit from TMP_Text.
            foreach (var text in canvas.GetComponentsInChildren<TMP_Text>(includeInactive: true))
            {
                if (text.font == null)
                {
                    Debug.LogError($"Text font is missing from {text.gameObject.GetFullPath()}", text);
                    continue;
                }
                CheckFontName(components, text, knownFontNames, text.font.name);
            }
            var scrollRectSettings = _windowPolicies.ScrollRect;
            foreach (var scrollRect in canvas.GetComponentsInChildren<ScrollRect>(includeInactive: true))
            {
                if (!Mathf.Approximately(scrollRect.scrollSensitivity, scrollRectSettings.DefaultScrollSensitivity))
                {
                    if (scrollRectSettings.FixScrollSensitivity)
                    {
                        Debug.LogWarning(
                            $"scrollRect {ScrollRectInfo(scrollRect)} {RichText.Yellow("FIXED")}", scrollRect);
                        scrollRect.scrollSensitivity = scrollRectSettings.DefaultScrollSensitivity;
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"scrollRect {ScrollRectInfo(scrollRect)} {RichText.Yellow("Sensitivity!")}", scrollRect);
                    }
                }
                else
                {
                    Debug.Log($"scrollRect {ScrollRectInfo(scrollRect)}", scrollRect);
                }
            }
            return;

            string ScrollRectInfo(ScrollRect scrollRect)
            {
                var builder =
                    new StringBuilder($"{scrollRect.name}");
                if (scrollRect.horizontal && scrollRect.vertical)
                {
                    builder.Append($" BOTH");
                }
                else if (scrollRect.horizontal)
                {
                    builder.Append($" HORIZ");
                }
                else if (scrollRect.vertical)
                {
                    builder.Append($" VERT");
                }
                else
                {
                    builder.Append($" NONE");
                }
                builder.Append($" {scrollRect.movementType}");
                if (scrollRect.movementType == ScrollRect.MovementType.Elastic)
                {
                    builder.Append($" elasticity {scrollRect.elasticity:0.00}");
                }
                if (scrollRect.inertia)
                {
                    builder.Append($" inertia {scrollRect.decelerationRate:0.00}");
                }
                builder.Append($" sensitivity {scrollRect.scrollSensitivity:0.00}");
                return builder.ToString();
            }
        }

        private static void CheckFontName(HashSet<Component> components, Component component,
            List<string> knownFontNames, string fontName)
        {
            if (!components.Add(component))
            {
                return;
            }
            // TMP_Text is the base class for TextMeshProUGUI so we have to check both of them to be sure.
            var isTmpText = component.GetType() == typeof(TMP_Text);
            var isTextMeshProUGUI = component.GetType() == typeof(TextMeshProUGUI);
            var isValidTextType = isTextMeshProUGUI && !isTmpText;
            var isKnownFont = false;
            foreach (var knownFontName in knownFontNames)
            {
                if (fontName != knownFontName)
                {
                    continue;
                }
                isKnownFont = true;
                break;
            }
            if (isValidTextType && isKnownFont)
            {
                // Nothing to complain.
                return;
            }
            var componentName = _isShowFullPath ? component.GetFullPath() : component.name;
            var componentText = isValidTextType
                ? componentName
                : component is TMP_Text
                    ? $"{RichText.Yellow(componentName)} <i>text type '{component.GetType().Name}' is deprecated</i>"
                    : $"{RichText.Yellow(componentName)} <i>text type '{component.GetType().Name}' is old/legacy</i>";
            var fontText = isKnownFont ? fontName : $"{RichText.Yellow(fontName)} <i>should not use this font</i>";
            if (_isWarning || isKnownFont)
            {
                // Just warning when Text component is not TextMeshProUGUI
                Debug.LogWarning($"{fontText} in {componentText}", component);
                return;
            }
            Debug.LogError($"{fontText} in {componentText}", component);
        }
    }
}
