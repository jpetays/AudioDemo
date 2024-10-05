using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Prg.Util;
using TMPro;
using UnityEditor;
using UnityEngine;
using FileUtil = Prg.Util.FileUtil;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

namespace Prg.Localization
{
    /// <summary>
    /// Simple <c>i18n</c> and <c>l10n</c> implementation to localize words and phrases.<br />
    /// Contains map of locales that contains map of localized words by localization key.<br />
    /// Localized words are read from game_localizations.tsv file in folder ./etc/localization<br />
    /// Localization data is cached in binary formatted <c>TextAsset</c> file in Assets/Resources folder.
    /// <br />
    /// </summary>
    public static class Localizer
    {
        private static readonly string TsvFilename = Path.Combine(".", "etc", "localization", "game_localizations.tsv");

        public static IVariableSubstitution VariableSubstitution = new DefaultVariableSubstitution();

        public static bool IsShowMissingKey;
        public static bool IsShowMissingKeyColor;
        public static Color MissingKeyColor;

        /// <summary>
        /// All known available locales.
        /// </summary>
        private static Dictionary<string, Dictionary<string, string>> _locales;

        /// <summary>
        /// Current locale code.
        /// </summary>
        private static string _currentLocaleCode;

        /// <summary>
        /// Current locale 'words and phrases' dictionary.
        /// </summary>
        private static Dictionary<string, string> _currentLocale;

#if UNITY_EDITOR
        public static readonly string TsvFilepath = TsvFilename;

        private static readonly HashSet<string> MissingKeys = new();

        public static bool HasMissingKeys => MissingKeys.Count > 0;
        public static HashSet<string> GetMissingKeys() => MissingKeys;
#endif

        #region Public Localization API

        /// <summary>
        /// Localize text using <c>Localized</c> component.
        /// </summary>
        /// <param name="localized">the component to localize</param>
        public static void Localize(this Localized localized)
        {
            if (!Localize(localized._key, localized, out var text))
            {
                ReportMissingKey(localized._key, localized);
                if (IsShowMissingKeyColor)
                {
                    localized.SetColor(MissingKeyColor);
                }
                if (IsShowMissingKey)
                {
                    localized.SetText(FormatMissingKey(localized._key));
                    return;
                }
                // Use default text!
                text = localized._defaultText;
            }
            localized.SetText(VariableSubstitution.Substitute(text));
        }

        /// <summary>
        /// Localize given <c>textMeshPro</c> component.
        /// </summary>
        /// <param name="textMeshPro">the component to localize</param>
        /// <param name="key">the localization key</param>
        public static void Localize(this TextMeshProUGUI textMeshPro, string key)
        {
            if (!_currentLocale.TryGetValue(key, out var text))
            {
                ReportMissingKey(key, textMeshPro);
                if (IsShowMissingKeyColor)
                {
                    textMeshPro.color = MissingKeyColor;
                }
                if (IsShowMissingKey)
                {
                    textMeshPro.text = FormatMissingKey(key);
                    return;
                }
                // Use current text!
                text = textMeshPro.text;
            }
            textMeshPro.text = VariableSubstitution.Substitute(text);
        }

        /// <summary>
        /// Get localized text.
        /// </summary>
        /// <param name="key">the localization key</param>
        /// <param name="context">for debug log missing keys</param>
        /// <param name="text">corresponding localized text</param>
        /// <returns>true if localization key was found</returns>
        public static bool Localize(string key, Object context, out string text)
        {
            if (!_currentLocale.TryGetValue(key, out text))
            {
                ReportMissingKey(key, context);
                if (IsShowMissingKey)
                {
                    text = FormatMissingKey(key);
                }
                return false;
            }
            text = VariableSubstitution.Substitute(text);
            return true;
        }

        public static string Localize(string key, Object context = null)
        {
            if (key == null)
            {
                key = "NULL_KEY";
            }
            else if (key.Length == 0)
            {
                key = "EMPTY_KEY";
            }
            if (!_currentLocale.TryGetValue(key, out var text))
            {
                ReportMissingKey(key, context);
                if (IsShowMissingKey)
                {
                    return FormatMissingKey(key);
                }
                return key;
            }
            return VariableSubstitution.Substitute(text);
        }

        private static string FormatMissingKey(string key) => $"[{key}]";

