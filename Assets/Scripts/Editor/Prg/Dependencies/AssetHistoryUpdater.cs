using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Prg;
using Prg.Util;
using UnityEditor;
using UnityEngine;
using Debug = Prg.Debug;

namespace Editor.Prg.Dependencies
{
    /// <summary>
    /// Entry point for <c>AssetHistory</c>, used for solving the Missing Prefab issue in Unity3d.<br />
    /// Most of resources used are long time ago forgotten, below is one that was found:
    /// https://medium.com/codex/solving-the-missing-prefab-issue-in-unity3d-ae5ba0a15ee9
    /// </summary>
    public static class AssetHistory
    {
        private const string AssetHistoryPrefix = "_local_";

        internal const string FileCommentLine =
            "# This is machine generated file to 'track' asset files 'used' in this UNITY project";

        internal const string AssetHistoryFilename = AssetHistoryPrefix + "AssetHistory.txt";
        internal const string AssetHistoryStateFilename = AssetHistoryPrefix + "AssetHistoryState.json";
        internal const string AssetPath = "Assets";
        internal static readonly int MetaExtensionLength = ".meta".Length;
        private static readonly Encoding Encoding = PlatformUtil.Encoding;

        public static string[] Load()
        {
            var lines = File.Exists(AssetHistoryFilename)
                ? File.ReadAllLines(AssetHistoryFilename, Encoding)
                : Array.Empty<string>();
            return lines;
        }
    }

    /// <summary>
    /// Local state for <c>AssetHistory</c>.
    /// </summary>
    /// <remarks>
    /// Note that all file extensions should be in lower case. 
    /// </remarks>
    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class AssetHistoryState
    {
        private static readonly Encoding Encoding = PlatformUtil.Encoding;

        public int DayNumber;
        public List<string> YamlExtensions = new();
        public List<string> OtherExtensions = new();

        /// <summary>
        /// Checks if assets is binary file format and should be skipped.
        /// </summary>
        public bool IsExcludedAsset(string filename) =>
            filename.EndsWith("lightingdata.asset") || filename.EndsWith("navmesh.asset");

        public static AssetHistoryState Load()
        {
            if (!File.Exists(AssetHistory.AssetHistoryStateFilename))
            {
                return new AssetHistoryState()
                {
                    YamlExtensions = new List<string>()
                    {
                        ".anim", ".asset", ".controller", ".cubemap", ".flare", ".guiskin", ".lighting", ".mat",
                        ".overridecontroller",
                        ".physicmaterial", ".physicmaterial", ".physicsmaterial2d", ".prefab", ".unity",
                    },
                    OtherExtensions = new List<string>()
                    {
                        ".aar", ".asmdef", ".blend", ".bmp", ".cginc", ".chm", ".cs", ".csv", ".dll", ".exr", ".fbx",
                        ".gif",
                        ".inputactions", ".jpg", ".jslib", ".json", ".lib", ".mp3", ".otf", ".pdb", ".pdf", ".png",
                        ".psd",
                        ".readme", ".shader", ".tga", ".tif", ".ttf", ".txt", ".wav", ".xcf", ".xlsx", ".xml",
                    },
                };
            }
            var jsonData = File.ReadAllText(AssetHistory.AssetHistoryStateFilename, Encoding);
            return JsonUtility.FromJson<AssetHistoryState>(jsonData);
        }

        public void Save()
        {
            var json = JsonUtility.ToJson(this);
            File.WriteAllText(AssetHistory.AssetHistoryStateFilename, json, Encoding);
        }
    }

    /// <summary>
    /// Keeps a list of files (assets) we have ever seen for a later case when files has been deleted or renamed and
    /// we need to find out what was the original name or location.<br />
    /// Initially files are in the order OS reports them and later additions are appended as they are found.<br />
    /// This facilitates tracking renamed files unambiguously.
    /// </summary>
    /// <remarks>
    /// File format (for lines) is: &lt;asset_name&gt; \t &lt;asset_guid&gt; \t &lt;asset_extension&gt;<br />
    /// We try to run this once a day when UNITY Editor is started first time.
    /// </remarks>
    public static class AssetHistoryUpdater
    {
        private const string DayNumberNamePrefix = "AssetHistoryUpdater.dayNumber";

        private static readonly Encoding Encoding = PlatformUtil.Encoding;

        // Save day number separately for each project with different name.
        private static string DayNumberName =>
            $"{DayNumberNamePrefix}.{new DirectoryInfo(Directory.GetCurrentDirectory()).Name}";

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            EditorApplication.delayCall += OnDelayCall;
        }

