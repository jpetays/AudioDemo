using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Prg.Util;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = Prg.Debug;

namespace Editor.Prg.BatchBuild
{
    /// <summary>
    /// Utility to build project with given settings (using .enf file) from command line.<br />
    /// https://docs.unity3d.com/Manual/EditorCommandLineArguments.html<br />
    /// Following arguments are mandatory:<br />
    /// -projectPath<br />
    /// -buildTarget<br />
    /// -logFile<br />
    /// -executeMethod Editor.Prg.BatchBuild.BatchBuild.BuildPlayer
    /// -envFile<br />
    /// </summary>
    /// <remarks>
    /// -envFile parameter is our own for our own build parameters in addition to required UNITY standard parameters.<br />
    /// '-executeMethod' parameter starts build process using this file.<br />
    /// </remarks>
    // ReSharper disable once UnusedType.Global
    internal static class BatchBuild
    {
        /// <summary>
        /// Called from command line or CI system to build the project 'executable'.
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        internal static void BuildPlayer()
        {
            LogFileWriter.CreateLogFileWriter();
            _BuildPlayer();
        }

        /// <summary>
        /// Called from command line or CI system to post process build output after successful build.
        /// </summary>
        /// <remarks>
        /// We can not access UNITY build log file during the build itself
        /// so we must handle it in separate step after the build has been fully completed.<br />
        /// </remarks>
        // ReSharper disable once UnusedMember.Global
        internal static void BuildPlayerPostProcessing()
        {
            LogFileWriter.CreateLogFileWriter();
            _BuildPlayerPostProcessing();
        }

        private static void _BuildPlayer()
        {
            BuildInfoUpdater.UpdateFile(PlayerSettings.Android.bundleVersionCode);
            var unityVersion = Application.unityVersion;
            Debug.Log($"batch_build_ start BUILD in UNITY {unityVersion}");
            var options = new BatchBuildOptions(Environment.GetCommandLineArgs());
            Debug.Log($"batch_build_ options {options}");
            Debug.Log($"batch_build_ LogFile {options.LogFile}");
            if (options.IsTestRun)
            {
                Debug.Log("batch_build_ IsTestRun build exit 0");
                EditorApplication.Exit(0);
                return;
            }
            if (!VerifyUnityVersionForBuild(unityVersion, out var editorVersion))
            {
                Debug.Log(
                    $"batch_build_ UNITY version {unityVersion} does not match last Editor version {editorVersion}");
                EditorApplication.Exit(2);
                return;
            }
            var timer = new Timer();
            var buildReport = BuildPLayer(options);
            var buildResult = buildReport.summary.result;
            if (options.IsBuildReport && buildResult == BuildResult.Succeeded)
            {
                var tsvReport = SafeReplaceFileExtension(options.LogFile, ".log", ".report.tsv");
                var jsReport = SafeReplaceFileExtension(options.LogFile, ".log", ".report.js");
                Debug.Log($"batch_build_ save tsvReport {tsvReport}");
                Debug.Log($"batch_build_ save jsReport {jsReport}");
                BatchBuildReport.SaveBuildReport(buildReport, tsvReport, jsReport);
            }
            timer.Stop();
            Debug.Log($"batch_build_ exit result {buildResult} time {timer.ElapsedTime}");
            Debug.Log($"batch_build_ Build Report" +
                      $"\tStarted\t{buildReport.summary.buildStartedAt:yyyy-dd-MM HH:mm}" +
                      $"\tEnded\t{buildReport.summary.buildEndedAt:yyyy-dd-MM HH:mm}");
            EditorApplication.Exit(buildResult == BuildResult.Succeeded ? 0 : 1);
        }