        [Conditional("UNITY_EDITOR")]
        private static void ReportMissingKey(string key, Object context)
        {
#if UNITY_EDITOR
            if (context is Localized localized)
            {
                // Save text 'as is' for MissingKeys in localization process (clipboard or .csv).
                var text = localized._defaultText;
                var localizedKey = $"{localized._key}\t{text}\t{localized.GetFullPath()}";
                if (MissingKeys.Add(localizedKey))
                {
                    // Yellow sanitized text extract for log.
                    text = RichText.Yellow(Localized.SanitizeText(localized._defaultText));
                    localizedKey = $"{localized._key}\t{text}\t{localized.GetFullPath()}";
                    MissingKey(localizedKey, localized);
                }
            }
            else
            {
                if (MissingKeys.Add(key))
                {
                    MissingKey(key, context);
                }
            }
#endif
        }

#if UNITY_EDITOR
        private static void MissingKey(string missingKey, Object context)
        {
            Debug.LogWarning(missingKey, context);
        }
#endif

        private class DefaultVariableSubstitution : IVariableSubstitution
        {
            public string Substitute(string text) => text;
        }

        #endregion

        #region Data loader

        public static bool HasLanguage(string localeCode) => _locales.ContainsKey(localeCode);

        public static bool SetLanguage(string localeCode)
        {
            if (!_locales.TryGetValue(localeCode, out var localeDictionary))
            {
                return false;
            }
            _currentLocale = localeDictionary;
            _currentLocaleCode = localeCode;
            return true;
        }

        /// <summary>
        /// Loads localization system localization data from given <c>TextAsset</c>.
        /// </summary>
        /// <param name="localeCode">current locale code to use</param>
        /// <param name="textAsset">textAsset for localization data in .tsv format</param>
        public static void LoadLocalizations(string localeCode, TextAsset textAsset)
        {
#if UNITY_EDITOR
            MissingKeys.Clear();
            ValidateOrCreateBinFile(textAsset);
#endif
            var timer = new Timer();
            // ReSharper disable once InlineOutVariableDeclaration
            List<string> localeCodes;
            _locales = TsvLoader.Load(textAsset, out localeCodes);
            timer.Stop();
            Debug.Log($"bin load {timer.ElapsedTime}");
            if (_locales.TryGetValue(localeCode, out _currentLocale))
            {
                _currentLocaleCode = localeCode;
            }
            else if (localeCodes.Count > 0)
            {
                _currentLocaleCode = localeCodes[0];
                _currentLocale = _locales[_currentLocaleCode];
            }
            else
            {
                _currentLocaleCode = string.Empty;
                _currentLocale = new Dictionary<string, string>();
                Debug.Log($"no locales available");
                return;
            }
            var missingWords = 0;
            if (_currentLocaleCode != localeCodes[0])
            {
                // Copy missing words from default locale.
                var defaultLocale = _locales[localeCodes[0]];
                foreach (var key in defaultLocale.Keys)
                {
                    if (_currentLocale.ContainsKey(key))
                    {
                        continue;
                    }
                    _currentLocale.Add(key, defaultLocale[key]);
                    missingWords += 1;
                }
            }
            Debug.Log($"locale {_currentLocaleCode} words  {_currentLocale.Count} missing {missingWords}");
        }

#if UNITY_EDITOR
        public static void ResetLocalizations(TextAsset textAsset)
        {
            ValidateOrCreateBinFile(textAsset, forceCreate: true);
        }

        private static void ValidateOrCreateBinFile(TextAsset textAsset, bool forceCreate = false)
        {
            if (textAsset == null)
            {
                Debug.LogError("*");
                Debug.LogError("* Localizer TextAsset is NULL, it must be created manually");
                Debug.LogError("*");
            }
            if (!File.Exists(TsvFilename))
            {
                CreateTsvFile(TsvFilename);
            }
            var tsvWriteTime = File.GetLastWriteTime(TsvFilename);
            var binPath = TextAssetUtil.GetAssetPathFrom(textAsset);
            var binWriteTime = forceCreate || !File.Exists(binPath)
                ? DateTime.MinValue
                : File.GetLastWriteTime(binPath);

            var isValid = binWriteTime >= tsvWriteTime;
            Debug.Log($"binWriteTime {binWriteTime} tsvWriteTime {tsvWriteTime} isValid {isValid}");
            if (isValid)
            {
                return;
            }
            File.Copy(TsvFilename, binPath, overwrite: true);
            if (forceCreate)
            {
                AssetDatabase.Refresh();
            }
            return;

            void CreateTsvFile(string filename)
            {
                // ReSharper disable StringLiteralTypo
                const string content = @"_.key	en	fi	sv
_Flag.#			
_flag.lang.en	English	English	
_flag.lang.fi	Suomi	Suomi	
_flag.lang.sv	Svenska	Svenska	
_Lang.#			
_lang.en	English	Englanti	Engelska
_lang.fi	Finnish	Suomi	Finska
_lang.sv	Swedish	Ruotsi	Svenska
_å_#			
_ä_keep lines sorted by column A #			
_ö_#			
";
                // ReSharper restore StringLiteralTypo
                FileUtil.WriteAllText(filename, content);
            }
        }
#endif

        #endregion
    }
}
