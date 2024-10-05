using System.IO;
using System.Linq;
using System.Text;
using Editor.Prg.EditorSupport;
using Prg;
using Prg.Localization;
using Prg.Util;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = Prg.Debug;

namespace Editor.Prg.Localization
{
    /// <summary>
    /// UNITY Editor menu commands for localization.
    /// </summary>
    public static class LocalizationMenu
    {
        private static bool _hasLogger;

        private static void SetLogger()
        {
            if (_hasLogger)
            {
                return;
            }
            _hasLogger = true;
            LogConfig.Create();
            LogConfig.ForceLogging(typeof(Localizer),
                typeof(CheckLocalization),
                typeof(LocalizedEditor));
        }

        private const string MenuRoot = "Prg/";
        private const string MenuItemRoot = MenuRoot + "Localization/";

        private const string UpdateLocalizationGameObjectName = "Update GameObject Localization!";

        private const string AddLocalizationName = "Add Prefab Localization!";
        private const string UpdateLocalizationName = "Update Prefab Localization!";
        private const string RemoveLocalizationName = "Remove Prefab Localization!";
        private const string AddNoLocalizeTagName = "Add Prefab NoLocalize Tag!";

        private const string LocalizationReportCsvLocalName = "Create Report to Local Csv";
        private const string LocalizationReportClipboardName = "Create Report to Clipboard";
        private const string LocalizationReportCsvWorkName = "Create Report to Work Csv (for VCS)";

        private const string OpenLocalizationsTsvName = "Open Localizations Tsv file";
        private const string CopyMissingKeysClipboardName = "Copy Missing Keys to Clipboard";
        private const string CopyMissingKeysCsvName = "Copy Missing Keys to Local Csv";

        [MenuItem(MenuItemRoot + AddLocalizationName, true, 0)]
        private static bool MenuAddLocalizationEnabled() => Selection.gameObjects.Length > 0;

        // Group 10

        [MenuItem(MenuItemRoot + AddLocalizationName, false, 10)]
        private static void MenuAddLocalization()
        {
            SetLogger();
            Debug.Log("*");
            foreach (var gameObject in Selection.gameObjects)
            {
                LocalizedEditorUtil.AddLocalization(gameObject);
            }
        }

        [MenuItem(MenuItemRoot + UpdateLocalizationName, true, 0)]
        private static bool MenuUpdateLocalizationEnabled() => Selection.gameObjects.Length > 0;

        [MenuItem(MenuItemRoot + UpdateLocalizationName, false, 11)]
        private static void MenuUpdateLocalization()
        {
            SetLogger();
            Debug.Log("*");
            foreach (var gameObject in Selection.gameObjects)
            {
                LocalizedEditorUtil.UpdateLocalization(gameObject);
            }
        }

        [MenuItem(MenuItemRoot + RemoveLocalizationName, true, 0)]
        private static bool RemoveLocalizationEnabled() => Selection.gameObjects.Length > 0;

        [MenuItem(MenuItemRoot + RemoveLocalizationName, false, 12)]
        private static void RemoveLocalization()
        {
            SetLogger();
            Debug.Log("*");
            foreach (var gameObject in Selection.gameObjects)
            {
                LocalizedEditorUtil.RemoveLocalization(gameObject);
            }
        }

        [MenuItem(MenuItemRoot + AddNoLocalizeTagName, true, 0)]
        private static bool AddNoLocalizeTagEnabled() => Selection.gameObjects.Length > 0;

        [MenuItem(MenuItemRoot + AddNoLocalizeTagName, false, 13)]
        private static void AddNoLocalizeTag()
        {
            SetLogger();
            Debug.Log("*");
            foreach (var gameObject in Selection.gameObjects)
            {
                LocalizedEditorUtil.AddNoLocalizeTag(gameObject);
            }
        }

        // Group 30

