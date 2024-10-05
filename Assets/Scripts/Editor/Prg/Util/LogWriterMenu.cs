using System;
using UnityEditor;
using Debug = Prg.Debug;
#if PRG_DEBUG
using System.IO;
using Prg;
using Prg.Util;
using UnityEditorInternal;
using UnityEngine;
#endif

namespace Editor.Prg.Util
{
    internal static class LogWriterMenu
    {
        private const string MenuRoot = "Prg/";
        private const string MenuItem = MenuRoot + "Logging/";

        [MenuItem(MenuItem + "Show log file location", false, 10)]
        private static void ShowLogFilePath() => MenuShowLogFilePath();

        [MenuItem(MenuItem + "Create '_local' LogConfig", false, 11)]
        private static void CreateLocalLogConfig() => MenuCreateLocalLogConfig();

        [MenuItem(MenuItem + "Open log file in text editor", false, 12)]
        private static void LoadLogFileToTextEditor() => MenuLoadLogFileToTextEditor();

#if PRG_DEBUG
        private static void MenuShowLogFilePath()
        {
            Debug.Log("*");
            var path = Path.Combine(
                Environment.ExpandEnvironmentVariables("%LocalAppData%"), "Unity", "Editor", "Editor.log");
            Debug.Log($"UNITY log {(File.Exists(path) ? "is in" : RichText.Brown("NOT found"))}: {path}");
            path = GetLogFilePath();
            Debug.Log($"Game log {(File.Exists(path) ? "is in" : RichText.Brown("NOT found"))}: {path}");
        }

        private static void MenuLoadLogFileToTextEditor()
        {
            Debug.Log("*");
            var path = GetLogFilePath();
            if (File.Exists(path))
            {
                InternalEditorUtility.OpenFileAtLineExternal(path, 1);
                return;
            }
            Debug.Log($"Editor log {RichText.Brown("NOT FOUND")}: {path}");
        }

        private static string GetLogFilePath()
        {
            var path = Path.Combine(Application.persistentDataPath, LogFileWriter.GetLogName());
            path = AppPlatform.ConvertToWindowsPath(path);
            return path;
        }

        private static void MenuCreateLocalLogConfig()
        {
            Debug.Log("*");
            var folder = $"Assets/Resources/{LogConfig.LocalResourceFolder}";
            var path = $"{folder}/{nameof(LogConfig)}.asset";
            if (File.Exists(path))
            {
                Debug.Log($"File exists: {path}");
                return;
            }
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            var instance = ScriptableObject.CreateInstance<LogConfig>();
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = instance;
            Debug.Log($"File created: {path}");
        }
#else
        private static void MenuShowLogFilePath()
        {
            Debug.LogWarning("PRG_DEBUG not defined");
        }

        private static void MenuLoadLogFileToTextEditor()
        {
            Debug.LogWarning("PRG_DEBUG not defined");
        }

        private static void MenuCreateLocalLogConfig()
        {
            Debug.LogWarning("PRG_DEBUG not defined");
        }
#endif
    }
}
