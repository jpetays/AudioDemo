using System.IO;
using Prg;
using Prg.Util;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Debug = Prg.Debug;

namespace Editor.Prg.Util
{
    internal static class LogWriterMenu
    {
        private const string MenuRoot = "Prg/";
        private const string MenuItem = MenuRoot + "Logging/";

        [MenuItem(MenuItem + "Show log file location", false, 10)]
        private static void ShowLogFilePath() => MenuShowLogFilePath();

        [MenuItem(MenuItem + "Open log file in text editor", false, 11)]
        private static void LoadLogFileToTextEditor() => MenuLoadLogFileToTextEditor();

        private static void MenuShowLogFilePath()
        {
            Debug.Log("*");
            var path = GetLogFilePath();
            Debug.Log($"Editor log {(File.Exists(path) ? "is in" : RichText.Brown("NOT found"))}: {path}");
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
            Debug.Log($"Editor log {RichText.Brown("NOT FOUND")}: {path} ");
        }

        private static string GetLogFilePath()
        {
            var path = Path.Combine(Application.persistentDataPath, LogFileWriter.GetLogName());
            if (AppPlatform.IsWindows)
            {
                path = AppPlatform.ConvertToWindowsPath(path);
            }
            return path;
        }
    }
}