        [MenuItem(MenuItemRoot + LocalizationReportClipboardName, true, 0)]
        private static bool LocalizationReportClipboardEnabled() => Selection.assetGUIDs.Length > 0;

        [MenuItem(MenuItemRoot + LocalizationReportClipboardName, false, 30)]
        private static void LocalizationReportClipboard()
        {
            SetLogger();
            Debug.Log("*");
            CheckLocalization.CheckLocalizationInPrefabs(Selection.assetGUIDs, isCsvReport: false);
        }

        [MenuItem(MenuItemRoot + LocalizationReportCsvLocalName, true, 0)]
        private static bool LocalizationReportCsvLocalEnabled() => Selection.assetGUIDs.Length > 0;

        [MenuItem(MenuItemRoot + LocalizationReportCsvLocalName, false, 31)]
        private static void LocalizationReportCsvLocal()
        {
            SetLogger();
            Debug.Log("*");
            CheckLocalization.CheckLocalizationInPrefabs(Selection.assetGUIDs, isCsvReport: true);
        }

        [MenuItem(MenuItemRoot + LocalizationReportCsvWorkName, true, 0)]
        private static bool LocalizationReportCsvWorkEnabled() =>
            !Application.isPlaying && Selection.assetGUIDs.Length > 0;

        [MenuItem(MenuItemRoot + LocalizationReportCsvWorkName, false, 32)]
        private static void LocalizationReportCsvWork()
        {
            SetLogger();
            Debug.Log("*");
            CheckLocalization.CheckLocalizationInAllPrefabs();
        }

        // Group 50

        [MenuItem(MenuItemRoot + OpenLocalizationsTsvName, true, 0)]
        private static bool OpenLocalizationsTsvEnabled() => !Application.isPlaying;

        [MenuItem(MenuItemRoot + OpenLocalizationsTsvName, false, 50)]
        private static void OpenLocalizationsTsv()
        {
            SetLogger();
            Debug.Log("*");
            var path = Localizer.TsvFilepath;
            if (File.Exists(path))
            {
                InternalEditorUtility.OpenFileAtLineExternal(path, 1);
                return;
            }
            Debug.Log($"Editor log {RichText.Brown("NOT FOUND")}: {path}");
        }

        [MenuItem(MenuItemRoot + CopyMissingKeysClipboardName, true, 0)]
        private static bool CopyMissingKeysClipboardEnabled() => Localizer.HasMissingKeys;

        [MenuItem(MenuItemRoot + CopyMissingKeysClipboardName, false, 51)]
        private static void CopyMissingKeysClipboard()
        {
            SetLogger();
            Debug.Log("*");
            CheckLocalization.WriteReport(GetMissingKeys(forClipboard: true), isCsvReport: false);
        }

        [MenuItem(MenuItemRoot + CopyMissingKeysCsvName, true, 0)]
        private static bool CopyMissingKeysCsvEnabled() => Localizer.HasMissingKeys;

        [MenuItem(MenuItemRoot + CopyMissingKeysCsvName, false, 52)]
        private static void CopyMissingKeysCsv()
        {
            SetLogger();
            Debug.Log("*");
            CheckLocalization.WriteReport(GetMissingKeys(), isCsvReport: true, CheckLocalization.TempFilename);
        }

        private static string GetMissingKeys(bool forClipboard = false)
        {
            var keys = Localizer.GetMissingKeys();
            Debug.Log($"key count {keys.Count}");
            var builder = new StringBuilder();
            foreach (var key in keys.OrderBy(x => x))
            {
                Assert.IsFalse(key.Contains('\r'), $"key must not contain CR character: {key}");
                // Unfortunately is seems that pasting text from clipboard causes LF to CR-LF expansion.
                var keyText = forClipboard
                    ? key.Replace("\n", "\n@\t")
                    : key;
                builder.Append(keyText).Append('\t').AppendLine();
            }
            return builder.ToString();
        }
    }
}
