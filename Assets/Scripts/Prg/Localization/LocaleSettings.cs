using System;
using System.Diagnostics.CodeAnalysis;
using NaughtyAttributes;
using Prg.EditorSupport;
using UnityEngine;

namespace Prg.Localization
{
    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class LocaleData
    {
        public SystemLanguage SystemLanguage;
        public string LocaleCode;
        public Sprite Flag;

        public override string ToString()
        {
            return $"{nameof(SystemLanguage)}: {SystemLanguage}, {nameof(LocaleCode)}: {LocaleCode}";
        }
    }

    [CreateAssetMenu(menuName = "Prg/Game/" + nameof(LocaleSettings), fileName = nameof(LocaleSettings))]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class LocaleSettings : ScriptableObject
    {
        #region UNITY Singleton Pattern

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void SubsystemRegistration()
        {
            // Manual reset if UNITY Domain Reloading is disabled.
            _instance = null;
        }

        private static LocaleSettings _instance;

        public static LocaleSettings Get()
        {
            return _instance ??= Resources.Load<LocaleSettings>(nameof(LocaleSettings));
        }

        #endregion

        public static readonly string BinFilename = $"{nameof(LocaleSettings)}.bin";

        private const string Info1 =
            "Available Locales, first item is default locale if specific locale is not found in here";

        private const string Info2 =
            "Use MissingKey Settings in production builds";

        private const string Info3 =
            "MissingKey Settings for development builds";

        [InfoBox(Info1)] public LocaleData[] Locales;

        [Header("MissingKey Settings"), InfoBox(Info2)] public bool IsForceInProduction;
        [InfoBox(Info3)] public bool IsShowMissingKey;
        public bool IsShowMissingKeyColor;
        [ColorHtmlProperty] public Color MissingKeyColor = Color.red;

        private LocaleData DefaultLocale => Locales[0];

        public LocaleData GetLocaleFor(SystemLanguage systemLanguage)
        {
            MyAssert.IsNotNull(Locales, "Locales array is required", this);
            var index = Array.FindIndex(Locales, x => x.SystemLanguage == systemLanguage);
            return index == -1
                ? DefaultLocale
                : Locales[index];
        }

        public LocaleData GetLocaleFor(string localeCode)
        {
            MyAssert.IsNotNull(Locales, "Locales array is required", this);
            var index = Array.FindIndex(Locales, x => x.LocaleCode == localeCode);
            return index == -1
                ? DefaultLocale
                : Locales[index];
        }
    }
}