        private static void _BuildPlayerPostProcessing()
        {
            LogFileWriter.CreateLogFileWriter();
            Debug.Log($"batch_build_ start POST PROCESS in UNITY {Application.unityVersion}");
            var options = new BatchBuildOptions(Environment.GetCommandLineArgs());
            Debug.Log($"batch_build_ Options {options}");
            if (options.IsTestRun)
            {
                Debug.Log("batch_build_ IsTestRun build exit 0");
                EditorApplication.Exit(0);
                return;
            }
            var timer = new Timer();
            // Load build report.
            var jsReport = SafeReplaceFileExtension(options.LogFile, ".log", ".report.js");
            var buildReportAssets = BatchBuildReport.LoadFromFile(jsReport);
            // Create log file data.
            var tsvOutput = SafeReplaceFileExtension(options.LogFile, ".log", ".log.tsv");
            var jsOutput = SafeReplaceFileExtension(options.LogFile, ".log", ".log.js");
            var buildReportLog = BatchBuildLog.SaveBuildReportLog(options.LogFilePost, tsvOutput, jsOutput);
            if (buildReportLog == null)
            {
                timer.Stop();
                Debug.Log($"batch_build_ player data was not rebuilt - no data to report on {options.LogFile}");
                Debug.Log($"batch_build_ exit time {timer.ElapsedTime}");
                EditorApplication.Exit(10);
                return;
            }
            // Create project files list.
            var tsvFiles = SafeReplaceFileExtension(options.LogFile, ".log", ".files.tsv");
            var jsFiles = SafeReplaceFileExtension(options.LogFile, ".log", ".files.js");
            var projectFiles = BatchBuildFiles.SaveProjectFiles(buildReportAssets, buildReportLog, tsvFiles, jsFiles);

            // Create final full build report results.
            var jsResult = SafeReplaceFileExtension(options.LogFile, ".log", ".build.result.js");
            BatchBuildResult.SaveBuildResult(buildReportAssets, buildReportLog, projectFiles, jsResult);

            timer.Stop();
            Debug.Log($"batch_build_ exit time {timer.ElapsedTime}");
        }

        private static BuildReport BuildPLayer(BatchBuildOptions options)
        {
            SavedWebGlSettings savedWebGlSettings = null;

            var scenes = EditorBuildSettings.scenes
                .Where(x => x.enabled)
                .Select(x => x.path)
                .ToArray();
            Assert.IsTrue(scenes.Length > 0, "Error: NO eligible SCENES FOUND for build in EditorBuildSettings");
            Debug.Log($"batch_build_ scenes {scenes.Length}: {string.Join(" ", scenes)}");
            var buildPlayerOptions = new BuildPlayerOptions
            {
                locationPathName = options.OutputPathName,
                options = options.BuildOptions,
                scenes = scenes,
                target = options.BuildTarget,
                targetGroup = options.BuildTargetGroup,
            };
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildPlayerOptions.targetGroup).Split(';');

            Debug.Log($"batch_build_ build productName: {Application.productName}");
            Debug.Log($"batch_build_ build version: {Application.version}");
            Debug.Log($"batch_build_ build bundleVersionCode: {PlayerSettings.Android.bundleVersionCode}");
            Debug.Log($"batch_build_ build output: {buildPlayerOptions.locationPathName}");
            Debug.Log($"batch_build_ defines:\r\n{string.Join("\r\n", defines)}");

            // General settings we enforce for any build.
            PlayerSettings.insecureHttpOption = InsecureHttpOption.NotAllowed;
            Debug.Log($"batch_build_ insecureHttpOption: {PlayerSettings.insecureHttpOption}");

