using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
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
            TagColor = "white";
            ContextColor = "orange";
            IsMethodAllowedFilter = _ => true;
            CachedLogLineMethods.Clear();
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

        private static bool _isEditorHook;
        private static int _mainThreadId;
        private static int _currentFrameCount;

        /// <summary>
        /// Color name for debug log tag.
        /// </summary>
        public static string TagColor = "white";

        /// <summary>
        /// Color name for debug context 'marker' in tag.
        /// </summary>
        public static string ContextColor = "orange";

        /// <summary>
        /// Filter to accept or reject logging based on method.
        /// </summary>
        public static Func<MethodBase, bool> IsMethodAllowedFilter = _ => true;

        private static readonly Dictionary<MethodBase, Tuple<bool, string>> CachedLogLineMethods = new();

        private static int GetSafeFrameCount()
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
            // Remove full context marker first.
            return message
                .Replace($"<color={ContextColor}>◆</color>", "")
                .Replace($"[<color={TagColor}>", "")
                .Replace("</color>]", "");
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
                $"{GetSafeFrameCount()} [<color={TagColor}>{caller}</color>]{(context != null ? $"<color={ContextColor}>◆</color>" : "")}";
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
                var className = method.ReflectedType?.Name ?? "CLASS";
                if (className.StartsWith("<"))
                {
                    // For anonymous types we try its parent type.
                    className = method.ReflectedType?.DeclaringType?.Name ?? "CLASS";
                }
                var methodName = method.Name;
                if (!methodName.StartsWith("<"))
                {
                    return $"{className}.{methodName}";
                }
                // Local methods are compiled to internal static methods with a name of the following form:
                // <Name1>g__Name2|x_y
                // Name1 is the name of the surrounding method. Name2 is the name of the local method.
                const string methodPrefix = ">g__";
                const string methodSuffix = "|";
                var pos1 = methodName.IndexOf(methodPrefix, StringComparison.Ordinal);
                if (pos1 > 0)
                {
                    pos1 += methodPrefix.Length;
                    var pos2 = methodName.IndexOf(methodSuffix, pos1, StringComparison.Ordinal);
                    if (pos2 > 0)
                    {
                        var localName = $">{methodName.Substring(pos1, pos2 - pos1)}";
                        methodName = memberName == null ? localName : memberName + localName;
                    }
                }
                return $"{className}.{methodName}";
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
