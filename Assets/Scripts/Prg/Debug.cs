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
#pragma warning disable CS1030 // #warning directive
#warning <b>NOTE</b>: Compiling WITH debug logging define <b>FORCE_LOG</b>
#pragma warning restore CS1030 // #warning directive
#endif

        #region Bootloader

        private static int _mainThreadId;
        private static int _currentFrameCount;
#if UNITY_EDITOR
        private static bool _isEditorHookSet;
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
            if (_isEditorHookSet)
            {
                return;
            }
            _isEditorHookSet = true;
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

        #endregion

        #region Debug output

        /// <summary>
        /// Filter to accept or reject logging based on method.
        /// </summary>
        public static Func<Type, bool> IsMethodAllowedFilter = _ => true;

        public static void ClearMethodCache() => CachedLogLineMethods.Clear();

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
            string tag;
            if (logType is LogType.Log or LogType.Warning)
            {
                if (!IsMethodAllowed(method, memberName, out var caller))
                {
                    if (logType is LogType.Log)
                    {
                        return;
                    }
                }
                tag =
#if UNITY_EDITOR
                    $"{GetSafeFrameCount(),3} [{RichText.White(caller)}]{(context != null ? "*" : string.Empty)}"
#else
                    $"{GetSafeFrameCount(),3} [{caller}]"
#endif
                    ;
            }
            else
            {
                tag = $"{GetSafeFrameCount(),3}";
            }
            UnityEngine.Debug.unityLogger.Log(logType, tag, message, context);
        }

        private static bool IsMethodAllowed(MethodBase method, string memberName, out string caller)
        {
            if (CachedLogLineMethods.TryGetValue(method, out var cached))
            {
                caller = cached.Item2;
                return cached.Item1;
            }
            var type = GetTypeFromMethod(method, memberName, out caller);
            var isAllowed = IsMethodAllowedFilter(type);
            CachedLogLineMethods.Add(method, new Tuple<bool, string>(isAllowed, caller));
            return isAllowed;
        }

        private static Type GetTypeFromMethod(MethodBase method, string memberName, out string caller)
        {
            var methodType = method.ReflectedType ?? typeof(Debug);
            var reflectedTypeName = methodType.Name;
            var className = reflectedTypeName;
            if (className.StartsWith("<"))
            {
                // For anonymous types we try its parent type.
                methodType = method.ReflectedType?.DeclaringType ?? typeof(Debug);
                className = methodType.Name;
            }
            var methodName = method.Name;
            if (!methodName.StartsWith("<"))
            {
                if (!reflectedTypeName.StartsWith("<"))
                {
                    if (methodName.Contains('.'))
                    {
                        // Explicit interface implementation - just grab last piece that is the method name?
                        methodName = methodName.Split('.')[^1];
                    }
                    caller = $"{className}.{methodName}";
                    return methodType;
                }
            }
            if (methodName == "MoveNext")
            {
                FixIterator();
            }
            else
            {
                FixLocalOrAnonymousMethod();
            }
            caller = $"{className}.{methodName}";
            return methodType;

            void FixIterator()
            {
                // IEnumerator methods have name "MoveNext"
                // <LoadServicesAsync>d__4
                const string enumeratorPrefix = "<";
                const string enumeratorSuffix = ">d__";
                var pos1 = reflectedTypeName.IndexOf(enumeratorPrefix, StringComparison.Ordinal);
                if (pos1 >= 0)
                {
                    var pos2 = reflectedTypeName.IndexOf(enumeratorSuffix, pos1, StringComparison.Ordinal);
                    if (pos2 > 0)
                    {
                        pos1 += enumeratorPrefix.Length;
                        var iteratorName = reflectedTypeName.Substring(pos1, pos2 - pos1);
                        methodName = $"{iteratorName}";
                    }
                }
            }

            void FixLocalOrAnonymousMethod()
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
                        methodName = $"{methodName.Substring(pos1, pos2 - pos1)}.(a)";
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
