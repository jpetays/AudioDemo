#define MENU_TEST_DEBUG
using System;
using System.Diagnostics;
using Prg;
using Prg.Util;
using UnityEditor;
using UnityEngine;
using Debug = Prg.Debug;
#if MENU_TEST_DEBUG
using Editor.Prg.Data;
using Editor.Prg.Secrets;
#endif

namespace Editor.Prg.BatchBuild
{
    internal static class BatchBuildMenu
    {
        private const string MenuRoot = "Prg/";
        private const string MenuItem = MenuRoot + "Build/Old/";

        [MenuItem(MenuItem + "Show Build Report in browser", false, 10)]
        private static void HtmlBuildReportBrowser() => Logged(() => BuildReportAnalyzer.HtmlBuildReportFast());

        [MenuItem(MenuItem + "Show Build Report with unused Assets", false, 11)]
        private static void HtmlBuildReportBrowserFull() => Logged(() => BuildReportAnalyzer.HtmlBuildReportFull());

        [MenuItem(MenuItem + @"Delete Old Build Reports", false, 12)]
        private static void DeleteOldBuildReports() =>
            Logged(() => UnityBuildReport.DeleteOldReports(DateTime.Today, true));

        [MenuItem(MenuItem + @"Show Folder .\etc\secretKeys content", false, 13)]
        private static void TestDumpSecretKeysFolder() => Logged(() =>
        {
            foreach (var buildTarget in new[]
                         { BuildTarget.Android, BuildTarget.WebGL, BuildTarget.StandaloneWindows64 })
            {
                Debug.Log("*");
                try
                {
                    var secretKeys = BatchBuild.LoadSecretKeys(@".\etc\secretKeys", buildTarget);
                    Debug.Log($"* BuildTarget {buildTarget}: keys {secretKeys.Count}");
                    foreach (var pair in secretKeys)
                    {
                        Debug.Log($"{RichText.White(pair.Key)}={pair.Value}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"* BuildTarget {buildTarget}: {RichText.Red(e.Message)}");
                }
            }
        });

        [MenuItem(MenuItem + @"Show Local Build Properties", false, 14)]
        private static void CreateLocalProperties() =>
            Logged(() =>
            {
                var buildInfoFilename = BuildInfoUpdater.BuildInfoFilename(Application.dataPath);
                var patchValue = BuildInfoUpdater.GetPatchValue(buildInfoFilename);
                var buildTarget = EditorUserBuildSettings.activeBuildTarget;
                var buildTargetName = BuildPipeline.GetBuildTargetName(buildTarget);
                var text = BuildInfoUpdater.CreateLocalProperties(Application.version,
                    PlayerSettings.Android.bundleVersionCode, patchValue, buildTargetName);
                Debug.Log($"BatchBuild.CreateLocalProperties output:\r\n{text}\r\n");
            });

#if MENU_TEST_DEBUG
        [MenuItem(MenuItem + @"Debug/Show file BuildInfoDataPart.cs", false, 20)]
        public static void ShowBuildInfoDataPart()
        {
            Debug.Log("*");
            var buildInfoFilename = BuildInfoUpdater.BuildInfoFilename(Application.dataPath);
            var bundleVersionCode = PlayerSettings.Android.bundleVersionCode;
            var patchValue = BuildInfoUpdater.GetPatchValue(buildInfoFilename);
            var muteOtherAudioSources = PlayerSettings.muteOtherAudioSources;
            Debug.Log(
                $"bundleVersionCode {bundleVersionCode} patchValue {patchValue} muteOtherAudioSources {muteOtherAudioSources}");
            Debug.Log($"buildInfoFilename {AppPlatform.ConvertToWindowsPath(buildInfoFilename)}");
            Debug.Log($"git filename {BuildInfoUpdater.GetGFitPath(Application.dataPath, buildInfoFilename)}");
        }

        [MenuItem(MenuItem + @"Debug/Update file BuildInfoDataPart.cs", false, 21)]
        public static void UpdateBuildInfoDataPart()
        {
            Debug.Log("*");
            var buildInfoFilename = BuildInfoUpdater.BuildInfoFilename(Application.dataPath);
            var bundleVersionCode = PlayerSettings.Android.bundleVersionCode + 1;
            var patchValue = BuildInfoUpdater.GetPatchValue(buildInfoFilename) + 1;
            Debug.Log(
                $"bundleVersionCode {bundleVersionCode} patchValue {patchValue}");
            Debug.Log($"buildInfoFilename {AppPlatform.ConvertToWindowsPath(buildInfoFilename)}");
            Debug.Log("");
            Debug.Log("Note that actual and displayed values may differ (due to Editor 'Enter Play Mode Settings')");
            Debug.Log("");
            BuildInfoUpdater.UpdateBuildInfo(
                buildInfoFilename, bundleVersionCode, patchValue, PlayerSettings.muteOtherAudioSources);
        }

        [MenuItem(MenuItem + "Debug/Update GameAnalytics Settings", false, 22)]
        private static void UpdateGameAnalyticsSettings()
        {
            Debug.Log("*");
            AnalyticsSettings.CreateForPlatform(BuildTarget.StandaloneWindows64, new Tuple<string, string>(
                "gameKey_win12340098006cee310b00x",
                "secretKey_win123400071b2911d0f286321a00x"));
            Debug.Log("*");
            Debug.LogWarning($"* {RichText.Magenta("Remember to revert changes GameAnalytics Settings asset")}");
            Debug.Log("*");
        }

        [MenuItem(MenuItem + @"Debug/Show Build FingerPrint for today", false, 23)]
        private static void ShowBuildFingerPrints() => Logged(() =>
        {
            Debug.Log("*");
            var today = $"{DateTime.Today:yyyy-MM-dd}";
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildTargetName = BuildPipeline.GetBuildTargetName(buildTarget);
            Debug.Log($"{buildTargetName} today {today} {SecretStrings.GetFingerPrint(buildTarget, today)}");
        });

#endif

        private static void Logged(Action action)
        {
            LogFileWriter.CreateLogFileWriter();
            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            Debug.Log($"Command took {stopwatch.Elapsed.TotalSeconds:0.0} s");
        }
    }
}
