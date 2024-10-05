using System.Diagnostics;
using NaughtyAttributes;
using UnityEngine;
#if PRG_DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.Assertions;
#endif

namespace Prg.Util
{
    [CreateAssetMenu(menuName = "Prg/Prg/LogConfig", fileName = "LogConfig")]
    public class LogConfig : ScriptableObject
    {
        // This should be setup in .gitignore
        public const string LocalResourceFolder = "_local";

        private const string Tp0 =
            "LogConfig should be placed in folder\r\n" +
            "'Resources/_local' with name 'LogConfig'.\r\n" +
            "Preferably add folder '_local' to .gitignore";

        private const string Tp1 = "Enable logging to file";
        private const string Tp5 = "Ignore Case in Regular expression";

        private const string Tp6 =
            "Regular expressions with int value 1 or 0 to (a) match logged lines and (b) enable/disable their logging";

        private const string Tp7 = "List of classes that use Debug.Log calls in Play Mode, just for your information";

        [InfoBox(Tp0)]
        [Header("Settings"), Tooltip(Tp1)] public bool _isLogToFile;

        [Header("Class Names Filter"), Tooltip(Tp5)] public bool _isIgnoreCase;
        [Tooltip(Tp6), TextArea(5, 100)] public string _loggerRules;

        [Header("Classes Seen in Last Play Mode"), Tooltip(Tp7), ResizableTextArea]
        public string _classesSeenInPlayMode;

        [Conditional("PRG_DEBUG")]
        public static void Create()
        {
#if PRG_DEBUG
            Debug.ClearMethodCache();
            LogConfigFactory.Create(LoadLogConfig(LocalResourceFolder, nameof(LogConfig)));
            ForceLogging(typeof(MyAssert));
            return;

            LogConfig LoadLogConfig(string localResourceFolder, string resourceName)
            {
                var resource = Resources.Load<LogConfig>($"{localResourceFolder}/{resourceName}");
                return resource != null ? resource : Resources.Load<LogConfig>(resourceName);
            }
#endif
        }

        [Conditional("PRG_DEBUG")]
        public static void ForceLogging(Type type)
        {
#if PRG_DEBUG
            LogConfigFactory.ForceLogging(type);
#endif
        }

        [Conditional("PRG_DEBUG")]
        public static void ForceLogging(params Type[] types)
        {
#if PRG_DEBUG
            foreach (var type in types)
            {
                LogConfigFactory.ForceLogging(type);
            }
#endif
        }
    }

#if PRG_DEBUG
    internal static class LogConfigFactory
    {
        private class RegExFilter
        {
            public readonly string Text;
            public readonly Regex Regex;
            public readonly bool IsLogged;

            public RegExFilter(string regex, bool isLogged, bool isIgnoreCase)
            {
                var regexOptions = RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled;
                if (isIgnoreCase)
                {
                    regexOptions |= RegexOptions.IgnoreCase;
                }
                Text = regex;
                Regex = new Regex(regex, regexOptions);
                IsLogged = isLogged;
            }
        }

        private static readonly List<RegExFilter> RegExFilters = new();
        private static readonly HashSet<string> ClassesSeenList = new();

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

        public static void ForceLogging(Type type)
        {
            var regEx = $"^{type.FullName}";
            if (RegExFilters.FirstOrDefault(x => x.Text == regEx && x.IsLogged) != null)
            {
                return;
            }
            var filter = Create(regEx, true, false);
            RegExFilters.Insert(0, filter);
        }

        private static void CreateLoggerFilterConfig(LogConfig logConfig, Action<string> classesSeenCallback)
        {
            if (logConfig._isLogToFile)
            {
                LogFileWriter.CreateLogFileWriter();
            }
#if UNITY_EDITOR
            // Clear previous run.
            ClassesSeenList.Clear();
            classesSeenCallback(string.Empty);
            RegExFilters.Clear();
#endif
            RegExFilters.AddRange(BuildFilter(logConfig._loggerRules ?? string.Empty));
            if (RegExFilters.Count == 0)
            {
                return;
            }

#if UNITY_EDITOR || FORCE_LOG
#else
            UnityEngine.Debug.LogWarning($"NOTE! Prg logging is totally disabled on platform: {Application.platform}");
#endif
            Debug.IsMethodAllowedFilter = LogLineAllowedFilterCallback;
            return;

            bool LogLineAllowedFilterCallback(Type type)
            {
                // Should not happen in this context because a method should always have a class (even anonymous).
                Assert.IsNotNull(type, "type can not be null in LogLineAllowedFilterCallback");
                // If filter does not match we log them always.
                // - add filter rule "^.*=0" to disable everything after this point
                var typename = type.FullName ?? "";
                var match = RegExFilters.FirstOrDefault(x => x.Regex.IsMatch(typename));
                var isAllowed = match?.IsLogged ?? true;
#if UNITY_EDITOR
                // Collect all logged types in Editor for your convenience to setup logging rules.
                var classRegExpLine = $"^{typename.Replace(".", "\\.")}={(isAllowed ? "01" : "0")}";
                if (!ClassesSeenList.Add(classRegExpLine))
                {
                    return isAllowed;
                }
                var list = ClassesSeenList.ToList();
                list.Sort();
                classesSeenCallback(string.Join('\n', list));
#endif
                return isAllowed;
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
                    if (line.StartsWith('#') || string.IsNullOrEmpty(line))
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
                        var regEx = parts[0].Trim();
                        var isLogged = loggedValue != 0;
                        var filter = Create(regEx, isLogged, logConfig._isIgnoreCase, line);
                        list.Add(filter);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError($"invalid Regex pattern '{line}': {e.GetType().Name} {e.Message}");
                    }
                }
#if UNITY_EDITOR
                if (list.Count == 0)
                {
                    // Log everything in Editor if nothing is specified.
                    list.Add(new RegExFilter("^.*=1", true, logConfig._isIgnoreCase));
                }
#endif
                return list;
            }
        }

        private static RegExFilter Create(string regEx, bool isLogged, bool isIgnoreCase, string line = null)
        {
            if (regEx.Contains('+'))
            {
                // Inner classes!
                if (regEx.Contains("\\+"))
                {
                    UnityEngine.Debug.LogError($"do not escape plus sign, it is done automatically: '{line}'");
                }
                // Poodle.Game.GameCamera+RawCameraFollow+<MoveCamera>d__12
                regEx = regEx.Replace("+", "\\+");
            }
            var filter = new RegExFilter(regEx, isLogged, isIgnoreCase);
            return filter;
        }
    }
#endif
}
