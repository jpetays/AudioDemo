using System;
using System.Diagnostics;
using Prg.Util;
using UnityEditor;
using Debug = Prg.Debug;

namespace Editor.Prg.BatchBuild
{
    internal static class BatchBuildMenu
    {
        private const string MenuRoot = "Prg/";
        private const string MenuItem = MenuRoot + "Build/";

        [MenuItem(MenuItem + "Show Build Report in browser", false, 10)]
        private static void HtmlBuildReportBrowser() => Logged(() => BuildReportAnalyzer.HtmlBuildReportFast());

        [MenuItem(MenuItem + "Show Build Report with unused Assets", false, 11)]
        private static void HtmlBuildReportBrowserFull() => Logged(() => BuildReportAnalyzer.HtmlBuildReportFull());

        private static void Logged(Action action)
        {
            var logFileWriter = LogFileWriter.CreateLogFileWriter();
            try
            {
                var stopwatch = Stopwatch.StartNew();
                action();
                stopwatch.Stop();
                Debug.Log($"Command took {stopwatch.Elapsed.TotalSeconds:0.0} s");
            }
            finally
            {
                logFileWriter.Close();
            }
        }
    }
}
