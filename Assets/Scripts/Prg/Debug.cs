#if PRG_DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Prg
{
    /// <summary>
    /// UnityEngine.Debug (thread-safe) wrapper for development (and optionally for production testing).
    /// </summary>
    /// <remarks>
    /// Using <c>Conditional</c> attribute to disable logging unless compiled with <b>UNITY_EDITOR</b> or <b>FORCE_LOG defines</b>.
    /// </remarks>
    [DefaultExecutionOrder(-100), SuppressMessage("ReSharper", "CheckNamespace")]
    public static class Debug
    {
        // See: https://answers.unity.com/questions/126315/debuglog-in-build.html
        // StackFrame: https://stackoverflow.com/questions/21884142/difference-between-declaringtype-and-reflectedtype
        // Method: https://stackoverflow.com/questions/2483023/how-to-test-if-a-type-is-anonymous

#if FORCE_LOG
#warning <b>NOTE</b>: Compiling WITH debug logging define <b>FORCE_LOG</b>
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void SubsystemRegistration()
        {
            // Manual reset if UNITY Domain Reloading is disabled.
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _currentFrameCount = 0;
            SetEditorStatus();
        }

        [Conditional("UNITY_EDITOR")]
        private static void SetEditorStatus()
        {
            // Reset log line filtering and caching in Editor when we switch form Player Mode to Edit Mode.
#if UNITY_EDITOR
            if (_isEditorHook)
            {
                return;
            }
            _isEditorHook = true;
            EditorApplication.playModeStateChanged += LogPlayModeState;
            return;

            void LogPlayModeState(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    CachedLogLineMethods.Clear();
                }
            }
#endif
        }

        #region Log formatting and filtering support

        private const string UnicodeBullet = "•";
        private static int _mainThreadId;
        private static int _currentFrameCount;
        private static bool _isEditorHook;

        static Debug()
        {
            // Set initial state for console rich text colours etc..
            TagColor = "white";
            ContextColor = "blue";
            ContextChar = UnicodeBullet;
        }

        /// <summary>
        /// Color name for debug log tag.
        /// </summary>
        public static string TagColor
        {
            set => _tagColorPrefix = $"<color={value}>";
        }

        /// <summary>
        /// Color name for debug context 'marker' in tag.
        /// </summary>
        public static string ContextColor
        {
            set
            {
                _contextColor = value;
                _contextMarker = $"<color={_contextColor}>{_contextChar}</color>";
            }
        }

        public static string ContextChar
        {
            set
            {
                _contextChar = value?.Length > 0 ? value : "*";
                _contextMarker = $"<color={_contextColor}>{_contextChar}</color>";
            }
        }

        private static string _tagColorPrefix;
        private static string _contextColor;
        private static string _contextChar;
        private static string _contextMarker;

        /// <summary>
        /// Filter to accept or reject logging based on method.
        /// </summary>
        public static Func<MethodBase, bool> IsMethodAllowedFilter = _ => true;

        private static readonly Dictionary<MethodBase, Tuple<bool, string>> CachedLogLineMethods = new();

        public static int GetSafeFrameCount()
        {
            if (_mainThreadId == Thread.CurrentThread.ManagedThreadId)
            {
                _currentFrameCount = Time.frameCount % 1000;
            }
            return _currentFrameCount;
        }

        /// <summary>
        /// Remove UNITY Editor color decorations.
        /// </summary>
        /// <param name="message">decorated message</param>
        /// <returns>undecorated message</returns>
        public static string FilterFormattedMessage(string message)
        {
            // Remove all known console specific parts first.
            message = message
                .Replace(_contextMarker, "")
                .Replace(_tagColorPrefix, "")
                .Replace("</color>", "");
            // Remove color start tags, if any.
            const string colorPrefix = "<color=";
            const string colorSuffix = ">";
            var pos1 = message.IndexOf(colorPrefix, StringComparison.Ordinal);
            while (pos1 >= 0)
            {
                var pos2 = message.IndexOf(colorSuffix, pos1, StringComparison.Ordinal);
                if (pos2 <= 0)
                {
                    break;
                }
                pos2 += colorSuffix.Length;
                var replacement = message.Substring(pos1, pos2 - pos1);
                message = message.Replace(replacement, "");
                pos1 = message.IndexOf(colorPrefix, StringComparison.Ordinal);
            }
            return message;
        }

        /// <summary>
        /// Format message and log it.
        /// </summary>
        /// <param name="logType">UNITY log type</param>
        /// <param name="message">message to log</param>
        /// <param name="context">UNITY context aka GameObject or similar selectable in Editor</param>
        /// <param name="memberName">Optional C# compiler generated caller name</param>
        /// <param name="method">Optional C# method e.g. from callstack</param>
        public static void FormatMessage(LogType logType, string message, Object context,
            string memberName = null, MethodBase method = null)
        {
            string caller = null;
            if (!GetClassName())
            {
                return;
            }
            var tag =
                $"{GetSafeFrameCount()} [{_tagColorPrefix}{caller}</color>]{(context != null ? _contextMarker : string.Empty)}";
            UnityEngine.Debug.unityLogger.Log(logType, tag, message, context);
            return;

            bool GetClassName()
            {
                if (method == null)
                {
                    caller = memberName ?? logType.ToString();
                    return true;
                }
                if (CachedLogLineMethods.TryGetValue(method, out var cached))
                {
                    if (!cached.Item1)
                    {
                        return false;
                    }
                    caller = cached.Item2;
                    return true;
                }
                var isAllowed = IsMethodAllowedFilter(method);
                caller = GetCallerName();
                CachedLogLineMethods.Add(method, new Tuple<bool, string>(isAllowed, caller));
                return isAllowed;
            }

            string GetCallerName()
            {
                var reflectedType = method.ReflectedType?.Name ?? "CLASS";
                var className = reflectedType;
                if (className.StartsWith("<"))
                {
                    // For anonymous types we try its parent type.
                    className = method.ReflectedType?.DeclaringType?.Name ?? "CLASS";
                }
                var methodName = method.Name;
                if (!methodName.StartsWith("<"))
                {
                    if (!reflectedType.StartsWith("<"))
                    {
                        if (methodName.Contains('.'))
                        {
                            // Explicit interface implementation - just grab last piece that is the method name?
                            methodName = methodName.Split('.')[^1];
                        }
                        return $"{className}.{methodName}";
                    }
                }
                if (methodName == "MoveNext")
                {
                    FixIterator();
                }
                else
                {
                    FixLocalMethod();
                }
                return $"{className}.{methodName}";

                void FixIterator()
                {
                    // IEnumerator methods have name "MoveNext"
                    // <LoadServicesAsync>d__4
                    const string enumeratorPrefix = "<";
                    const string enumeratorSuffix = ">d__";
                    var pos1 = reflectedType.IndexOf(enumeratorPrefix, StringComparison.Ordinal);
                    if (pos1 >= 0)
                    {
                        var pos2 = reflectedType.IndexOf(enumeratorSuffix, pos1, StringComparison.Ordinal);
                        if (pos2 > 0)
                        {
                            pos1 += enumeratorPrefix.Length;
                            var iteratorName = reflectedType.Substring(pos1, pos2 - pos1);
                            methodName = $"{iteratorName}";
                        }
                    }
                }

                void FixLocalMethod()
                {
                    // Local methods are compiled to internal static methods with a name of the following form:
                    // <Name1>g__Name2|x_y
                    // Name1 is the name of the surrounding method. Name2 is the name of the local method.
                    const string localMethodPrefix = ">g__";
                    const string localMethodSuffix = "|";
                    var pos1 = methodName.IndexOf(localMethodPrefix, StringComparison.Ordinal);
                    if (pos1 > 0)
                    {
                        pos1 += localMethodPrefix.Length;
                        var pos2 = methodName.IndexOf(localMethodSuffix, pos1, StringComparison.Ordinal);
                        if (pos2 > 0)
                        {
                            var localName = methodName.Substring(pos1, pos2 - pos1);
                            methodName = $"{memberName}.{localName}";
                        }
                    }
                    // 'Wrapped' anonymous lambda method.
                    // <Name1>b__41_0
                    const string lambdaMethodPrefix = "<";
                    const string lambdaMethodSuffix = ">b__";
                    pos1 = methodName.IndexOf(lambdaMethodPrefix, StringComparison.Ordinal);
                    if (pos1 == 0)
                    {
                        pos1 += lambdaMethodPrefix.Length;
                        var pos2 = methodName.IndexOf(lambdaMethodSuffix, pos1, StringComparison.Ordinal);
                        if (pos2 > 0)
                        {
                            methodName = methodName.Substring(pos1, pos2 - pos1);
                        }
                    }
                }
            }
        }

        #endregion

        #region DUPLICATED : Just for compability in UnityEngine namespace

        public static void Break() => UnityEngine.Debug.Break();
        public static void DebugBreak() => UnityEngine.Debug.DebugBreak();

        [Conditional("UNITY_EDITOR")]
        public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration, bool depthTest) =>
            UnityEngine.Debug.DrawLine(start, end, color, duration, depthTest);

        [Conditional("UNITY_EDITOR")]
        public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration) =>
            UnityEngine.Debug.DrawLine(start, end, color, duration);

        [Conditional("UNITY_EDITOR")]
        public static void DrawLine(Vector3 start, Vector3 end, Color color) =>
            UnityEngine.Debug.DrawLine(start, end, color);

        [Conditional("UNITY_EDITOR")]
        public static void DrawLine(Vector3 start, Vector3 end) => UnityEngine.Debug.DrawLine(start, end);

        [Conditional("UNITY_EDITOR")]
        public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration, bool depthTest) =>
            UnityEngine.Debug.DrawRay(start, dir, color, duration, depthTest);

        [Conditional("UNITY_EDITOR")]
        public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration) =>
            UnityEngine.Debug.DrawRay(start, dir, color, duration);

        [Conditional("UNITY_EDITOR")]
        public static void DrawRay(Vector3 start, Vector3 dir, Color color) =>
            UnityEngine.Debug.DrawRay(start, dir, color);

        [Conditional("UNITY_EDITOR")]
        public static void DrawRay(Vector3 start, Vector3 dir) => UnityEngine.Debug.DrawRay(start, dir);

        #endregion

        #region DUPLICATED : Actual UnityEngine.Debug Logging API

        /// <summary>
        /// Logs a string message.
        /// </summary>
        /// <remarks>
        /// Note that string interpolation using $ can be expensive and should be avoided if logging is intended for production builds.<br />
        /// It is bettor to use <c>LogFormat</c> 'composite formatting' to delay actual string formatting when (if) message is logged.
        /// </remarks>
        [Conditional("UNITY_EDITOR"), Conditional("FORCE_LOG")]
        public static void Log(string message, Object context = null, [CallerMemberName] string memberName = null)
        {
            FormatMessage(LogType.Log, message, context, memberName, new StackFrame(1).GetMethod());
        }

        /// <summary>
        /// Logs a string message using 'composite formatting'.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("FORCE_LOG")]
        public static void LogFormat(string format, params object[] args)
        {
            FormatMessage(LogType.Log, string.Format(format, args), null, null, new StackFrame(1).GetMethod());
        }

        [Conditional("UNITY_EDITOR"), Conditional("FORCE_LOG")]
        public static void LogFormat(Object context, string format, params object[] args)
        {
            FormatMessage(LogType.Log, string.Format(format, args), context, null, new StackFrame(1).GetMethod());
        }

        [Conditional("UNITY_EDITOR"), Conditional("FORCE_LOG")]
        public static void LogWarning(string message, Object context = null)
        {
            FormatMessage(LogType.Warning, message, context, null, new StackFrame(1).GetMethod());
        }

        [Conditional("UNITY_EDITOR"), Conditional("FORCE_LOG")]
        public static void LogWarningFormat(string format, params object[] args)
        {
            FormatMessage(LogType.Warning, string.Format(format, args), null, null, new StackFrame(1).GetMethod());
        }

        [Conditional("UNITY_EDITOR"), Conditional("FORCE_LOG")]
        public static void LogWarningFormat(Object context, string format, params object[] args)
        {
            FormatMessage(LogType.Warning, string.Format(format, args), context, null, new StackFrame(1).GetMethod());
        }

        public static void LogError(string message, Object context = null)
        {
            FormatMessage(LogType.Error, message, context, null, new StackFrame(1).GetMethod());
        }

        public static void LogErrorFormat(string format, params object[] args)
        {
            FormatMessage(LogType.Error, string.Format(format, args), null, null, new StackFrame(1).GetMethod());
        }

        public static void LogErrorFormat(Object context, string format, params object[] args)
        {
            FormatMessage(LogType.Error, string.Format(format, args), context, null, new StackFrame(1).GetMethod());
        }

        public static void LogException(Exception exception)
        {
            var message = $"{exception.GetType()} : {exception.Message}";
            FormatMessage(LogType.Warning, message, null, null, new StackFrame(1).GetMethod());
            UnityEngine.Debug.unityLogger.LogException(exception);
        }

        #endregion
    }
}
#else
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Prg
{
    public class Debug
    {
        public static void Break() => UnityEngine.Debug.Break();
        public static void DebugBreak() => UnityEngine.Debug.DebugBreak();

        public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration, bool depthTest) =>
            UnityEngine.Debug.DrawLine(start, end, color, duration, depthTest);

        public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration) =>
            UnityEngine.Debug.DrawLine(start, end, color, duration);

        public static void DrawLine(Vector3 start, Vector3 end, Color color) =>
            UnityEngine.Debug.DrawLine(start, end, color);

        public static void DrawLine(Vector3 start, Vector3 end) => UnityEngine.Debug.DrawLine(start, end);

        public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration, bool depthTest) =>
            UnityEngine.Debug.DrawRay(start, dir, color, duration, depthTest);

        public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration) =>
            UnityEngine.Debug.DrawRay(start, dir, color, duration);

        public static void DrawRay(Vector3 start, Vector3 dir, Color color) =>
            UnityEngine.Debug.DrawRay(start, dir, color);

        public static void DrawRay(Vector3 start, Vector3 dir) => UnityEngine.Debug.DrawRay(start, dir);

        public static void Log(string message, Object context = null) => UnityEngine.Debug.Log(message, context);

        public static void LogFormat(string format, params object[] args) => UnityEngine.Debug.LogFormat(format, args);

        public static void LogFormat(Object context, string format, params object[] args) =>
            UnityEngine.Debug.LogFormat(context, format, args);

        public static void LogWarning(string message, Object context = null) =>
            UnityEngine.Debug.LogWarning(message, context);

        public static void LogWarningFormat(string format, params object[] args) =>
            UnityEngine.Debug.LogWarningFormat(format, args);

        public static void LogWarningFormat(Object context, string format, params object[] args) =>
            UnityEngine.Debug.LogWarningFormat(context, format, args);

        public static void LogError(string message, Object context = null) =>
            UnityEngine.Debug.LogError(message, context);

        public static void LogErrorFormat(string format, params object[] args) =>
            UnityEngine.Debug.LogErrorFormat(format, args);

        public static void LogErrorFormat(Object context, string format, params object[] args) =>
            UnityEngine.Debug.LogErrorFormat(context, format, args);

        public static void LogException(Exception exception) => UnityEngine.Debug.LogException(exception);

        public static int GetSafeFrameCount() => 0;
    }
}
#endif
