using System.Diagnostics;
using Prg.EditorSupport;
using UnityEngine;
#if PRG_DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine.Assertions;
#endif

namespace Prg.Util
{
    [CreateAssetMenu(menuName = "Prg/Prg/LogConfig", fileName = "LogConfig")]
    public class LogConfig : ScriptableObject
    {
        // This should be setup in .gitignore
        private const string LocalResourceFolder = "_local";
        private const string UnicodeBullet = "•";

        public const string Tp0 =
            "LogConfig should be placed in folder\r\n" +
            "'Resources/_local' with name 'LogConfig'.\r\n" +
            "Preferably add folder '_local' to .gitignore";

        private const string Tp1 = "Enable logging to file";
        private const string Tp2 = "Color for Logged Class name and method";
        private const string Tp3 = "Marker Color for logged Context Objects";
        private const string Tp4 = "Marker Character for logged Context Objects";

        private const string Tp5 =
            "Regular expressions with int value 1 or 0 to (a) match logged lines and (b) enable/disable their logging";

        private const string Tp6 = "List of classes that use Debug.Log calls in Play Mode, just for your information";

        [Header("Notes"), Tooltip(Tp0), InspectorReadOnly] public string _notes = Tp0;

        [Header("Settings"), Tooltip(Tp1)] public bool _isLogToFile;
        [Tooltip(Tp2)] public Color _classNameColor = new(1f, 1f, 1f, 1f);
        [Tooltip(Tp3)] public Color _contextTagColor = new(1f, 0.5f, 0f, 1f);
        [Tooltip(Tp4)] public string _contextTagChar = UnicodeBullet;

        [Header("Class Names Filter"), Tooltip(Tp5), TextArea(5, 20)] public string _loggerRules;

        [Header("Classes Seen in Last Play Mode"), Tooltip(Tp6), TextArea(5, 20)]
        public string _classesSeenInPlayMode;

        [Conditional("PRG_DEBUG")]
        public static void Create()
        {
#if PRG_DEBUG
            LogConfigFactory.Create(LoadLogConfig(LocalResourceFolder, nameof(LogConfig)));
            return;

            LogConfig LoadLogConfig(string localResourceFolder, string resourceName)
            {
                var resource = Resources.Load<LogConfig>($"{localResourceFolder}/{resourceName}");
                return resource != null ? resource : Resources.Load<LogConfig>(resourceName);
            }
#endif
        }
    }

#if PRG_DEBUG
    internal static class LogConfigFactory
    {
        private class RegExFilter
        {
            public readonly Regex Regex;
            public readonly bool IsLogged;

            public RegExFilter(string regex, bool isLogged)
            {
                const RegexOptions regexOptions = RegexOptions.Singleline | RegexOptions.CultureInvariant;
                Regex = new Regex(regex, regexOptions);
                IsLogged = isLogged;
            }
        }

        private static readonly HashSet<string> classesSeenList = new();

        public static void Create(LogConfig logConfig)
        {
            if (logConfig == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"{RichText.Yellow(nameof(LogConfig))} not found, using default values");
                return;
            }
            CreateLoggerFilterConfig(logConfig, ClassesSeenInPlayMode);
            return;

            void ClassesSeenInPlayMode(string lines)
            {
                logConfig._classesSeenInPlayMode = lines;
            }
        }

        private static void CreateLoggerFilterConfig(LogConfig logConfig, Action<string> classesSeenCallback)
        {
            if (logConfig._isLogToFile)
            {
                LogFileWriter.CreateLogFileWriter();
            }
            if (AppPlatform.IsEditor)
            {
                Debug.TagColor = $"#{ColorUtility.ToHtmlStringRGBA(logConfig._classNameColor)}";
                Debug.ContextColor = $"#{ColorUtility.ToHtmlStringRGBA(logConfig._contextTagColor)}";
                Debug.ContextChar = logConfig._contextTagChar.Length > 0 ? logConfig._contextTagChar[..1] : "*";
                // Clear previous run.
                classesSeenList.Clear();
                classesSeenCallback(string.Empty);
            }
            var capturedRegExFilters = BuildFilter(logConfig._loggerRules ?? string.Empty);
            if (capturedRegExFilters.Count == 0)
            {
#if UNITY_EDITOR
                const string GreedyFilter = "^.*=1";
                logConfig._notes = LogConfig.Tp0;
                Assert.IsNotNull(logConfig._classesSeenInPlayMode);
                // Add greedy filter to catch everything in Editor.
                logConfig._loggerRules = GreedyFilter;
                capturedRegExFilters.Add(new RegExFilter(GreedyFilter, true));
#else
                // No filtering configured, everything will be logged by default.
                return;
#endif
            }

#if FORCE_LOG || UNITY_EDITOR
#else
            UnityEngine.Debug.LogWarning($"NOTE! Application logging is totally disabled on platform: {Application.platform}");
#endif
            Debug.IsMethodAllowedFilter = LogLineAllowedFilterCallback;
            return;

            bool LogLineAllowedFilterCallback(MethodBase method)
            {
                // For anonymous types we try its parent type.
                var isAnonymous = method.ReflectedType?.Name.StartsWith("<");
                var type = isAnonymous.HasValue && isAnonymous.Value
                    ? method.ReflectedType?.DeclaringType
                    : method.ReflectedType;
                if (type?.FullName == null)
                {
                    // Should not happen in this context because a method should have a class (even anonymous).
                    return true;
                }
#if UNITY_EDITOR
                // Collect all logged types in Editor just for fun.
                if (classesSeenList.Add(type.FullName))
                {
                    var list = classesSeenList.ToList();
                    list.Sort();
                    // Lines should be put into something/somewhere that is not kept in the version control if it is saved on the disk.
                    classesSeenCallback(string.Join('\n', list));
                }
#endif
                // If filter does not match we log them always.
                // - add filter rule "^.*=0" to disable everything after this point
                var match = capturedRegExFilters.FirstOrDefault(x => x.Regex.IsMatch(type.FullName));
                return match?.IsLogged ?? true;
            }

            List<RegExFilter> BuildFilter(string lines)
            {
                // Note that line parsing relies on TextArea JSON serialization which I have not tested very well!
                // - lines can start and end with "'" if content has something that needs to be "protected" during JSON parsing
                // - JSON multiline separator is LF "\n"
                var list = new List<RegExFilter>();
                if (lines.StartsWith("'") && lines.EndsWith("'"))
                {
                    lines = lines.Substring(1, lines.Length - 2);
                }
                foreach (var token in lines.Split('\n'))
                {
                    var line = token.Trim();
                    if (line.StartsWith("#") || string.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    try
                    {
                        var parts = line.Split('=');
                        if (parts.Length != 2)
                        {
                            UnityEngine.Debug.LogError($"invalid Regex pattern '{line}', are you missing '=' here");
                            continue;
                        }
                        if (!int.TryParse(parts[1].Trim(), out var loggedValue))
                        {
                            UnityEngine.Debug.LogError(
                                $"invalid Regex pattern '{line}', not a valid integer after '=' sign");
                            continue;
                        }
                        var isLogged = loggedValue != 0;
                        var filter = new RegExFilter(parts[0].Trim(), isLogged);
                        list.Add(filter);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError($"invalid Regex pattern '{line}': {e.GetType().Name} {e.Message}");
                    }
                }
                return list;
            }
        }
    }
#endif
}