        private static void OnDelayCall()
        {
            EditorApplication.delayCall -= OnDelayCall;

            var now = DateTime.Now;
            var dayNumber = now.DayOfYear;
            if (dayNumber == PlayerPrefs.GetInt(DayNumberName, -1))
            {
                return;
            }
            PlayerPrefs.SetInt(DayNumberName, dayNumber);
            var state = AssetHistoryState.Load();
            var dayOfYear = now.Year * 1000 + dayNumber;
            if (dayOfYear == state.DayNumber && File.Exists(AssetHistory.AssetHistoryFilename))
            {
                return;
            }
            UpdateAssetHistory();
            state.DayNumber = dayOfYear;
            state.Save();
            CheckPlayModeSettings();
        }

        public static void ForceUpdateAssetHistory()
        {
            var now = DateTime.Now;
            var dayNumber = now.DayOfYear;
            PlayerPrefs.SetInt(DayNumberName, dayNumber);
            var state = AssetHistoryState.Load();
            var dayOfYear = now.Year * 1000 + dayNumber;
            UpdateAssetHistory();
            state.DayNumber = dayOfYear;
            state.Save();
            CheckPlayModeSettings();
        }

        private static void CheckPlayModeSettings()
        {
            // Our recommended settings for "Enable Play Mode Options" is:
            // - do not DisableDomainReload
            // - do not DisableSceneReload
            if (!EditorSettings.enterPlayModeOptionsEnabled)
            {
                Debug.Log(
                    $"{RichText.Yellow("EditorSettings.enterPlayModeOptionsEnabled")}={EditorSettings.enterPlayModeOptionsEnabled}");
                return;
            }

            var enableDomainReload =
                (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) !=
                EnterPlayModeOptions.DisableDomainReload;
            var enableSceneReload =
                (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableSceneReload) !=
                EnterPlayModeOptions.DisableSceneReload;

            if (!enableDomainReload && !enableSceneReload)
            {
                return;
            }
            Debug.Log(RichText.Yellow("EditorSettings.enterPlayModeOptions"));
            Debug.Log(
                $"{RichText.Yellow("DomainReload")}={enableDomainReload}" +
                $" {(enableDomainReload ? RichText.Magenta("Why?") : RichText.Green("(ok)"))}");
            Debug.Log($"{RichText.Yellow("SceneReload")}={enableSceneReload}" +
                      $" {(enableSceneReload ? RichText.Magenta("Why?") : RichText.Green("(ok)"))}");
        }

        private static void UpdateAssetHistory()
        {
            var lines = AssetHistory.Load();
            var hasLines = lines.Length > 0;
            var fileHistory = new HashSet<string>(lines);
            var files = Directory.GetFiles(AssetHistory.AssetPath, "*.meta", SearchOption.AllDirectories);
            var currentStatus =
                $"{RichText.Magenta("UpdateAssetHistory")} {AssetHistory.AssetHistoryFilename} with {fileHistory.Count} entries and {files.Length} meta files";
            var newFileCount = 0;
            var isShowNewFiles = Math.Abs(fileHistory.Count - files.Length) < 100;
            var newLines = new StringBuilder();
            if (!hasLines)
            {
                newLines
                    .Append(AssetHistory.FileCommentLine)
                    .AppendLine();
            }
            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(file))
                {
                    continue;
                }
                var assetPath = file.Substring(0, file.Length - AssetHistory.MetaExtensionLength);
                var guid = AssetDatabase.GUIDFromAssetPath(assetPath);
                var line = $"{assetPath}\t{guid}";
                if (!fileHistory.Add(line))
                {
                    continue;
                }
                newFileCount += 1;
                newLines.Append(line).AppendLine();
                if (isShowNewFiles)
                {
                    Debug.Log(line);
                }
            }
            if (newFileCount == 0)
            {
                Debug.Log($"{currentStatus} {RichText.White("ok")}");
                return;
            }
            // Remove last CR-LF
            newLines.Length -= 2;
            if (hasLines)
            {
                using var streamWriter = File.AppendText(AssetHistory.AssetHistoryFilename);
                // Add CR-LF
                streamWriter.WriteLine();
                streamWriter.Write(newLines.ToString());
            }
            else
            {
                File.WriteAllText(AssetHistory.AssetHistoryFilename, newLines.ToString(), Encoding);
            }
            Debug.Log($"{currentStatus} {RichText.Yellow($"updated with {newFileCount} entries")}");
        }
    }
}