            switch (options.BuildTarget)
            {
                case BuildTarget.Android:
                {
                    // Android setting we enforce.
                    PlayerSettings.Android.minifyRelease = true;
                    PlayerSettings.Android.useCustomKeystore = true;
                    Debug.Log($"batch_build_ Android.minifyRelease: {PlayerSettings.Android.minifyRelease}");
                    Debug.Log($"batch_build_ Android.useCustomKeystore: {PlayerSettings.Android.useCustomKeystore}");
                    if (PlayerSettings.Android.useCustomKeystore)
                    {
                        // Build Manager is responsible for these Editor settings and how they are managed in the project.
                        PlayerSettings.Android.keyaliasName = options.Android.keyaliasName;
                        PlayerSettings.Android.keystoreName = options.Android.keystoreName;
                        Debug.Log($"batch_build_ Android.keyaliasName: {PlayerSettings.Android.keyaliasName}");
                        Debug.Log($"batch_build_ Android.keystoreName: {PlayerSettings.Android.keystoreName} " +
                                  $"Exists={File.Exists(PlayerSettings.Android.keystoreName)}");
                        var passwordFolder = Path.GetDirectoryName(options.Android.keystoreName);
                        PlayerSettings.keystorePass = GetLocalPasswordFor(passwordFolder, "keystore_password");
                        PlayerSettings.keyaliasPass = GetLocalPasswordFor(passwordFolder, "alias_password");
                    }
                    break;
                }
                case BuildTarget.WebGL:
                    // Save current Editor settings so they can be restored after build to original values
                    // to prevent unnecessary changes in version control.
                    savedWebGlSettings = new SavedWebGlSettings();
                    PlayerSettings.WebGL.compressionFormat = options.WebGL.compressionFormat;
                    Debug.Log($"batch_build_ WebGL.compressionFormat: {PlayerSettings.WebGL.compressionFormat}");
                    // No use to show stack trace in browser.
                    Debug.Log($"batch_build_ SetStackTraceLogType: {StackTraceLogType.None}");
                    PlayerSettings.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
                    PlayerSettings.SetStackTraceLogType(LogType.Assert, StackTraceLogType.None);
                    PlayerSettings.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
                    PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
                    PlayerSettings.SetStackTraceLogType(LogType.Exception, StackTraceLogType.None);
                    break;
            }
            if (Directory.Exists(options.OutputFolder))
            {
                Directory.Delete(options.OutputFolder, recursive: true);
            }
            Directory.CreateDirectory(options.OutputFolder);
            var buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (savedWebGlSettings != null)
            {
                savedWebGlSettings.Restore();
            }
            return buildReport;

            string GetLocalPasswordFor(string folder, string filename)
            {
                var file = Path.Combine(folder, filename);
                if (!File.Exists(file))
                {
                    throw new UnityException($"batch_build_ file not found: {file}");
                }
                var line = File.ReadAllLines(file)[0];
                Debug.Log($"batch_build_ file {filename}: {line[..2]}********{line[^2..]}");
                return line;
            }
        }

        private record SavedWebGlSettings
        {
            private readonly WebGLCompressionFormat _compressionFormat = PlayerSettings.WebGL.compressionFormat;
            private readonly StackTraceLogType _error = PlayerSettings.GetStackTraceLogType(LogType.Error);
            private readonly StackTraceLogType _assert = PlayerSettings.GetStackTraceLogType(LogType.Assert);
            private readonly StackTraceLogType _warning = PlayerSettings.GetStackTraceLogType(LogType.Warning);
            private readonly StackTraceLogType _log = PlayerSettings.GetStackTraceLogType(LogType.Log);
            private readonly StackTraceLogType _exception = PlayerSettings.GetStackTraceLogType(LogType.Exception);

            public void Restore()
            {
                PlayerSettings.WebGL.compressionFormat = _compressionFormat;
                PlayerSettings.SetStackTraceLogType(LogType.Error, _error);
                PlayerSettings.SetStackTraceLogType(LogType.Assert, _assert);
                PlayerSettings.SetStackTraceLogType(LogType.Warning, _warning);
                PlayerSettings.SetStackTraceLogType(LogType.Log, _log);
                PlayerSettings.SetStackTraceLogType(LogType.Exception, _exception);
            }
        }

        private static bool VerifyUnityVersionForBuild(string unityVersion, out string editorVersion)
        {
            editorVersion = File
                .ReadAllLines(Path.Combine("ProjectSettings", "ProjectVersion.txt"))[0]
                .Split(" ")[1];
            return unityVersion == editorVersion;
        }

        private static string SafeReplaceFileExtension(string filename, string oldExtension, string newExtension)
        {
            Assert.IsTrue(oldExtension.StartsWith('.'));
            Assert.IsTrue(newExtension.StartsWith('.'));
            return filename.EndsWith(oldExtension)
                ? $"{filename.Substring(0, filename.Length - oldExtension.Length)}{newExtension}"
                : $"{filename}{newExtension}";
        }

        #region BatchBuildOptions

        private class BatchBuildOptions
        {
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            public class AndroidOptions
            {
                public readonly string keyaliasName = SanitizePath(Application.productName).ToLower();

                // This is path to android keystore file.
                // Same directory will have an other file that contains required passwords!
                public string keystoreName;
            }

            [SuppressMessage("ReSharper", "InconsistentNaming")]
            public class WebGlOptions
            {
                // PlayerSettings.WebGL.compressionFormat
                // ReSharper disable once ConvertToConstant.Local
                public readonly WebGLCompressionFormat compressionFormat = WebGLCompressionFormat.Brotli;
            }

            // Paths and file names.
            public readonly string ProjectPath;
            public readonly string LogFile;
            public readonly string EnvFile;

            // Actual build settings etc.
            public readonly BuildTarget BuildTarget;
            public readonly BuildTargetGroup BuildTargetGroup;
            public readonly BuildOptions BuildOptions;
            public readonly string OutputPathName;
            public readonly AndroidOptions Android = new();
            public readonly WebGlOptions WebGL = new();

            // Just for information, if needed.
            public readonly string OutputFolder;
            public readonly bool IsDevelopmentBuild;
            public readonly bool IsBuildReport;

            // Build post processing.
            public readonly string LogFilePost;

            public readonly bool IsTestRun;

            public BatchBuildOptions(string[] args)
            {
                // Parse command line arguments
                // -projectPath - project folder name (for UNITY)
                // -buildTarget - build target name (for UNITY)
                // -logFile - log file name (for UNITY)
                // -envFile - settings file name (for BatchBuild to read actual build options etc)
                {
                    var buildTargetName = string.Empty;
                    for (var i = 0; i < args.Length; ++i)
                    {
                        var arg = args[i];
                        switch (arg)
                        {
                            case "-projectPath":
                                i += 1;
                                ProjectPath = args[i];
                                if (!Directory.Exists(ProjectPath))
                                {
                                    throw new ArgumentException($"Directory -projectPath ${ProjectPath} is not found");
                                }
                                break;
                            case "-buildTarget":
                                i += 1;
                                buildTargetName = args[i];
                                break;
                            case "-logFile":
                                i += 1;
                                LogFile = args[i];
                                break;
                            case "-envFile":
                                i += 1;
                                EnvFile = args[i];
                                if (!File.Exists(EnvFile))
                                {
                                    throw new ArgumentException($"File -envFile '{EnvFile}' is not found");
                                }
                                break;
                        }
                    }
                    if (string.IsNullOrWhiteSpace(ProjectPath))
                    {
                        throw new ArgumentException($"-projectPath is mandatory for batch build");
                    }
                    if (KnownBuildTargets.TryGetValue(buildTargetName, out var buildTarget))
                    {
                        BuildTarget = buildTarget.Item1;
                        BuildTargetGroup = buildTarget.Item2;
                        switch (BuildTarget)
                        {
                            // Primary.
                            case BuildTarget.Android:
                                break;
                            // Secondary.
                            case BuildTarget.WebGL:
                                break;
                            // For testing only.
                            case BuildTarget.StandaloneWindows64:
                                break;
                            default:
                                throw new UnityException($"Build target '{BuildTarget}' is not supported");
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"-buildTarget '{buildTargetName}' is invalid or unsupported");
                    }
                    if (string.IsNullOrWhiteSpace(LogFile))
                    {
                        throw new ArgumentException($"-logFile is mandatory for batch build");
                    }
                    if (string.IsNullOrWhiteSpace(EnvFile))
                    {
                        throw new ArgumentException($"-envFile is mandatory for batch build");
                    }
                }

                // Parse settings file.
                var lines = File.ReadAllLines(EnvFile);
                // Find Build Report line.
                foreach (var line in lines)
                {
                    if (line.StartsWith("#") || string.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    var tokens = line.Split('=', StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length == 1 && line.Contains('='))
                    {
                        // Skip empty values!
                        continue;
                    }
                    var key = tokens[0].Trim();
                    var value = tokens[1].Trim();
                    switch (key)
                    {
                        case "IsDevelopmentBuild":
                            IsDevelopmentBuild = bool.Parse(value);
                            break;
                        case "IsBuildReport":
                            IsBuildReport = bool.Parse(value);
                            break;
                        case "Keystore":
                            // This requires actual keystore file name and two passwords!
                            Android.keystoreName = value;
                            break;
                        case "IsTestRun":
                            IsTestRun = bool.Parse(value);
                            break;
                        case "LOG_FILE_POST":
                            LogFilePost = value;
                            break;
                    }
                }

                // Create actual build options
                BuildOptions = BuildOptions.StrictMode | BuildOptions.DetailedBuildReport;
                if (IsDevelopmentBuild)
                {
                    BuildOptions |= BuildOptions.Development;
                }
                OutputFolder = Path.Combine(ProjectPath, $"build{BuildPipeline.GetBuildTargetName(BuildTarget)}");
                if (BuildTarget == BuildTarget.WebGL)
                {
                    OutputPathName = OutputFolder;
                    return;
                }
                var appName =
                    SanitizePath(
                        $"{Application.productName}_{Application.version}_{PlayerSettings.Android.bundleVersionCode}");
                var appExtension = BuildTarget == BuildTarget.Android ? "aab" : "exe";
                OutputPathName = Path.Combine(OutputFolder, $"{appName}.{appExtension}");
            }

            public override string ToString()
            {
                return
                    $"{nameof(ProjectPath)}: {ProjectPath}, {nameof(LogFile)}: {LogFile}, {nameof(EnvFile)}: {EnvFile}" +
                    $", {nameof(BuildTarget)}: {BuildTarget}, {nameof(BuildTargetGroup)}: {BuildTargetGroup}" +
                    $", {nameof(BuildOptions)}: [{BuildOptions}]" +
                    $", {nameof(OutputFolder)}: {OutputFolder}, {nameof(OutputPathName)}: {OutputPathName}" +
                    $", {nameof(IsDevelopmentBuild)}: {IsDevelopmentBuild}, {nameof(IsTestRun)}: {IsTestRun}" +
                    $", {nameof(LogFilePost)}: {LogFilePost}";
            }

            // Build target parameter mapping
            // See: https://docs.unity3d.com/Manual/CommandLineArguments.html
            // See: https://docs.unity3d.com/2019.4/Documentation/ScriptReference/BuildTarget.html
            // See: https://docs.unity3d.com/ScriptReference/BuildPipeline.GetBuildTargetName.html
            private static readonly Dictionary<string, Tuple<BuildTarget, BuildTargetGroup>> KnownBuildTargets = new()
            {
                {
                    /*" Win64" */ BuildPipeline.GetBuildTargetName(BuildTarget.StandaloneWindows64),
                    new Tuple<BuildTarget, BuildTargetGroup>(BuildTarget.StandaloneWindows64,
                        BuildTargetGroup.Standalone)
                },
                {
                    /*" Android" */ BuildPipeline.GetBuildTargetName(BuildTarget.Android),
                    new Tuple<BuildTarget, BuildTargetGroup>(BuildTarget.Android, BuildTargetGroup.Android)
                },
                {
                    /*" WebGL" */ BuildPipeline.GetBuildTargetName(BuildTarget.WebGL),
                    new Tuple<BuildTarget, BuildTargetGroup>(BuildTarget.WebGL, BuildTargetGroup.WebGL)
                },
            };

            private static string SanitizePath(string path)
            {
                // https://www.mtu.edu/umc/services/websites/writing/characters-avoid/
                var illegalCharacters = new[]
                {
                    '#', '<', '$', '+',
                    '%', '>', '!', '`',
                    '&', '*', '\'', '|',
                    '{', '?', '"', '=',
                    '}', '/', ':', '@',
                    '\\', ' '
                };
                for (var i = 0; i < path.Length; ++i)
                {
                    var c = path[i];
                    if (illegalCharacters.Contains(c))
                    {
                        path = path.Replace(c, '_');
                    }
                }
                return path;
            }
        }

        #endregion
    }
}
